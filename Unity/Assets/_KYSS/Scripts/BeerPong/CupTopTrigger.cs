using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(Collider))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class CupTopTrigger : UdonSharpBehaviour
{
    public BeerPongGameManager game;
    [Tooltip("Who DEFENDS this cup (their opponent scores here).")]
    public Team defendedBy = Team.Red;
    public int cupIndex = 0;
    public bool debug;

    [Header("Networked Audio on this cup")]
    public NetworkedAudio sfxTopHit;

    private bool _scored;

    private void Start()
    {
        ((Collider)GetComponent(typeof(Collider))).isTrigger = true;
        _scored = false;
    }

    public void SetScored(bool on)
    {
        _scored = on;
        gameObject.SetActive(!on);
        if (debug) Debug.Log("[CupTopTrigger] Cup " + defendedBy + "#" + cupIndex + " scored=" + on);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_scored) return;

        TeamBall ball = other.GetComponent<TeamBall>();
        if (ball == null) ball = other.GetComponentInParent<TeamBall>();
        if (ball == null) { if (debug) Debug.Log("[CupTopTrigger] No TeamBall on " + other.name); return; }

        // Ignore own team's ball (bounce-back)
        if (ball.team == defendedBy) { if (debug) Debug.Log("[CupTopTrigger] Ignoring own-team ball."); return; }

        // Must have center gate this possession
        if (!ball.hasTouchedCenter) { if (debug) Debug.Log("[CupTopTrigger] Not center-armed."); return; }

        // Only the BALL OWNER should score + announce (prevents double fires)
        if (!Networking.IsOwner(ball.gameObject)) return;

        if (sfxTopHit != null) sfxTopHit.PlayForAll();

        if (game) game.RegisterCupScored(defendedBy, cupIndex, other.transform.position);

        // Clear per-ball gate for next possession
        ball.hasTouchedCenter = false;
    }
}
