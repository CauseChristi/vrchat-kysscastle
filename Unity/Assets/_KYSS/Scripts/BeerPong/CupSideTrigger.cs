using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(Collider))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class CupSideTrigger : UdonSharpBehaviour
{
    public Team defendedBy = Team.Red; // for ignoring own-ball if you want parity
    public NetworkedAudio sfxSideHit;
    public bool debug;

    private void Start()
    {
        ((Collider)GetComponent(typeof(Collider))).isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TeamBall ball = other.GetComponent<TeamBall>();
        if (ball == null) ball = other.GetComponentInParent<TeamBall>();
        if (ball == null) return;

        if (ball.team == defendedBy) return; // ignore own-team side hits if desired

        // Only ball owner announces
        if (!Networking.IsOwner(ball.gameObject)) return;

        if (sfxSideHit != null) sfxSideHit.PlayForAll();
    }
}
