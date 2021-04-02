using System;
using System.Collections;

using UnityEngine;

using Game.Lib.Media;
using Game.Api.Pooling;
using Game.Api.Rendering;
using Game.Lib.Interface;

namespace Game.Api.Interface
{

/// <summary>
/// Presenter for radial panel.
/// </summary>
public sealed class RadbarView
    {
    // --- Inner Members:

    /// <summary>
    /// Controller for ring frame and slice elements.
    /// </summary>
    private sealed class Ring
        {
        // --- Inner Members:

        /// <summary>
        /// Enumeration of non-themed ring elements.
        /// Ring frame and sleeve highlight graphics.
        /// </summary>
        public enum RingElements : byte
            {
            RingFrame00 = 00,
            RingFrame01 = 01,
            RingFrame02 = 02,
            RingFrame03 = 03,
            RingFrame04 = 04,
            RingFrame05 = 05,
            RingFrame06 = 06,
            RingFrame07 = 07,
            RingFrame08 = 08,
            RingFrame09 = 09,
            RingFrame10 = 10,
            RingFrame11 = 11,
            RingFrame12 = 12,
            RingFrame13 = 13,
            RingFrame14 = 14,
            RingFrame15 = 15,
            RingFrame16 = 16,
            RingFrame17 = 17,
            RingFrame18 = 18,
            RingFrame19 = 19,
            RingFrame20 = 20,
            RingFrame21 = 21,
            RingFrame22 = 22,
            RingFrame23 = 23,
            RingFrame24 = 24,
            RingFrame25 = 25,
            RingFrame26 = 26,
            RingFrame27 = 27,
            RingFrame28 = 28,
            RingFrame29 = 29,
            RingFrame30 = 30,
            RingFrame31 = 31,
            RingFrame32 = 32,

            RingSleeveA = 101,
            RingSleeveB = 102,
            RingSleeveC = 103,
            RingSleeveD = 104,
            RingSleeveE = 105,
            RingSleeveF = 106,
            RingSleeveG = 107,
            RingSleeveH = 108
            }

        /// <summary>
        /// Enumeration of ring frame states.
        /// </summary>
        private enum FrameState
            {
            Hidden,             // fully hidden

            DisplayFull,        // fully extended
            DisplayPartial,     // halfway extended
            }

        /// <summary>
        /// Enumeration of ring slice states.
        /// </summary>
        private enum SliceState
            {
            Hidden,         // fully hidden

            DisplayFull,    // fully visible
            DisplayPartial  // partial visibility
            }

        /// <summary>
        /// Ring slice button/sleeve wrapper.
        /// </summary>
        private sealed class Slice
            {
            // --- Properties:

            private GameObject sliceRoot;                   // hierarchy root

            private ButtonRay sliceButton;                  // slice button element
            private float sliceAlpha;                       // track button alpha value

            private int sliceIndex;                         // slice index identity/position

            private Proxy<SpriteRender> sliceSleeve;        // slice cover image
            private float sleeveAlpha;                      // track cover sleeve alpha value

            // --- Initialization:

            public Slice(GameObject slicesRoot, Context.Task.Set.Index index)
                {
                // setup hierarchy

                sliceRoot = new GameObject(ResourceLabels.gui_RadbarSlice);
                sliceRoot.layer = SceneAdapter.GetLayerIndex(SceneAdapter.LayerTag.Virtual);
                sliceRoot.transform.SetParent(slicesRoot.transform);

                // setup sleeve

                sliceIndex = (byte)index;
                RingElements sleeveIndex = RingElements.RingSleeveA+(byte)index;

                sliceSleeve = new Proxy<SpriteRender>(MasterController.Display.Render.Pool_Sprites);
                sliceSleeve.Entity.SetRender(matchSprite(sleeveIndex));
                sliceSleeve.Entity.SetHierarchy(sliceRoot);

                sliceSleeve.Entity.SetLayer(LayerOrder.gui_radialCap);   // render above ring frame and themed buttons 

                // setup button
            
                sliceButton = new ButtonRay(MasterController.Display.Screen.InterfaceTheme,index);
                sliceButton.SetRoot(sliceRoot);

                // initial state

                ResetState();
                }

