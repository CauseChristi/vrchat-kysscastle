// LocalBillboard.cs
// UdonSharp (VRChat SDK3 Worlds)
// Keeps this object facing (and optionally following) the LOCAL player's camera only.
// Editor testing: assign 'editorFallback' to any Transform (e.g., a scene camera).

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LocalBillboard : UdonSharpBehaviour
{
    [Header("Billboard Mode")]
    [Tooltip("Rotate this object to face the camera.")]
    public bool faceCamera = true;

    [Tooltip("If true, only rotate around global Y (keeps the object upright).")]
    public bool keepUprightYOnly = true;

    [Header("Follow (Position)")]
    [Tooltip("Also move this object relative to the camera.")]
    public bool followPosition = false;

    [Tooltip("Local-space offset from the camera (x=right, y=up, z=forward).")]
    public Vector3 cameraLocalOffset = new Vector3(0f, -0.05f, 0.5f);

    [Tooltip("If > 0, forces a fixed distance in front of the camera.")]
    public float fixedDistance = 0f;

    [Header("Smoothing")]
    [Tooltip("0 = instant. Higher values smooth more. Try 12–18.")]
    public float smoothSpeed = 0f;

    [Header("Update")]
    [Tooltip("If true, updates in LateUpdate (after animations).")]
    public bool useLateUpdate = true;

    [Header("Editor Fallback (no networking)")]
    [Tooltip("Optional Transform used for editor testing when not in VRChat runtime.")]
    public Transform editorFallback;

    // Internals
    private VRCPlayerApi _local;
    private Transform _t;

    private void Start()
    {
        _t = transform;
        _local = Networking.LocalPlayer;
    }

    private void Update()
    {
        if (!useLateUpdate) ApplyBillboard();
    }

    private void LateUpdate()
    {
        if (useLateUpdate) ApplyBillboard();
    }

    private void ApplyBillboard()
    {
        // Determine camera pose
        Vector3 camPos;
        Quaternion camRot;

        if (_local != null)
        {
            // VRChat runtime
            var head = _local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            camPos = head.position;
            camRot = head.rotation;
        }
        else if (editorFallback != null)
        {
            // Editor/testing fallback (no forbidden APIs)
            camPos = editorFallback.position;
            camRot = editorFallback.rotation;
        }
        else
        {
            // Nothing to track
            return;
        }

        // --- Target position ---
        Vector3 targetPos = _t.position;
        if (followPosition)
        {
            Vector3 offset = cameraLocalOffset;
            if (fixedDistance > 0f) offset.z = fixedDistance;
            targetPos = camPos + (camRot * offset);
        }

        // --- Target rotation ---
        Quaternion targetRot = _t.rotation;
        if (faceCamera)
        {
            if (keepUprightYOnly)
            {
                Vector3 toCam = camPos - _t.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude < 0.0001f)
                {
                    toCam = camRot * Vector3.forward;
                    toCam.y = 0f;
                }
                if (toCam.sqrMagnitude > 0f)
                {
                    toCam.Normalize();
                    targetRot = Quaternion.LookRotation(toCam, Vector3.up);
                }
            }
            else
            {
                Vector3 lookDir = camPos - _t.position;
                if (lookDir.sqrMagnitude > 0f)
                {
                    lookDir.Normalize();
                    targetRot = Quaternion.LookRotation(lookDir, camRot * Vector3.up);
                }
            }
        }

        // --- Apply (optionally smoothed) ---
        if (smoothSpeed > 0f)
        {
            float t = Time.deltaTime * smoothSpeed;
            if (followPosition) _t.position = Vector3.Lerp(_t.position, targetPos, t);
            if (faceCamera)     _t.rotation = Quaternion.Slerp(_t.rotation, targetRot, t);
        }
        else
        {
            if (followPosition) _t.position = targetPos;
            if (faceCamera)     _t.rotation = targetRot;
        }
    }
}
