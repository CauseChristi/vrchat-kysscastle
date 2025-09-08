using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AdjustableMicStand : UdonSharpBehaviour
{
    [Header("Mic Stand Components")]
    public Transform micStandGrip;
    public Transform micStandTopHalf;

    [Header("Positional Limits")]
    public Transform upperLimit;
    public Transform lowerLimit;

    // Grip position and rotation cache
    private Quaternion micStandGripRotation;
    private Vector3 micStandTopHalfOffset;

    void Start()
    {
        // Cache position and rotation of grip
        micStandGripRotation = micStandGrip.rotation;

        // Cache position of top half of mic stand and calculate offset
        micStandTopHalfOffset = micStandTopHalf.position - micStandGrip.position;
    }

    void Update()
    {
        // Ensure grip stays within positional limits
        micStandGrip.SetPositionAndRotation(new Vector3(
            Mathf.Clamp(micStandGrip.position.x, lowerLimit.position.x, upperLimit.position.x),
            Mathf.Clamp(micStandGrip.position.y, lowerLimit.position.y, upperLimit.position.y),
            Mathf.Clamp(micStandGrip.position.z, lowerLimit.position.z, upperLimit.position.z)
        ), micStandGripRotation);

        // Set top half of mic stand to grip position plus offset
        micStandTopHalf.position = micStandGrip.position + micStandTopHalfOffset;
    }
}