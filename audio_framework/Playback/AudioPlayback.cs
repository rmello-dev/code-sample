using UnityEngine;

using Game.Lib.Space;

namespace Game.Api.Playback
{

/// <summary>
/// Provides utility methods and operation parameters for audio playback.
/// </summary>
public static class AudioPlayback
    {
    // --- Parameter Definitions:

    /// <summary>
    /// Enumeration of audio playback behavior groups.
    /// </summary>
    public enum Group : byte
        {
        aux,        // interface feedback - sfx volume soft - handled by sfx mixer - high priority
        env,        // world environment - sfx volume slider - handled by sfx mixer - low priority
        bgm,        // background music - bgm volume slider - handled by bgm mixer - top priority
        }

    /// <summary>
    /// Audio attenuation parameter data structure.
    /// </summary>
    public readonly struct Range
        {
        // --- Inner Members:

        /// <summary>
        /// Audio attenuation modes - range evaluation type.
        /// </summary>
        public enum RMode : byte
            {
            Area,       // non-point source, cut-off zones only             (ambient sfx)      
            Grid,       // grid-point source, linear distance attenuation   (local sfx)
            }

        /// <summary>
        /// Audio attenuation zones - area cut-offs.
        /// </summary>
        public enum RZone : byte
            {
            Undeground,     // cut-off if Z>0 (overground)         // TODO differentiate depth zone-biomes?
            Exterior,       // cut-off if Z<0 (undeground)          ~ surface weather, season ambiance, etc

            Omni,           // no zone cut-off                      ~ local sfx, bgm, gui, etc
            }

        // --- Properties:

        public RMode Mode { get; }      // range evaluation mode (zone/linear)

        public RZone Zone { get; }      // Zone: undeground or exterior zone

        public byte xyMax { get; }      // Linear: XY source-listener cut-off distance
        public byte zMax { get; }       // Linear: Z-level source-listener cut-off distance

        // --- Initialization:    

        /// <summary>
        /// Defines area-based audio sources, with a zoned cut-off range.
        /// </summary>
        public Range(RZone zone)
            {
            Mode = RMode.Area;
            Zone = zone;
    
            xyMax = zMax = byte.MaxValue;   // TODO byte max not enough for large maps, use 0 / treat as special case?
            }

        /// <summary>
        /// Defines point-like audio sources, with linear attenuation range.
        /// Sources with range 0 will not be heard in adjacent spaces.
        /// </summary>
        public Range(byte xyRange, byte zRange = 1)
            {
            Mode = RMode.Grid;    
            Zone = RZone.Omni;        

            xyMax = xyRange;
            zMax = zRange;
            }  

        }

    /// <summary>
    /// Enumeration of playback strategies for active sources.
    /// </summary>
    public enum Strategy : byte
        {
        none = 0,   // undefined - default - error

        mock = 1,   // virtual playback: no spatial source & audio channel required
        live = 2,   // actual playback: actual spatial source & free audio channel required
        }

    // --- Utility Methods:

    #region Strategy Factory
    
    /// <summary>
    /// Creates instance of playback controller for the given strategy.
    /// </summary>
    public static PlaybackController ImplementStrategy(Strategy strategy)
        {
        PlaybackController concreteImplementation;

        switch(strategy)
            {
            default:
            case(Strategy.mock): concreteImplementation = new MockSource(); break;
            case(Strategy.live): concreteImplementation = new LiveSource(); break;
            }

        return(concreteImplementation);
        }

    #endregion

    #region Playback Spatialization

