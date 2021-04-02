
using System.Collections;

using UnityEngine;

using Game.Api.Playback;
using Game.Api.Pooling;
using Game.Lib.Media;

namespace Game.Api
{

/// <summary>
/// Background music manager.
/// Controller of BGM mixing.
/// </summary>
public sealed class MusicManager : AudioMixer
    {
    // --- Inner Definitions:

    /// <summary>
    /// Enumeration of silent wait periods.
    /// </summary>
    private enum WaitPeriod : byte
        {
        initialGap,
        shortGap,
        longGap,
        noGap,
        }

    /// <summary>
    /// Enumeration of playlist contexts.
    /// </summary>
    private enum Playlist : byte
        {
        unchanged = 0,

        menu,
        season,
        battle,
        }

    // --- Fields:

        // Time Constants:

    private const float FADE_TAIL = 2f;     // volume tail for crossover fade, in seconds
    private const float RISE_TAIL = 5f;     // volume tail for crossover rise, in seconds

    private const byte INIT_WAIT = 4;       // initial wait for playlist context change, in seconds

    private const byte SHORT_MIN = 20;      // lower bound on short gap between tracks, in seconds
    private const byte SHORT_MAX = 35;      // upper bound on short gap between tracks, in seconds

    private const byte LONG_MIN = 50;       // lower bound on long gap between playlists, in seconds
    private const byte LONG_MAX = 75;       // upper bound on long gap between playlists, in seconds

        // Playlist Managers:

    private SeasonJukebox worldThemes;      // playlist for season themes
    private CombatJukebox battleThemes;     // playlist for battle themes

        // Playstate Tracking:

    private Playlist currentContext;        // tracks current playlist setting
    private IdentityData? currentTrack;     // stores ongoing track ID or null value

    private bool introPerformed;            // flag marking one-off performance of menu intro

        // Coroutine Tracking:

    private Coroutine scheduledTransition;  // tracks transition coroutine for interruption

    // --- Initialization:

    /// <summary>
    /// Initial state constructor.
    /// </summary>
    public MusicManager(ReusablePool<AudioChannel> commonAudioPool, byte reservedChannels) : base(commonAudioPool,reservedChannels)
        {
        // setup playlist organizers

        worldThemes = new SeasonJukebox();
        battleThemes = new CombatJukebox();

        // setup finish event handler

        playbackFinishEvent += onEnd;

        // setup initial state

        currentContext = Playlist.unchanged;
        currentTrack = null;

        introPerformed = false;

        scheduledTransition = null;

        Ducking = DuckingEffect.ManualDucking;
        }

    // --- External Behaviors:

    /// <summary>
    /// Starts playback of menu playlist.
    /// </summary>
    public void PlayMenu()
        {
        clearSchedule();

        findNext(WaitPeriod.noGap,Playlist.menu);
        }

    /// <summary>
    /// Starts playback of season playlist.
    /// </summary>
    public void PlaySeason()
        {
        introPerformed = false;     // re-enable menu-intro after world session

        clearSchedule();

        findNext(WaitPeriod.initialGap,Playlist.season);
        }

    /// <summary>
    /// Starts playback of battle playlist.
    /// </summary>
    public void PlayBattle()
        {
        clearSchedule();

        findNext(WaitPeriod.initialGap,Playlist.battle);
        }

    /// <summary>
    /// Toggles temporary dampening of playback level without changing volume setting.
    /// </summary>
    public void ToggleDampening(bool dampen)
        {

        DuckingSwitch = dampen;
        }

    // --- Internal Procedures:

    #region Playlist Transitions

    /// <summary>
    /// Starts next track from given playlist context after appropriate wait period.
    /// </summary>
    private void findNext(WaitPeriod gap, Playlist context = Playlist.unchanged)
        {
        if (context != Playlist.unchanged) { currentContext = context; }

        switch(currentContext)
              {
              case(Playlist.season): worldNext(gap); break;
              case(Playlist.battle): battleNext(gap); break;
              case(Playlist.menu): menuNext(gap); break;
              }
        }

    /// <summary>
    /// Delegates track discovery to world playlist manager and starts playback.
    /// </summary>
    private void worldNext(WaitPeriod gap)
        {
        AudioData track = worldThemes.QueryNextSong(MasterController.Game.World.WorldTime.CurrentCycle);

        scheduledTransition = MasterController.Audio.StartCoroutine(transition(gap,track));
        }

    /// <summary>
    /// Delegates track discovery to battle playlist manager and starts playback.
    /// </summary>
    private void battleNext(WaitPeriod gap)
        {
        AudioData track = battleThemes.QueryNextSong();

        scheduledTransition = MasterController.Audio.StartCoroutine(transition(gap,track));
        }
    
