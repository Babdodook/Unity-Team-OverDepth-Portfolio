using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitanicHydraSound : MonoBehaviour
{
    public AudioSource Footstep;
    public AudioSource Attack;
    public AudioClip[] Walk;
    public AudioClip[] Run;
    public AudioClip Roar;

    public AudioClip ComboAttack;
    public AudioClip SmashAttack;
    public AudioClip RushAttack;

    public AudioClip FootQuake;
    public AudioClip WaterSplash;

    public AudioClip ThrowStoneRoar;
    public AudioClip GrabStoneRoar;

    public BaseMonsterController controller;

    int RandomSound;
    public void PlaySound(string State)
    {
        switch (State)
        {
            case "Walk":
                if (/*0.4f <= controller.v && controller.v <= 0.5f &&*/
                    controller.m_moveType == MoveType.WalkForward)
                {
                    RandomSound = UnityEngine.Random.Range(0, Walk.Length);
                    Footstep.PlayOneShot(Walk[RandomSound]);
                    Footstep.PlayOneShot(FootQuake);
                }
                break;
            case "Run":
                if (/*controller.v >= 0.6f &&*/
                    controller.m_moveType == MoveType.RunForward)
                {
                    RandomSound = UnityEngine.Random.Range(0, Run.Length);
                    Footstep.PlayOneShot(Run[RandomSound]);
                    Footstep.PlayOneShot(FootQuake);
                }
                break;
            case "AttackStep":
                RandomSound = UnityEngine.Random.Range(0, Run.Length);
                Footstep.PlayOneShot(Run[RandomSound]);
                break;
            case "Roar":
                Attack.PlayOneShot(Roar);
                break;
            case "ComboAttack":
                Attack.PlayOneShot(ComboAttack);
                break;
            case "SmashAttack":
                Attack.PlayOneShot(SmashAttack);
                break;
            case "RushAttack":
                Attack.PlayOneShot(RushAttack);
                break;
            case "FootQuake":
                Footstep.PlayOneShot(FootQuake);
                break;
            case "WaterSplash":
                Attack.PlayOneShot(WaterSplash);
                break;
            case "ThrowStoneRoar":
                Attack.PlayOneShot(ThrowStoneRoar);
                break;
            case "GrabStoneRoar":
                Attack.PlayOneShot(GrabStoneRoar);
                break;
        }
    }
}
