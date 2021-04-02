using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Game.Lib.Space;
using Game.Lib.Media;
using Game.Api.Pooling;

namespace Game.Api.Playback
{

/// <summary>
/// Provides base functionality for mixing playback sources.
/// </summary>
public abstract class AudioMixer
    {
    // --- Inner Members:

    /// <summary>
    /// Defines exclusive volume dampening effects.
    /// </summary>
    protected enum DuckingEffect : byte
        {
        none = 0,       // default, volume controlled via setting only

        AutoDucking,    // gradual process in reaction to ducking threshold
        ManualDucking,  // immediate effect in reaction to ducking switch
        }

    /// <summary>
    /// Defines soundscape activity levels for auto ducking.
    /// </summary>
    private enum ActivityLevel : byte
        {
        calm = 0,   // average noise & below
        busy = 1    // many sources & above
        }

    // --- Fields:

        // Source control:
        
    private ReusablePool<AudioChannel> idleChannels;    // idle audio sources
    private List<ActiveSource> openChannels;            // active audio sources, live and mock

    protected event Action playbackFinishEvent;         // event triggered on natural playback finish

        // Strategy control:                            // when live, assign new as virtual, periodically sort & activate up to maximum number
                                                        // when virtual, transform any remaining live and stop new activations

    private bool virtualized;                           // flag for current playback/source strategy

    private byte maxChannels;                           // designated limit of live channels for this mixer
    
        // Volume control:

    private ActivityLevel soundscape;           // current soundscape activity level
    private IEnumerator duckingProcess;         // tracks gradual adjustment-factor change-process

    private float volumeSetting;                // [0…1] baseline mixer volume
    private float volumeModifier;               // adjustment factor for dampening effects
    protected float volumeLevel;                // adjusted mixer volume, modulates ideal volume of all sources

    private const float MOD_DUCK = 0.4f;        // dampening factor for manual ducking on out-of-focus contexts
    private const float MOD_BUSY = 0.7f;        // dampening factor for auto ducking on noisy soundscapes
    private const float MOD_FULL = 1.0f;        // normalized volume modifier

    private const float VOLUME_FLOOR = 0.05f;   // minimum volume / mute threshold

    // --- Properties:

    /// <summary>
    /// Activates use of volume dampening effects on mixer.
    /// Manual enables immediate effect in reaction to Ducking Switch.
    /// Auto enables gradual process in reaction to Ducking Threshold.
    /// None disables all volume dampening effects.
    /// </summary>
    protected DuckingEffect Ducking { get; set; }

    /// <summary>
    /// Determines number of sources for busy threshold used to dampen soundscape noise levels.
    /// Activity above threshold causes gradual ducking until below threshold again.
    /// No effect unless Auto ducking is enabled.
    /// </summary>
    protected byte DuckingThreshold { get; set; }

    /// <summary>
    /// Determines playback volume as dampened or full.
    /// No effect unless Manual ducking is enabled and setting changes value.
    /// </summary>
    protected bool DuckingSwitch
        {
        get { return(volumeModifier == MOD_DUCK); }

        set {
            if (Ducking == DuckingEffect.ManualDucking)
               {
               if ((value && volumeModifier != MOD_DUCK) || (!value && volumeModifier != MOD_FULL)) 
                  { 
                  manualDuck(value); 
                  }
               }
            }
        }

    /// <summary>
    /// Flags mixer as virtual or live.
    /// Adjusts default playback and source strategies.
    /// No effect unless setting changes value.
    /// </summary>
    public bool Virtual
        {
        get { return(virtualized); }

        set {
            if (value != virtualized)
               {
               if (value) { forceVirtualSources(); }
               else { restoreLiveSources(); }
               }
            }
        }

    /// <summary>
    /// Defines mixer volume setting. 
    /// Applies to all mixed sources.
    /// Values are clamped within [0…1] valid range.
    /// </summary>
    public float Volume
        {
        get { return(volumeSetting); }
        set { changeMixerVolume(value); }
        }

