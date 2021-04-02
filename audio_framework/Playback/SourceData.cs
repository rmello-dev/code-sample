
using Game.Lib.Media;
using Game.Lib.Space;

namespace Game.Api.Playback
{
    
/// <summary>
/// Encapsulates active channel/source data needed for playback.
/// </summary>
public readonly struct SourceData
    {
    // --- Properties:

    public IdentityData AudioID { get; }        // identity token for playback source  

    public AudioPlayback.Range Range { get; }   // indicates attenuation parameters
    public GridPoint Bearing { get; }           // indicates spatial origin
    public float Volume { get; }                // indicates ideal volume setting
    public bool Loop { get; }                   // indicates repetition request

    public double Offset { get; }               // tracks partially elapsed playback and enables mid-clip data handover/resume

    public bool Paused { get; }                 // flags currently interrupted playback
    public bool Muted { get; }                  // flags currently muted playback
    
    // --- Initialization:

    public SourceData(IdentityData audioID, GridPoint bearing, AudioPlayback.Range range, float volume, bool loop = false, double offset = 0, bool pause = false, bool mute = false)
        {
        AudioID = audioID;

        Range = range;        
        Bearing = bearing;
        Volume = volume;
        Loop = loop;

        Offset = offset;

        Paused = pause;
        Muted = mute;
        }

    }


}
