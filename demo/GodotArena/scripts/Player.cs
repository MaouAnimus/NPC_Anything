using Godot;

namespace GodotArena.Scripts;

/// <summary>
/// Controlador táctico del jugador en primera/tercera persona.
/// Emite eventos de ruido (caminar/correr) y sirve como objetivo para la IA.
/// </summary>
public partial class Player : CharacterBody3D
{
    [Export] public float WalkSpeed { get; set; } = 4.5f;
    [Export] public float SprintSpeed { get; set; } = 7.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.003f;
    [Export] public float MaxHealth { get; set; } = 100.0f;

    public float CurrentHealth { get; private set; }
    public bool IsSprinting { get; private set; }
    public float CurrentNoiseRadius { get; private set; } = 0f;

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

    public void ApplyDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        GD.Print($"[Player] Daño recibido: {amount}. Salud restante: {CurrentHealth:F1}");
    }
}
