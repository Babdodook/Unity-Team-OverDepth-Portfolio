using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TCP;

public class TitanicHydraAnimEvent : BaseMonsterAnimEvent
{
    TitanicHydraController sc_Controller;

    Transform Cam;
    public Transform LHandWeapon;
    public Transform RHandWeapon;
    //bool JumpTranslate = false;

    public Transform WaterSplash;
    public Transform SmashAttackSplashPoint;

    Transform Stone;
    public Transform LHand;
    public Transform RHand;
    public Transform RushAttackWeapon;
    public Transform JumpAttackWeapon;
    bool StoneAttack;

    protected override void Awake()
    {
        base.Awake();
        sc_Controller = GetComponentInParent<TitanicHydraController>();
        Cam = Camera.main.transform;
        WaterSplash.GetComponent<ParticleSystem>().Stop();

        CanHitOff();
    }

    private void Update()
    {
        if(StoneAttack)
        {
            Stone.position = GetStonePosition();
        }
    }

    // 서버에서 돌 위치 받기
    public void TCP_RecvStonePosition(Vector3 _position, Quaternion _rotation)
    {
        Stone.position = _position;
        Stone.rotation = _rotation;
    }

    // 공격 종료
    public override void EndAttack()
    {
        if (sc_Controller.m_nowState == MONSTER_STATE.ONHIT)
            return;

        sc_Controller.m_isAttack = false;
        sc_Controller.m_moveType = MoveType.Stay;
        sc_Controller.m_THydraActionType = THydraActionType.Walk_forward;
        m_Animator.SetInteger("Attack", (int)AnimBattleSTATE.None);

        //print("공격종료 실행");
    }

    public void SetAttackTranslate(float Speed)
    {
        sc_Controller.AttackTranlsate = true;
        sc_Controller.AnimTranslateSpeed = Speed;

        //print("이동 중");
    }

    public void SetOnHitTranslate(float Speed)
    {
        sc_Controller.OnHitTranlsate = true;
        sc_Controller.AnimTranslateSpeed = Speed;
    }

    public void EndTranslate()
    {
        sc_Controller.AttackTranlsate = false;
        sc_Controller.OnHitTranlsate = false;
    }

    public void AttackSpeed(float Speed)
    {
        m_Animator.SetFloat("AttackSpeed", Speed);
    }

    // 콤보어택 공격 속도 지정
    public void Combo_AttackSpeed(int turn)
    {
        // 페이즈에 따라 공격 속도 다르게 지정
        switch(sc_Controller.m_battlePhase)
        {
            case 1:
                if(turn == 0)
                {
                    m_Animator.SetFloat("AttackSpeed", 0.7f);
                }
                else if(turn == 1)
                {
                    m_Animator.SetFloat("AttackSpeed", 1);
                }

                break;
            case 2:
                if (turn == 0)
                {
                    m_Animator.SetFloat("AttackSpeed", 0.9f);
                }
                else if (turn == 1)
                {
                    m_Animator.SetFloat("AttackSpeed", 1.2f);
                }

                break;
            case 3:
                if (turn == 0)
                {
                    m_Animator.SetFloat("AttackSpeed", 1f);
                }
                else if (turn == 1)
                {
                    m_Animator.SetFloat("AttackSpeed", 1.3f);
                }

                break;
        }
    }

    // 스매쉬어택 공격 속도 지정
    public void Smash_AttackSpeed(int turn)
    {
        // 페이즈에 따라 공격 속도 다르게 지정
        switch (sc_Controller.m_battlePhase)
        {
            case 1:
                if (turn == 0)
                {
                    m_Animator.SetFloat("AttackSpeed", 0.6f);
                }
                else if (turn == 1)
                {
                    m_Animator.SetFloat("AttackSpeed", 1);
                }

                break;
            case 2:
                if (turn == 0)
                {
                    m_Animator.SetFloat("AttackSpeed", 0.9f);
                }
                else if (turn == 1)
                {
                    m_Animator.SetFloat("AttackSpeed", 1.3f);
                }

                break;
            case 3:
                if (turn == 0)
                {
                    m_Animator.SetFloat("AttackSpeed", 1f);
                }
                else if (turn == 1)
                {
                    m_Animator.SetFloat("AttackSpeed", 1.4f);
                }

                break;
        }
    }

