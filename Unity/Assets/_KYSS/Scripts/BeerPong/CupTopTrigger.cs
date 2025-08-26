using UdonSharp;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CupTopTrigger : UdonSharpBehaviour
{
    public BeerPongGameManager game;
    public Team defendedBy = Team.Red; // whose cup is this? (Red cups → Blue scores here)
    public int cupIndex = 0;

    private bool _scored; // disables scoring after first success

    private void Start()
    {
        var col = (Collider)GetComponent(typeof(Collider));
        col.isTrigger = true;
        _scored = false;
    }

    public void SetScored(bool on)
    {
        _scored = on;
        gameObject.SetActive(!on); // disable trigger when scored
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_scored) return;

        // ❌ old (not allowed): var ball = (TeamBall)other.GetComponent(typeof(TeamBall));
        TeamBall ball = other.GetComponent<TeamBall>();   // ✅ use generic form
        if (ball == null) return;

        if (!ball.hasTouchedCenter) return; // gate not met; no score

        if (game) game.RegisterCupScored(defendedBy, cupIndex, other.transform.position);

        // Clear for next possession
        ball.hasTouchedCenter = false;
    }
}
