
namespace Game.Api.Raycast
{

/// <summary>
/// Adapter for raw and processed raycast signals.
/// </summary>
public static class RaySignal
    {
    // --- Inner Members:

    /// <summary>
    /// Enumeration of raycast source-input signals.
    /// </summary>
    public enum Source
        {
        RayIdle,        // pointer hover
        
        RayPressFocus,  // main pointer-press
        RayHoldFocus,   // main pointer-hold
        
        RayPressQuery,  // alt pointer-press
        RayHoldQuery,   // alt pointer-hold
        }
    
    // RayMediator processes event sources as:
    //  HoverIn,        > hover-in listener area
    //  HoverOut,       > hover-out listener area
    //  ClickPress,     > press down
    //  ClickHold,      > press keep
    //  ClickRelease,   > press up
    //  ClickFocus,     > focus action
    //  ClickQuery,     > query action
    // Actions trigger upon release if still within area.

    /// <summary>
    /// Token structure for identification of signal listeners.
    /// </summary>
    public struct IDToken
        {
        // --- Fields:

        private readonly int identifier;                        // unique identifier

        private static IDToken zero = new IDToken(0);           // empty token value

        public static IDToken None { get { return(zero); } }    // empty token property

        // --- Initialization:

        /// <summary>
        /// Internal constructor.
        /// </summary>
        private IDToken(int ID) { identifier = ID; }

        /// <summary>
        /// Creates a non-zero token with very high probability of uniqueness.
        /// </summary>
        public static IDToken TokenFactory() 
            {
            int integer;

            do {
               integer = MasterController.Random.Rand_Integer(int.MinValue,int.MaxValue);
               } while(integer == 0);

            IDToken token = new IDToken(integer);

            return(token); 
            }

        // --- Comparison:

        /// <summary>
        /// Compares any two tokens and returns true when identical, false when different.
        /// </summary>
        public static bool CompareTokens(IDToken tokenA, IDToken tokenB)
            {
            bool equality = (tokenA.identifier == tokenB.identifier) ? (true):(false);

            return(equality);
            }

        }

    }


}
