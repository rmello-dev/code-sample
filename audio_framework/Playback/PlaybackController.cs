
using Game.Lib.Space;

namespace Game.Api.Playback
{

/// <summary>
/// Interface for playback channel and audio source control.
/// </summary>
public interface PlaybackController
    {
    // --- Properties:
    
    /// <summary>
    /// Identifies audio content and source object.
    /// </summary>
    IdentityData AudioID { get; }

    /// <summary>
    /// Indicates whether playback is ongoing.
    /// </summary>
    bool isBusy { get; }

    /// <summary>
    /// Indicates whether playback is silenced.
    /// </summary>
    bool isMute { get; }

    /// <summary>
    /// Indicates priority order of audio source.
    /// Based on current spatial and playback parameters.
    /// Lower values are higher priority.
    /// </summary>
    uint Priority { get; }

    /// <summary>
    /// Indicates ideal volume of audio source.
    /// </summary>
    float Volume { get; }

    // --- Lifecycle:

    /// <summary>
    /// Supplies source parameters to implement playback strategy.
    /// </summary>
    void SetSourceParameters(SourceData playbackData);

    /// <summary>
    /// Retrieves source parameters to implement playback strategy.
    /// </summary>
    SourceData GetSourceParameters();

    // --- External Behaviors:

    /// <summary>
    /// Requests active source to start or resume playback.
    /// Only applicable while stopped or paused.
    /// </summary>
    void Play();

    /// <summary>
    /// Requests active source to interrupt playback.
    /// Optional parameter resumes playback when false.
    /// Only applicable while playing or paused.
    /// </summary>
    void Pause(bool interrupt = true);

    /// <summary>
    /// Requests to deactivate audio source and recycle playback channel.
    /// </summary>
    void Stop();

    /// <summary>
    /// Requests active source to silence playback.
    /// Optional parameter restores original volume when false.
    /// </summary>
    void Mute(bool silence = true);

    /// <summary>
    /// Requests active source to change volume components.
    /// An ideal setting of -1 keeps previous setting unchanged.
    /// Other given values are clamped within [0…1] valid range.
    /// </summary>
    void ChangeVolume(float mixedLevel, float idealSetting = -1f);

    /// <summary>
    /// Requests active source to reposition audio origin.
    /// </summary>
    void ChangePosition(GridPoint position);

    /// <summary>
    /// Updates spatialization effects and priority score.
    /// Return indicates whether within listener audible range.
    /// </summary>
    bool UpdateSpatialization();

    }


}