            // --- External Behaviours:

            /// <summary>
            /// Changes slice to action/icon.
            /// </summary>
            public void DisplayIcon(Context.Task.Option option)
                {

                setOption(sliceButton,sliceIndex,option);
                }

            /// <summary>
            /// Changes visibility of slice theme element.
            /// Alpha values are limited to interval between 0 and 1.
            /// </summary>
            public void ThemeVisibility(float alpha)
                {
                sliceAlpha = alpha;

                sliceButton.SetVisibility(sliceAlpha);
                }

            /// <summary>
            /// Changes visibility of slice sleeves.
            /// Alpha values are limited to interval between 0 and 1.
            /// </summary>
            public void SleeveVisibility(float alpha)
                {
                sleeveAlpha = alpha;

                sliceSleeve.Entity.SetAlpha(sleeveAlpha);
                }

            /// <summary>
            /// Resets slice sleeve and button icon to initial state.
            /// </summary>
            public void ResetState()
                {
                SleeveVisibility(0);                                                // hidden sleeve
                ThemeVisibility(0);                                                 // hidden theme

                setOption(sliceButton,sliceIndex,Context.Task.Option.L0_Blank);     // blank button
                }

            // --- Internal Procedures:

            /// <summary>
            /// Defines slice symbol, tooltip, shortcut, and click action.
            /// </summary>
            private void setOption(ButtonRay button, int optionIndex, Context.Task.Option option) 
                {
                button.SetSymbol(Context.Task.SymbolFromOption(option));
                button.SetAction(Context.Task.ActionFromOption(option));
                button.SetTooltip(Context.Task.TooltipFromOption(option));
                button.SetShortcut(Context.Task.ShortcutFromOption(option,optionIndex));

                button.SetInteraction((option != Context.Task.Option.L0_Blank));
                }

            }

        // --- Fields:

        private GameObject frameRoot;                   // ring frame hierarchy root
        private GameObject slicesRoot;                  // hierarchy root

        private Proxy<ReusableTimer> timer;             // animation scheduler

        private IEnumerator animation;                  // ongoing animation coroutine
        private int animationIndex;                     // index of last complete frame step

        private FrameState frameState;                  // track ring frame display state
        private SliceState sliceState;                  // track ring slices display state

        private Proxy<SpriteRender>[] frameSet;         // array of radial-frame assets

        private const int frameHead = (byte)RingElements.RingFrame00;       // frame set iterator head = 0
        private const int frameTail = (byte)RingElements.RingFrame32;       // frame set iterator tail = 32

        private Slice[] sliceSet;                       // array of radial-slice buttons

        private const int sliceHead = (byte)Context.Task.Set.Index.A;     // slice set iterator head = 0
        private const int sliceTail = (byte)Context.Task.Set.Index.H;     // slice set iterator tail = 7

        /// <summary>
        /// Visibility of ring elements - at least partial display.
        /// </summary>
        public bool Open { get { return(frameState != FrameState.Hidden); } }

        /// <summary>
        /// Visibility of ring elements - finished context display.
        /// </summary>
        public bool Ready { get { return(sliceState == SliceState.DisplayFull); } }

        // --- Initialization:

