using System;

using Game.Lib.Space;
using Game.Lib.Media;

namespace Game.Api.Playback
{

/// <summary>
/// Implements playback strategy for mock audio sources.
/// Mock sources do not need actual sources or channels.
/// </summary>
public sealed class MockSource : PlaybackController
    {
    // --- Fields:

        // Handover parameters:

    private AudioPlayback.Range range;  // attenuation settings
    private GridPoint bearing;          // position setting
    private float volume;               // volume setting
    private bool loop;                  // repeat setting

        // Playstate tracking:

    private bool playFlag;              // indicates requested playback
    private bool pauseFlag;             // indicates requested interruption

        // Playtime tracking:

    private double maxInterval;         // marks audio duration / max playback time.

    private double playInterval;        // accumulates elapsed playtime since start
    private double gapInterval;         // accumulates elapsed interruption since pause

    private long playTick;              // marks tick-instant at play, negative when none
    private long gapTick;               // marks tick-instant instant at pause, negative when none

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
    /// Indicates whether playback would be ongoing.
    /// </summary>
    public bool isBusy { get { return(queryPlayback()); } }

    /// <summary>
    /// Indicates whether playback would be silenced.
    /// </summary>
    public bool isMute { get; private set; }

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

        // playtime discovery - quick load/unload channel clip

        FrameworkData config = MasterController.Audio.GetFrameworkData();
        AudioChannel channel = config.AudioPool.RequestObject();

        channel.SetStream(AudioLibrary.GetClip(AudioID.ContentID));
        maxInterval = channel.Duration;

        config.AudioPool.ReleaseObject(channel);

        // setup initial state

        resetPartial(playbackData.Offset,playbackData.Paused);

        Mute(playbackData.Muted);

        UpdateSpatialization();
        }

    /// <summary>
    /// Retrieves source parameters reflecting the current playback state.
    /// </summary>
    public SourceData GetSourceParameters()
        {
        // build handover data

        SourceData data = new SourceData(AudioID,bearing,range,volume,loop,measurePlaytime(),pauseFlag,isMute);

        return(data);
        }

    // --- External Behaviors:

    /// <summary>
    /// Simulates beginning or resuming playback.
    /// </summary>
    public void Play()
        {
        if (playFlag && pauseFlag) { Pause(false); }    // if playing but paused, resume from midpoint

        if (!playFlag)                                  // if not double-play, play & track
           {
           playTick = DateTime.Now.Ticks;               // track play

           playFlag = true;                             // flag state
           }
        }

    /// <summary>
    /// Simulates interrupting/resuming ongoing playback.
    /// </summary>
    public void Pause(bool interrupt = true)
        {
        if (interrupt && playFlag)      // ignore pause unless playing
           {
           updatePlay();

           gapTick = DateTime.Now.Ticks;

           pauseFlag = interrupt;
           }

        if (!interrupt && pauseFlag)    // ignore resume unless paused
           {
           updateGap();                     // tally gap

           playTick = DateTime.Now.Ticks;   // track play

           pauseFlag = interrupt;           // flag state
           }
        }

    /// <summary>
    /// Simulates stopping playback and resetting stream.
    /// </summary>
    public void Stop()
        {

        resetOrigin();
        }

    /// <summary>
    /// Simulates silencing/restoring playback volume.
    /// </summary>
    public void Mute(bool silence = true)
        {

        isMute = silence;
        }

    /// <summary>
    /// Simulates changing volume components of playback source.
    /// An ideal setting of -1 keeps previous setting unchanged.
    /// Other given values are clamped within [0…1] valid range.
    /// </summary>
    public void ChangeVolume(float mixedLevel, float idealSetting)
        {

        if (idealSetting != -1) { volume = (idealSetting < 0f) ? (0f) : ((idealSetting > 1f) ? (1f):(idealSetting)); }
        }

    /// <summary>
    /// Simulates changing origin coordinates of playback source.
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
        // effective range

        float rangeAttenuation = AudioPlayback.CalculateAttenuator(range,bearing);
        bool audibleRange = (volume * rangeAttenuation > 0);

        // recalculate priority
        
        bool silent = (isMute||pauseFlag);

        Priority = AudioPlayback.AssignPriority(range,audibleRange,silent);

        return(audibleRange);
        }

    // --- Internal Procedures:

    /// <summary>
    /// Prepares initial state and playback tracking.
    /// </summary>
    private void resetPartial(double offset, bool paused)
        {
        if (offset != 0)                    // partial playback settings
           {
           playInterval = offset;
           gapInterval = 0;

           playFlag = true;

           if (paused)
              {
              playTick = -1;
              gapTick = -1;

              Pause(paused);
              }

           else {
                playTick = DateTime.Now.Ticks;
                gapTick = -1;

                pauseFlag = false;
                }
           }

        else { resetOrigin(); }             // stopped at origin settings
        }

    /// <summary>
    /// Prepares zero-state and playback tracking.
    /// </summary>
    private void resetOrigin()
        {
        // playtime tracking

        playInterval = 0;
        gapInterval = 0;

        playTick = -1;
        gapTick = -1;

        // playstate tracking
        
        playFlag = false;
        pauseFlag = false;
        }

    /// <summary>
    /// Indicates whether playback would be ongoing based on play state, play time, elapsed time, and interruption/repetition factors.
    /// </summary>
    private bool queryPlayback()
        {
        // ongoing IF play requested AND looping, or paused, or playtime incomplete

        bool ongoing = (playFlag && (loop || pauseFlag || (measurePlaytime() <= maxInterval)));

        // measure calculations not evaluated unless necessary

        return(ongoing);
        }

    /// <summary>
    /// Measures current simulated playback time accounting for interruptions.
    /// </summary>
    private double measurePlaytime()
        {
        double currentOffset = 0;                                                           // null case: at origin

        if (playTick != -1)                                                                 // some case: midway point
           {
           updateGap();
           updatePlay();
           
           currentOffset = playInterval - gapInterval;
           }

        while (loop && currentOffset >= maxInterval) { currentOffset -= maxInterval; }     // more case: loop reset

        return(currentOffset);
        }

    /// <summary>
    /// Ensures total play interval is updated.
    /// Converts tick delta to time interval and resets tick marker.
    /// </summary>
    private void updatePlay()
        {
        if (playTick != -1)
           {
           playInterval += (DateTime.Now.Ticks-playTick)/TimeSpan.TicksPerSecond;
           playTick = -1;
           }
        }

    /// <summary>
    /// Ensures total gap interval is updated.
    /// Converts tick delta to time interval and resets tick marker.
    /// </summary>
    private void updateGap()
        {
        if (gapTick != -1)
           {
           gapInterval += (DateTime.Now.Ticks-gapTick)/TimeSpan.TicksPerSecond;
           gapTick = -1;
           }
        }

    }


}
