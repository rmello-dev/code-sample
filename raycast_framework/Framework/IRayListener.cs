
namespace Game.Api.Raycast
{

/// <summary>
/// Interface for entities that react to raycast event notifications.
/// Enforces ability to receive and handle processed signals. 
/// </summary>
public interface IRayListener
    {

    /// <summary>
    /// ID token for listeners.
    /// </summary>
    RaySignal.IDToken RayToken { get; }
    
    /// <summary>
    /// Flag indicating sensitivity to focus events.
    /// Listener will not react to query press/actions when false.
    /// </summary>
    bool RayFocusSensitivity { get; }

    /// <summary>
    /// Flag indicating sensitivity to query events.
    /// Listener will not react to query press/actions when false.
    /// </summary>
    bool RayQuerySensitivity { get; }

    /// <summary>
    /// Flag indicating sensitivity to hover events.
    /// Listener will not react to pointer movement when false.
    /// </summary>
    bool RayHoverSensitivity { get; }

    /// <summary>
    /// Triggered when pointer enters listener ray-collider area, if sensitive to hovering.
    /// </summary>
    void RayHoverEnter();

    /// <summary>
    /// Triggered when pointer leaves listener ray-collider area, if sensitive to hovering.
    /// </summary>
    void RayHoverExit();

    /// <summary>
    /// Triggered by pointer-press signal, if sensitive to focus action.
    /// </summary>
    void RayFocusPress();

    /// <summary>
    /// Triggered by pointer-release signal, if sensitive to focus action.
    /// </summary>
    void RayFocusRelease();

    /// <summary>
    /// Triggered alongside pointer-release signal to execute main action.
    /// </summary>
    void RayFocusAction();

    /// <summary>
    /// Triggered by pointer-press signal, if sensitive to query action.
    /// </summary>
    void RayQueryPress();

    /// <summary>
    /// Triggered by pointer-release signal, if sensitive to query action.
    /// </summary>
    void RayQueryRelease();

    /// <summary>
    /// Triggered alongside pointer-release signal to execute alternate action.
    /// </summary>
    void RayQueryAction();
    }


}
