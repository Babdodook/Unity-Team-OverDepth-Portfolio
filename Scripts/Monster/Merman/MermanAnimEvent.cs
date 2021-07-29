using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MermanAnimEvent : BaseMonsterAnimEvent
{
    MermanController sc_Controller;

    public GameObject charObj;
    public GameObject ragdollObj;

    public Transform LeftHand;
    public Transform RightHand;
    public Transform Head;

    bool JumpTranslate = false;
    float AnimWalkSpeed;
    
    protected override void Awake()
    {
        base.Awake();
        sc_Controller = GetComponentInParent<MermanController>();
    }

    void Update()
    {
        transform.localPosition = new Vector3(0, 0, 0);
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        if (JumpTranslate)
        {
            pTransform.Translate(Vector3.up * 2 * Time.deltaTime);
            pTransform.Translate(Vector3.forward * 1 * Time.deltaTime);
        }

        if (AttackTranlsate)
        {
            // print("달리기");
            if (sc_Controller.TargetDistance > 1f)
                sc_Controller.CC.Move(pTransform.forward * sc_Controller.WalkSpeed * Time.deltaTime);
            sc_Controller.WalkSpeed = Mathf.Lerp(sc_Controller.WalkSpeed, 3.0f, 2.5f * Time.deltaTime);
        }

        if (OnHitTranlsate)
        {
            sc_Controller.CC.Move((pTransform.forward * -1) * TranslateSpeed * Time.deltaTime);
        }
    }

    public void startTrace()
    {
        sc_Controller.m_nowState = MONSTER_STATE.TRACE;
        //print("추적 상태로");
    }

    public void AttackSpeed(float Speed)
    {
        m_Animator.SetFloat("AttackSpeed", Speed);
    }

    public void LeftHandAttack()
    {
        ColliderOn(LeftHand);
        ColliderOff(RightHand);
    }

    public void RightHandAttack()
    {
        ColliderOn(RightHand);
        ColliderOff(LeftHand);
    }

    public void TurnOffHandCollider()
    {
        ColliderOff(LeftHand);
        ColliderOff(RightHand);
    }

    public void TurnOnHandCollider()
    {
        ColliderOn(LeftHand);
        ColliderOn(RightHand);
    }

    public void HeadColliderOn()
    {
        ColliderOn(Head);
    }

    public void HeadColliderOff()
    {
        ColliderOff(Head);
    }

    // 콜라이더 켜기
    public void ColliderOn(Transform Obj)
    {
        Obj.GetComponent<SphereCollider>().enabled = true;
    }

    // 콜라이더 끄기
    public void ColliderOff(Transform Obj)
    {
        Obj.GetComponent<SphereCollider>().enabled = false;
    }

    public override void CanHitOn()
    {
        sc_Controller.m_canHit = true;
    }

    public override void CanHitOff()
    {
        sc_Controller.m_canHit = false;
    }

    // 점프 시작
    public void StartJump()
    {
        JumpTranslate = true;
    }

    public void StopJump()
    {
        JumpTranslate = false;
    }

    // 점프 종료
    public void EndJump()
    {
        m_Animator.SetBool("TraceSTATE", true);
        sc_Controller.m_nowState = MONSTER_STATE.TRACE;
        sc_Controller.LockRotation = false;
        sc_Controller.EndJump = true;
        //pTransform.GetComponentInChildren<Ycheck>().Enable = true;
        //print("점프 종료");
    }

    // 공격 종료
    public override void EndAttack()
    {
        m_Animator.SetInteger("AttackValue", -1);
        sc_Controller.m_isAttack = false;

        //StartCoroutine(AttackDelay());
    }

    public void SetAttackValue(int value)
    {
        m_Animator.SetInteger("AttackValue", value);
    }
    
    public void SetTranslateSpeed()
    {
        TranslateSpeed = sc_Controller.WalkSpeed;
    }

    public void EndOnHitTranslate()
    {
        OnHitTranlsate = false;
    }

    public void SetOnHitTranslate(float Speed)
    {
        OnHitTranlsate = true;
        TranslateSpeed = Speed;
    }

    public void SetMoveType(string type)
    {
        switch(type)
        {
            case "Walk":
                sc_Controller.m_moveType = MoveType.WalkForward;
                break;
            case "Run":
                sc_Controller.m_moveType = MoveType.RunForward;
                break;
        }
    }

    public void EndTranslate()
    {
        AttackTranlsate = false;
        sc_Controller.m_isAttack = false;
        //print("이동 끝 실행");
    }

    public void UnLockRotation()
    {
        sc_Controller.LockRotation = false;
    }

    public void LockRotation()
    {
        sc_Controller.LockRotation = true;
    }

    // 방어 시작
    public void DefenceStart()
    {
        m_Animator.SetInteger("DefenceSTATE", -1);
    }

    // 방어 딜레이
    public void DefenceDelayEvent()
    {
        StartCoroutine(DefenceDelay());
    }

    // 방어 끝
    public void DefenceEnd()
    {
        sc_Controller.LockRotation = false;
        sc_Controller.OnHitFlag = false;
        sc_Controller.m_nowState = MONSTER_STATE.BATTLE;
    }

    // 피격 시작
    public override void OnHitStart()
    {
        m_Animator.SetInteger("OnHit", -1);
        m_Animator.SetInteger("AttackValue", -1);
        sc_Controller.m_isAttack = false;

        // 무기 콜라이더 끄기
        TurnOffHandCollider();
        CanHitOff();

        sc_Controller.LockRotation = true;
        AttackTranlsate = false;

        //print("온 히트 스타트");

        //m_Animator.SetFloat("HitStopSpeed", 0f);
        //StartCoroutine(HitStopDelay());
    }

    // 피격 종료
    public override void OnHitEnd()
    {
        sc_Controller.OnHitOnce = false;
        sc_Controller.OnHitFlag = false;
        sc_Controller.LockRotation = false;

        // 아이들 상태에서 피격 당했다면, 타겟 지정하고 추적 상태로ㄱㄱ
        if (sc_Controller.m_prevState == MONSTER_STATE.IDLE)
        {
            sc_Controller.Target = GameManager.Instance.ClientPlayer;
        }

        sc_Controller.m_nowState = MONSTER_STATE.TRACE;

        //print("온히트 끝 " + sc_Controller.m_prevState);
    }

    public override IEnumerator HitDelay()
    {
        sc_Controller.EndHitDelayTime = 0;
        yield return new WaitUntil(() => sc_Controller.CheckHitDelay() == true);

        //sc_Controller.LockRotation = false;
        sc_Controller.OnHitOnce = false;
        sc_Controller.OnHitFlag = false;
        sc_Controller.m_nowState = MONSTER_STATE.BATTLE;
    }

    public override void Death()
    {
        m_Animator.SetInteger("Death", -1);
        m_Animator.SetInteger("AttackValue", -1);
        sc_Controller.HitBox.gameObject.SetActive(false);
        sc_Controller.CC.enabled = false;

        // 무기 콜라이더 끄기
        TurnOffHandCollider();
        CanHitOff();

        sc_Controller.LockRotation = true;
        AttackTranlsate = false;
    }

    public void DeathEnd()
    {
        sc_Controller.Target = null;
    }

    IEnumerator DefenceDelay()
    {
        m_Animator.SetFloat("DefenceDelay", 0f);
        yield return new WaitForSeconds(2f);
        m_Animator.SetFloat("DefenceDelay", 1f);
    }

    public override IEnumerator HitStopDelay()
    {
        print("어인 히트 스탑 시작");
        OnHitTranlsate = false;

        if (sc_Controller.currentAttackType == 0)
        {
            yield return new WaitForSeconds(0.2f);
            print("어인 히트 스탑 타입 0");
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            print("어인 히트 스탑 타입 1");
        }

        m_Animator.SetFloat("HitStopSpeed", 0.8f);

        OnHitTranlsate = true;
        print("어인 히트 스탑 끝");
    }

    public override IEnumerator AttackDelay()
    {
        //print("어택 딜레이");
        sc_Controller.m_moveType = MoveType.Stay;
        //m_Animator.SetInteger("BattleSTATE", (int)MermanBattleType.Idle);
        m_Animator.SetInteger("AttackValue", -1);
        sc_Controller.m_isAttack = false;
        sc_Controller.LockRotation = true;
        sc_Controller.m_isAttackDelay = true;
        m_Animator.SetInteger("BattleSTATE", -1);
        m_Animator.SetFloat("RunSpeed", 1f);
        sc_Controller.AttackDelayTime = 0;

        yield return new WaitUntil(() => sc_Controller.CheckAttackDelay() == true);

        sc_Controller.m_isAttackDelay = false;

        // 맞았을 경우에는 회전 잠궈두기
        if (!sc_Controller.OnHitFlag)
            sc_Controller.LockRotation = false;
    }

    public void ChangeRagdoll()
    {
        CopyAnimCharacterTransformToRagdoll(charObj.transform, ragdollObj.transform);

        ragdollObj.gameObject.SetActive(true);
        charObj.gameObject.SetActive(false);
    }

    private void CopyAnimCharacterTransformToRagdoll(Transform origin, Transform rag)
    {
        for (int i = 0; i < origin.transform.childCount; i++)
        {
            if (origin.transform.childCount != 0)
            {
                CopyAnimCharacterTransformToRagdoll(origin.transform.GetChild(i), rag.transform.GetChild(i));
            }
            rag.transform.GetChild(i).localPosition = origin.transform.GetChild(i).localPosition;
            rag.transform.GetChild(i).localRotation = origin.transform.GetChild(i).localRotation;
        }
    }
}
