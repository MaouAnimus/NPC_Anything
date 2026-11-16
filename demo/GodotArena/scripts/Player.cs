using Godot;
using NpcAnything.GodotAdapter;

namespace GodotArena.Scripts;

/// <summary>
/// Controlador táctico del jugador en primera/tercera persona.
/// Emite eventos de ruido (caminar/correr) y sirve como objetivo para la IA.
/// </summary>
public partial class Player : CharacterBody3D, IAcousticEmitter
{
    [Export] public float WalkSpeed { get; set; } = 4.5f;
    [Export] public float SprintSpeed { get; set; } = 7.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.003f;
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public bool EnableDebugPositionShortcuts { get; set; } = false;

    public float CurrentHealth { get; private set; }
    public bool IsSprinting { get; private set; }
    public float CurrentNoiseRadius { get; private set; } = 0f;

    private static readonly float[] DebugPlacementAngles =
    {
        0f, 5f, -5f, 10f, -10f, 15f, -15f, 20f, -20f, 25f, -25f,
        30f, -30f, 35f, -35f, 40f, -40f, 45f, -45f, 50f, -50f,
        55f, -55f, 60f, -60f, 65f, -65f, 70f, -70f, 75f, -75f,
        80f, -80f, 85f, -85f, 90f, -90f, 100f, -100f, 110f, -110f,
        120f, -120f, 130f, -130f, 140f, -140f, 150f, -150f,
        160f, -160f, 170f, -170f, 180f
    };

    private Node3D? _cameraPivot;
    private Camera3D? _camera;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        AddToGroup("Player");
        CurrentHealth = MaxHealth;
        _cameraPivot = GetNodeOrNull<Node3D>("CameraPivot");
        _camera = GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);
            if (_cameraPivot != null)
            {
                float newRotationX = _cameraPivot.Rotation.X - (mouseMotion.Relative.Y * MouseSensitivity);
                newRotationX = Mathf.Clamp(newRotationX, Mathf.DegToRad(-80), Mathf.DegToRad(80));
                _cameraPivot.Rotation = new Vector3(newRotationX, 0, 0);
            }
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured 
                ? Input.MouseModeEnum.Visible 
                : Input.MouseModeEnum.Captured;
        }

        if (EnableDebugPositionShortcuts &&
            @event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            !keyEvent.Echo)
        {
            int shortcut = keyEvent.PhysicalKeycode switch
            {
                Key.Key1 => 1,
                Key.Key2 => 2,
                Key.Key3 => 3,
                Key.Key4 => 4,
                _ => 0
            };

            if (shortcut != 0)
            {
                RepositionForDebug(shortcut);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Gravedad
        if (!IsOnFloor())
        {
            velocity.Y -= _gravity * (float)delta;
        }

        // Sprint
        IsSprinting = Input.IsKeyPressed(Key.Shift);
        float currentSpeed = IsSprinting ? SprintSpeed : WalkSpeed;

        // Movimiento en el plano horizontal
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * currentSpeed;
            velocity.Z = direction.Z * currentSpeed;
            // Ruido acústico emitido para sensores de los NPCs
            CurrentNoiseRadius = IsSprinting ? 14.0f : 6.0f;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, currentSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentSpeed);
            CurrentNoiseRadius = 0.0f;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void RepositionForDebug(int shortcut)
    {
        CharacterBody3D? npc = FindDebugNpc();
        if (npc == null)
        {
            GD.PushWarning("[Player] No se encontró un NPC para el atajo de depuración.");
            return;
        }

        Vector3 destination;
        if (shortcut == 4)
        {
            destination = new Vector3(0f, npc.GlobalPosition.Y, npc.GlobalPosition.Z <= 0f ? 11f : -11f);
        }
        else
        {
            float distance = shortcut switch
            {
                1 => 11.9f,
                2 => 12.1f,
                3 => 19.9f,
                _ => 0f
            };

            if (!TryFindDebugPosition(npc, distance, out destination))
            {
                GD.PushWarning($"[Player] No hay una posición libre para el atajo {shortcut}.");
                return;
            }
        }

        GlobalPosition = destination;
        Velocity = Vector3.Zero;
        CurrentNoiseRadius = 0f;
        float actualDistance = GlobalPosition.DistanceTo(npc.GlobalPosition);
        GD.Print($"[Player] Atajo {shortcut}: posición {GlobalPosition}, distancia NPC {actualDistance:F2} m.");
    }

    private CharacterBody3D? FindDebugNpc()
    {
        foreach (Node node in GetTree().GetNodesInGroup("Enemies"))
        {
            if (node is CharacterBody3D npc && npc != this)
            {
                return npc;
            }
        }

        return null;
    }

    private bool TryFindDebugPosition(CharacterBody3D npc, float distance, out Vector3 destination)
    {
        Vector3 forward = -npc.GlobalTransform.Basis.Z;
        forward.Y = 0f;
        if (forward.LengthSquared() < 0.001f)
        {
            forward = Vector3.Forward;
        }
        else
        {
            forward = forward.Normalized();
        }

        foreach (float angle in DebugPlacementAngles)
        {
            Vector3 direction = forward.Rotated(Vector3.Up, Mathf.DegToRad(angle));
            Vector3 candidate = npc.GlobalPosition + direction * distance;
            candidate.Y = npc.GlobalPosition.Y;

            if (!IsInsideArenaBounds(candidate) ||
                SegmentCrossesCentralWall(npc.GlobalPosition, candidate) ||
                IsInsideCoverFootprint(candidate))
            {
                continue;
            }

            destination = candidate;
            return true;
        }

        destination = default;
        return false;
    }

    private static bool IsInsideArenaBounds(Vector3 position)
    {
        const float safeLimit = 14.5f;
        return Mathf.Abs(position.X) <= safeLimit && Mathf.Abs(position.Z) <= safeLimit;
    }

    private static bool IsInsideCoverFootprint(Vector3 position)
    {
        // Cover footprints include a 0.5 m margin for the player's collision radius.
        return IsWithinRectangle(position, -7f, -3f, 4.1f, 5.9f) ||
               IsWithinRectangle(position, 2.75f, 7.25f, 4.1f, 5.9f) ||
               IsWithinRectangle(position, 6.25f, 9.75f, -5.9f, -4.1f);
    }

    private static bool IsWithinRectangle(Vector3 position, float minX, float maxX, float minZ, float maxZ)
    {
        return position.X >= minX && position.X <= maxX &&
               position.Z >= minZ && position.Z <= maxZ;
    }

    private static bool SegmentCrossesCentralWall(Vector3 start, Vector3 end)
    {
        float enter = 0f;
        float exit = 1f;
        Vector3 delta = end - start;

        // The central wall bounds are expanded by the same capsule clearance.
        return ClipSegmentAxis(start.X, delta.X, -6.5f, 6.5f, ref enter, ref exit) &&
               ClipSegmentAxis(start.Z, delta.Z, -1.1f, 1.1f, ref enter, ref exit);
    }

    private static bool ClipSegmentAxis(float start, float delta, float min, float max, ref float enter, ref float exit)
    {
        if (Mathf.Abs(delta) < 0.0001f)
        {
            return start >= min && start <= max;
        }

        float first = (min - start) / delta;
        float second = (max - start) / delta;
        if (first > second)
        {
            (first, second) = (second, first);
        }

        enter = Mathf.Max(enter, first);
        exit = Mathf.Min(exit, second);
        return enter <= exit;
    }

    public void ApplyDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        GD.Print($"[Player] Daño recibido: {amount}. Salud restante: {CurrentHealth:F1}");
    }
}
