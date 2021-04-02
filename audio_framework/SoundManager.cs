using System.Collections;

using UnityEngine;

using Game.Api.Playback;
using Game.Api.Pooling;
using Game.Lib.Media;
using Game.Lib.Space;

namespace Game.Api
{

/// <summary>
/// Sound effects manager.
/// Controller of SFX mixing.
/// </summary>
public sealed class SoundManager : AudioMixer
    {
    // --- Fields:
  
    private const float AUX_LEVEL = 0.7f;       // aux volume modifier, applied on top of sfx setting
        
    // --- Properties:
    

    // --- Initialization:

    /// <summary>
    /// Initial state constructor.
    /// </summary>
    public SoundManager(ReusablePool<AudioChannel> commonAudioPool, byte reservedChannels) : base(commonAudioPool,reservedChannels)
        {
        // setup initial state

            // TODO
            
        Ducking = DuckingEffect.AutoDucking;                                // number of simultaneous sources before auto noise dampening
        DuckingThreshold = (byte)Mathf.RoundToInt(reservedChannels*0.6f);   // dampen at 10 for max 16 live channels
        }

    // --- External Behaviors:

    /// <summary>
    /// Requests playback of interface-feedback sfx.
    /// Uses pre-defined parameters.
    /// </summary>
    public IdentityData PlayAux(AudioData auxAudio)
        {

        return(Play(auxAudio,AudioPlayback.Range.RZone.Omni,1f*AUX_LEVEL,false));
        }

    /// <summary>
    /// Requests playback of environmental sfx.
    /// Receives parameters for fixed/zone sources.
    /// </summary>
    public IdentityData PlayEnv(AudioData audio, AudioPlayback.Range.RZone zone, float idealVolume = 1f, bool loop = false)
        {

        return(Play(audio,zone,idealVolume,loop));
        }

    /// <summary>
    /// Requests playback of environmental sfx.
    /// Receives parameters for dynamic/grid sources.
    /// </summary>
    public IdentityData PlayEnv(AudioData audio, GridPoint bearing, AudioPlayback.Range range, bool loop = false)
        {

        return(Play(audio,bearing,range,1f,loop));      // TODO needs volume parameter for external gradient control?
        }

    // --- Internal Procedures:


    }


}
