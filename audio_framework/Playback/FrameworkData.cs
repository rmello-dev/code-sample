
using UnityEngine;

using Game.Api.Pooling;

namespace Game.Api.Playback
{

/// <summary>
/// Encapsulation structure for audio source configuration parameters.
/// </summary>
public readonly struct FrameworkData
    {
    // --- Properties:

    /// <summary>
    /// Exposes hierarchy root needed for live channel attachment.
    /// </summary>
    public GameObject AudioRoot { get; }

    /// <summary>
    /// Exposes audio pool needed for reusable channel allocation.
    /// </summary>
    public ReusablePool<AudioChannel> AudioPool { get; }

    // --- Initialization:

    /// <summary>
    /// Creates data structure with parameters necessary to configure live audio sources.
    /// </summary>
    public FrameworkData(GameObject audioRoot, ReusablePool<AudioChannel> audioPool)
        {
        AudioRoot = audioRoot;
        AudioPool = audioPool;
        }

    }


}
