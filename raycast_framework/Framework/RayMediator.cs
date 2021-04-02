using Game.Lib.Interface;

namespace Game.Api.Raycast
{

/// <summary>
/// Concrete implementation for raycast mediator interface. 
/// Mediates interaction between published broadcasts and target listeners.
/// </summary>
public sealed class RayMediator : IRayMediator
    {
    // --- Fields:

    IRayListener pressTarget;           // tracks valid clicks - cleared when press/hold released
    IRayListener previousTarget;        // tracks change in target - replaced every frame

    RaySignal.Source previousEvent;     // tracks multi-frame events - replaced every frame
    
    // --- Initialization:

    public RayMediator()
        {
        pressTarget = null;
        previousTarget = null;

        previousEvent = RaySignal.Source.RayIdle;
        }

    // --- Interface Methods:

    /// <summary>
    /// Processes event data and triggers action signal for target listener.
    /// </summary>
    public void ProcessBroadcast(RaySignal.Source eventType, IRayListener eventTarget)
        {
        // source signal classification flags

        bool pressSignal = (eventType == RaySignal.Source.RayPressFocus || eventType == RaySignal.Source.RayPressQuery);
        bool holdSignal = (eventType == RaySignal.Source.RayHoldFocus || eventType == RaySignal.Source.RayHoldQuery);

        bool focusSignal = (eventType == RaySignal.Source.RayPressFocus || eventType == RaySignal.Source.RayHoldFocus);
        bool querySignal = (eventType == RaySignal.Source.RayPressQuery || eventType == RaySignal.Source.RayHoldQuery);

        bool focusAction = (previousEvent == RaySignal.Source.RayPressFocus || previousEvent == RaySignal.Source.RayHoldFocus);
        bool queryAction = (previousEvent == RaySignal.Source.RayPressQuery || previousEvent == RaySignal.Source.RayHoldQuery);

        bool actionSignal = (focusAction || queryAction);

        // process listener-target

        if (eventTarget != null) 
           {
           bool sameAsPrevious = RaySignal.IDToken.CompareTokens(eventTarget.RayToken,MasterController.Input.Rays.CurrentTarget);
           bool sameAsPress = (pressTarget != null && RaySignal.IDToken.CompareTokens(eventTarget.RayToken,pressTarget.RayToken));

           // pointer-press

           if (pressSignal) 
              { 
              if (focusSignal) { eventTarget.RayFocusPress(); }
              if (querySignal) { eventTarget.RayQueryPress(); }

              pressTarget = eventTarget;
              }

           // pointer-release

           if (actionSignal && !holdSignal)
              {
              if (!sameAsPress) { eventType = RaySignal.Source.RayIdle; }
              else {
                   if (focusAction) { eventTarget.RayFocusAction(); eventTarget.RayFocusRelease(); }
                   if (queryAction) { eventTarget.RayQueryAction(); eventTarget.RayQueryRelease(); }

                   pressTarget = null;
                   }
              }
           
           // hover in/out

           if (!pressSignal && !sameAsPrevious)
              {
              if (previousTarget != null) { previousTarget.RayHoverExit(); }

              eventTarget.RayHoverEnter();
              }

           // external update

           MasterController.Input.Rays.CurrentTarget = eventTarget.RayToken;
           // MasterController.Input.Rays.CurrentFocus = TODO update focus
           }

        // process empty-target 

        else {
             if (pressSignal) { pressTarget = null; } // FIX: terminate outbound press-hold
                
             if (previousTarget != null) 
                {
                // pointer-release 

                if (previousEvent != RaySignal.Source.RayIdle)
                   {
                   if (focusAction) { previousTarget.RayFocusRelease(); }
                   if (queryAction) { previousTarget.RayQueryRelease(); }

                   eventType = RaySignal.Source.RayIdle;
                   }

                // hover-out

                previousTarget.RayHoverExit(); 
                }

             // external update

             MasterController.Input.Rays.CurrentTarget = RaySignal.IDToken.None;
             MasterController.Input.Rays.CurrentFocus = Context.Focus.None; 
             }

        // update previous

        previousTarget = eventTarget;
        previousEvent = eventType;
        }

    }


}