        public Ring(GameObject viewRoot)
            {
            // ring frame initialization

            frameRoot = new GameObject(ResourceLabels.gui_RadbarFrame);
            frameRoot.layer = SceneAdapter.GetLayerIndex(SceneAdapter.LayerTag.Virtual);
            frameRoot.transform.SetParent(viewRoot.transform);

            frameSet = new Proxy<SpriteRender>[frameTail+1];

            for(int frame = frameHead; frame <= frameTail; frame++)
               {
               frameSet[frame] = new Proxy<SpriteRender>(MasterController.Display.Render.Pool_Sprites);
               frameSet[frame].Entity.SetRender(matchSprite((RingElements)frame));
               frameSet[frame].Entity.SetHierarchy(frameRoot);
               frameSet[frame].Entity.SetAlpha(0);

               frameSet[frame].Entity.SetLayer(LayerOrder.gui_radialLow);
               }

            animationIndex = frameHead;

            frameState = FrameState.Hidden;
            frameRoot.SetActive(false);

            // ring slices initialization
            
            slicesRoot = new GameObject(ResourceLabels.gui_RadbarSlices);
            slicesRoot.layer = SceneAdapter.GetLayerIndex(SceneAdapter.LayerTag.Virtual);
            slicesRoot.transform.SetParent(viewRoot.transform);

            sliceSet = new Slice[sliceTail+1];

            for(int slice = sliceHead; slice <= sliceTail; slice++)
               {

               sliceSet[slice] = new Slice(slicesRoot,(Context.Task.Set.Index)slice);
               }

            sliceState = SliceState.Hidden;
            slicesRoot.SetActive(false);

            // shared state initialization

            timer = new Proxy<ReusableTimer>(MasterController.Events.Pool_Timers);
            animation = null;
            }

        // --- External Behavior:

        /// <summary>
        /// Shows initial frame and radial expansion of frame, then blinks new context slices.
        /// Interrupts or reverts ongoing/finished animations to start from scratch.
        /// </summary>
        public IEnumerator ShowRing(Context.Task.Set actions)
            {
            if (frameState != FrameState.Hidden)
               {
               if (animation != null) 
                  { 
                  MasterController.Events.StopCoroutine(animation);
                  animation = null;
                  }

               instantHide(); 
               }

            animation = gradualExpand();

            yield return MasterController.Events.StartCoroutine(animation);

            animation = gradualBlink(actions);

            yield return MasterController.Events.StartCoroutine(animation);

            animation = null;
            }
            
        /// <summary>
        /// Hides ring frame and slices.
        /// Interrupts and reverts any ongoing show animation.
        /// </summary>
        public IEnumerator HideRing(bool immediate)
            {
            if (animation != null) 
                  { 
                  MasterController.Events.StopCoroutine(animation);
                  animation = null;
                  }

            if (immediate) { instantHide(); }
            else {
                 animation = gradualFade();

                 yield return MasterController.Events.StartCoroutine(animation);

                 animation = null;
                 }
            }
        
        /// <summary>
        /// Blinks sleeves to show new content on current ring.
        /// </summary>
        public IEnumerator SwapContext(Context.Task.Set actions)
            {
            if (sliceState == SliceState.DisplayFull)
               {
               animation = gradualBlink(actions);

               yield return MasterController.Events.StartCoroutine(animation);

               animation = null;
               }
            }

        // --- Internal Procedures:

        /// <summary>
        /// Gradually expands radial ring frame.
        /// </summary>
        private IEnumerator gradualExpand()
            {
            // setup timer

            float frameTime = MasterController.Display.Render.FrameDelta_Safe;

            timer.Entity.Tag("real-time/radbar-expand");
            timer.Entity.RealTimer.ScaledTime = false;
            timer.Entity.RealTimer.Set(frameTime);

            // initial frame

            frameState = FrameState.DisplayPartial;

            frameRoot.SetActive(true);
            frameSet[frameHead].Entity.SetAlpha(1);             // first step

            // radial expansion

            float totalTime = SLOWPERIOD;                       // animation period
            float totalSteps = totalTime/frameTime;             // number of time steps
            float framesByStep = (frameTail)/totalSteps;        // number of frames advanced each step

            int prevFrame = frameHead;                          // previous keyframe
            int currFrame = frameHead+1;                        // current keyframe
            float jumpFrame = 0;                                // progress towards next/skip frame

            while (currFrame < frameTail)                       // from second to just before the final step
                {
                if (currFrame != prevFrame)                     // only execute if step advanced at least one full frame 
                   {
                   frameSet[prevFrame].Entity.SetAlpha(0);      // hide previous frame
                   frameSet[currFrame].Entity.SetAlpha(1);      // show current frame
                   animationIndex = currFrame;                  // track active index
                   }

                prevFrame = currFrame;                          // track completed frame

                jumpFrame += framesByStep;                      // accumulate partial progress or multiple discrete frames

                if (jumpFrame > 1)                              // jump to next frame
                    {
                    currFrame += Mathf.FloorToInt(jumpFrame);   // stay at / advance to nearest discrete frame
                    jumpFrame = 0;                              // reset accumulator
                    }
                
                yield return timer.Entity.RealTimer.Cycle();    // suspend until next step
                }

            // additional step to ensure final frame is not skipped

            frameSet[prevFrame].Entity.SetAlpha(0);
            frameSet[frameTail].Entity.SetAlpha(1);
            animationIndex = frameTail;

            // end coroutine

            frameState = FrameState.DisplayFull;

            timer.Unload();
            yield break;
            }

