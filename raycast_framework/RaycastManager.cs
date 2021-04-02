using Game.Api.Raycast;
using Game.Lib.Interface;

namespace Game.Api
{

/// <summary>
/// Raycast manager.
/// Controller for raycast event aggregation, processing, and broadcast.
/// </summary>
public sealed class RaycastManager
    {
    // --- Fields:

    private IRayPublisher publisher;    // collect raycast event data, broadcast signal
    private IRayMediator mediator;      // process signal, notify target listener

    /// <summary>
    /// Focus of current pointer context.
    /// </summary>
    public Context.Focus CurrentFocus { get; set; }

    /// <summary>
    /// Unique identifier for current raycast target.
    /// </summary>
    public RaySignal.IDToken CurrentTarget { get; set; }

    // --- Initialization:

    /// <summary>
    /// Initial state constructor.
    /// </summary>
    public RaycastManager()
        {
        publisher = new RayPublisher();
        mediator = new RayMediator();

        publisher.RegisterMediator(mediator);

        CurrentFocus = Context.Focus.None;
        CurrentTarget = RaySignal.IDToken.None;
        }

    // --- External Behaviours:

    /// <summary>
    /// Starts raycast event processing chain.
    /// </summary>
    public void RegisterEvent(RaySignal.Source eventType)
        {

        publisher.RegisterEvent(eventType);
        }

    }


}
