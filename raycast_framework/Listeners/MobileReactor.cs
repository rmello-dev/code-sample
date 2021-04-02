using UnityEngine;

namespace Game.Api.Raycast
{

/// <summary>
/// Base class for mobile sprite-based raycast-collision reactors.
/// </summary>
public abstract class MobileReactor : FixedReactor
    {
    // --- Fields:

    private Rigidbody2D kinematicArea = null;     // physics body, unaffected by forces
    private bool kinematic = false;               // physicality flag

    // --- Initialization:
    
    protected override void setupReactionArea(bool activate)
        {
        base.setupReactionArea(activate);   // sprite collision

        if (activate && !kinematic)         // create physics body
           {
           kinematicArea = gameObject.AddComponent<Rigidbody2D>();
           kinematicArea.isKinematic = true;

           kinematic = true;
           }

        if (!activate && kinematic)         // remove physics body
           { 
           Destroy(kinematicArea);
           
           kinematic = false;
           }
        }
        
    // --- External Methods:

        // TODO move? adjustKinematicArea?

    }


}