        /// <summary>
        /// Gradually blinks sleeves and reveals themed context slices.
        /// </summary>
        private IEnumerator gradualBlink(Context.Task.Set actions)
            {
            // setup timer

            float frameTime = MasterController.Display.Render.FrameDelta_Safe;

            timer.Entity.Tag("real-time/radbar-blink");
            timer.Entity.RealTimer.ScaledTime = false;
            timer.Entity.RealTimer.Set(frameTime);

            // quick sleeve fade-in

            sliceState = SliceState.DisplayPartial;

            slicesRoot.SetActive(true);

            float fadePeriod = FASTPERIOD;
            float fadeFrames = fadePeriod/frameTime;
            float alphaStep = 1/fadeFrames;

            for(float alpha = 0; alpha <= 1; alpha += alphaStep)
                {
                if (alpha > 1-alphaStep) { alpha = 1; }

                for(int slice = sliceHead; slice <= sliceTail; slice++)
                   {

                   sliceSet[slice].SleeveVisibility(alpha);
                   }

                yield return timer.Entity.RealTimer.Cycle();
                }

            // instantly display themed slices & replace slice action icons with new set

            for(int slice = sliceHead; slice <= sliceTail; slice++)
               {
               sliceSet[slice].ThemeVisibility(1);

               sliceSet[slice].DisplayIcon(actions.ByIndex(slice));
               }

            // slow sleeve fade-out

            for(float alpha = 1; alpha >= 0; alpha -= alphaStep)
                {
                if (alpha < alphaStep) { alpha = 0; } 

                for(int slice = sliceHead; slice <= sliceTail; slice++)
                   {

                   sliceSet[slice].SleeveVisibility(alpha);
                   }

                yield return timer.Entity.RealTimer.Cycle();
                }

            // end coroutine

            sliceState = SliceState.DisplayFull;

            timer.Unload();
            yield break;
            }

        /// <summary>
        /// Gradually fades ring frame and slices until completely hidden.
        /// </summary>
        private IEnumerator gradualFade()
            {
            // setup timer

            float frameTime = MasterController.Display.Render.FrameDelta_Safe;

            timer.Entity.Tag("real-time/radbar-fade");
            timer.Entity.RealTimer.ScaledTime = false;
            timer.Entity.RealTimer.Set(frameTime);

            // adapt to frame-rate
            
            float fadeTime = FASTPERIOD;
            float fadeSteps = fadeTime/frameTime;
            float alphaStep = 1/fadeSteps;

            // begin alpha fade
            
            frameState = FrameState.DisplayPartial;
            sliceState = SliceState.DisplayPartial;

            for(float alpha = 1; alpha >= 0; alpha -= alphaStep)
                {
                if (alpha < alphaStep) { alpha = 0; }                       // compensate for rounding on last iteration

                frameSet[animationIndex].Entity.SetAlpha(alpha);            // fade open ring frame

                for(int slice = sliceHead; slice <= sliceTail; slice++)
                   {

                   sliceSet[slice].ThemeVisibility(alpha);                  // fade each themed slice
                   }

                yield return timer.Entity.RealTimer.Cycle();                // wait cycle
                }

            // end coroutine

            frameRoot.SetActive(false);
            frameState = FrameState.Hidden;

            slicesRoot.SetActive(false);
            sliceState = SliceState.Hidden;

                // TODO deactivate objects~unload sprites

            timer.Unload();
            }
            
