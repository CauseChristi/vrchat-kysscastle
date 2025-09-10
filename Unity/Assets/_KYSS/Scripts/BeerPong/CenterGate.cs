using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(Collider))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class CenterGate : UdonSharpBehaviour
{
    public bool debug;

    private void Start()
    {
        ((Collider)GetComponent(typeof(Collider))).isTrigger = true;
    }

    public void ResetGate() { } // no-op (per-ball gate lives on TeamBall)

    private void OnTriggerEnter(Collider other)
    {
        TeamBall ball = other.GetComponent<TeamBall>();
        if (ball == null) ball = other.GetComponentInParent<TeamBall>();
        if (ball == null) { if (debug) Debug.Log("[CenterGate] No TeamBall on " + other.name); return; }

        // Only the BALL OWNER should set/announce center success
        if (!Networking.IsOwner(ball.gameObject)) return;

        ball.CenterSuccessLocal(); // sets hasTouchedCenter + plays SFX to All
        if (debug) Debug.Log("[CenterGate] Center success for " + ball.team + " ball.");
    }
}
