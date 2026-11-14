using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Ports;

/// <summary>
/// Puerto espacial para consultas de geometría, navegación y puntos de cobertura alcanzables.
/// Mantiene la separación de responsabilidades: IWorldSensorPort se encarga de los estímulos,
/// mientras que ISpatialQueryPort consulta la navegabilidad física del entorno.
/// </summary>
public interface ISpatialQueryPort
{
    /// <summary>
    /// Consulta los puntos de cobertura físicamente alcanzables para un NPC desde un origen dado.
    /// Las posiciones retornadas deben ser puntos transitables donde el NPC puede posicionarse.
    /// </summary>
    /// <param name="npcId">Identificador del NPC que realiza la consulta.</param>
    /// <param name="origin">Punto de origen de la consulta (habitualmente la posición actual del NPC).</param>
    /// <param name="maxRadiusMeters">Radio máximo de búsqueda en metros.</param>
    /// <returns>Lista de posiciones transitables válidas que ofrecen cobertura.</returns>
    IReadOnlyList<Vec3> QueryReachableCovers(string npcId, Vec3 origin, float maxRadiusMeters);
}