        /// <summary>
        /// Immediately hides ring frame and slices to smoothly interrupt/restart animations. 
        /// </summary>
        private void instantHide()
            {
            if (frameState != FrameState.Hidden)
               {
               frameSet[animationIndex].Entity.SetAlpha(0);
               animationIndex = frameHead;
            
               frameRoot.SetActive(false);
               frameState = FrameState.Hidden;
               }

            if (sliceState != SliceState.Hidden)
               {
               for(int slice = sliceHead; slice <= sliceTail; slice++)
                   {
                   sliceSet[slice].ThemeVisibility(0);
                   sliceSet[slice].SleeveVisibility(0);
                   }
                
               slicesRoot.SetActive(false);
               sliceState = SliceState.Hidden;
               }
            }

        /// <summary>
        /// Retrieves sprite resource path matching a ring element.
        /// </summary>
        private static string matchSprite(RingElements element)
            {
            string path = string.Empty;

            switch(element)
                {
                case(RingElements.RingFrame00): path = ResourcePaths.UI_CustomRadbarRing00_url; break;
                case(RingElements.RingFrame01): path = ResourcePaths.UI_CustomRadbarRing01_url; break;
                case(RingElements.RingFrame02): path = ResourcePaths.UI_CustomRadbarRing02_url; break;
                case(RingElements.RingFrame03): path = ResourcePaths.UI_CustomRadbarRing03_url; break;
                case(RingElements.RingFrame04): path = ResourcePaths.UI_CustomRadbarRing04_url; break;
                case(RingElements.RingFrame05): path = ResourcePaths.UI_CustomRadbarRing05_url; break;
                case(RingElements.RingFrame06): path = ResourcePaths.UI_CustomRadbarRing06_url; break;
                case(RingElements.RingFrame07): path = ResourcePaths.UI_CustomRadbarRing07_url; break;
                case(RingElements.RingFrame08): path = ResourcePaths.UI_CustomRadbarRing08_url; break;
                case(RingElements.RingFrame09): path = ResourcePaths.UI_CustomRadbarRing09_url; break;
                case(RingElements.RingFrame10): path = ResourcePaths.UI_CustomRadbarRing10_url; break;
                case(RingElements.RingFrame11): path = ResourcePaths.UI_CustomRadbarRing11_url; break;
                case(RingElements.RingFrame12): path = ResourcePaths.UI_CustomRadbarRing12_url; break;
                case(RingElements.RingFrame13): path = ResourcePaths.UI_CustomRadbarRing13_url; break;
                case(RingElements.RingFrame14): path = ResourcePaths.UI_CustomRadbarRing14_url; break;
                case(RingElements.RingFrame15): path = ResourcePaths.UI_CustomRadbarRing15_url; break;
                case(RingElements.RingFrame16): path = ResourcePaths.UI_CustomRadbarRing16_url; break;
                case(RingElements.RingFrame17): path = ResourcePaths.UI_CustomRadbarRing17_url; break;
                case(RingElements.RingFrame18): path = ResourcePaths.UI_CustomRadbarRing18_url; break;
                case(RingElements.RingFrame19): path = ResourcePaths.UI_CustomRadbarRing19_url; break;
                case(RingElements.RingFrame20): path = ResourcePaths.UI_CustomRadbarRing20_url; break;
                case(RingElements.RingFrame21): path = ResourcePaths.UI_CustomRadbarRing21_url; break;
                case(RingElements.RingFrame22): path = ResourcePaths.UI_CustomRadbarRing22_url; break;
                case(RingElements.RingFrame23): path = ResourcePaths.UI_CustomRadbarRing23_url; break;
                case(RingElements.RingFrame24): path = ResourcePaths.UI_CustomRadbarRing24_url; break;
                case(RingElements.RingFrame25): path = ResourcePaths.UI_CustomRadbarRing25_url; break;
                case(RingElements.RingFrame26): path = ResourcePaths.UI_CustomRadbarRing26_url; break;
                case(RingElements.RingFrame27): path = ResourcePaths.UI_CustomRadbarRing27_url; break;
                case(RingElements.RingFrame28): path = ResourcePaths.UI_CustomRadbarRing28_url; break;
                case(RingElements.RingFrame29): path = ResourcePaths.UI_CustomRadbarRing29_url; break;
                case(RingElements.RingFrame30): path = ResourcePaths.UI_CustomRadbarRing30_url; break;
                case(RingElements.RingFrame31): path = ResourcePaths.UI_CustomRadbarRing31_url; break;
                case(RingElements.RingFrame32): path = ResourcePaths.UI_CustomRadbarRing32_url; break;
                
                case(RingElements.RingSleeveA): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveA_url; break;
                case(RingElements.RingSleeveB): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveB_url; break;
                case(RingElements.RingSleeveC): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveC_url; break;
                case(RingElements.RingSleeveD): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveD_url; break;
                case(RingElements.RingSleeveE): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveE_url; break;
                case(RingElements.RingSleeveF): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveF_url; break;
                case(RingElements.RingSleeveG): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveG_url; break;
                case(RingElements.RingSleeveH): path = ResourcePaths.UI_CustomRadbarSliceTxSleeveH_url; break;
                }

            return(path);
            }

        }

