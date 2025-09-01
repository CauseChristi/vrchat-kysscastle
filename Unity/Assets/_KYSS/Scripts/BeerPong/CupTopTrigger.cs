// CupTopTrigger.cs
using UdonSharp;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CupTopTrigger : UdonSharpBehaviour
{
    public BeerPongGameManager game;
    [Tooltip("Who DEFENDS this cup (their opponent scores here).")]
    public Team defendedBy = Team.Red;
    public int cupIndex = 0;
    public bool debug;

    private bool _scored;

    private void Start()
    {
        ((Collider)GetComponent(typeof(Collider))).isTrigger = true;
        _scored = false;
    }

    public void SetScored(bool on)
    {
        _scored = on;
        gameObject.SetActive(!on); // disable this trigger once scored
        if (debug) Debug.Log("[CupTopTrigger] Cup " + defendedBy + "#" + cupIndex + " scored=" + on);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_scored) { if (debug) Debug.Log("[CupTopTrigger] Already scored; ignoring."); return; }

        // Find the TeamBall (support child colliders)
        TeamBall ball = other.GetComponent<TeamBall>();
        if (ball == null) ball = other.GetComponentInParent<TeamBall>();
        if (ball == null) { if (debug) Debug.Log("[CupTopTrigger] No TeamBall on " + other.name); return; }

        // Ignore our own team’s ball (prevents bounce-back self-light)
        if (ball.team == defendedBy) { if (debug) Debug.Log("[CupTopTrigger] Ignoring own-team ball: " + ball.team); return; }

        // Require center hit this possession
        if (!ball.hasTouchedCenter) { if (debug) Debug.Log("[CupTopTrigger] Ball not center-armed yet."); return; }

        // Valid: report to manager -> it will light the cup & update masks/scores
        if (game) game.RegisterCupScored(defendedBy, cupIndex, other.transform.position);

        // Clear the per-ball gate for the next possession
        ball.hasTouchedCenter = false;
    }
}