    // 러쉬어택 공격 속도 지정
    public void Rush_AttackSpeed()
    {
        // 페이즈에 따라 공격 속도 다르게 지정
        switch (sc_Controller.m_battlePhase)
        {
            case 1:
                m_Animator.SetFloat("AttackSpeed", 1f);

                break;
            case 2:
                m_Animator.SetFloat("AttackSpeed", 1.1f);

                break;
            case 3:
                m_Animator.SetFloat("AttackSpeed", 1.2f);

                break;
        }
    }

    // 스매쉬 어택 물 튀김
    public void Smash_Splash()
    {
        WaterSplash.position = SmashAttackSplashPoint.position;
        WaterSplash.GetComponent<ParticleSystem>().Play();
    }

    public override void CanHitOn()
    {
        sc_Controller.m_canHit = true;

        switch (m_Animator.GetInteger("Attack"))
        {
            case 1:
            case 2:
                LHandWeapon.GetComponent<BoxCollider>().enabled = true;
                RHandWeapon.GetComponent<BoxCollider>().enabled = true;
                break;
            case 3:
                RushAttackWeapon.GetComponent<BoxCollider>().enabled = true;
                break;
            case 5:
            case 6:
                JumpAttackWeapon.GetComponent<BoxCollider>().enabled = true;
                break;

        }
    }

    public override void CanHitOff()
    {
        sc_Controller.m_canHit = false;
        LHandWeapon.GetComponent<BoxCollider>().enabled = false;
        RHandWeapon.GetComponent<BoxCollider>().enabled = false;
        RushAttackWeapon.GetComponent<BoxCollider>().enabled = false;
        JumpAttackWeapon.GetComponent<BoxCollider>().enabled = false;
    }

    // 피격 시작
    public override void OnHitStart()
    {
        m_Animator.SetInteger("OnHit", -1);
        m_Animator.SetInteger("AttackValue", -1);
        m_Animator.SetInteger("BattleSTATE", -1);
        CanHitOff();

        sc_Controller.LockRotation = true;
        sc_Controller.AttackTranlsate = false;
    }

    public override void OnHitEnd()
    {
        sc_Controller.m_nowState = MONSTER_STATE.BATTLE;

        sc_Controller.m_isAttack = false;

        sc_Controller.OnHitOnce = false;
        sc_Controller.OnHitFlag = false;

        sc_Controller.LockRotation = false;
    }

    public override void Death()
    {
        //m_Animator.SetInteger("Death", -1);
        //sc_Controller.HitBox.gameObject.SetActive(false);
        CanHitOff();
        sc_Controller.CC.enabled = false;
        sc_Controller.LockRotation = true;
        sc_Controller.AttackTranlsate = false;
    }

    // 발 동작 진동
    float strength;
    public void FootStepQuake()
    {
        if(sc_Controller.TargetDistance <= 6)
        {
            strength = 0.2f;
        }
        else if(sc_Controller.TargetDistance <= 9)
        {
            strength = 0.18f;
        }
        else if(sc_Controller.TargetDistance <= 13)
        {
            strength = 0.16f;
        }
        else if(sc_Controller.TargetDistance <= 15)
        {
            strength = 0.15f;
        }
        else
        {
            strength = 0.1f;
        }

        Cam.GetComponent<TestCameraScript>().Camera_Shake(1f, strength, 5, -1);

        //print("Strength: " + strength);
    }

    // 달리기 진동
    public void FootStepQuake_Run()
    {
        if (sc_Controller.TargetDistance <= 3)
        {
            strength = 0.3f;
        }
        else if (sc_Controller.TargetDistance <= 6)
        {
            strength = 0.25f;
        }
        else if (sc_Controller.TargetDistance <= 8)
        {
            strength = 0.22f;
        }
        else if (sc_Controller.TargetDistance <= 11)
        {
            strength = 0.2f;
        }
        else
        {
            strength = 0.13f;
        }

        Cam.GetComponent<TestCameraScript>().Camera_Shake(1f, strength, 5, -1);

        //print("Strength: " + strength);
    }

