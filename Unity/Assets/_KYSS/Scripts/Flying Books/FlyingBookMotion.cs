using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FlyingBookMotion : UdonSharpBehaviour
{
    [Header("Orbit (base)")]
    [Tooltip("Base deg/sec around local Y. Use negative for clockwise.")]
    public float rotationSpeedDegPerSec = 30f;

    [Header("Orbit Modulation")]
    [Tooltip("Enable modulation of rotation speed so orbit isn't perfectly constant.")]
    public bool modulateRotation = true;
    [Tooltip("±variance applied to base rotation from motion/noise. 0.35 = ±35%.")]
    [Range(0f, 1f)] public float rotVariance = 0.35f;
    [Tooltip("Weight of Perlin noise in modulation vs Y-velocity (0=noise off, 1=all noise).")]
    [Range(0f, 1f)] public float rotNoiseWeight = 0.35f;
    [Tooltip("Perlin noise frequency in Hz (how quickly orbit speed meanders).")]
    [Range(0.01f, 1.5f)] public float rotNoiseFreqHz = 0.15f;
    [Tooltip("How snappily the orbit speed follows the target (higher = snappier).")]
    public float rotResponsiveness = 3.0f;
    [Tooltip("Never let the absolute orbit speed fall below this (deg/sec).")]
    public float minOrbitDegPerSec = 5f;

    [Header("Bob on Y (noisy)")]
    public float yAmplitude = 0.75f;
    public float yMoveSpeed = 1.0f;
    [Range(0f, 1.5f)] public float yNoiseJitter = 0.4f;
    public float yMaxPauseSeconds = 1.25f;
    [Range(0f, 1f)] public float yArriveSmoothing = 0.25f;

    [Header("Slide on X (noisy)")]
    public float xAmplitude = 0.5f;
    public float xMoveSpeed = 1.0f;
    [Range(0f, 1.5f)] public float xNoiseJitter = 0.4f;
    public float xMaxPauseSeconds = 1.0f;
    [Range(0f, 1f)] public float xArriveSmoothing = 0.2f;

    [Header("Wing Flap (Animator speed from Y velocity)")]
    public Animator batAnimator;
    public bool useAnimatorFloatParam = false;
    public string animatorSpeedParam = "Speed";
    public float animSpeedIdle = 0.7f;
    public float animSpeedMax = 1.9f;
    public float animResponsiveness = 8f;
    [Range(0f, 0.75f)] public float animVariance = 0.25f;
    [Range(0f, 1f)] public float velocitySmoothing = 0.3f;

    [Header("Misc")]
    public bool randomizeStart = true;
    public bool debugLogs = false;

    // --- runtime state (Y) ---
    private float _targetY = 0f;
    private float _ySegSpeed = 1f;
    private float _yPauseUntil = 0f;

    // --- runtime state (X) ---
    private float _targetX = 0f;
    private float _xSegSpeed = 1f;
    private float _xPauseUntil = 0f;

    // --- animator/flap ---
    private float _prevY = 0f;
    private float _smoothedAbsYVel = 0f;
    private float _animSpeed = 1f;
    private float _animVarianceMul = 1f;
    private float _yVelNormT = 0f; // 0..1 normalized Y speed (stored for orbit mod)

    // --- orbit modulation ---
    private float _currentRotSpeed = 0f; // signed deg/sec actually applied this frame
    private float _perlinSeed = 0f;      // per-instance seed

    private void Start()
    {
        if (batAnimator == null) batAnimator = GetComponentInChildren<Animator>();

        if (randomizeStart)
        {
            transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var p = transform.localPosition;
            if (yAmplitude > 0f) p.y = Random.Range(-yAmplitude, yAmplitude);
            if (xAmplitude > 0f) p.x = Random.Range(-xAmplitude, xAmplitude);
            transform.localPosition = p;
            _yPauseUntil = Time.time + Random.Range(0f, yMaxPauseSeconds);
            _xPauseUntil = Time.time + Random.Range(0f, xMaxPauseSeconds);
        }

        _prevY = transform.localPosition.y;
        _animVarianceMul = 1f + (animVariance <= 0f ? 0f : Random.Range(-animVariance, animVariance));
        _animSpeed = Mathf.Clamp(animSpeedIdle * _animVarianceMul, 0.05f, 5f);
        ApplyAnimatorSpeed(_animSpeed);

        // per-instance noise seed
        _perlinSeed = Random.Range(0.0f, 1000.0f);
        _currentRotSpeed = rotationSpeedDegPerSec;

        PickNextY();
        PickNextX();
    }

    private void Update()
    {
        var pos = transform.localPosition;

        // (1) Y bob
        if (yAmplitude > 0f && Time.time >= _yPauseUntil)
        {
            float dy = _targetY - pos.y;
            float baseStep = _ySegSpeed * Time.deltaTime;
            float distanceFactor = Mathf.Clamp01(Mathf.Abs(dy));
            float smoothFactor = Mathf.Lerp(1f, 0.25f, yArriveSmoothing);
            float step = baseStep * (0.5f + distanceFactor) * smoothFactor;

            if (Mathf.Abs(dy) <= step)
            {
                pos.y = _targetY;
                float pause = (yMaxPauseSeconds <= 0f) ? 0f : Random.Range(0f, yMaxPauseSeconds);
                _yPauseUntil = Time.time + pause;
                PickNextY();
            }
            else pos.y += Mathf.Sign(dy) * step;
        }

        // (2) X slide
        if (xAmplitude > 0f && Time.time >= _xPauseUntil)
        {
            float dx = _targetX - pos.x;
            float baseStep = _xSegSpeed * Time.deltaTime;
            float distanceFactor = Mathf.Clamp01(Mathf.Abs(dx));
            float smoothFactor = Mathf.Lerp(1f, 0.25f, xArriveSmoothing);
            float step = baseStep * (0.5f + distanceFactor) * smoothFactor;

            if (Mathf.Abs(dx) <= step)
            {
                pos.x = _targetX;
                float pause = (xMaxPauseSeconds <= 0f) ? 0f : Random.Range(0f, xMaxPauseSeconds);
                _xPauseUntil = Time.time + pause;
                PickNextX();
            }
            else pos.x += Mathf.Sign(dx) * step;
        }

        transform.localPosition = pos;

        // (3) Flap speed from Y velocity (also stores _yVelNormT for orbit modulation)
        UpdateAnimatorFromYVelocity(pos.y);

        // (4) Orbit rotation with optional modulation
        float signedBase = rotationSpeedDegPerSec;
        if (modulateRotation)
        {
            // Normalize Y velocity to [-1,1] around center
            float yCentered = (_yVelNormT - 0.5f) * 2f; // -1..1

            // Perlin noise in [-1,1]
            float t = Time.time * rotNoiseFreqHz;
            float noise = Mathf.PerlinNoise(_perlinSeed, t) * 2f - 1f;

            // Blend Y-velocity influence with slow noise
            float blend = Mathf.Clamp(yCentered * (1f - rotNoiseWeight) + noise * rotNoiseWeight, -1f, 1f);

            // Target multiplier in [1 - var .. 1 + var]
            float targetMul = 1f + rotVariance * blend;

            // Preserve sign of base speed; apply smoothing to magnitude
            float targetSpeed = Mathf.Sign(signedBase) * Mathf.Max(minOrbitDegPerSec, Mathf.Abs(signedBase * targetMul));

            // Exponential smoothing toward target
            float dt = Time.deltaTime;
            float a = 1f - Mathf.Exp(-Mathf.Max(0f, rotResponsiveness) * dt);
            _currentRotSpeed = Mathf.Lerp(_currentRotSpeed, targetSpeed, a);
        }
        else
        {
            _currentRotSpeed = signedBase;
        }

        if (_currentRotSpeed != 0f)
            transform.Rotate(0f, _currentRotSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void UpdateAnimatorFromYVelocity(float currentY)
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float absVel = Mathf.Abs((currentY - _prevY) / dt);
        _smoothedAbsYVel = Mathf.Lerp(_smoothedAbsYVel, absVel, 1f - Mathf.Clamp01(velocitySmoothing));

        float refSpeed = Mathf.Max(0.001f, yMoveSpeed * 1.5f);
        _yVelNormT = Mathf.Clamp01(_smoothedAbsYVel / refSpeed); // store for orbit modulation

        float target = Mathf.Lerp(animSpeedIdle, animSpeedMax, _yVelNormT) * _animVarianceMul;

        float k = Mathf.Max(0f, animResponsiveness);
        float a = 1f - Mathf.Exp(-k * dt);
        _animSpeed = Mathf.Lerp(_animSpeed, target, a);
        ApplyAnimatorSpeed(_animSpeed);

        _prevY = currentY;
    }

    private void ApplyAnimatorSpeed(float s)
    {
        if (batAnimator == null) return;
        float clamped = Mathf.Clamp(s, 0.05f, 5f);
        if (useAnimatorFloatParam) batAnimator.SetFloat(animatorSpeedParam, clamped);
        else batAnimator.speed = clamped;
    }

    private void PickNextY()
    {
        float baseTarget = (yAmplitude <= 0f) ? 0f : Random.Range(-yAmplitude, yAmplitude);
        float jitterScale = 1f + (yNoiseJitter <= 0f ? 0f : Random.Range(-yNoiseJitter, yNoiseJitter));
        _targetY = Mathf.Clamp(baseTarget * jitterScale, -yAmplitude, yAmplitude);

        float speedJitter = 1f + (yNoiseJitter <= 0f ? 0f : Random.Range(-yNoiseJitter, yNoiseJitter));
        _ySegSpeed = Mathf.Max(0.2f * yMoveSpeed, yMoveSpeed * speedJitter);

        if (debugLogs) Debug.Log($"[Bat] Y→ {_targetY:F2} @ {_ySegSpeed:F2}");
    }

    private void PickNextX()
    {
        float baseTarget = (xAmplitude <= 0f) ? 0f : Random.Range(-xAmplitude, xAmplitude);
        float jitterScale = 1f + (xNoiseJitter <= 0f ? 0f : Random.Range(-xNoiseJitter, xNoiseJitter));
        _targetX = Mathf.Clamp(baseTarget * jitterScale, -xAmplitude, xAmplitude);

        float speedJitter = 1f + (xNoiseJitter <= 0f ? 0f : Random.Range(-xNoiseJitter, xNoiseJitter));
        _xSegSpeed = Mathf.Max(0.2f * xMoveSpeed, xMoveSpeed * speedJitter);

        if (debugLogs) Debug.Log($"[Bat] X→ {_targetX:F2} @ {_xSegSpeed:F2}");
    }

    // Optional runtime hooks
    public void SetRotationSpeed(float degPerSec) { rotationSpeedDegPerSec = degPerSec; }
    public void SetYAmplitude(float amp) { yAmplitude = Mathf.Max(0f, amp); }
    public void SetXAmplitude(float amp) { xAmplitude = Mathf.Max(0f, amp); }
    public void NudgeNow() { PickNextY(); _yPauseUntil = 0f; PickNextX(); _xPauseUntil = 0f; }
}
