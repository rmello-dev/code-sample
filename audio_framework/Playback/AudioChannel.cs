using UnityEngine;

using Game.Lib.Space;
using Game.Api.Pooling;

namespace Game.Api.Playback
{

/// <summary>
/// Reusable audio source playback channel.
/// </summary>
public sealed class AudioChannel : ReusableEntity
    {
    // --- Fields:

    private readonly AudioSource source;        // engine component

    private double sourceLength;                // duration of source clip
    private float baseVolume;                   // non-spatial volume
    private bool pauseFlag;                     // pause status
    private bool muteFlag;                      // mute status

    private float volumeAttenuation;            // latest spatial multiplier for volume

    // --- Properties:

    /// <summary>
    /// Determines precise playback position of loaded stream.
    /// Loops back when set above clip duration.
    /// Negative when stream is unloaded.
    /// </summary>
    public double Progress
        {
        get { return(getProgress()); }
        set { setProgress(value); }
        }

    /// <summary>
    /// Provides precise playback length when source stream is set, negative otherwise.
    /// </summary>
    public double Duration { get { return(sourceLength); } }

    /// <summary>
    /// Flag indicating current playback activity - including pause interval.
    /// </summary>
    public bool isBusy { get { return(pauseFlag || source.isPlaying); } }

    /// <summary>
    /// Flag indicating if playback is requested but interrupted.
    /// </summary>
    public bool isPaused { get { return(pauseFlag); } }

    /// <summary>
    /// Flag indicating channel has been temporarily muted. 
    /// </summary>
    public bool isMute { get { return(muteFlag); } }

    // --- Initialization:

    /// <summary>
    /// Base channel initialization.
    /// </summary>
    public AudioChannel()
        {
        // initial config

        baseObject.name = ResourceLabels.std_sfxSource;
        baseObject.layer = SceneAdapter.GetLayerIndex(SceneAdapter.LayerTag.Logic);

        source = baseObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.dopplerLevel = 0.0f;
        source.spatialBlend = 0.0f;
        source.spatialize = false;

        // initial state

        SetWaitState();
        }

    /// <summary>
    /// Baseline inactive state.
    /// Basic clean-up only, does not reset to default behavior.
    /// Audio channels require full re-assignment to prevent leaking state.
    /// </summary>
    public override void SetWaitState()
        {
        SetStream(string.Empty);
        Pause(false);
        Mute(false);

        baseObject.transform.localPosition = Vector3.zero;
        volumeAttenuation = 1f;

        base.SetWaitState(); 
        }

    // --- Unique Behaviors:

    /// <summary>
    /// Executes playback of channel stream.
    /// Optional parameter applies small pitch variance when true.
    /// </summary>
    public void Play(bool variance = false)
        {
        float pitch = 1.0f;

        if (variance)   // pitch variance to avoid repetitiveness
           {
           float modifier = MasterController.Random.Rand_Normal()/4f;
           bool signal = MasterController.Random.Rand_Coin();

           pitch = (signal) ? (pitch+modifier):(pitch-modifier);  // +- 0.25
           }
        
        source.pitch = pitch;
        source.Play();
        }

    /// <summary>
    /// Stops playback and resets channel stream.
    /// </summary>
    public void Stop() { source.Stop(); Progress = 0; }

    /// <summary>
    /// Interrupts ongoing playback without resetting channel stream.
    /// Optional parameter resumes playback when false.
    /// </summary>
    public void Pause(bool interrupt = true)
        {
        if (source.isPlaying && interrupt) { source.Pause(); pauseFlag = interrupt; }   // ignore pause unless playing
        if (pauseFlag && !interrupt) { source.UnPause(); pauseFlag = interrupt; }       // ignore resume unless paused
        }

    /// <summary>
    /// Silences audio channel while preserving volume.
    /// Optional parameter restores original volume when false.
    /// </summary>
    public void Mute(bool silence = true)
        {

        source.mute = muteFlag = silence;
        }

    /// <summary>
    /// Changes audio channel content stream.
    /// </summary>
    public void SetStream(string resourcePath)
        {
        if (resourcePath == string.Empty) { source.clip = null; sourceLength = -1; }
        else {
             source.clip = Resources.Load<AudioClip>(resourcePath);

             sourceLength = source.clip.samples/source.clip.frequency;
             }
        }

    /// <summary>
    /// Changes audio channel object label.
    /// </summary>
    public void SetLabel(AudioPlayback.Group label)
        {
        switch (label)
               {
               case(AudioPlayback.Group.env): baseObject.name = ResourceLabels.std_sfxSource; break;
               case(AudioPlayback.Group.bgm): baseObject.name = ResourceLabels.std_bgmSource; break;
               case(AudioPlayback.Group.aux): baseObject.name = ResourceLabels.std_auxSource; break;
               }
        }

    /// <summary>
    /// Changes audio channel baseline volume at origin.
    /// Given value is clamped within [0…1] valid range.
    /// </summary>
    public void SetVolume(float volume) 
        {
        baseVolume = Mathf.Clamp01(volume);

        source.volume = baseVolume * volumeAttenuation;
        }

    /// <summary>
    /// Changes audio channel playback mode - single or repeat.
    /// </summary>
    public void SetPlayback(bool loop) { source.loop = loop; }

    /// <summary>
    /// Updates spatialization effects and indicates audible playback range.
    /// </summary>
    public bool UpdateSpatialization(AudioPlayback.Range sourceRange, GridPoint sourceBearing)
        {
        volumeAttenuation = AudioPlayback.CalculateAttenuator(sourceRange,sourceBearing);

        SetVolume(baseVolume);

        bool effectiveRange = (source.volume > 0);

                // TODO reposition AudioSource relative to listener to maintain stereo orientation?

        return(effectiveRange);
        }

    // --- Internal Procedures:

    /// <summary>
    /// Gets playback position.
    /// Finds elapsed time from sample index.
    /// </summary>
    private double getProgress()
        {
        double currentTime = -1;

        if (sourceLength != -1) { currentTime = source.timeSamples/source.clip.frequency; }

        return(currentTime);
        }

    /// <summary>
    /// Sets playback position.
    /// Discovers sample index from elapsed time.
    /// </summary>
    private void setProgress(double time)
        {
        if (time >= 0 && sourceLength != -1)                            // needs positive value && loaded stream duration
           {
           while (time > Duration) { time -= Duration; }                // reduces values over duration with loopback

           int currentSample = (int) (time * source.clip.frequency);    // find sample at time

           source.timeSamples = currentSample;
           }
        }

    }


}
