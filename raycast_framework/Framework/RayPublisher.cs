using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Api.Raycast
{

/// <summary>
/// Concrete implementation for raycast publisher interface.
/// Aggregates raycast events and prepares broadcast.
/// </summary>
public sealed class RayPublisher : IRayPublisher
    {
    // --- Fields:

    private IRayMediator signalProcessor;       // source-to-action adapter

    private readonly int virtualLayerMask;      // raycast layer bit-mask, virtual only
    private readonly int spatialLayerMask;      // raycast layer bit-mask, spatial only

    // --- Initialization:

    public RayPublisher()
        {
        virtualLayerMask = SceneAdapter.GetLayerMask(SceneAdapter.LayerTag.Virtual);
        spatialLayerMask = SceneAdapter.GetLayerMask(SceneAdapter.LayerTag.Spatial);
        }

    // --- Interface Methods:

    /// <summary>
    /// Defines the broadcast signal processor.
    /// </summary>
    public void RegisterMediator(IRayMediator mediator)
        {

        signalProcessor = mediator;
        }

    /// <summary>
    /// Receives notification of raycast event, collects event data, and broadcasts signal for processing.
    /// </summary>
    public void RegisterEvent(RaySignal.Source eventType)
        {
        bool virtualCollision = false;
        bool spatialCollision = false;

        if (!EventSystem.current.IsPointerOverGameObject())                         // skip collision processing under canvas-interface areas
           {
           virtualCollision = layerRay(eventType,false);                            // if unblocked attempt to broadcast ray-interface collision target                                                                       
        
           spatialCollision = (!virtualCollision && layerRay(eventType,true));      // if still unblocked attempt to broadcast world-content collision target                                            
           }

        if (!virtualCollision && !spatialCollision) { emptyRay(eventType); }        // broadcast no collisions during update cycle
        }

    // --- Internal Procedures:

    /// <summary>
    /// Broadcasts empty signal.
    /// </summary>
    private void emptyRay(RaySignal.Source eventType)
        {

        signalProcessor.ProcessBroadcast(eventType,null);
        }

    /// <summary>
    /// Attempts to hit collision areas in spatial-grid or virtual-interface layers.
    /// </summary>
    private bool layerRay(RaySignal.Source eventType, bool spatial)
        {
        Camera camera = (spatial) ? (MasterController.Display.Camera.LinkSpatial.GetComponent<Camera>()):(MasterController.Display.Camera.LinkVirtual.GetComponent<Camera>());
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        int mask = (spatial) ? (spatialLayerMask):(virtualLayerMask);

        RaycastHit2D collision = Physics2D.Raycast(ray.origin,ray.direction,Mathf.Infinity,mask);
        IRayListener listener = (collision) ? (collision.collider.GetComponent<IRayListener>()):(null);
        
        if (listener != null) { signalProcessor.ProcessBroadcast(eventType,listener); }

        return(listener != null);
        }

    }


}
