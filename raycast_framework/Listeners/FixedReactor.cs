using UnityEngine;

using System.Collections.Generic;

namespace Game.Api.Raycast
{

/// <summary>
/// Base class for stationary sprite-based raycast-collision reactors.
/// </summary>
public abstract class FixedReactor : BaseReactor
    {
    // --- Fields:

    private bool raycastActive = false;                 // raycast collisions flag
    private PolygonCollider2D raycastArea = null;       // collider component for raycast 
    
    // --- Initialization:
    
    /// <summary>
    /// Adds or removes raycast collider component.
    /// </summary>
    protected override void setupReactionArea(bool activate)
        {
        // BUG: collision area is duplicated on wrong camera-layer
        // FIX: collision raycasts only target specific layers
        // TODO: find why collider physics are being duplicated

        if (activate && !raycastActive)     // create collider
           {
           raycastArea = gameObject.AddComponent<PolygonCollider2D>();
           raycastArea.isTrigger = false;

           raycastActive = true;
           }

        if (!activate && raycastActive)     // remove collider
           {
           Destroy(raycastArea);

           raycastActive = false;
           }
        }

    // --- External Methods:

    /// <summary>
    /// Forces update to raycast area boundaries to fit sprite image.
    /// </summary>
    public void AdjustReactionArea(Sprite shapeTemplate)
        {
        // TODO make work with null sprite (no area)
        // BUG make work with empty-tile (tile area)
        
        if (shapeTemplate != null)
           {
           // clear shape

           int shapeCount = shapeTemplate.GetPhysicsShapeCount();    // BUG: sprite import-settings require "Generate Physics Shape" enabled

           List<Vector2> path = new List<Vector2>(shapeCount); 

           for (int index = 0; index < raycastArea.pathCount; index++) { raycastArea.SetPath(index,path); }

           // update shape
 
           raycastArea.pathCount = shapeCount;
 
           for (int index = 0; index < raycastArea.pathCount; index++)
               {
               path.Clear();
               shapeTemplate.GetPhysicsShape(index,path);
               raycastArea.SetPath(index,path.ToArray());
               }
           }
        }

    }


}
