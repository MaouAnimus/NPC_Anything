using Godot;

namespace GodotArena.Scripts;

/// <summary>
/// Controlador del entorno de la arena.
/// Asegura que la malla de navegación (NavigationMesh) esté horneada e inicializada al arrancar la escena.
/// </summary>
public partial class ArenaEnvironment : Node3D
{
    private NavigationRegion3D? _navRegion;

    public override void _Ready()
    {
        _navRegion = GetNodeOrNull<NavigationRegion3D>("NavigationRegion3D");

        if (_navRegion != null)
        {
            if (_navRegion.NavigationMesh == null)
            {
                var navMesh = new NavigationMesh
                {
                    AgentRadius = 0.4f,
                    AgentHeight = 1.8f,
                    AgentMaxClimb = 0.25f,
                    AgentMaxSlope = 45.0f,
                    CellSize = 0.25f,
                    CellHeight = 0.15f
                };
                _navRegion.NavigationMesh = navMesh;
            }

            // Hornear la malla de forma síncrona en el hilo principal antes de que los actores soliciten rutas
            _navRegion.BakeNavigationMesh(onThread: false);
            GD.Print("[ArenaEnvironment] NavigationMesh horneada y registrada correctamente.");
        }
        else
        {
            GD.PushWarning("[ArenaEnvironment] No se encontró el nodo NavigationRegion3D.");
        }
    }
}
