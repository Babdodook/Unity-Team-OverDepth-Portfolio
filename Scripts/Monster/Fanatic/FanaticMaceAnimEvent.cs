using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanaticMaceAnimEvent : BaseMonsterAnimEvent
{
    FanaticMaceController sc_Controller;
    public Transform Weapon;
    public Transform Weapon_Attached;
    bool escapeFlag = false;
    EvadeType m_evadeType;

    protected override void Awake()
    {
        base.Awake();
        sc_Controller = GetComponentInParent<FanaticMaceController>();
    }

    public void GetWeapon()
    {
        Weapon.gameObject.SetActive(false);
        Weapon_Attached.gameObject.SetActive(true);
    }

    public override void StartEvade()
    {
        //print("회피기 시작");
        EndTranslate();
        m_Animator.SetInteger("Evade", -1);
        sc_Controller.LockRotation = true;
        sc_Controller.m_isEvade = true;
        sc_Controller.HitBox.GetComponent<BoxCollider>().enabled = false;
    }

    public void SetEvadeTranslate(float Speed)
    {
        sc_Controller.EvadeTranslate = true;
        sc_Controller.AnimTranslateSpeed = Speed;
    }

    public void EndEvadeTranslate()
    {
        sc_Controller.EvadeTranslate = false;
    }

    public override void EndEvade()
    {
        //print("엔드 이베이드");
        sc_Controller.m_isAttack = false;
        sc_Controller.LockRotation = false;
        sc_Controller.m_isEvade = false;
        sc_Controller.HitBox.GetComponent<BoxCollider>().enabled = true;

        sc_Controller.OnHitOnce = false;
        sc_Controller.OnHitFlag = false;
        sc_Controller.m_nowState = MONSTER_STATE.BATTLE;
    }

    // 공격 종료
    public override void EndAttack()
    {
        if (sc_Controller.m_nowState == MONSTER_STATE.ONHIT)
            return;

        sc_Controller.m_isAttack = false;
        sc_Controller.m_FanaticBattleType = FanaticBattleType.Walk_forward;
    }

    public void SetAttackTranslate(float Speed)
    {
        EndTranslate();

        sc_Controller.AttackTranlsate = true;
        sc_Controller.AnimTranslateSpeed = Speed;
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
        sc_Controller.EvadeTranslate = false;
    }

    public void AttackSpeed(float Speed)
    {
        m_Animator.SetFloat("AttackSpeed", Speed);
    }

    public void SetAttackValueRandom()
    {
        int randnum = UnityEngine.Random.Range(0, 2);
        if (randnum == 0)
        {
            m_Animator.SetInteger("BattleSTATE", (int)AnimBattleSTATE.Move);
            EndAttack();
        }
        else
        {
            m_Animator.SetInteger("BattleSTATE", (int)AnimBattleSTATE.Attack3);
            sc_Controller.m_FanaticBattleType = FanaticBattleType.Attack3;
        }
    }

    public void SetAttackValue(int value)
    {
        if (sc_Controller.m_nowState == MONSTER_STATE.ONHIT)
            return;

        m_Animator.SetInteger("BattleSTATE", value);
    }

    public override void CanHitOn()
    {
        sc_Controller.m_canHit = true;
        Weapon_Attached.GetComponent<BoxCollider>().enabled = true;
    }

    public override void CanHitOff()
    {
        sc_Controller.m_canHit = false;
        Weapon_Attached.GetComponent<BoxCollider>().enabled = false;
    }

    //int onHitRandomAction;
    // 피격 시작
    public override void OnHitStart()
    {
        EndTranslate();

        m_Animator.SetInteger("OnHit", -1);
        m_Animator.SetInteger("AttackValue", -1);
        m_Animator.SetInteger("BattleSTATE", -1);
        //sc_Controller.m_isAttack = false;
        CanHitOff();

        sc_Controller.LockRotation = true;
        sc_Controller.AttackTranlsate = false;

        //sc_Controller.onHitRandomAction = UnityEngine.Random.Range(0, 2);
        StartCoroutine(RandomHitAction());

        //print("OnHitStart");
    }

    IEnumerator RandomHitAction()
    {
        yield return null;
        EndTranslate();

        if (sc_Controller.onHitRandomAction == 0)
        {
            //yield return new WaitForSeconds(0.1f);

            //print(" 회피기 실행 ");
            RandomEvade();
        }
        else
        {
            //yield return new WaitForSeconds(0.1f);

            //print(" 다시 이동으로 ");
            m_Animator.SetTrigger("OnHitEnd");
            sc_Controller.m_nowState = MONSTER_STATE.BATTLE;

            sc_Controller.m_isAttack = false;

            sc_Controller.OnHitOnce = false;
            sc_Controller.OnHitFlag = false;
        }

        sc_Controller.LockRotation = false;
    }

    public void RandomEvade()
    {
        m_Animator.SetInteger("Evade", 1);
        sc_Controller.m_evadeType = EvadeType.left;

        //int randnum = UnityEngine.Random.Range(0, 2);
        //switch (randnum)
        //{
        //    case 0: // 뒤로 회피
        //        m_Animator.SetInteger("Evade", 0);
        //        m_evadeType = EvadeType.backward;
        //        break;
        //    case 1: // 왼쪽 회피
        //        m_Animator.SetInteger("Evade", 1);
        //        m_evadeType = EvadeType.left;
        //        break;
        //}
    }

    // 히트 스탑
    public override IEnumerator HitStopDelay()
    {
        yield return new WaitForSeconds(0.15f);
        m_Animator.SetFloat("HitStopSpeed", 1f);
    }

    public override void Death()
    {
        EndTranslate();

        m_Animator.SetInteger("Death", -1);
        sc_Controller.HitBox.gameObject.SetActive(false);
        sc_Controller.CC.enabled = false;
        sc_Controller.LockRotation = true;

        CanHitOff();

        sc_Controller.LockRotation = true;
        sc_Controller.AttackTranlsate = false;
    }

    public void EndGetWeapon()
    {
        sc_Controller.LockRotation = false;
        sc_Controller.EndGetWeapon = true;
    }
}