    // --- Lifecycle:

    /// <summary>
    /// Initial state constructor.
    /// </summary>
    public AudioMixer(ReusablePool<AudioChannel> commonAudioPool, byte reservedChannels)
        {
        // setup audio channels

        idleChannels = commonAudioPool;

        openChannels = new List<ActiveSource>();
        maxChannels = reservedChannels;

        // setup initial allocation strategy

        virtualized = false;

        // setup initial activity level & volume

        duckingProcess = null;

        soundscape = ActivityLevel.calm;
        volumeModifier = MOD_FULL;

        changeMixerVolume(1f);

        // setup default effects - disabled

        Ducking = DuckingEffect.none;
        DuckingSwitch = false;
        DuckingThreshold = 0;
        }

    // --- Frame Update:

    /// <summary>
    /// Frame update loop with basic source management cycle.
    /// </summary>
    public void UpdateActivity()
        {
        cleanActive();

        turnLive();
        }

    /// <summary>
    /// Updates source spatialization in sync with audio listener refresh cycle.
    /// </summary>
    public void UpdateSpatial()
        {
        byte activity = updateActive();

        if (Ducking == DuckingEffect.AutoDucking) 
           { 
           autoDuck(activity >= DuckingThreshold); 
           }

        sortActive();
        }

    // --- External Behaviors:

    #region Playback Control

    /// <summary>
    /// Begins audio playback, with fixed/zone spatialization options.
    /// Returns unique identifier for audio source.
    /// </summary>
    protected IdentityData Play(AudioData audio, AudioPlayback.Range.RZone zone, float idealVolume, bool loop)
        {
        GridPoint bearing = GridPoint.Zero;
        AudioPlayback.Range range = new AudioPlayback.Range(zone);

        return(Play(audio,bearing,range,idealVolume,loop));
        }

    /// <summary>                                       
    /// Begins audio playback, with dynamic/grid spatialization options.
    /// Returns unique identifier for audio source.
    /// </summary>
    protected IdentityData Play(AudioData audio, GridPoint bearing, AudioPlayback.Range range, float idealVolume, bool loop) 
        {

        return(makeSource(audio,bearing,range,idealVolume,loop));
        }

    /// <summary>
    /// Stops playback associated with given audio source.
    /// Return indicates success unless match not found.
    /// </summary>
    public bool Stop(IdentityData audio)
        {
        bool found = closeSource(findSource(audio));

        return(found);
        }

    /// <summary>
    /// Stops playback of all active audio sources.
    /// </summary>
    public void StopAll()
        {

        for(int index = openChannels.Count-1; index >= 0; index--) { closeSource(index); }
        }

    /// <summary> 
    /// Pauses or resumes playback of all active audio sources.
    /// </summary>
    public void PauseAll(bool pause)
        {

        for(int index = 0; index < openChannels.Count; index++) { openChannels[index].Controller.Pause(pause); }           
        }

    /// <summary>
    /// Indicates ideal volume for matching audio source. 
    /// Use only when a unique instance of the sound is expected.
    /// Returns value within [0…1] range, or negative if match not found.
    /// </summary>
    public float QueryVolume(IdentityData audio)   
        {

        return(findAudioVolume(audio));
        }

    /// <summary>
    /// Adjusts ideal volume for matching audio source.
    /// Given value should be within [0…1] valid range.
    /// Return indicates success unless match not found.
    /// </summary>
    public bool ChangeVolume(IdentityData audio, float idealVolume)
        {

        return(changeAudioVolume(audio,idealVolume*volumeSetting,idealVolume));
        }

    #endregion

    // --- Internal Procedures:

    #region Channel strategy

    /// <summary>
    /// Enables virtualization flag and ensures no live sources.
    /// </summary>
    private void forceVirtualSources()
        {
        virtualized = true;

        turnVirtual();
        }

