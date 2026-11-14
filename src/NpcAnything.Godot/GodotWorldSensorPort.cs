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
    private readonly float _visionAngleDegrees;

    public GodotWorldSensorPort(
        CharacterBody3D actor,
        RayCast3D? lineOfSightRay,
        float maxVisionDistance = 20.0f,
        float visionAngleDegrees = 85.0f)
    {
        _actor = actor;
        _lineOfSightRay = lineOfSightRay;
        _maxVisionDistance = maxVisionDistance;
        _visionAngleDegrees = visionAngleDegrees;
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

            // 1. Percepción Visual (Cono FOV + Oclusión por RayCast)
            if (distance <= _maxVisionDistance)
            {
                Vector3 forward = -_actor.Transform.Basis.Z;
                float angle = Mathf.RadToDeg(forward.AngleTo(toTarget));

                if (angle <= _visionAngleDegrees && _lineOfSightRay != null)
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

            // 2. Percepción Acústica (Ruido emitido por el jugador)
            float noiseRadius = 0f;
            var noiseProp = playerBody.Get("CurrentNoiseRadius");
            if (noiseProp.VariantType == Variant.Type.Float)
            {
                noiseRadius = noiseProp.AsSingle();
            }

            if (noiseRadius > 0f && distance <= noiseRadius)
            {
                // El sonido decae con la distancia y aporta incertidumbre espacial
                float acousticConfidence = Mathf.Clamp(1.0f - (distance / noiseRadius) * 0.4f, 0.4f, 0.9f);
                float uncertaintyMeters = 1.0f + (distance * 0.1f);

                stimuli.Add(new Stimulus(
                    SubjectId: playerBody.Name,
                    Kind: StimulusType.Audio,
                    EstimatedPosition: playerBody.GlobalPosition.ToVec3(),
                    Confidence: acousticConfidence,
                    PrecisionMeters: uncertaintyMeters,
                    ObservedAtMs: simulationTimeMs,
                    SourceId: "godot_acoustic_hearing"));
            }
        }

        return stimuli;
    }
}
