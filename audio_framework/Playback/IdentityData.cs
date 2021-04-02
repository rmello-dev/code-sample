
using Game.Lib.Media;

namespace Game.Api.Playback
{
    
/// <summary>
/// Encapsulates identity data for audio content and source.
/// </summary>
public readonly struct IdentityData
    {
    // --- Properties:

    /// <summary>
    /// Provides library identifier matching audio content. 
    /// Useful when only one instance of the sound is expected.
    /// </summary>
    public AudioData ContentID { get; }     // NOTE: to match unique zone sounds
    
    /// <summary>
    /// Provides unique identifier matching specific audio source.
    /// Useful to distinguish between duplicate instances of the same sound.
    /// </summary>
    private SSID SourceID { get; }          // NOTE: to distinguish duplicate object sounds

    // --- Initialization:

    /// <summary>
    /// Creates unique identity for given audio content.
    /// </summary>
    public IdentityData(AudioData audio)
        {
        ContentID = audio;

        SourceID = SSID.New();
        }

    // --- External Behaviors:

    /// <summary>
    /// Compares two identity tokens.
    /// Return indicates equality.
    /// </summary>
    public bool Match(IdentityData otherID)
        {
        bool match = (ContentID.LibID == otherID.ContentID.LibID && SourceID == otherID.SourceID);

        return(match);
        }

    }


}