    /// <summary>
    /// Disables virtualization flag and ensures maximum live sources. 
    /// </summary>
    private void restoreLiveSources()
        {
        virtualized = false;

        turnLive();
        }

    #endregion

    #region Channel management

    /// <summary>
    /// Ensures channels are released after playback is finished.
    /// </summary>
    private void cleanActive()
        {
        for (int index = openChannels.Count-1; index >= 0; index--)                 // iterate backwards to maintain index on removal
            {
            if (!openChannels[index].Controller.isBusy) { closeSource(index); }     // remove when idle
            }
        }

    /// <summary>
    /// Ensures spatialization parameters are current.
    /// Return indicates audible activity within listener range.
    /// </summary>
    private byte updateActive()
        {
        byte audibleSources = 0;

        for (int index = 0; index < openChannels.Count; index++)
            {
            bool audible = openChannels[index].Controller.UpdateSpatialization();   // refresh spatial attenuation & priority score

            if (audible && openChannels[index].Strategy == AudioPlayback.Strategy.live) { audibleSources++; }   // audible & live
            }

        return(audibleSources);
        }

    /// <summary>
    /// Ensures waitlisted sources are queued according to priority.
    /// </summary>
    private void sortActive()
        {
        int waitlistHead = 0;
        int waitlistTail = 0;

        // find waitlist bounds - iterate backwards to optimize for case where non-live sources are uncommon

        for (int openIndex = openChannels.Count-1; openIndex >= 0; openIndex--)
            {
            if (waitlistTail == 0 && openChannels[openIndex].Strategy == AudioPlayback.Strategy.mock) { waitlistTail = openIndex; }

            if (openChannels[openIndex].Strategy == AudioPlayback.Strategy.live) { waitlistHead = openIndex+1; break; }
            }

        // determine if waitlist exists and requires sorting

        int sortables = waitlistTail-waitlistHead+1;
        bool sortable = (waitlistTail != 0 && sortables > 1) ? (true):(false); 

        // sort in place according to source priority

        if (sortable) 
           {
           IComparer<ActiveSource> comparer = Comparer<ActiveSource>.Create(AudioPlayback.OrderPriority);

           openChannels.Sort(waitlistHead,sortables,comparer); 
           }
        }

    /// <summary>
    /// Ensures maximum number of active sources are live.
    /// </summary>
    private void turnLive()
        {
        if (!virtualized)
           {
           byte liveChannels = 0;

           for (int index = 0; index < openChannels.Count; index++)
               {
               openChannels[index].EnforceStrategy(AudioPlayback.Strategy.live);

               liveChannels++;

               if (liveChannels == maxChannels) { break; }
               }
           }
        }

    /// <summary>
    /// Ensures any active sources are virtualized.
    /// </summary>
    private void turnVirtual()
        {
        if (virtualized)
           {
           for (int index = 0; index < openChannels.Count; index++)
               {

               openChannels[index].EnforceStrategy(AudioPlayback.Strategy.mock);
               }
           }
        }

    #endregion

    #region Source playback

    /// <summary>
    /// Creates active source on virtual channel and signals playback start.
    /// </summary>
    private IdentityData makeSource(AudioData audio, GridPoint bearing, AudioPlayback.Range range, float idealVolume, bool loop)
        {
        // prepare channel

        IdentityData token = new IdentityData(audio);
        SourceData data = new SourceData(token,bearing,range,idealVolume,loop);

        ActiveSource source = new ActiveSource(AudioPlayback.Strategy.mock,data);

        // track channel

        openChannels.Add(source);

        // play audio

        source.Controller.ChangeVolume(idealVolume*volumeLevel);     // adjust volume with current mixed level, keep base setting
        source.Controller.Play();

        return(token);
        }

