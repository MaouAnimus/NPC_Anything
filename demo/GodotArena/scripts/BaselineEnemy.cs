using Godot;

namespace GodotArena.Scripts;

/// <summary>
/// Máquina de estados tradicional (FSM Baseline) para comparación experimental.
/// Paridad estadística estricta con el enemigo avanzado para garantizar validez científica.
/// Flujo: Patrol -> Chase -> Attack -> Search -> Patrol
/// </summary>
public partial class BaselineEnemy : CharacterBody3D
{
    public enum State
    {
        Patrol,
        Chase,
        Attack,
        Search
    }

    [Export] public float PatrolSpeed { get; set; } = 3.5f;
    [Export] public float ChaseSpeed { get; set; } = 5.5f;
    [Export] public float AttackRange { get; set; } = 12.0f;
    [Export] public float AttackDamage { get; set; } = 10.0f;
    [Export] public float VisionDistance { get; set; } = 20.0f;
    [Export] public float VisionHalfAngleDegrees { get; set; } = 45.0f; // 90° FOV total
    [Export] public float SearchDurationSeconds { get; set; } = 4.0f;

    public State CurrentState { get; private set; } = State.Patrol;

    private NavigationAgent3D? _navAgent;
    private RayCast3D? _lineOfSightRay;
    private Player? _targetPlayer;
    private Vector3 _lastKnownPosition = Vector3.Zero;
    private float _searchTimer = 0f;
    private float _attackCooldown = 0f;

    [Export] public Godot.Collections.Array<Vector3> PatrolWaypoints { get; set; } = new();
    private int _currentWaypointIndex = 0;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        AddToGroup("Enemies");
        _navAgent = GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
        _lineOfSightRay = GetNodeOrNull<RayCast3D>("LineOfSightRay");

        _targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Player;

        if (PatrolWaypoints.Count == 0)
        {
            PatrolWaypoints.Add(GlobalPosition + new Vector3(8, 0, 0));
            PatrolWaypoints.Add(GlobalPosition + new Vector3(-8, 0, 0));
        }

        UpdatePatrolTarget();
    }

    public override void _PhysicsProcess(double delta)
    {
        // P1-04: Impedir que el enemigo opere si la navegación física no está sincronizada
        if (!ArenaEnvironment.IsNavigationReady) return;

        Vector3 velocity = Velocity;
        if (!IsOnFloor()) velocity.Y -= _gravity * (float)delta;

        if (_attackCooldown > 0f) _attackCooldown -= (float)delta;

        bool seesPlayer = CheckLineOfSightToPlayer();

        switch (CurrentState)
        {
            case State.Patrol:
                if (seesPlayer && _targetPlayer != null)
                {
                    ChangeState(State.Chase);
                }
                else
                {
                    ExecutePatrolStep((float)delta, ref velocity);
                }
                break;

            case State.Chase:
                if (seesPlayer && _targetPlayer != null)
                {
                    _lastKnownPosition = _targetPlayer.GlobalPosition;
                    float dist = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);

                    if (dist <= AttackRange)
                    {
                        ChangeState(State.Attack);
                    }
                    else
                    {
                        ExecuteChaseStep(_targetPlayer.GlobalPosition, ref velocity);
                    }
                }
                else
                {
                    ChangeState(State.Search);
                }
                break;

            case State.Attack:
                if (seesPlayer && _targetPlayer != null)
                {
                    float dist = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);
                    if (dist > AttackRange)
                    {
                        ChangeState(State.Chase);
                    }
                    else
                    {
                        velocity.X = 0;
                        velocity.Z = 0;
                        ExecuteAttackStep();
                    }
                }
                else
                {
                    ChangeState(State.Search);
                }
                break;

            case State.Search:
                if (seesPlayer && _targetPlayer != null)
                {
                    ChangeState(State.Chase);
                }
                else
                {
                    _searchTimer -= (float)delta;
                    if (_searchTimer <= 0f)
                    {
                        ChangeState(State.Patrol);
                    }
                    else
                    {
                        ExecuteChaseStep(_lastKnownPosition, ref velocity);
                    }
                }
                break;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void ChangeState(State newState)
    {
        if (CurrentState == newState) return;

        // Registrar en telemetría de la escena
        var manager = GetTree().CurrentScene as ArenaManager;
        manager?.LogEvent("state_changed", Name, newState.ToString(), 1.0f, $"FSM_{newState}");

        GD.Print($"[BaselineEnemy {Name}] Estado: {CurrentState} -> {newState}");
        CurrentState = newState;

        if (newState == State.Search)
        {
            _searchTimer = SearchDurationSeconds;
        }
        else if (newState == State.Patrol)
        {
            UpdatePatrolTarget();
        }
    }

    private bool CheckLineOfSightToPlayer()
    {
        if (_targetPlayer == null || _lineOfSightRay == null) return false;

        Vector3 toPlayer = _targetPlayer.GlobalPosition - GlobalPosition;
        float dist = toPlayer.Length();
        if (dist > VisionDistance) return false;

        // Cono FOV de 90° (45° a cada lado)
        Vector3 forward = -Transform.Basis.Z;
        float angle = Mathf.RadToDeg(forward.AngleTo(toPlayer));
        if (angle > VisionHalfAngleDegrees) return false;

        _lineOfSightRay.GlobalPosition = GlobalPosition + new Vector3(0, 1.2f, 0);
        _lineOfSightRay.TargetPosition = _lineOfSightRay.ToLocal(_targetPlayer.GlobalPosition + new Vector3(0, 1.0f, 0));
        _lineOfSightRay.ForceRaycastUpdate();

        if (_lineOfSightRay.IsColliding())
        {
            var collider = _lineOfSightRay.GetCollider() as Node;
            return collider != null && collider.IsInGroup("Player");
        }

        return false;
    }

    private void ExecutePatrolStep(float delta, ref Vector3 velocity)
    {
        if (_navAgent == null) return;
        if (_navAgent.IsNavigationFinished())
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % PatrolWaypoints.Count;
            UpdatePatrolTarget();
        }

        MoveAlongNavPath(PatrolSpeed, ref velocity);
    }

    private void ExecuteChaseStep(Vector3 targetPos, ref Vector3 velocity)
    {
        if (_navAgent == null) return;
        _navAgent.TargetPosition = targetPos;
        MoveAlongNavPath(ChaseSpeed, ref velocity);
    }

    private void ExecuteAttackStep()
    {
        if (_attackCooldown <= 0f && _targetPlayer != null)
        {
            _attackCooldown = 1.0f;
            _targetPlayer.ApplyDamage(AttackDamage);

            var manager = GetTree().CurrentScene as ArenaManager;
            manager?.LogEvent("attack_executed", Name, "Attack", AttackDamage, "FSM_DIRECT_FIRE");

            GD.Print($"[BaselineEnemy {Name}] ¡Disparo certero sobre el jugador!");
        }
    }

    private void MoveAlongNavPath(float speed, ref Vector3 velocity)
    {
        if (_navAgent == null) return;
        Vector3 nextPathPos = _navAgent.GetNextPathPosition();
        Vector3 dir = (nextPathPos - GlobalPosition).Normalized();
        dir.Y = 0;

        if (dir != Vector3.Zero)
        {
            velocity.X = dir.X * speed;
            velocity.Z = dir.Z * speed;
            LookAt(GlobalPosition + dir, Vector3.Up);
        }
    }

    private void UpdatePatrolTarget()
    {
        if (_navAgent != null && PatrolWaypoints.Count > 0)
        {
            _navAgent.TargetPosition = PatrolWaypoints[_currentWaypointIndex];
        }
    }
}
