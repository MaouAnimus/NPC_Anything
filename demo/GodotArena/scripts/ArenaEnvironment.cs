using Godot;

namespace GodotArena.Scripts;

/// <summary>
/// Controlador del entorno de la arena.
/// Asegura que la malla de navegación (NavigationMesh) esté horneada e inicializada al arrancar la escena.
/// Incluye validación explícita de polígonos navegables y conectividad física alrededor del muro central.
/// </summary>
public partial class ArenaEnvironment : Node3D
{
    private NavigationRegion3D? _navRegion;

    public bool IsNavigationReady { get; private set; } = false;

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

            int polygonCount = _navRegion.NavigationMesh.GetPolygonCount();
            if (polygonCount > 0)
            {
                GD.Print($"[ArenaEnvironment] NavigationMesh horneada con éxito: {polygonCount} polígonos navegables.");
            }
            else
            {
                GD.PushError("[ArenaEnvironment] Error: NavigationMesh se horneó con 0 polígonos navegables.");
            }

            // Esperar el siguiente ciclo para validar la sincronización en NavigationServer3D
            Callable.From(ValidateNavigationServerSync).CallDeferred();
        }
        else
        {
            GD.PushWarning("[ArenaEnvironment] No se encontró el nodo NavigationRegion3D.");
        }
    }

    private void ValidateNavigationServerSync()
    {
        Rid navMap = GetWorld3D()?.NavigationMap ?? default;
        if (navMap == default || !navMap.IsValid)
        {
            GD.PushWarning("[ArenaEnvironment] NavigationMap aún no está listo en el frame diferido.");
            return;
        }

        // Validación de prueba de ruta alrededor del muro central (de Z = -8 a Z = 8 cruzando X = 0)
        Vector3 northPoint = new Vector3(0f, 0f, -8f);
        Vector3 southPoint = new Vector3(0f, 0f, 8f);

        Vector3[] path = NavigationServer3D.MapGetPath(navMap, northPoint, southPoint, optimize: true);
        if (path.Length >= 2)
        {
            // Comprobar que ningún segmento atraviesa el muro central (|X| < 5.8 && |Z| < 0.5)
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
                IsNavigationReady = true;
                GD.Print($"[ArenaEnvironment] Verificación de navegación aprobada: Ruta de {path.Length} nodos rodea correctamente el muro central.");
            }
            else
            {
                GD.PushError("[ArenaEnvironment] FALLO DE NAVEGACIÓN: La ruta calculada atraviesa el muro central.");
            }
        }
        else
        {
            GD.PushWarning("[ArenaEnvironment] Mapa de navegación pendiente de sincronización en servidor.");
        }
    }
}
