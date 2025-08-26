using UdonSharp;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CenterGate : UdonSharpBehaviour
{
    public BeerPongGameManager game;

    private bool _armed = true;

    private void Start()
    {
        var col = (Collider)GetComponent(typeof(Collider));
        col.isTrigger = true;
    }

    public void ResetGate()
    {
        _armed = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_armed) return;

        // ❌ old: var ball = (TeamBall)other.GetComponent(typeof(TeamBall));
        TeamBall ball = other.GetComponent<TeamBall>();   // ✅
        if (ball == null) return;

        ball.hasTouchedCenter = true;
        _armed = false;
        if (game) game.OnCenterSuccess(other.transform.position);
    }

}
