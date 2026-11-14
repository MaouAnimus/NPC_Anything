namespace NpcAnything.GodotAdapter;

/// <summary>
/// Contrato fuertemente tipado para actores del juego que emiten perturbaciones acústicas (pasos, disparos).
/// Evita accesos por reflexión dinámica (Get) en el adaptador.
/// </summary>
public interface IAcousticEmitter
{
    float CurrentNoiseRadius { get; }
}
