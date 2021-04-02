
namespace Game.Api.Raycast
{

/// <summary>
/// Interface for entity that aggregates raycast events and prepares broadcast.
/// Enforces ability to receive event notification and trigger signal processing.
/// </summary>
public interface IRayPublisher
    {

    /// <summary>
    /// Defines the broadcast signal processor.
    /// </summary>
    void RegisterMediator(IRayMediator mediator);

    /// <summary>
    /// Receives notification of raycast event, collects event data, and broadcasts signal for processing.
    /// </summary>
    void RegisterEvent(RaySignal.Source eventType);
    }


}
