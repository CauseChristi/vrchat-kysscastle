// LocalBillboard.cs
// UdonSharp (VRChat SDK3 Worlds)
// Faces the local camera by rotating ONLY around Y (no pitch/roll).
// Uses parent space to avoid weird tilts when the parent is rotated.
// Editor testing: assign 'editorFallback' to any Transform.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LocalBillboard : UdonSharpBehaviour
{
    [Header("Follow (Position)")]
    public bool followPosition = false;
    public Vector3 cameraLocalOffset = new Vector3(0f, -0.05f, 0.5f);
    public float fixedDistance = 0f;

    [Header("Rotation")]
    [Tooltip("If true, compute yaw in parent space. If false, use world space.")]
    public bool yawInParentSpace = true;

    [Tooltip("Add a constant yaw offset if your mesh's forward isn't +Z.")]
    public float yawOffsetDegrees = 0f;

    [Header("Smoothing")]
    [Tooltip("0 = instant; try 12–18 for gentle smoothing.")]
    public float smoothSpeed = 0f;

    [Header("Update")]
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
        if (!useLateUpdate) Tick();
    }

    private void LateUpdate()
    {
        if (useLateUpdate) Tick();
    }

    private void Tick()
    {
        // --- Get camera pose (local-only) ---
        Vector3 camPos;
        Quaternion camRot;

        if (_local != null)
        {
            var head = _local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            camPos = head.position;
            camRot = head.rotation;
        }
        else if (editorFallback != null)
        {
            camPos = editorFallback.position;
            camRot = editorFallback.rotation;
        }
        else
        {
            return;
        }

        // --- Optional position follow (world space) ---
        if (followPosition)
        {
            Vector3 offset = cameraLocalOffset;
            if (fixedDistance > 0f) offset.z = fixedDistance;
            Vector3 targetPos = camPos + (camRot * offset);

            if (smoothSpeed > 0f)
                _t.position = Vector3.Lerp(_t.position, targetPos, Time.deltaTime * smoothSpeed);
            else
                _t.position = targetPos;
        }

        // --- YAW ONLY rotation ---
        if (yawInParentSpace && _t.parent != null)
        {
            // Work entirely in parent space so only local Y changes.
            Vector3 camLocal = _t.parent.InverseTransformPoint(camPos);
            Vector3 myLocal = _t.localPosition;
            Vector3 toCamLocal = camLocal - myLocal;
            toCamLocal.y = 0f;

            if (toCamLocal.sqrMagnitude > 1e-8f)
            {
                float yaw = Mathf.Atan2(toCamLocal.x, toCamLocal.z) * Mathf.Rad2Deg + yawOffsetDegrees;
                Quaternion desiredLocal = Quaternion.Euler(0f, yaw, 0f);

                if (smoothSpeed > 0f)
                    _t.localRotation = Quaternion.Slerp(_t.localRotation, desiredLocal, Time.deltaTime * smoothSpeed);
                else
                    _t.localRotation = desiredLocal;
            }
        }
        else
        {
            // No parent (or forced world-space): make world yaw only.
            Vector3 toCam = camPos - _t.position;
            toCam.y = 0f;

            if (toCam.sqrMagnitude > 1e-8f)
            {
                float yaw = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg + yawOffsetDegrees;
                Quaternion desiredWorld = Quaternion.Euler(0f, yaw, 0f);

                if (smoothSpeed > 0f)
                    _t.rotation = Quaternion.Slerp(_t.rotation, desiredWorld, Time.deltaTime * smoothSpeed);
                else
                    _t.rotation = desiredWorld;
            }
        }

        // Hard guarantee: zero out any stray tilt from hierarchy math (cheap, safe).
        Vector3 e = _t.localEulerAngles;
        _t.localEulerAngles = new Vector3(0f, e.y, 0f);
    }
}