    // --- Fields:

    private const float SLOWPERIOD = 0.4f;      // long animation period in seconds - used by ring controller
    private const float FASTPERIOD = 0.3f;      // short animation period in seconds - used by ring controller

    private readonly GameObject viewRoot;       // hierarchy root

    private Ring ringController;                // controller for radial frame and slice elements

    private AudioData ringSfx;                  // audible feedback for ring-show events

    // --- Initialization:

    public RadbarView(GameObject hierarchyRoot)
        {
        // view root

        viewRoot = new GameObject(ResourceLabels.gui_Radbar);
        viewRoot.layer = SceneAdapter.GetLayerIndex(SceneAdapter.LayerTag.Virtual);      
        viewRoot.transform.SetParent(hierarchyRoot.transform);

        // setup - ring frame + empty slices

        ringController = new Ring(viewRoot);

        // initial state - hidden

        InterfaceTheme.RadbarDecorator(MasterController.Display.Screen.InterfaceTheme, out ringSfx);

        viewRoot.SetActive(false);
        }

    // --- External Behaviors:

    /// <summary>
    /// Shows control with contextual options.
    /// </summary>
    public void Show(Context.Task.Group context)
        {
        // discover context/set actions

        Context.Task.Set actionSet = Context.Task.OptionsFromGroup(context,true);

        // summon control/context

        viewRoot.SetActive(true);

        MasterController.Events.StartCoroutine(ringController.ShowRing(actionSet));
        
        // summon control sfx

        MasterController.Audio.SFX.PlayAux(ringSfx);
        }
    
    /// <summary>
    /// Adapts control to new contextual options.
    /// </summary>
    public void Swap(Context.Task.Group context)
        {
        // discover context/set actions

        Context.Task.Set actionSet = Context.Task.OptionsFromGroup(context,true);

        // adapt control/context

        MasterController.Events.StartCoroutine(ringController.SwapContext(actionSet));
        }
    
    /// <summary>
    /// Hides control and resets state.
    /// </summary>
    public void Hide()                  
        {                               // OLD: performing hide animation when already hidden
        if (ringController.Open)        // FIX: test display before dismissing control/context
           {
           IEnumerator coroutine = ringController.HideRing(false);
           Action callback = delegate { viewRoot.SetActive(false); };

           MasterController.Events.SequenceHandler(coroutine,callback);
           }
        }

    /// <summary>
    /// Relocates control to new screen coordinates.
    /// </summary>
    public void Reposition(ViewPoint.Virtual newPosition)
        {

        viewRoot.transform.position = newPosition;
        }
    
    }


}