    /// <summary>
    /// Searches active sources for given audio and returns associated index.
    /// Returns negative when audio is not found.
    /// </summary>
    private int findSource(IdentityData audio)
        {
        int searchResult = -1;

        for(int index = 0; index < openChannels.Count; index++)
           {
           if (openChannels[index].Controller.AudioID.Match(audio)) 
              {
              searchResult = index;
              break; 
              }
           }

        return(searchResult);
        }

    /// <summary>
    /// Deactivates and recycles matching source/channel.
    /// Return indicates success unless match not found.
    /// </summary>
    private bool closeSource(int liveIndex)
        {
        if (liveIndex != -1)
           {
           bool ended = !openChannels[liveIndex].Controller.isBusy;
           openChannels[liveIndex].Controller.Stop();

           openChannels.RemoveAt(liveIndex);

           // if ended naturally and event has listener, signal handler action

           if (ended && playbackFinishEvent != null) { playbackFinishEvent.Invoke(); }

           return(true);
           }

        else return(false);
        }

    #endregion

    #region Soundscape volume

    /// <summary>
    /// Applies or suspends volume dampening with immediate effect.
    /// </summary>
    private void manualDuck(bool dampen)           
        {                                               
        // OLD: overrides auto ducking
        // FIX: exclusive ducking modes, never active simultaneously

        volumeModifier = (dampen) ? (MOD_DUCK):(MOD_FULL);      

        changeMixerVolume(volumeSetting);                       // apply modifier with unchanged setting
        }

    /// <summary>
    /// Begins process to dampen noisy soundscape or restore calm volume level.
    /// No effect unless activity level changed.
    /// </summary>
    private void autoDuck(bool busy)        // TODO stack dampening factor on threshold multiples?
        {
        if (busy && soundscape != ActivityLevel.busy)
           {
           soundscape = ActivityLevel.busy;
           attenuateSoundscape();
           }

        if (!busy && soundscape != ActivityLevel.calm)
           {
           soundscape = ActivityLevel.calm;
           normalizeSoundscape();
           }
        }

    /// <summary>
    /// Interrupts soundscape activity level adjustment, if ongoing.
    /// </summary>
    private void interruptChange()
        {
        if (duckingProcess != null)
           {
           MasterController.Audio.StopCoroutine(duckingProcess);

           duckingProcess = null;
           }
        }

    /// <summary>
    /// Performs gradient change of soundscape adjustment factor. 
    /// </summary>
    private IEnumerator performChange(bool increase)
        {
            /*                           value       delta
             *  1 2 3 4 5 6 7 8 9 0     pos = 7     max = 3
             *  #.#.#.#.#.#.#.*.*.*     end = 7     ini = 0 
             *              ^                       end = 0
             *              0 1 2 3                 dif = 0
             *                                      
             *  1 2 3 4 5 6 7 8 9 0     pos = 9     max = 3
             *  #.#.#.#.#.#.#.#.#.*     end = 7     ini = 2 
             *              ^                       end = 0
             *              0 1 2 3                 dif =+2
             *
             *  1 2 3 4 5 6 7 8 9 0     pos = 7     max = 3
             *  #.#.#.#.#.#.#.*.*.*     end = 10    ini = 0
             *                    ^                 end = 3
             *              0 1 2 3                 dif =-3
             */

        float targetValue = (increase) ? (MOD_FULL):(MOD_BUSY);     // target value for adjustment factor

        float maxDelta = MOD_FULL - MOD_BUSY;                       // maximum value of delta segment
        float initialDelta = volumeModifier - MOD_BUSY;             // origin value of delta segment
        float finalDelta = targetValue - MOD_BUSY;                  // endpoint value for delta segment
        float deltaDelta = initialDelta - finalDelta;               // displacement between initial origin and target endpoint

        float maxPeriod = 1f;                                       // maximum time of change process
        float changePeriod = (initialDelta/maxDelta)*maxPeriod;     // remaining time after accounting for initial offset

        float elapsedTime = 0;                                      // scaled time counter

        while (elapsedTime < changePeriod)                          // perform change && skip divide-by-zero error on no-change
              {
              yield return null;                                                                    // pause coroutine until next update frame

              elapsedTime += Time.deltaTime;                                                        // reflect in-world timescaling
              if (elapsedTime > changePeriod) elapsedTime = changePeriod;                           // limit time overflow

              volumeModifier = MOD_BUSY + (initialDelta - (elapsedTime/changePeriod)*deltaDelta);   // update adjustment factor

              changeMixerVolume(volumeSetting);                                                     // apply adjustment with unchanged baseline
              }

        duckingProcess = null;                                   // mark performance complete
        }

