
using Game.Lib.Space;
using Game.Lib.World;

namespace Game.Api.Playback
{
    
/// <summary>
/// Encapsulates listener location data for spatialized playback.
/// </summary>
public readonly struct ListenerData
    {
    // --- Properties:

    /// <summary>
    /// Current gridspace location of listener.
    /// </summary>
    public GridPoint Location { get; }

    /// <summary>
    /// Provides Z value of exterior surface at current location.
    /// </summary>
    public sbyte SurfaceLevel { get; }

    /// <summary>
    /// Provides playback zone applicable at current location.
    /// </summary>
    public AudioPlayback.Range.RZone Zone { get; }

    // --- Initialization:

    /// <summary>
    /// Defines listener parameters from current camera focus.
    /// </summary>
    public ListenerData(GridPoint focus)
        {
        Location = focus;

        WorldModel world = MasterController.Game.World;

        if (world != null)
           {
           SurfaceLevel = world.LocalSettlement.QuerySurface(focus);

           Zone = (focus.Z >= SurfaceLevel) ? (AudioPlayback.Range.RZone.Exterior):(AudioPlayback.Range.RZone.Undeground);
           }

        else {
             Zone = AudioPlayback.Range.RZone.Omni;
             
             Location = GridPoint.Zero;
             SurfaceLevel = 0;
             }
        }

    }


}
