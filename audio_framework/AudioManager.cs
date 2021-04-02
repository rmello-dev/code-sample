
using UnityEngine;

using Game.Lib.Space;
using Game.Api.Pooling;
using Game.Api.Playback;

namespace Game.Api
{

/// <summary>
/// Audio framework manager.
/// Controls framework configuration.
/// Provides controllers for BGM and SFX playback.
/// </summary>
public sealed class AudioManager : MonoBehaviour
    {
    // --- Fields:

    private FrameworkData framework;                // maintains references to persistent pool for reusable audio objects & scene root for audio hierarchy

    private float master_setting;                   // master volume, [0…1] range
    private float bgm_setting;                      // music volume setting, [0…1] range 
    private float sfx_setting;                      // effects volume setting, [0…1] range

    private ListenerData listener;                  // maintains latest spatial data for camera focus

    private const float INTERVAL_DURATION = 0.25f;  // recalculation skip interval for listener/source refresh cycle
    private float interval_elapsed;                 // counter for skip interval measurement

    // --- Properties:

    /// <summary>
    /// Controls sound effect playback.
    /// </summary>
    public SoundManager SFX { get; private set; }

    /// <summary>
    /// Controls background music playback.
    /// </summary>
    public MusicManager BGM { get; private set; }

    /// <summary>
    /// Provides spatial parameters for current listening focus.
    /// </summary>
    public ListenerData Listener { get { return(listener); } }

    // --- Initialization:

    /// <summary>
    /// Initial state constructor, local-only dependencies.
    /// </summary>
    public void Awake()
        {
        // channel/source limits - engine max: 32

        byte maxSfx = 16;   // concurrent sfx channels
        byte maxBgm = 2;    // concurrent bgm channels
        uint batch = 9;     // reusable median (16+2/2)

        // audio pool 

        GameObject poolHierarchy = GameObject.Find(ResourceLabels.fixed_uid_MasterPool);

        ReusablePool<AudioChannel> audioPool = new ReusablePool<AudioChannel>(batch,poolHierarchy);
        
        // audio mixers

        SFX = new SoundManager(audioPool,maxSfx);
        BGM = new MusicManager(audioPool,maxBgm);

        // framework hooks - incomplete

        framework = new FrameworkData(null,audioPool);

        // refresh cycle

        interval_elapsed = 0.0f;
        }

    /// <summary>
    /// Builds audio framework within scene.
    /// Restores any ongoing virtualized sounds. 
    /// </summary>
    public void MakeFramework()
        {
        GameObject audioRoot = new GameObject(ResourceLabels.org_RootSfx);                                  // rebuild hierarchy root
        audioRoot.layer = SceneAdapter.GetLayerIndex(SceneAdapter.LayerTag.Logic);                          // .
        audioRoot.transform.SetParent(GameObject.Find(ResourceLabels.fixed_uid_SceneVirtual).transform);    // .

        framework = new FrameworkData(audioRoot,framework.AudioPool);                                       // update framework hooks

        GameObject cameraAttachment = MasterController.Display.Camera.LinkSpatial;                                      // rebuild camera-listener

        if (cameraAttachment != null) { listener = new ListenerData(MasterController.Display.Camera.CameraFocus); }     // attach in spatial scenes   
        else { cameraAttachment = MasterController.Display.Camera.LinkVirtual; }                                        // attach in virtual scenes

        cameraAttachment.AddComponent<AudioListener>();                                                                 // bind spatial listener to worldspace camera

        // restore any previously virtualized bgm

        BGM.Virtual = false;
        }

    /// <summary>
    /// Cleans audio framework between scenes.
    /// Stops sfx playback but virtualizes any ongoing bgm.
    /// </summary>
    public void ResetFramework()
        {
        interruptMixer(true,false);
                                        // OLD: ongoing sounds abruptly interrupted on scene change
        BGM.Virtual = true;             // FIX: virtualize ongoing BGMs on framework Reset and revert on Make
        }

    // --- Frame Update:

    /// <summary>
    /// Frame update loop that tracks audio listener location and refreshes sources and effects.
    /// </summary>
    public void Update()
        {
        // refresh source activity

        BGM.UpdateActivity();
        SFX.UpdateActivity();

        // track recalculation delay

        interval_elapsed += Time.deltaTime;

        if (interval_elapsed > INTERVAL_DURATION)
           {
           // refresh listener data if camera focus changed

           GridPoint latest = MasterController.Display.Camera.CameraFocus;

           if (listener.Location != latest) { listener = new ListenerData(latest); }

           // refresh source attenuation effects

           BGM.UpdateSpatial();
           SFX.UpdateSpatial();
           
           // reset refresh cycle period

           interval_elapsed = 0f;
           }
        }

    // --- External Behaviours:

    /// <summary>
    /// Applies configuration settings.
    /// Volume components should be within 0-100 range.
    /// </summary>
    public void ApplyConfig(byte masterVolume, byte musicLevel, byte effectLevel)
        {
        // convert 0-100 settings to 0-1 value range

        master_setting = Mathf.Clamp01(masterVolume/100f);  
        bgm_setting = Mathf.Clamp01(musicLevel/100f);
        sfx_setting = Mathf.Clamp01(effectLevel/100f);

        // apply volume settings

        BGM.Volume = master_setting*bgm_setting;
        SFX.Volume = master_setting*sfx_setting;
        }

    /// <summary>
    /// Provides updated framework data for configuring live playback.
    /// </summary>
    public FrameworkData GetFrameworkData()
        {

        return(framework);
        }

    /// <summary>
    /// Stops all audio playback immediately. 
    /// </summary>
    public void ForceInterrupt()
        {
        
        interruptMixer(true,true);
        }

    // --- Internal Procedures:

    private void interruptMixer(bool sfx, bool bgm)
        {
        if (sfx) { SFX.StopAll(); }
        if (bgm) { BGM.StopAll(); }
        }

    }


}
