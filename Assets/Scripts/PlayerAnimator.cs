using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Animator playerAnim;
    public PlayerController player;

    public string SteeringParam = "Steering";
    public string GroundedParam = "Grounded";

    int m_SteerHash, m_GroundHash;

    float steeringSmoother;

    void Start()
    {
        // Assert.IsNotNull(player, "No ArcadeKart found!");
        // Assert.IsNotNull(PlayerAnimator, "No PlayerAnimator found!");
        m_SteerHash  = Animator.StringToHash(SteeringParam);
        m_GroundHash = Animator.StringToHash(GroundedParam);
    }

    void Update()
    {
        // steeringSmoother = Mathf.Lerp(steeringSmoother, player.turnInput, Time.deltaTime * 5f);
        playerAnim.SetFloat(m_SteerHash, player.turnInput);

        // If more than 2 wheels are above the ground then we consider that the kart is airbourne.
        // playerAnim.SetBool(m_GroundHash, player.GroundPercent >= 0.5f);
    }
}
