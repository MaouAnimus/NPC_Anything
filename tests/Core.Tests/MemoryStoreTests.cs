using NpcAnything.Core.Domain;
using NpcAnything.Core.Memory;
using Xunit;

namespace NpcAnything.Core.Tests;

public class MemoryStoreTests
{
    [Fact]
    public void ProcessStimuli_VisualStimulus_StoresFactWithFullConfidence()
    {
        var store = new MemoryStore(defaultTtlMs: 3000);
        long now = 1000;

        var stimuli = new[]
        {
            new Stimulus("player", StimulusType.Visual, new Vec3(10, 0, 5), 1.0f, 0.5f, now, "eye_sensor")
        };

        store.ProcessStimuli(stimuli, now);
        var fact = store.GetFact("player", now);

        Assert.NotNull(fact);
        Assert.Equal("player", fact.SubjectId);
        Assert.Equal(new Vec3(10, 0, 5), fact.EstimatedPosition);
        Assert.Equal(1.0f, fact.Confidence);
        Assert.False(fact.IsExpired(now + 2000));
        Assert.True(fact.IsExpired(now + 3001));
    }

    [Fact]
    public void GetFact_ExpiredFact_ReturnsNullAndPurges()
    {
        var store = new MemoryStore(defaultTtlMs: 2000);
        long now = 1000;

        store.ProcessStimuli(new[]
        {
            new Stimulus("player", StimulusType.Visual, new Vec3(0, 0, 0), 1.0f, 0.5f, now, "sensor")
        }, now);

        // Aún válido a los 1500ms
        Assert.NotNull(store.GetFact("player", now + 500));

        // Expirado a los 3500ms (pasaron 2500ms > TTL 2000ms)
        Assert.Null(store.GetFact("player", now + 2500));
    }

    [Fact]
    public void Fact_DecayedConfidence_DecreasesOverTime()
    {
        var fact = new MemoryFact("enemy", new Vec3(0, 0, 0), 1.0f, 1.0f, 1000, 5000, "eye");

        float initialConf = fact.GetDecayedConfidence(1000);
        float laterConf = fact.GetDecayedConfidence(3000);
        float expiredConf = fact.GetDecayedConfidence(6000);

        Assert.Equal(1.0f, initialConf, 2);
        Assert.True(laterConf < initialConf);
        Assert.True(laterConf > 0f);
        Assert.Equal(0f, expiredConf);
    }
}