    /// <summary>
    /// Starts playback of menu introduction or idle theme.
    /// </summary>
    private void menuNext(WaitPeriod gap)
        {
        if (!introPerformed)                            
           {
           // perform menu-intro on first time at main screen 

           introPerformed = true;

           AudioData introTheme = AudioLibrary.BGM.AUX_MenuIntro_bgm;
           scheduledTransition = MasterController.Audio.StartCoroutine(transition(gap,introTheme,false));
           }

        else {     
             // if already playing idle theme then do-nothing/continue
             // else transition to idle theme loop until further notice

             AudioData menuTheme = AudioLibrary.BGM.AUX_MenuOther_bgm;

             if (currentTrack.HasValue && currentTrack.Value.ContentID.LibID != menuTheme.LibID)       
                {                                       
                
                scheduledTransition = MasterController.Audio.StartCoroutine(transition(gap,menuTheme,false,true));
                }
             }
        }

    #endregion

    #region Track Transitions

    private IEnumerator transition(WaitPeriod gap, AudioData bgm, bool rise = true, bool loop = false)
        {
        // transition out

        if (currentTrack.HasValue) { MasterController.Audio.StartCoroutine(gradualFade(currentTrack.Value)); }

        // transition wait

        if (gap != WaitPeriod.noGap) 
           { 
           // discover wait period

           float silencePeriod;

           switch(gap)             // no-gap case ignored: wait loop already skipped
                 {
                 default:
                 case(WaitPeriod.initialGap): silencePeriod = INIT_WAIT; break;

                 case(WaitPeriod.shortGap): silencePeriod = MasterController.Random.Rand_Integer(SHORT_MIN,SHORT_MAX); break;
                 case(WaitPeriod.longGap): silencePeriod = MasterController.Random.Rand_Integer(LONG_MIN,LONG_MAX); break;
                 }

            // suspend wait period

            for (float elapsedTime = 0f; elapsedTime < silencePeriod; elapsedTime += Time.deltaTime) { yield return null; }
            }

        // transition in

        play(bgm,rise,loop);

        // transition done

        scheduledTransition = null;
        }

    private void play(AudioData bgm, bool rise, bool loop)
        {
        // playback & tracking

        currentTrack = Play(bgm,AudioPlayback.Range.RZone.Omni,1f,loop);

        // optional rise

        if (rise) { MasterController.Audio.StartCoroutine(gradualRise(currentTrack.Value)); }
        }

    /// <summary>
    /// Indicates end of previous track playback, schedules next in playlist queue after appropriate gap.
    /// </summary>
    private void onEnd()
        {
        currentTrack = null;

        WaitPeriod trackGap = WaitPeriod.shortGap;
        if (currentContext == Playlist.season && worldThemes.PlaysetEnding) { trackGap = WaitPeriod.longGap; }

        findNext(trackGap);
        }

    private IEnumerator gradualFade(IdentityData bgmID)
        {
        float elapsedPeriod = 0;
        float targetPeriod = FADE_TAIL;
        float initialValue = QueryVolume(bgmID);

        do {
           elapsedPeriod += Time.deltaTime;
           Mathf.Clamp(elapsedPeriod,0,targetPeriod);

           float instantValue = initialValue - (elapsedPeriod/targetPeriod * initialValue);
           ChangeVolume(bgmID,instantValue);

           yield return null;

           } while (elapsedPeriod < targetPeriod);

        Stop(bgmID);
        }

    private IEnumerator gradualRise(IdentityData bgmID)
        {
        // track rise period

        float elapsedPeriod = 0;
        float targetPeriod = RISE_TAIL;
        float targetVolume = QueryVolume(bgmID);

        // crescendo over period until target value

        do {
           elapsedPeriod += Time.deltaTime;
           Mathf.Clamp(elapsedPeriod,0,targetPeriod);

           float instantVolume = (elapsedPeriod/targetPeriod)*targetVolume;
           ChangeVolume(bgmID,instantVolume);

           yield return null;

           } while (elapsedPeriod < targetPeriod);
        
        yield break;
        }

    #endregion 

    #region Safe Interrupts

    /// <summary>
    /// Interrupts ongoing transition or wait period coroutines.
    /// Cancels playback of tracks that were already scheduled but cannot be stopped yet.
    /// </summary>
    private void clearSchedule()    // OLD: double scheduling when context switched before source activation
        {                           // FIX: track & interrupt scheduler coroutines

        if (scheduledTransition != null) { MasterController.Audio.StopCoroutine(scheduledTransition); }

        // BUG: already ongoing not stopped?
        }

    #endregion

    }


}