    // 공격 진동
    public void AttackQuake(float _strength)
    {
        switch(m_Animator.GetInteger("Attack"))
        {
            case (int)AnimBattleSTATE.Attack1:      // 콤보 어택
                Cam.GetComponent<TestCameraScript>().Camera_Shake(1f, 0.3f, 5, -1);
                break;
            case (int)AnimBattleSTATE.Attack2:      // 스매쉬 어택
                Cam.GetComponent<TestCameraScript>().Camera_Shake(2f, 0.3f, 7, -1);
                break;
            case (int)AnimBattleSTATE.Attack3:      // 러쉬 어택
                Cam.GetComponent<TestCameraScript>().Camera_Shake(1f, _strength, 8, -1);
                break;
            case (int)AnimBattleSTATE.Attack4:      // 돌던지기
                break;
            case (int)AnimBattleSTATE.Attack5:      // 짧은 점프
                Cam.GetComponent<TestCameraScript>().Camera_Shake(3f, _strength, 15, -1);
                break;
            case (int)AnimBattleSTATE.Attack6:      // 긴 점프
                Cam.GetComponent<TestCameraScript>().Camera_Shake(3f, _strength, 15, -1);
                break;
        }
    }

    // 포효 진동
    public void RoarQuake()
    {
        Cam.GetComponent<TestCameraScript>().Camera_Shake(3f, 0.2f, 10, -1);
    }

    public void DeathQuake1()
    {
        Cam.GetComponent<TestCameraScript>().Camera_Shake(1f, 0.2f, 5, -1);
    }

    public void DeathQuake2()
    {
        Cam.GetComponent<TestCameraScript>().Camera_Shake(2f, 0.3f, 10, -1);
    }

    public void testQuake(float _strength)
    {
        Cam.GetComponent<TestCameraScript>().Camera_Shake(2f, _strength, 15, -1);
    }

    // 회전 잠금
    public void LockRotation()
    {
        sc_Controller.LockRotation = true;

        //print("회전 잠금");
    }

    // 회전 잠금 해제
    public void UnLockRotation()
    {
        sc_Controller.LockRotation = false;

        //print("회전 잠금 해제");
    }

    // 짧은 점프 시작
    public void S_StartJump()
    {
        sc_Controller.JumpTranslate = true;
        sc_Controller.SetJumpforce = true;
        sc_Controller.jumpForce = 2f;
        sc_Controller.AnimTranslateSpeed = 18f;
    }

    // 긴 점프 시작
    public void L_StartJump()
    {
        sc_Controller.JumpTranslate = true;
        sc_Controller.jumpForce = 5f;
        sc_Controller.AnimTranslateSpeed = 35f;
    }

    // 점프 종료
    public void EndJump()
    {
        sc_Controller.JumpTranslate = false;
        sc_Controller.AnimTranslateSpeed = 0f;
    }

    public void NextAction()
    {
        m_Animator.SetTrigger("NextAction");
    }

    // 일어서기
    public void CrawlToStand()
    {
        sc_Controller.toStand = true;
    }

    // 돌 위치 지정
    // 왼손 -> 오른손의 벡터를 구해서 1/2 지점의 위치를 알아낸다
    public Vector3 GetStonePosition()
    {
        // 왼손에서 오른손 방향벡터 구하기
        Vector3 dir = RHand.position - LHand.position;
        dir.Normalize();

        // 왼손에서 오른손 거리의 중간 지점 구하기
        Vector3 StonePos = dir * (Vector3.Distance(RHand.position, LHand.position) / 2);
        StonePos = LHand.position + StonePos;

        return StonePos;
    }

    public void StoneOn()
    {
        Stone = sc_Controller.Stone;
        Stone.position = GetStonePosition();
        StoneAttack = true;
    }

    public void ThrowStone()
    {
        StoneAttack = false;
        StartCoroutine(Stone.GetComponent<StoneMove>().SimulateProjectile());
    }
}
