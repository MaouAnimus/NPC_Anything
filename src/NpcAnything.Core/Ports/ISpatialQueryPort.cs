using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Ports;

/// <summary>
/// Puerto espacial para consultas de geometría, navegación y puntos de cobertura alcanzables.
/// Responde consultas sobre conectividad física y rutas sin decidir sobre conveniencia táctica.
/// </summary>
public interface ISpatialQueryPort
{
    /// <summary>
    /// Indica si la malla y el servidor de navegación espacial están sincronizados y disponibles para consultas.
    /// </summary>
    bool IsNavigationReady => true;

    /// <summary>
    /// Consulta los puntos de cobertura físicamente alcanzables mediante una ruta válida en la malla de navegación.
    /// </summary>
    IReadOnlyList<Vec3> QueryReachableCovers(string npcId, Vec3 origin, float maxRadiusMeters);

    /// <summary>
    /// Verifica si existe una ruta navegable válida y completa entre dos puntos sin superar una distancia máxima de recorrido.
    /// </summary>
    bool IsPathReachable(Vec3 origin, Vec3 destination, float maxPathLength);
}
