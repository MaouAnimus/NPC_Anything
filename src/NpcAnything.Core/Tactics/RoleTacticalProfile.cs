using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Tactics;

/// <summary>
/// Perfil táctico desacoplado que modula las ponderaciones de utilidad de cada acción
/// según la especialización del rol del NPC (RF-07, CU-04).
/// </summary>
public record RoleTacticalProfile(
    NpcRole Role,
    float AttackWeight = 1.0f,
    float PursueWeight = 1.0f,
    float CoverWeight = 1.0f,
    float FlankWeight = 1.0f,
    float InvestigateWeight = 1.0f,
    float CriticalHealthThreshold = 0.35f,
    float PreferredEngagementDistance = 10.0f)
{
    /// <summary>
    /// Retorna el perfil táctico predeterminado calibrado para el rol especificado.
    /// </summary>
    public static RoleTacticalProfile ForRole(NpcRole role) => role switch
    {
        // Asalto: Alta presión frontal, avance agresivo, tolera menor cobertura.
        NpcRole.Assault => new(
            NpcRole.Assault,
            AttackWeight: 1.25f,
            PursueWeight: 1.20f,
            CoverWeight: 0.85f,
            FlankWeight: 0.70f,
            InvestigateWeight: 0.90f,
            CriticalHealthThreshold: 0.35f,
            PreferredEngagementDistance: 6.0f),

        // Flanqueador: Prioriza movilidad lateral, búsqueda de ángulos y sorpresa sobre el avance frontal.
        NpcRole.Flanker => new(
            NpcRole.Flanker,
            AttackWeight: 0.80f,
            PursueWeight: 0.85f,
            CoverWeight: 1.10f,
            FlankWeight: 1.45f,
            InvestigateWeight: 1.00f,
            CriticalHealthThreshold: 0.40f,
            PreferredEngagementDistance: 11.0f),

        // Apoyo: Cobertura defensiva, fuego a media distancia, protege compañeros.
        NpcRole.Support => new(
            NpcRole.Support,
            AttackWeight: 0.85f,
            PursueWeight: 0.75f,
            CoverWeight: 1.35f,
            FlankWeight: 0.80f,
            InvestigateWeight: 1.00f,
            CriticalHealthThreshold: 0.50f,
            PreferredEngagementDistance: 12.0f),

        // Explorador: Cautela, investigación, observación a distancia, evita combate frontal abierto.
        NpcRole.Scout => new(
            NpcRole.Scout,
            AttackWeight: 0.65f,
            PursueWeight: 0.80f,
            CoverWeight: 1.15f,
            FlankWeight: 1.10f,
            InvestigateWeight: 1.40f,
            CriticalHealthThreshold: 0.60f,
            PreferredEngagementDistance: 16.0f),

        // Compañero: Soporte al jugador sin bloquear paso ni robar objetivos.
        NpcRole.Companion => new(
            NpcRole.Companion,
            AttackWeight: 0.80f,
            PursueWeight: 0.70f,
            CoverWeight: 1.10f,
            FlankWeight: 0.70f,
            InvestigateWeight: 1.00f,
            CriticalHealthThreshold: 0.40f,
            PreferredEngagementDistance: 8.0f),

        _ => new(role)
    };
}
