using Godot;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Componente físico de movimiento y seguimiento de rutas para Godot.
/// Encargado de iterar GetNextPathPosition(), orientar la cápsula y llamar MoveAndSlide().
/// Evita recalcular rutas si la orden de destino no ha variado sustancialmente.
/// </summary>
public partial class NpcMotorController : Node
{
    [Export] public float WalkSpeed { get; set; } = 3.5f;
    [Export] public float SprintSpeed { get; set; } = 5.5f;
    [Export] public float RotationSpeed { get; set; } = 10.0f;
    [Export] public float DestinationThresholdSquared { get; set; } = 0.25f; // 0.5 metros

    private CharacterBody3D? _actor;
    private NavigationAgent3D? _navAgent;
    private Vector3? _currentDestination;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private bool _isNavigating = false;

    public void Initialize(CharacterBody3D actor, NavigationAgent3D navAgent)
    {
        _actor = actor;
        _navAgent = navAgent;
    }

    public override void _Ready()
    {
        if (_actor == null)
        {
            _actor = GetParentOrNull<CharacterBody3D>();
        }
        if (_navAgent == null && _actor != null)
        {
            _navAgent = _actor.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
        }
    }

    /// <summary>
    /// Asigna un nuevo objetivo de navegación. Si el objetivo es prácticamente idéntico al actual,
    /// se omite el recálculo de la ruta para no saturar NavigationServer3D.
    /// </summary>
    public void SetDestination(Vector3 targetPos)
    {
        if (_navAgent == null) return;

        if (_currentDestination.HasValue &&
            _currentDestination.Value.DistanceSquaredTo(targetPos) <= DestinationThresholdSquared)
        {
            // Destino prácticamente idéntico: se mantiene la ruta en curso
            return;
        }

        _currentDestination = targetPos;
        _navAgent.TargetPosition = targetPos;
        _isNavigating = true;
    }

    public void Stop()
    {
        _isNavigating = false;
        _currentDestination = null;
        if (_actor != null)
        {
            Vector3 vel = _actor.Velocity;
            vel.X = 0;
            vel.Z = 0;
            _actor.Velocity = vel;
        }
    }

    public void FaceTowards(Vector3 targetPos)
    {
        if (_actor == null) return;
        Vector3 dir = (targetPos - _actor.GlobalPosition);
        dir.Y = 0;
        if (dir.LengthSquared() > 0.01f)
        {
            _actor.LookAt(_actor.GlobalPosition + dir.Normalized(), Vector3.Up);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_actor == null || _navAgent == null) return;

        Vector3 velocity = _actor.Velocity;

        // Aplicar gravedad
        if (!_actor.IsOnFloor())
        {
            velocity.Y -= _gravity * (float)delta;
        }

        if (_isNavigating)
        {
            if (_navAgent.IsNavigationFinished())
            {
                _isNavigating = false;
                velocity.X = 0;
                velocity.Z = 0;
            }
            else
            {
                Vector3 nextPos = _navAgent.GetNextPathPosition();
                Vector3 moveDir = (nextPos - _actor.GlobalPosition);
                moveDir.Y = 0;

                if (moveDir.LengthSquared() > 0.001f)
                {
                    moveDir = moveDir.Normalized();
                    velocity.X = moveDir.X * WalkSpeed;
                    velocity.Z = moveDir.Z * WalkSpeed;

                    // Orientar el personaje suavemente hacia la dirección de avance
                    Vector3 lookTarget = _actor.GlobalPosition + moveDir;
                    _actor.LookAt(lookTarget, Vector3.Up);
                }
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, WalkSpeed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, WalkSpeed);
        }

        _actor.Velocity = velocity;
        _actor.MoveAndSlide();
    }
}
