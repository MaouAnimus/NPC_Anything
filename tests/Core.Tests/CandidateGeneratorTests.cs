using NpcAnything.Core.Domain;
using NpcAnything.Core.Tactics;
using Xunit;

namespace NpcAnything.Core.Tests;

public class CandidateGeneratorTests
{
    private static NpcContext CreateContext(
        Dictionary<string, MemoryFact> memory,
        List<Vec3>? covers = null,
        float healthRatio = 1.0f,
        List<Vec3>? waypoints = null,
        Vec3? position = null)
    {
        return new NpcContext(
            NpcId: "npc_01",
            Role: NpcRole.Assault,
            Position: position ?? new Vec3(0, 0, 0),
            HealthRatio: healthRatio,
            CurrentAction: ActionKind.Hold,
            CurrentActionScore: 0.30f,
            CurrentTimeMs: 1000,
            Memory: memory,
            ReachableCoverPoints: covers ?? new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>(),
            PatrolWaypoints: waypoints);
    }

    [Fact]
    public void GenerateCandidates_AcousticMemory_DoesNotGenerateAttackCandidate()
    {
        var generator = new ActionCandidateGenerator();

        // Hecho proveniente de audición (pasos o disparos)
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(5, 0, 0),
                Confidence: 0.8f,
                PrecisionMeters: 2.0f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_acoustic_hearing")
        };

        var context = CreateContext(memory);
        var candidates = generator.GenerateCandidates(context);