    /// <summary>
    /// Begins lowering the volume adjustment factor when soundscape becomes busy.
    /// </summary>
    private void attenuateSoundscape()
        {
        interruptChange();

        duckingProcess = performChange(false);

        MasterController.Audio.StartCoroutine(duckingProcess);
        }

    /// <summary>
    /// Begins raising the volume adjustment factor when soundscape becomes calm.
    /// </summary>
    private void normalizeSoundscape()
        {
        interruptChange();

        duckingProcess = performChange(true);

        MasterController.Audio.StartCoroutine(duckingProcess);
        }

    #endregion

    #region Channel volume

        /* Volume Components:
         * 
         * mixer setting: from audio settings, base value from general & mixer values
         * mixer modifier: from soundscape activity & dampening, modifier for mixer
         * mixer level: from setting & modifier, modulates all mixed sources
         * 
         * ideal setting: from playback calls, base value for individual source
         * mixed level: from ideal base & mixer level, background adjusted value for source
         * real volume: from mixed level & listener spatial attenuation, actual value for channel playback
         */

    /// <summary>
    /// Changes baseline volume setting of mixer and updates mixed level of active sources.
    /// </summary>
    private void changeMixerVolume(float mixerSetting)
        {
        volumeSetting = Mathf.Clamp01(mixerSetting);            // set new baseline mixer volume
        volumeLevel = volumeSetting*volumeModifier;             // update effective mixer volume   

        for(int index = openChannels.Count-1; index >= 0; index--) 
           {
           float sourceVolume = openChannels[index].Controller.Volume * volumeLevel;

           changeSourceVolume(index,sourceVolume);              // apply new mixed level without changing base setting
           }
        }

    /// <summary>
    /// Finds ideal volume of given audio currently playing.
    /// Returns negative value if given audio not found.
    /// </summary>
    private float findAudioVolume(IdentityData audio)
        {
        float idealSetting = -1;

        int audioIndex = findSource(audio);

        if (audioIndex != -1) { idealSetting = openChannels[audioIndex].Controller.Volume; }

        return(idealSetting);
        }

    /// <summary>
    /// Changes volume components of given audio.
    /// No effect if given audio is not found.
    /// </summary>
    private bool changeAudioVolume(IdentityData audio, float mixedLevel, float idealSetting)
        {
        int index = findSource(audio);

        if (index != -1) 
           { 
           changeSourceVolume(index,mixedLevel,idealSetting); 
           return(true); 
           }

        else return(false);
        }

    /// <summary>
    /// Changes mixed volume of active source.
    /// No effect if given index is not within bounds.
    /// </summary>
    private void changeSourceVolume(int liveIndex, float mixedLevel, float idealSetting = -1f)
        {
        if (liveIndex >= 0 && liveIndex < openChannels.Count) 
           {
           PlaybackController source = openChannels[liveIndex].Controller;

           bool isSilence = source.isMute;
           bool doSilence = mixedLevel <= VOLUME_FLOOR;

           source.ChangeVolume(mixedLevel,idealSetting);            // apply mixed level, store ideal setting

           if (doSilence != isSilence) { source.Mute(doSilence); }  // silence/unsilence channel
           }
        }

    #endregion

    }


}
