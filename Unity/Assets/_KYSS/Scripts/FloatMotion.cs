// FloatMotion.cs
// Local-space sine bob + optional small rotation around the object's LOCAL origin.
// Starts automatically; no networking. Keeps most external velocity if a Rigidbody is present.
//
// Usage: add to the SAME GameObject you want to float. Optional Rigidbody (Dynamic) recommended.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class FloatMotion : UdonSharpBehaviour
{
    [Header("Easing at Start")]
    [Tooltip("Seconds to ramp from 0 → full motion at startup.")]
    public float easeInSeconds = 0.4f;

    [Header("Local Position Sine (meters & Hz)")]
    public float distanceX = 0.10f;
    public float distanceY = 0.05f;
    public float distanceZ = 0.10f;
    public float speedX = 0.6f; // Hz
    public float speedY = 0.8f; // Hz
    public float speedZ = 0.6f; // Hz

    [Header("Local Rotation Sine (degrees & Hz)")]
    public float rotDistanceX = 0.0f;
    public float rotDistanceY = 2.0f;
    public float rotDistanceZ = 0.0f;
    public float rotSpeedX = 0.3f; // Hz
    public float rotSpeedY = 0.3f; // Hz
    public float rotSpeedZ = 0.3f; // Hz

    [Header("Velocity Preservation (if Rigidbody present)")]
    [Range(0f, 1f)]
    public float preserveVelocityFactor = 0.85f;

    [Header("Debug")]
    public bool debugLogs = false;

    // Internals
    private Rigidbody _rb;          // optional
    private Vector3 _localPos0;     // local origin
    private Quaternion _localRot0;  // local origin rotation
    private float _t;
    private float _easeT;

    private const float TWO_PI = 6.28318530718f;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();

        // Capture LOCAL origin (relative to parent)
        _localPos0 = transform.localPosition;
        _localRot0 = transform.localRotation;

        _t = 0f;
        _easeT = 0f;

        if (_rb != null && _rb.isKinematic && debugLogs)
            Debug.Log("[FloatMotionLocalUdon] Rigidbody is kinematic; external pushes won't be preserved.");
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        _t += dt;

        // Smooth start
        if (easeInSeconds > 0f) _easeT = Mathf.Clamp(_easeT + dt, 0f, easeInSeconds);
        else _easeT = 1f;

        float a = easeInSeconds > 0f ? (_easeT / easeInSeconds) : 1f;
        float ease = a * a * (3f - 2f * a); // smoothstep

        // Sine offsets (LOCAL space)
        float dx = Mathf.Sin(_t * speedX * TWO_PI) * distanceX * ease;
        float dy = Mathf.Sin(_t * speedY * TWO_PI) * distanceY * ease;
        float dz = Mathf.Sin(_t * speedZ * TWO_PI) * distanceZ * ease;
        Vector3 targetLocalPos = _localPos0 + new Vector3(dx, dy, dz);

        // Small local rotation offset
        Quaternion targetLocalRot = _localRot0;
        if (rotDistanceX != 0f || rotDistanceY != 0f || rotDistanceZ != 0f)
        {
            float rx = Mathf.Sin(_t * rotSpeedX * TWO_PI) * rotDistanceX * ease;
            float ry = Mathf.Sin(_t * rotSpeedY * TWO_PI) * rotDistanceY * ease;
            float rz = Mathf.Sin(_t * rotSpeedZ * TWO_PI) * rotDistanceZ * ease;
            targetLocalRot = _localRot0 * Quaternion.Euler(rx, ry, rz);
        }

        // Preserve current world velocities (if RB), clamp LOCAL pose, restore most of velocity
        Vector3 vel = Vector3.zero;
        Vector3 ang = Vector3.zero;
        if (_rb != null)
        {
            vel = _rb.velocity;
            ang = _rb.angularVelocity;
        }

        transform.localPosition = targetLocalPos;
        transform.localRotation = targetLocalRot;

        if (_rb != null)
        {
            float keep = Mathf.Clamp01(preserveVelocityFactor);
            _rb.velocity = vel * keep;
            _rb.angularVelocity = ang * keep;
            _rb.WakeUp();
        }

        if (debugLogs && (Time.frameCount % 120 == 0))
        {
            Vector3 p = transform.localPosition;
            Debug.Log($"[FloatMotionLocalUdon] {gameObject.name} local pos {p.x:F3}, {p.y:F3}, {p.z:F3}");
        }
    }

    // Re-center to current LOCAL transform (optional helper)
    public void ResetLocalOrigin()
    {
        _localPos0 = transform.localPosition;
        _localRot0 = transform.localRotation;
        _t = 0f;
        _easeT = 0f;
    }
}
