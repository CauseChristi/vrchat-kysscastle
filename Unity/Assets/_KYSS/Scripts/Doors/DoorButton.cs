using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Simple button to toggle a linked DoorSlide if it is not already moving.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DoorButton : UdonSharpBehaviour
{
    [Header("Linked Door")]
    public DoorSlide linkedDoor;

    [Header("Settings")]
    public bool debugLogs = false;

    public override void Interact()
    {
        if (linkedDoor == null) return;

        if (!linkedDoor.IsMoving)
        {
            linkedDoor.Toggle();

            if (debugLogs)
                Debug.Log("[DoorButton] Toggled door " + linkedDoor.name);
        }
        else
        {
            if (debugLogs)
                Debug.Log("[DoorButton] Door is currently moving, ignoring input.");
        }
    }
}
