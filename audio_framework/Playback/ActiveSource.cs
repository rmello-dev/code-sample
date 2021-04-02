
namespace Game.Api.Playback
{

/// <summary>
/// Functional controller for playback channels and audio sources.
/// Transparent bridge for playback strategy implementations.
/// </summary>
public sealed class ActiveSource
    {
    // --- Properties:

    /// <summary>
    /// Provides definition of current playback strategy.
    /// </summary>
    public AudioPlayback.Strategy Strategy { get; private set; }

    /// <summary>
    /// Provides reference to playback controller.
    /// </summary>
    public PlaybackController Controller { get; private set; }

    // --- Initialization:

    /// <summary>
    /// Creates active source for given audio data using defined playback strategy.
    /// </summary>
    public ActiveSource(AudioPlayback.Strategy playbackStrategy, SourceData playbackData)
        {
        makeStrategy(playbackStrategy,playbackData);

        Strategy = playbackStrategy;
        }

    // --- External Behaviors:

    /// <summary>
    /// Enforces use of given playback strategy.
    /// No change when strategy is already in use.
    /// </summary>
    public void EnforceStrategy(AudioPlayback.Strategy strategy)
        {

        if (strategy != Strategy) { swapStrategy(strategy); }
        }

    // --- Internal Procedures:

    /// <summary>
    /// Defines playback strategy for active source from parameter data.
    /// </summary>
    private void makeStrategy(AudioPlayback.Strategy playbackStrategy, SourceData playbackData)
        {
        Controller = AudioPlayback.ImplementStrategy(playbackStrategy);
        Controller.SetSourceParameters(playbackData);
        }

    /// <summary>
    /// Redefines playback strategy for active source from handover data.
    /// </summary>
    private void swapStrategy(AudioPlayback.Strategy newStrategy)
        {
        SourceData handoverData = Controller.GetSourceParameters();     // get snapshot of current state

        Controller.Stop();                                              // dispose old implementation
        makeStrategy(newStrategy,handoverData);                         // apply new implementation, restore from snapshot

        Strategy = newStrategy;                                         // update state flag
        }

    }


}
