using Godot;
using NpcAnything.GodotAdapter;

namespace GodotArena.Scripts;

/// <summary>
/// Controlador del entorno de la arena.
/// Asegura que la malla de navegación (NavigationMesh) esté horneada e inicializada al arrancar la escena.
/// Incluye validación explícita de polígonos navegables y conectividad física alrededor del muro central.
/// </summary>
public partial class ArenaEnvironment : Node3D
{
    private NavigationRegion3D? _navRegion;

    public static bool IsNavigationReady => GodotNavigationReadiness.IsReady;

    public override void _Ready()
    {
        GodotNavigationReadiness.SetReady(false);
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

            int polygonCount = _navRegion.NavigationMesh.GetPolygonCount();
            if (polygonCount > 0)
            {
                GD.Print($"[ArenaEnvironment] NavigationMesh horneada con éxito: {polygonCount} polígonos navegables.");
            }
            else
            {
                GD.PushError("[ArenaEnvironment] Error: NavigationMesh se horneó con 0 polígonos navegables.");
            }

            // Esperar explícitamente a que transcurran ciclos de física para sincronización en NavigationServer3D (P1-04)
            Callable.From(AwaitAndValidateNavigationSync).CallDeferred();
        }
        else
        {
            GD.PushWarning("[ArenaEnvironment] No se encontró el nodo NavigationRegion3D.");
        }
    }

    private async void AwaitAndValidateNavigationSync()
    {
        // Esperar ciclos de física reales del árbol de escena
        for (int frame = 0; frame < 5 && !IsNavigationReady; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            Rid navMap = GetWorld3D()?.NavigationMap ?? default;
            if (navMap == default || !navMap.IsValid || NavigationServer3D.MapGetIterationId(navMap) == 0)
            {
                continue;
            }

            // Validación de prueba de ruta alrededor del muro central (de Z = -8 a Z = 8 cruzando X = 0)
            Vector3 northPoint = new Vector3(0f, 0f, -8f);
            Vector3 southPoint = new Vector3(0f, 0f, 8f);

            Vector3[] path = NavigationServer3D.MapGetPath(navMap, northPoint, southPoint, optimize: true);
            if (path.Length >= 2)
            {
                bool crossesWall = false;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    Vector3 p1 = path[i];
                    Vector3 p2 = path[i + 1];
                    Vector3 mid = (p1 + p2) * 0.5f;
                    if (Mathf.Abs(mid.X) < 5.8f && Mathf.Abs(mid.Z) < 0.5f)
                    {
                        crossesWall = true;
                        break;
                    }
                }

                if (!crossesWall)
                {
                    GodotNavigationReadiness.SetReady(true);
                    GD.Print($"[ArenaEnvironment] Sincronización exitosa en frame {frame + 1}: Ruta de {path.Length} nodos rodea correctamente el muro central.");
                    return;
                }
            }
        }

        if (!IsNavigationReady)
        {
            GD.PushError("[ArenaEnvironment] FALLO DE NAVEGACIÓN: NavigationServer3D no sincronizó rutas válidas.");
        }
    }

    public override void _ExitTree()
    {
        GodotNavigationReadiness.SetReady(false);
    }
}
