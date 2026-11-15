using System.Collections.Generic;
using Godot;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Adaptador del puerto sensorial para Godot.
/// Traduce físicas (raycasts, conos angulares y áreas acústicas) a estímulos puros.
/// REGLA ESTRICTA: Cero decisiones tácticas dentro del adaptador.
/// </summary>
public class GodotWorldSensorPort : IWorldSensorPort
{
    private readonly CharacterBody3D _actor;
    private readonly RayCast3D? _lineOfSightRay;
    private readonly float _maxVisionDistance;
    private readonly float _visionHalfAngleDegrees;
    private readonly uint _randomSeed;

    public GodotWorldSensorPort(
        CharacterBody3D actor,
        RayCast3D? lineOfSightRay,
        float maxVisionDistance = 20.0f,
        float visionHalfAngleDegrees = 45.0f,
        uint randomSeed = 42) // 45° a cada lado = 90° de FOV total
    {
        _actor = actor;
        _lineOfSightRay = lineOfSightRay;
        _maxVisionDistance = maxVisionDistance;
        _visionHalfAngleDegrees = visionHalfAngleDegrees;
        _randomSeed = randomSeed;
    }

    /// <summary>
    /// Hash determinista FNV-1a de 32 bits estable entre diferentes procesos y ejecuciones independientes.
    /// Garantiza reproducibilidad científica estricta bajo una misma semilla sin depender de System.HashCode.
    /// </summary>
    public static uint ComputeFnv1a32(uint seed, string npcId, string targetId, int timeBucket)
    {
        const uint fnvPrime = 16777619;
        uint hash = 2166136261 ^ seed;

        for (int i = 0; i < npcId.Length; i++)
        {
            hash ^= (byte)npcId[i];
            hash *= fnvPrime;
            hash ^= (byte)(npcId[i] >> 8);
            hash *= fnvPrime;
        }

        for (int i = 0; i < targetId.Length; i++)
        {
            hash ^= (byte)targetId[i];
            hash *= fnvPrime;
            hash ^= (byte)(targetId[i] >> 8);
            hash *= fnvPrime;
        }

        uint tb = (uint)timeBucket;
        hash ^= (byte)(tb & 0xFF);
        hash *= fnvPrime;
        hash ^= (byte)((tb >> 8) & 0xFF);
        hash *= fnvPrime;
        hash ^= (byte)((tb >> 16) & 0xFF);
        hash *= fnvPrime;
        hash ^= (byte)((tb >> 24) & 0xFF);
        hash *= fnvPrime;

        return hash;
    }

    public IReadOnlyList<Stimulus> Observe(string npcId, long simulationTimeMs)
    {
        var stimuli = new List<Stimulus>();
        if (!_actor.IsInsideTree()) return stimuli;

        var tree = _actor.GetTree();
        var players = tree.GetNodesInGroup("Player");

        foreach (var node in players)
        {
            if (node is not CharacterBody3D playerBody) continue;

            Vector3 actorEyePos = _actor.GlobalPosition + new Vector3(0f, 1.2f, 0f);
            Vector3 targetEyePos = playerBody.GlobalPosition + new Vector3(0f, 1.0f, 0f);
            Vector3 toTarget = targetEyePos - actorEyePos;
            float distance = toTarget.Length();

            // 1. Percepción Visual (Cono FOV de 90° + Oclusión física por RayCast)
            if (distance <= _maxVisionDistance)
            {
                Vector3 forward = -_actor.Transform.Basis.Z;
                float angle = Mathf.RadToDeg(forward.AngleTo(toTarget));

                if (angle <= _visionHalfAngleDegrees && _lineOfSightRay != null)
                {
                    _lineOfSightRay.GlobalPosition = actorEyePos;
                    _lineOfSightRay.TargetPosition = _lineOfSightRay.ToLocal(targetEyePos);
                    _lineOfSightRay.ForceRaycastUpdate();

                    if (_lineOfSightRay.IsColliding())
                    {
                        var collider = _lineOfSightRay.GetCollider() as Node;
                        if (collider != null && collider.IsInGroup("Player"))
                        {
                            // Visión directa confirmada sin oclusión
                            stimuli.Add(new Stimulus(
                                SubjectId: playerBody.Name,
                                Kind: StimulusType.Visual,
                                EstimatedPosition: playerBody.GlobalPosition.ToVec3(),
                                Confidence: 1.0f,
                                PrecisionMeters: 0.3f,
                                ObservedAtMs: simulationTimeMs,
                                SourceId: "godot_vision_raycast"));
                        }
                    }
                }
            }

            // 2. Percepción Acústica (Ruido emitido por el jugador mediante interfaz tipada)
            float noiseRadius = 0f;
            if (playerBody is IAcousticEmitter emitter)
            {
                noiseRadius = emitter.CurrentNoiseRadius;
            }

            if (noiseRadius > 0f && distance <= noiseRadius)
            {
                // El sonido decae con la distancia y aporta incertidumbre espacial
                float acousticConfidence = Mathf.Clamp(1.0f - (distance / noiseRadius) * 0.4f, 0.4f, 0.85f);
                float uncertaintyMeters = 1.0f + (distance * 0.1f);

                // Dispersión acústica reproducible en el plano horizontal basada en PrecisionMeters
                // Ventana temporal de 500ms para coherencia acústica sin saltos erráticos por frame
                int timeBucket = (int)(simulationTimeMs / 500);
                uint fnvHash = ComputeFnv1a32(_randomSeed, npcId, playerBody.Name, timeBucket);
                var rng = new System.Random((int)(fnvHash & 0x7FFFFFFF));
                double angleRad = rng.NextDouble() * System.Math.PI * 2.0;
                double offsetDist = rng.NextDouble() * uncertaintyMeters;

                Vector3 noisyPos = playerBody.GlobalPosition + new Vector3(
                    (float)(System.Math.Cos(angleRad) * offsetDist),
                    0f,
                    (float)(System.Math.Sin(angleRad) * offsetDist));

                stimuli.Add(new Stimulus(
                    SubjectId: playerBody.Name,
                    Kind: StimulusType.Audio,
                    EstimatedPosition: noisyPos.ToVec3(),
                    Confidence: acousticConfidence,
                    PrecisionMeters: uncertaintyMeters,
                    ObservedAtMs: simulationTimeMs,
                    SourceId: "godot_acoustic_hearing"));
            }
        }

        return stimuli;
    }
}
