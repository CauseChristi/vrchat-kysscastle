using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(Collider))]
public class PlayerZone : UdonSharpBehaviour
{
    public BeerPongGameManager game;
    public Team team = Team.Red;

    [Tooltip("Seconds allowed outside the zone before forfeiting this seat.")]
    public float graceSeconds = 10f;

    private VRCPlayerApi _current;
    private bool _waitingForReturn;

    private void Start()
    {
        var col = (Collider)GetComponent(typeof(Collider));
        col.isTrigger = true;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return;

        if (_current == null)
        {
            _current = player;
            _waitingForReturn = false;
            if (game) game.OnPlayerZoneJoined(team, player);
        }
        else if (_waitingForReturn && player.playerId == _current.playerId)
        {
            // Returned within grace
            _waitingForReturn = false;
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return;
        if (_current == null || player.playerId != _current.playerId) return;

        _waitingForReturn = true;
        SendCustomEventDelayedSeconds(nameof(_GraceTimeout), graceSeconds);
    }

    public void _GraceTimeout()
    {
        if (!_waitingForReturn || _current == null) return;

        // Forfeit seat
        if (game) game.OnPlayerZoneLeft(team, _current);
        _current = null;
        _waitingForReturn = false;
    }

    public VRCPlayerApi GetPlayer() => _current;
}
