namespace NpcAnything.Core.Domain;

/// <summary>
/// Roles tácticos del escuadrón. Modulan los pesos de utilidad y las prioridades de decisión.
/// </summary>
public enum NpcRole
{
    Assault,    // Presión frontal, avance entre coberturas cortas
    Flanker,    // Búsqueda de ángulos abiertos y trayectorias laterales
    Support,    // Fuego de supresión y cobertura defensiva a compañeros
    Scout,      // Detección anticipada, sigilo y transmisión de alertas
    Companion   // Asistencia al jugador: no bloquear paso ni robar objetivos
}
