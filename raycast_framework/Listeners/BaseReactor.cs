using UnityEngine;

namespace Game.Api.Raycast
{

/// <summary>
/// Base class for raycast event handler components.
/// </summary>
public abstract class BaseReactor : MonoBehaviour, IRayListener
    {
    // --- Fields:

    public RaySignal.IDToken RayToken { get; private set; }     // listener identity token

    public bool RayFocusSensitivity { get; protected set; }     // flag indicating if listener reacts to primary action
    public bool RayQuerySensitivity { get; protected set; }     // flag indicating if listener reacts to secondary action
    public bool RayHoverSensitivity { get; protected set; }     // flag indicating if listener reacts to pointer movement

    // --- Initialization:

    /// <summary>
    /// Initializes primitive state.
    /// </summary>
    public void Awake()
        {

        RayToken = RaySignal.IDToken.TokenFactory();
        }

    /// <summary>
    /// Configures baseline passive initial state.
    /// </summary>
    public void PassiveReactor() 
        {
        RayFocusSensitivity = false;
        RayQuerySensitivity = false;
        RayHoverSensitivity = false;

        setupReactionArea(false);
        }

    /// <summary>
    /// Initializes reactive state.
    /// </summary>
    public void InitializeReactor()
        {
        setupReactionTriggers();

        setupReactionArea(true);
        }

    // --- Interface Methods:

    public void RayHoverEnter()   { if (RayHoverSensitivity) { onEnter();   } }
    public void RayHoverExit()    { if (RayHoverSensitivity) { onExit();    } }

    public void RayFocusPress()   { if (RayFocusSensitivity) { onPress();   } }
    public void RayFocusRelease() { if (RayFocusSensitivity) { onRelease(); } }
    public void RayFocusAction()  { if (RayFocusSensitivity) { onFocus();   } }

    public void RayQueryPress()   { if (RayQuerySensitivity) { onPress();   } }
    public void RayQueryRelease() { if (RayQuerySensitivity) { onRelease(); } }
    public void RayQueryAction()  { if (RayQuerySensitivity) { onQuery();   } }

    // --- Internal Reactions:

    protected abstract void onFocus();

    protected abstract void onQuery();

    protected abstract void onPress();

    protected abstract void onRelease();

    protected abstract void onEnter();

    protected abstract void onExit();

    // --- Internal Procedures:

    /// <summary>
    /// Defines listener-type reaction triggers.
    /// </summary>
    protected abstract void setupReactionTriggers();
        
    /// <summary>
    /// Implements addition and removal of reaction area.
    /// </summary>
    protected abstract void setupReactionArea(bool activate);

    }


}
