using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MermanSound : MonoBehaviour
{
    public AudioSource Footstep;
    public AudioSource Attack;
    public AudioSource Howling;
    public AudioClip[] Walk;
    public AudioClip[] Run;
    public AudioClip[] Swing;
    public AudioClip Roar;
    public AudioClip[] Roar2;
    //public AudioClip[] Hit;
    public AudioClip[] Damaged;
    public AudioClip Death;
    public AudioClip Howling_Idle;
    public AudioClip Howling_Run;

    public MermanController controller;

    int RandomSound;
    public void PlaySound(string State)
    {
        switch (State)
        {
            case "Walk":
                if (controller.m_moveType == MoveType.WalkForward)
                {
                    RandomSound = UnityEngine.Random.Range(0, Walk.Length);
                    Footstep.PlayOneShot(Walk[RandomSound]);
                }
                break;
            case "Run":
                if (controller.m_moveType == MoveType.RunForward)
                {
                    RandomSound = UnityEngine.Random.Range(0, Run.Length);
                    Footstep.PlayOneShot(Run[RandomSound]);
                }
                break;
            case "Walk_Left":
                if (controller.m_FanaticBattleType == FanaticBattleType.Walk_left)
                {
                    RandomSound = UnityEngine.Random.Range(0, Walk.Length);
                    Footstep.PlayOneShot(Walk[RandomSound]);
                }
                break;
            case "Walk_Right":
                if (controller.m_FanaticBattleType == FanaticBattleType.Walk_right)
                {
                    RandomSound = UnityEngine.Random.Range(0, Walk.Length);
                    Footstep.PlayOneShot(Walk[RandomSound]);
                }
                break;
            case "AttackStep":
                RandomSound = UnityEngine.Random.Range(0, Run.Length);
                Footstep.PlayOneShot(Run[RandomSound]);
                break;
            case "Swing1":
                Attack.PlayOneShot(Swing[0]);
                break;
            case "Swing2":
                Attack.PlayOneShot(Swing[1]);
                break;
            case "Roar":
                Attack.PlayOneShot(Roar);
                break;
            case "Roar2":
                //RandomSound = UnityEngine.Random.Range(0, Roar2.Length);
                //Howling.PlayOneShot(Roar2[RandomSound]);
                break;
            case "Damaged1":
                Howling.PlayOneShot(Damaged[0]);
                break;
            case "Damaged2":
                Howling.PlayOneShot(Damaged[1]);
                break;
            case "Death":
                Howling.PlayOneShot(Death);
                break;
            case "Howling_Idle":
                Howling.PlayOneShot(Howling_Idle);
                break;
            case "Howling_Run":
                Howling.PlayOneShot(Howling_Idle);
                break;
        }
    }

    public void TargetHitSound()
    {
        //if(controller.m_MermanBattleType == MermanBattleType.ComboAttack)
        //    Attack.PlayOneShot(Hit[0]);
        //else if(controller.m_MermanBattleType == MermanBattleType.RunAttack)
        //    Attack.PlayOneShot(Hit[1]);

    }

}
