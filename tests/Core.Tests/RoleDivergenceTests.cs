using NpcAnything.Core.Domain;
using NpcAnything.Core.Tactics;
using Xunit;

namespace NpcAnything.Core.Tests;

public class RoleDivergenceTests
{
    private static NpcContext CreateContext(
        NpcRole role,
        float healthRatio = 1.0f,
        Vec3? npcPos = null,
        Vec3? targetPos = null,
        List<Vec3>? covers = null,
        List<Vec3>? allies = null)
    {
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                targetPos ?? new Vec3(0, 0, 10), // Objetivo a 10m al frente (eje Z)
                Confidence: 0.95f,
                PrecisionMeters: 0.3f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_vision_raycast")
        };

        return new NpcContext(
            NpcId: $"npc_{role.ToString().ToLower()}",
            Role: role,
            Position: npcPos ?? new Vec3(0, 0, 0),
            HealthRatio: healthRatio,
            CurrentAction: ActionKind.Hold,
            CurrentActionScore: 0.30f,
            CurrentTimeMs: 1000,
            Memory: memory,
            ReachableCoverPoints: covers ?? new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>(),
            KnownAllyPositions: allies);
    }

    [Fact]
    public void AssaultRole_PrioritizesFrontalAttack_OverFlank()
    {
        var generator = new ActionCandidateGenerator();

        // Cobertura lateral a 45 grados (flanqueo potencial)
        var lateralCover = new List<Vec3> { new Vec3(7, 0, 7) };
        var context = CreateContext(NpcRole.Assault, covers: lateralCover);

        var candidates = generator.GenerateCandidates(context);

        var attack = candidates.FirstOrDefault(c => c.Kind == ActionKind.Attack);
        var flank = candidates.FirstOrDefault(c => c.Kind == ActionKind.Flank);

        Assert.NotNull(attack);
        // Asalto pondera el ataque frontal por encima del flanqueo
        if (flank != null)
        {
            Assert.True(attack.Score > flank.Score);
        }
    }

    [Fact]
    public void FlankerRole_PrioritizesLateralFlank_OverFrontalAttack()
    {
        var generator = new ActionCandidateGenerator();

        // Misma escena: Cobertura lateral a 45 grados
        var lateralCover = new List<Vec3> { new Vec3(7, 0, 7) };
        var context = CreateContext(NpcRole.Flanker, covers: lateralCover);

        var candidates = generator.GenerateCandidates(context);

        var attack = candidates.FirstOrDefault(c => c.Kind == ActionKind.Attack);
        var flank = candidates.FirstOrDefault(c => c.Kind == ActionKind.Flank);

        Assert.NotNull(flank);
        Assert.NotNull(attack);

        // Flanqueador prioriza el movimiento lateral sobre el fuego frontal directo
        Assert.True(flank.Score > attack.Score);
        Assert.Equal("FLANK_LATERAL_ROUTE", flank.ReasonCode);
    }

    [Fact]
    public void SupportRole_PrioritizesDefensiveCover_AtMediumHealth()
    {
        var generator = new ActionCandidateGenerator();

        var safeCover = new List<Vec3> { new Vec3(0, 0, -3) };

        // Misma salud (50%) para Asalto y Apoyo
        var assaultContext = CreateContext(NpcRole.Assault, healthRatio: 0.50f, covers: safeCover);
        var supportContext = CreateContext(NpcRole.Support, healthRatio: 0.50f, covers: safeCover);

        var assaultCandidates = generator.GenerateCandidates(assaultContext);
        var supportCandidates = generator.GenerateCandidates(supportContext);

        var assaultCover = assaultCandidates.First(c => c.Kind == ActionKind.TakeCover);
        var assaultAttack = assaultCandidates.First(c => c.Kind == ActionKind.Attack);

        var supportCover = supportCandidates.First(c => c.Kind == ActionKind.TakeCover);
        var supportAttack = supportCandidates.First(c => c.Kind == ActionKind.Attack);

        // Asalto a 50% de vida aún presiona con ataque por encima de la cobertura
        Assert.True(assaultAttack.Score > assaultCover.Score);

        // Apoyo a 50% de vida ya activó su umbral crítico y prioriza cobertura defensiva
        Assert.True(supportCover.Score > supportAttack.Score);
    }

    [Fact]
    public void CustomRoleProfile_ModulatesScores_WithoutRecompilingCore()
    {
        var generator = new ActionCandidateGenerator();

        // Perfil personalizado ultra-defensivo para Asalto
        var customAssault = new RoleTacticalProfile(
            NpcRole.Assault,
            AttackWeight: 0.20f,
            CoverWeight: 1.80f,
            CriticalHealthThreshold: 0.90f);

        generator.CustomRoleProfile = customAssault;

        var cover = new List<Vec3> { new Vec3(2, 0, 2) };
        var context = CreateContext(NpcRole.Assault, healthRatio: 0.80f, covers: cover);

        var candidates = generator.GenerateCandidates(context);

        var attack = candidates.First(c => c.Kind == ActionKind.Attack);
        var takeCover = candidates.First(c => c.Kind == ActionKind.TakeCover);

        // Con el perfil personalizado, la cobertura supera al ataque
        Assert.True(takeCover.Score > attack.Score);
    }

    [Fact]
    public void ScoutRole_PrioritizesCoverOverAttack_WhenThreatIsClose()
    {
        var generator = new ActionCandidateGenerator();

        var cover = new List<Vec3> { new Vec3(2, 0, 2) };
        // Scout con salud completa ante amenaza directa a 10m
        var scoutContext = CreateContext(NpcRole.Scout, healthRatio: 1.0f, covers: cover);
        var assaultContext = CreateContext(NpcRole.Assault, healthRatio: 1.0f, covers: cover);

        var scoutCandidates = generator.GenerateCandidates(scoutContext);
        var assaultCandidates = generator.GenerateCandidates(assaultContext);

        var scoutCover = scoutCandidates.First(c => c.Kind == ActionKind.TakeCover);
        var scoutAttack = scoutCandidates.First(c => c.Kind == ActionKind.Attack);

        var assaultCover = assaultCandidates.First(c => c.Kind == ActionKind.TakeCover);
        var assaultAttack = assaultCandidates.First(c => c.Kind == ActionKind.Attack);

        // Asalto con vida completa ataca decididamente
        Assert.True(assaultAttack.Score > assaultCover.Score);

        // Scout es cauteloso y prioriza protección defensiva frente al combate frontal directo
        Assert.True(scoutCover.Score > scoutAttack.Score);
    }

    [Fact]
    public void CompanionRole_SelectsCoverNearProtectedAlly_AndRestrainsPursue()
    {
        var generator = new ActionCandidateGenerator();

        // Dos coberturas: una lejana (X=15, Z=15) y otra cercana al aliado (X=1, Z=1)
        var farCover = new Vec3(15, 0, 15);
        var nearAllyCover = new Vec3(1, 0, 1);
        var covers = new List<Vec3> { farCover, nearAllyCover };

        // Aliado protegido (jugador) ubicado en (0, 0, 0), NPC compañero en (0, 0, 9)
        var allyPos = new List<Vec3> { new Vec3(0, 0, 0) };
        var companionContext = CreateContext(
            NpcRole.Companion,
            healthRatio: 0.6f,
            npcPos: new Vec3(0, 0, 9),
            targetPos: new Vec3(0, 0, 25), // Objetivo a 16m del compañero (distancia de persecución)
            covers: covers,
            allies: allyPos);

        var candidates = generator.GenerateCandidates(companionContext);

        var coverCandidate = candidates.FirstOrDefault(c => c.Kind == ActionKind.TakeCover);
        var pursueCandidate = candidates.FirstOrDefault(c => c.Kind == ActionKind.Pursue);

        Assert.NotNull(coverCandidate);
        // Debe haber seleccionado la cobertura cercana al aliado para no abandonarlo
        Assert.Equal(nearAllyCover, coverCandidate.TargetPosition);

        // Como el compañero está a 9m del aliado (>8m), su score de Pursue fue moderado a la mitad
        if (pursueCandidate != null)
        {
            Assert.True(pursueCandidate.Score <= 0.50f);
        }
    }
}