    /// <summary>
    /// Determines volume attenuation multiplier for audio source, based on range, bearing, and listener location.
    /// Multiplier value is between 1 (full volume) and 0 (cutt-off).
    /// </summary>
    public static float CalculateAttenuator(Range sourceRange, GridPoint sourceBearing)
        {
        float attenuator = 0f;                                                          // default case: silent source 

        // get listener parameters
        
        ListenerData listener = MasterController.Audio.Listener;

        // calculate ranged attenuation
    
        if (sourceRange.Mode == Range.RMode.Area)                                       // calculate zone attenuation mode
            {
            // unrestricted zone
        
            if (sourceRange.Zone == Range.RZone.Omni) { attenuator = 1.0f; }            // omni effects, full volume
            else {
                sbyte surfaceLevel = listener.SurfaceLevel;                             // surface level at listening point
                sbyte absoluteDepth = listener.Location.Z;                              // indicates surface/underground
                int relativeDepth = surfaceLevel - absoluteDepth;                       // indicates interior/exterior

                // surface/exterior zone
        
                if (absoluteDepth >= -2 && sourceRange.Zone == Range.RZone.Exterior)    // guarantee near surface
                   {
                   switch(absoluteDepth)
                         {
                         case(-2): attenuator = 0.2f; break;                            // -2, near adjacent, faint volume
                         case(-1): attenuator = 0.4f; break;                            // -1, adjacent zone, reduced volume

                         default: attenuator = 1.0f; break;                             // 0+, surface zone, full volume 
                         }

                   if (relativeDepth > 0) { attenuator *= 0.4f; }                       // surface interior modifier, muffled volume
                   }
            
                // undeground/interior zone
        
                if (absoluteDepth < 0 && sourceRange.Zone == Range.RZone.Undeground)    // guarantee underground
                   {
                   if (relativeDepth > 0)                                               // guarantee interior
                      {
                      switch(relativeDepth)
                            {
                            case(1):
                            case(2): attenuator = 0.2f; break;                          // 1-2, shallow zone, muffled volume

                            case(3):
                            case(4):
                            case(5): attenuator = 0.4f; break;                          // 3-5, middle zone, soft volume

                            default: attenuator = 1.0f; break;                          // 6+, deep zone, full volume
                            }
                      }
                   }
                }
            }
    
        if (sourceRange.Mode == Range.RMode.Grid)                                       // calculate spatial grid attenuation mode
           {
           GridPoint listenerBearing = listener.Location;
           
           Vector2Int sourcePos = new Vector2Int(sourceBearing.X,sourceBearing.Y);
           Vector2Int listenerPos = new Vector2Int(listenerBearing.X,listenerBearing.Z);

           // XY component
    
           float xyGap = Vector2Int.Distance(sourcePos,listenerPos);                    // planar distance between source/listener

           float xyLoss = 1/(sourceRange.xyMax+1);                                      // linear attenuation per spatial unit beyond initial
           
           float xyAttenuation = 1 - Mathf.Clamp01((xyGap*xyLoss));                     // complement for linear attenuation
           
           // Z component

           int zGap = Mathf.Abs(listenerBearing.Z - sourceBearing.Z);                   // Z levels between source/listener
    
           float zLoss = 1/(sourceRange.zMax+1);                                        // linear attenuation per level beyond initial
               
           float zAttenuation = 1 - Mathf.Clamp01((zGap*zLoss));                        // complement for linear attenuation

           // XYZ: total spatial attenuation

           attenuator = 1f * xyAttenuation * zAttenuation;
           }

        if (attenuator < 0.1f) { attenuator = 0f; }                                     // approximates too faint sources as silent

        return(attenuator);
        }

    #endregion

    #region Playback Priority

    /// <summary>
    /// Determines audio source priority from current playback parameters.
    /// </summary>
    public static uint AssignPriority(Range range, bool effectiveRange, bool silent)
        {
        // PRIORITY CLASSES
        // 100: omni essentials (bgm, gui)
        // 200: near local
        // 300: zoned ambient
        // 400: far local
        // 500: cut-off ambient
        // 600: silent sources (pause/mute)
        // 999: default

        uint priorityScore = 999;
        
        if (silent) { priorityScore = 600; }
        else {
             if (effectiveRange)
                {
                if (range.Mode == Range.RMode.Grid) { priorityScore = 200; }
                else {
                     if (range.Zone == Range.RZone.Omni) { priorityScore = 100; }
                     else { priorityScore = 300; }
                     }
                }

             else {
                  if (range.Mode == Range.RMode.Area) { priorityScore = 500; }
                  else { priorityScore = 400; }
                  }
             }
             
        return(priorityScore);
        }

    /// <summary>
    /// Compares playback sources and indicates priority ordering.
    /// Supports sorting lists ordered from high-to-low priority.
    /// </summary>
    public static int OrderPriority(ActiveSource a, ActiveSource b)
        {
        // order: low-to-high values == high-to-low priority list

        return(-1*comparePriority(a.Controller.Priority,b.Controller.Priority));
        }

    /// <summary>
    /// Flags order of priority values.
    /// Returns 0 if A==B, positive if A is higher priority, negative if B is higher priority.
    /// </summary>
    private static int comparePriority(uint priorityA, uint priorityB)
        {
        // compare: higher value == lower priority

        return((int)priorityB - (int)priorityA);
        }

    #endregion

    }


}
