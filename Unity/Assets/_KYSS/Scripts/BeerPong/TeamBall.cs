using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(VRC_Pickup))]
[RequireComponent(typeof(Rigidbody))]
public class TeamBall : UdonSharpBehaviour
{
    public BeerPongGameManager game;
    public Team team = Team.Red;

    public CenterGate centerGate; // (optional) if you want direct reset calls too

    private VRC_Pickup _pickup;
    private Rigidbody _rb;

    [HideInInspector] public bool hasTouchedCenter; // gate flag for scoring
    private bool _enabledForTurn;

    private void Start()
    {
        _pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        _rb = (Rigidbody)GetComponent(typeof(Rigidbody));
        SetBallEnabled(false);
    }

    public void SetBallEnabled(bool on)
    {
        _enabledForTurn = on;
        gameObject.SetActive(on);
        if (!on) hasTouchedCenter = false;
    }

    public override void OnPickup()
    {
        var p = Networking.LocalPlayer;
        // Only allow pickup if it's our team's turn AND this is our assigned player
        if (game && !game.CanPickupBall(p, team))
        {
            // Not your turn → drop immediately
            if (_pickup != null) _pickup.Drop();
            return;
        }
        if (game) game.OnBallPickup(team, transform.position);
    }

    public override void OnDrop()
    {
        // no-op
    }

    private void OnCollisionEnter(Collision c)
    {
        if (c != null && game != null)
        {
            game.OnBallBounce(c.GetContact(0).point);
        }
    }

    // Called by your existing respawn script when it returns the ball
    public void OnBallRespawned()
    {
        // Clear the center gate requirement for this possession
        hasTouchedCenter = false;
        if (centerGate != null) centerGate.ResetGate();
    }
}
