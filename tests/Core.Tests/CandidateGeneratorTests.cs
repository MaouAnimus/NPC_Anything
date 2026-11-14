using NpcAnything.Core.Domain;
using NpcAnything.Core.Tactics;
using Xunit;

namespace NpcAnything.Core.Tests;

public class CandidateGeneratorTests
{
    private static NpcContext CreateContext(
        Dictionary<string, MemoryFact> memory,
        List<Vec3>? covers = null,
        float healthRatio = 1.0f)
    {
        return new NpcContext(
            NpcId: "npc_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: healthRatio,
            CurrentAction: ActionKind.Hold,
            CurrentActionScore: 0.4f,
            CurrentTimeMs: 1000,
            Memory: memory,
            ReachableCoverPoints: covers ?? new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());
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
}
