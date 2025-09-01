// CenterGate.cs
using UdonSharp;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CenterGate : UdonSharpBehaviour
{
    public BeerPongGameManager game;
    public bool debug;

    private void Start()
    {
        ((Collider)GetComponent(typeof(Collider))).isTrigger = true;
    }

    public void ResetGate() { } // no-op by design

    private void OnTriggerEnter(Collider other)
    {
        TeamBall ball = other.GetComponent<TeamBall>();
        if (ball == null) ball = other.GetComponentInParent<TeamBall>();   // << key line
        if (ball == null) { if (debug) Debug.Log("[CenterGate] No TeamBall on " + other.name); return; }

        ball.hasTouchedCenter = true;
        if (debug) Debug.Log("[CenterGate] Center success for " + ball.team + " ball.");
        if (game) game.OnCenterSuccess(other.transform.position);
    }
}
