using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FootSound
{
    Water=0,
    Ground,

    Max
}

public class MonsterSound : MonoBehaviour
{
    public FootSound FootSoundType;

    public AudioSource Footstep;
    public AudioSource Attack;
    public AudioClip[] Water_Walk;
    public AudioClip[] Run;
    public AudioClip Swing;
    public AudioClip Yell;
    public AudioClip Hit;
    public AudioClip TakeWeapon;

    public AudioClip Fire;

    public AudioClip[] Ground_Walk;

    public BaseMonsterController controller;

    int RandomSound;
    public void PlaySound(string State)
    {
        switch(State)
        {
            case "Walk":
                if(controller.m_FanaticBattleType == FanaticBattleType.Walk_forward)
                {
                    if (FootSoundType == FootSound.Water)
                    {
                        RandomSound = UnityEngine.Random.Range(0, Water_Walk.Length);
                        Footstep.PlayOneShot(Water_Walk[RandomSound]);
                    }
                    else
                    {
                        RandomSound = UnityEngine.Random.Range(0, Ground_Walk.Length);
                        Footstep.PlayOneShot(Ground_Walk[RandomSound]);
                    }
                }
                break;
            case "Run":
                if (controller.m_FanaticBattleType == FanaticBattleType.FastRun)
                {
                    if (FootSoundType == FootSound.Water)
                    {
                        RandomSound = UnityEngine.Random.Range(0, Run.Length);
                        Footstep.PlayOneShot(Run[RandomSound]);
                    }
                    else
                    {
                        RandomSound = UnityEngine.Random.Range(0, Ground_Walk.Length);
                        Footstep.PlayOneShot(Ground_Walk[RandomSound]);
                    }
                }
                break;
            case "Walk_Left":
                if (controller.m_FanaticBattleType == FanaticBattleType.Walk_left)
                {
                    if (FootSoundType == FootSound.Water)
                    {
                        RandomSound = UnityEngine.Random.Range(0, Water_Walk.Length);
                        Footstep.PlayOneShot(Water_Walk[RandomSound]);
                    }
                    else
                    {
                        RandomSound = UnityEngine.Random.Range(0, Ground_Walk.Length);
                        Footstep.PlayOneShot(Ground_Walk[RandomSound]);
                    }
                }
                break;
            case "Walk_Right":
                if (controller.m_FanaticBattleType == FanaticBattleType.Walk_right)
                {
                    if (FootSoundType == FootSound.Water)
                    {
                        RandomSound = UnityEngine.Random.Range(0, Water_Walk.Length);
                        Footstep.PlayOneShot(Water_Walk[RandomSound]);
                    }
                    else
                    {
                        RandomSound = UnityEngine.Random.Range(0, Ground_Walk.Length);
                        Footstep.PlayOneShot(Ground_Walk[RandomSound]);
                    }
                }
                break;
            case "AttackStep":
                if (FootSoundType == FootSound.Water)
                {
                    RandomSound = UnityEngine.Random.Range(0, Run.Length);
                    Footstep.PlayOneShot(Run[RandomSound]);
                }
                else
                {
                    RandomSound = UnityEngine.Random.Range(0, Ground_Walk.Length);
                    Footstep.PlayOneShot(Ground_Walk[RandomSound]);
                }
                break;
            case "Swing":
                Attack.PlayOneShot(Swing);
                break;
            case "Yell":
                Attack.PlayOneShot(Yell);
                break;
            case "Evade":
                if (FootSoundType == FootSound.Water)
                {
                    RandomSound = UnityEngine.Random.Range(0, Run.Length);
                    Footstep.PlayOneShot(Run[RandomSound]);
                }
                else
                {
                    RandomSound = UnityEngine.Random.Range(0, Ground_Walk.Length);
                    Footstep.PlayOneShot(Ground_Walk[RandomSound]);
                }
                break;
            case "TakeWeapon":
                Attack.PlayOneShot(TakeWeapon);
                break;
            case "Fire":
                Attack.PlayOneShot(Fire);
                break;
        }
    }

    public void TargetHitSound()
    {
        Attack.PlayOneShot(Hit);
    }
}
