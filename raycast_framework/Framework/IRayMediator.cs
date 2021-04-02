using UnityEngine;

namespace Game.Api.Raycast
{

/// <summary>
/// Interface for entity that mediates interaction between published broadcasts and target listeners.
/// Enforces ability to receive broadcasts, process signals, and notify target.
/// </summary>
public interface IRayMediator
    {

    /// <summary>
    /// Processes event data and triggers action signal for target listener.
    /// </summary>
    void ProcessBroadcast(RaySignal.Source eventType, IRayListener eventTarget);
    }


}
