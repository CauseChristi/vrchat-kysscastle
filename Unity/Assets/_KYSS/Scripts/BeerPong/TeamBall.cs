using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;   // <-- add this

[RequireComponent(typeof(VRC_Pickup))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VRCObjectSync))]   // <-- update this
public class TeamBall : UdonSharpBehaviour
{
    public BeerPongGameManager game;
    public Team team = Team.Red;

    [Header("Networked Audio on this ball")]
    public NetworkedAudio sfxPickup;
    public NetworkedAudio sfxBounce;
    public NetworkedAudio sfxCenterSuccess;

    public CenterGate centerGate;

    private VRC_Pickup _pickup;
    private VRCObjectSync _sync;   // <-- update type

    [HideInInspector] public bool hasTouchedCenter;

    private void Start()
    {
        _pickup = GetComponent<VRC_Pickup>();
        _sync   = GetComponent<VRCObjectSync>();  // <-- update type
        SetBallEnabled(false);
    }

    public void SetBallEnabled(bool on)
    {
        gameObject.SetActive(on);
        if (!on) hasTouchedCenter = false;
        RPC_SeatsChanged();
    }

    public void RPC_SeatsChanged()
    {
        bool can = (game != null) && game.CanPickupBall(Networking.LocalPlayer, team);
        if (_pickup != null) _pickup.pickupable = can;
    }

    public override void OnPickup()
    {
        if (_pickup == null) return;
        var lp = Networking.LocalPlayer;

        if (game && !game.CanPickupBall(lp, team))
        {
            _pickup.Drop();
            return;
        }

        if (!Networking.IsOwner(gameObject) && lp != null)
            Networking.SetOwner(lp, gameObject);

        if (sfxPickup != null) sfxPickup.PlayForAll();
    }

    private void OnCollisionEnter(Collision c)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (sfxBounce != null) sfxBounce.PlayForAll();
    }

    public void CenterSuccessLocal()
    {
        hasTouchedCenter = true;
        if (sfxCenterSuccess != null) sfxCenterSuccess.PlayForAll();
    }

    public void OnBallRespawned()
    {
        hasTouchedCenter = false;
        if (centerGate != null) centerGate.ResetGate();
    }
}