        // Debe generar Investigate, pero JAMÁS Attack
        Assert.DoesNotContain(candidates, c => c.Kind == ActionKind.Attack);
        Assert.Contains(candidates, c => c.Kind == ActionKind.Investigate);
    }

    [Fact]
    public void GenerateCandidates_FreshVisualMemory_GeneratesAttackCandidate()
    {
        var generator = new ActionCandidateGenerator();

        // Hecho visual directo fresco
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(5, 0, 0),
                Confidence: 1.0f,
                PrecisionMeters: 0.3f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_vision_raycast")
        };

        var context = CreateContext(memory);
        var candidates = generator.GenerateCandidates(context);

        Assert.Contains(candidates, c => c.Kind == ActionKind.Attack);
    }

    [Fact]
    public void GenerateCandidates_LowHealthAndCoversAvailable_GeneratesTakeCoverCandidate()
    {
        var generator = new ActionCandidateGenerator();

        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(5, 0, 0),
                Confidence: 1.0f,
                PrecisionMeters: 0.3f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_vision_raycast")
        };

        var covers = new List<Vec3> { new(2, 0, 2) };
        var context = CreateContext(memory, covers, healthRatio: 0.3f); // Salud baja (30%)

        var candidates = generator.GenerateCandidates(context);

        var coverCandidate = candidates.FirstOrDefault(c => c.Kind == ActionKind.TakeCover);
        Assert.NotNull(coverCandidate);
        Assert.True(coverCandidate.Score >= 0.70f); // Alta prioridad por salud crítica
    }

    [Fact]
    public void GenerateCandidates_NoThreat_WithPatrolWaypoints_GeneratesPatrolCandidateWithTargetPosition()
    {
        var generator = new ActionCandidateGenerator();
        var emptyMemory = new Dictionary<string, MemoryFact>();
        var waypoints = new List<Vec3> { new(6, 0, 0), new(-6, 0, 0) };

        var context = CreateContext(emptyMemory, waypoints: waypoints, position: new Vec3(0, 0, 0));
        var candidates = generator.GenerateCandidates(context);

        var patrolCandidate = candidates.FirstOrDefault(c => c.Kind == ActionKind.Patrol);
        Assert.NotNull(patrolCandidate);
        Assert.True(patrolCandidate.TargetPosition.HasValue);
        Assert.Equal(0.60f, patrolCandidate.Score);
        Assert.Equal("WAYPOINT_PATROL", patrolCandidate.ReasonCode);
    }

    [Fact]
    public void GenerateCandidates_NoThreat_SelectsNextWaypoint_WhenAtCurrent()
    {
        var generator = new ActionCandidateGenerator();
        var emptyMemory = new Dictionary<string, MemoryFact>();
        var waypoints = new List<Vec3> { new(6, 0, 0), new(-6, 0, 0) };

        // NPC situado exactamente en el waypoint [0] (6, 0, 0)
        var context = CreateContext(emptyMemory, waypoints: waypoints, position: new Vec3(6, 0, 0));
        var candidates = generator.GenerateCandidates(context);

        var patrolCandidate = candidates.FirstOrDefault(c => c.Kind == ActionKind.Patrol);
        Assert.NotNull(patrolCandidate);
        Assert.True(patrolCandidate.TargetPosition.HasValue);
        // Debe avanzar al waypoint [1] (-6, 0, 0)
        Assert.Equal(new Vec3(-6, 0, 0), patrolCandidate.TargetPosition.Value);
    }

    [Fact]
    public void GenerateCandidates_NoThreat_PatrolScoreOverridesHoldWithHysteresis()
    {
        var generator = new ActionCandidateGenerator();
        var emptyMemory = new Dictionary<string, MemoryFact>();
        var waypoints = new List<Vec3> { new(6, 0, 0), new(-6, 0, 0) };

        var context = CreateContext(emptyMemory, waypoints: waypoints, position: new Vec3(0, 0, 0));
        var candidates = generator.GenerateCandidates(context);

        var hold = candidates.First(c => c.Kind == ActionKind.Hold);
        var patrol = candidates.First(c => c.Kind == ActionKind.Patrol);

        // Histeresis margin estándar = 0.12f. Patrol (0.60) debe superar a Hold (0.30) + 0.12 = 0.42
        float hysteresisMargin = 0.12f;
        Assert.True(patrol.Score >= (hold.Score + hysteresisMargin));
    }

    [Fact]
    public void GenerateCandidates_VisualContact_BetweenAttackAndVisionRange_GeneratesPursueCandidate()
    {
        var generator = new ActionCandidateGenerator { MaxAttackRange = 12.0f, MaxVisionDistance = 20.0f };

        // Objetivo visible a 16 metros (entre 12m y 20m)
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(16, 0, 0),
                Confidence: 0.95f,
                PrecisionMeters: 0.3f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_vision_raycast")
        };

        var context = CreateContext(memory, position: new Vec3(0, 0, 0));
        var candidates = generator.GenerateCandidates(context);

        // Debe generar Pursue para cerrar distancia hacia el jugador visible
        var pursueCandidate = candidates.FirstOrDefault(c => c.Kind == ActionKind.Pursue);
        Assert.NotNull(pursueCandidate);
        Assert.Equal("Player", pursueCandidate.TargetId);
        Assert.True(pursueCandidate.TargetPosition.HasValue);
        Assert.True(pursueCandidate.Score >= 0.70f);
        Assert.Equal("TARGET_IN_SIGHT_CLOSE_DISTANCE", pursueCandidate.ReasonCode);

        // No debe generar Attack porque está fuera del alcance de 12m
        Assert.DoesNotContain(candidates, c => c.Kind == ActionKind.Attack);
    }

    [Fact]
    public void GenerateCandidates_VisualContact_WithinAttackRange_GeneratesAttackNotPursue()
    {
        var generator = new ActionCandidateGenerator { MaxAttackRange = 12.0f, MaxVisionDistance = 20.0f };

        // Objetivo visible a 8 metros (dentro de 12m)
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(8, 0, 0),
                Confidence: 0.95f,
                PrecisionMeters: 0.3f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_vision_raycast")
        };

        var context = CreateContext(memory, position: new Vec3(0, 0, 0));
        var candidates = generator.GenerateCandidates(context);

        Assert.Contains(candidates, c => c.Kind == ActionKind.Attack);
        Assert.DoesNotContain(candidates, c => c.Kind == ActionKind.Pursue);
    }

    [Theory]
    [InlineData(11.9f, true, false, false)]  // 11.9m: En rango de ataque (Attack, no Pursue)
    [InlineData(12.1f, false, true, false)]  // 12.1m: Umbral de persecución (Pursue, no Attack)
    [InlineData(19.9f, false, true, false)]  // 19.9m: Límite superior de visión (Pursue, no Attack ni Investigate)
    [InlineData(20.1f, false, false, true)]  // 20.1m: Fuera de rango de visión (Investigate última posición, no Pursue ni Attack)
    [InlineData(25.0f, false, false, true)]  // >20m: Investigar último avistamiento
    public void GenerateCandidates_DistanceBoundaries_TransitionProperly(
        float distance,
        bool expectAttack,
        bool expectPursue,
        bool expectInvestigate)
    {
        var generator = new ActionCandidateGenerator { MaxAttackRange = 12.0f, MaxVisionDistance = 20.0f };

        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(distance, 0, 0),
                Confidence: 0.95f,
                PrecisionMeters: 0.3f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_vision_raycast")
        };

        var context = CreateContext(memory, position: new Vec3(0, 0, 0));
        var candidates = generator.GenerateCandidates(context);

        bool hasAttack = candidates.Any(c => c.Kind == ActionKind.Attack);
        bool hasPursue = candidates.Any(c => c.Kind == ActionKind.Pursue);
        bool hasInvestigate = candidates.Any(c => c.Kind == ActionKind.Investigate);

        Assert.Equal(expectAttack, hasAttack);
        Assert.Equal(expectPursue, hasPursue);
        Assert.Equal(expectInvestigate, hasInvestigate);
    }
}
