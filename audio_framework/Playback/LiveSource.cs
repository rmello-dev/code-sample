
using Game.Lib.Space;
using Game.Lib.Media;
using Game.Api.Pooling;

namespace Game.Api.Playback
{

/// <summary>
/// Implements playback strategy for live audio sources.
/// Live sources require a free audio channel.
/// </summary>
public sealed class LiveSource : PlaybackController
    {
    // --- Fields:

    private Proxy<AudioChannel> proxy;  // reusable channel proxy
    private AudioChannel channel;       // reference to actual channel

        // Handover parameters:

    private AudioPlayback.Range range;  // attenuation settings
    private GridPoint bearing;          // position setting
    private float volume;               // volume setting
    private bool loop;                  // repeat setting

    private bool pauseFlag;             // track pause state when channel is unloaded
    private bool muteFlag;              // track mute state when channel is unloaded
    private double offset;              // track playback position when channel is unloaded

    // --- Properties:

    /// <summary>
    /// Identifies audio content and source object.
    /// </summary>
    public IdentityData AudioID { get; private set; }

    /// <summary>
    /// Indicates priority order of audio source.
    /// Based on current spatial and playback parameters.
    /// Lower values are higher priority.
    /// </summary>
    public uint Priority { get; private set; }

    /// <summary>
    /// Indicates whether playback is ongoing.
    /// </summary>
    public bool isBusy
        {
        get {
            if (!proxy.Peek()) { return(false); }
            else { return(channel.isBusy); }
            }
        }

    /// <summary>
    /// Indicates whether playback is silenced.
    /// </summary>
    public bool isMute
        {
        get {
            if (!proxy.Peek()) { return(muteFlag); }
            else { return(channel.isMute); }
            }
        }

    /// <summary>
    /// Indicates ideal volume of audio source.
    /// </summary>
    public float Volume { get { return(volume); } }

    // --- Lifecycle:

    /// <summary>
    /// Supplies source parameters to resume playback state.
    /// </summary>
    public void SetSourceParameters(SourceData playbackData)
        {
        // set content

        AudioID = playbackData.AudioID;

        // store parameters 

        range = playbackData.Range;
        bearing = playbackData.Bearing;
        volume = playbackData.Volume;
        loop = playbackData.Loop;

        // configure channel

        FrameworkData config = MasterController.Audio.GetFrameworkData();

        proxy = new Proxy<AudioChannel>(config.AudioPool);
        channel = proxy.Entity;

        channel.SetHierarchy(config.AudioRoot);

        channel.SetLabel(AudioID.ContentID.Type);
        channel.SetVolume(volume);
        channel.SetPlayback(loop);
        channel.SetStream(AudioLibrary.GetClip(AudioID.ContentID));

        // initial state

        restorePlayback(playbackData.Offset,playbackData.Paused);

        Mute(playbackData.Muted);

        UpdateSpatialization();
        }

    /// <summary>
    /// Retrieves source parameters reflecting the current playback state.
    /// </summary>
    public SourceData GetSourceParameters()
        {
        SourceData data = new SourceData(AudioID,bearing,range,volume,loop,measureOffset(),pauseFlag,isMute);

        return(data);
        }

    // --- External Behaviors:

    /// <summary>
    /// Begins or resumes playback.
    /// </summary>
    public void Play()
        {
        if (pauseFlag && isBusy) { Pause(false); }  // if play when playing but paused, unpause

        if (!isBusy)                                // if stopped, play
           {
           // sfx/aux: pitch adjustment to avoid repetitiveness

           bool variance = (AudioID.ContentID.Type != AudioPlayback.Group.bgm);

           // play from origin/partial

           channel.Progress = offset;
           channel.Play(variance);
           }
        }

    /// <summary>
    /// Interrupts or resumes ongoing playback.
    /// </summary>
    public void Pause(bool interrupt = true)
        {
        channel.Pause(interrupt);

        pauseFlag = channel.isPaused;
        }

    /// <summary>
    /// Stops playback and releases audio source/channel. 
    /// </summary>
    public void Stop()
        {
        channel.Stop();     // stop playback, reset stream

        proxy.Unload();     // recycle source/channel

        resetPlayback();
        }

    /// <summary>
    /// Silences or restores playback volume.
    /// </summary>
    public void Mute(bool silence = true)
        {
        channel.Mute(silence);

        muteFlag = channel.isMute;
        }

    /// <summary>
    /// Changes volume components of playback source.
    /// An ideal setting of -1 keeps previous setting unchanged.
    /// Other given values are clamped within [0…1] valid range.
    /// </summary>
    public void ChangeVolume(float mixedLevel, float idealSetting)
        {
        channel.SetVolume(mixedLevel);

        if (idealSetting != -1) { volume = (idealSetting < 0f) ? (0f) : ((idealSetting > 1f) ? (1f):(idealSetting)); }
        }

    /// <summary>
    /// Changes origin coordinates of playback source.
    /// </summary>
    public void ChangePosition(GridPoint newPosition)
        {

        bearing = newPosition;
        }

    /// <summary>
    /// Recalculates spatialization effects and playback priority.
    /// Return indicates whether within listener audible range.
    /// </summary>
    public bool UpdateSpatialization()
        {
        // update spatial effects

        bool effectiveRange = channel.UpdateSpatialization(range,bearing);

        // update priority

        Priority = AudioPlayback.AssignPriority(range,effectiveRange,(isMute || pauseFlag));

        return(effectiveRange);
        }

    // --- Internal Procedures:

    /// <summary>
    /// Prepares initial playback state with origin or partial settings.
    /// </summary>
    private void restorePlayback(double playOffset, bool pause)
        {
        //if (playOffset != 0)        // partial playback settings      // TODO how to differentiate between stop-reset 0 & start-origin 0?
           //{
           offset = playOffset;

           Play();
           Pause(pause);
           //}

        //else { resetPlayback(); }   // stopped at origin settings
        }

    /// <summary>
    /// Resets playback state to origin settings.
    /// </summary>
    private void resetPlayback()
        {
        offset = 0;

        pauseFlag = false;
        }

    /// <summary>
    /// Determines current playback position offset.
    /// </summary>
    private double measureOffset()
        {
        double progress = 0;

        if (proxy.Peek()) { progress = channel.Progress; }

        return(progress);
        }

    }


}
