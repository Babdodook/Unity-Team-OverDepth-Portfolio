using Inven;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanaticGunnerAnimEvent : BaseMonsterAnimEvent
{
    FanaticGunnerController sc_Controller;

    public Transform Weapon_Attached;
    public Transform ShotPos;
    public Transform MuzzleEffectPos;
    public Transform MuzzleFlare;
    public Transform Projectile;
    public Transform Impact;
    bool escapeFlag = false;

    protected override void Awake()
    {
        base.Awake();
        sc_Controller = GetComponentInParent<FanaticGunnerController>();
    }

    public override void StartEvade()
    {
        EndTranslate();
        //print("회피기 시작");
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

        //print("엔드어택 실행");
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

    public void SetAttackValue()
    {
        if (sc_Controller.m_nowState == MONSTER_STATE.ONHIT)
            return;

        m_Animator.SetInteger("BattleSTATE", (int)FanaticBattleType.Idle);

        //print("셋어택밸류 실행");
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

        // onHitRandomAction = UnityEngine.Random.Range(0, 100);
        StartCoroutine(RandomHitAction());

        //print("OnHitStart");
    }

    IEnumerator RandomHitAction()
    {
        yield return null;
        EndTranslate();

        if (sc_Controller.onHitRandomAction >= 70)
        {
            //yield return new WaitForSeconds(0.1f);

            //print(" 회피기 실행 ");
            m_Animator.SetInteger("Evade", 0);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);

            //print(" 다시 이동으로 ");
            m_Animator.SetTrigger("OnHitEnd");
            sc_Controller.m_nowState = MONSTER_STATE.BATTLE;

            sc_Controller.m_isAttack = false;

            sc_Controller.OnHitOnce = false;
            sc_Controller.OnHitFlag = false;
        }

        sc_Controller.LockRotation = false;
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

    Vector3 AfterShotPos = Vector3.zero;
    // 원거리 무기 발사
    public void Shot()
    {
        RaycastHit hit;

        // 발사 이펙트 생성
        var item = Instantiate(MuzzleFlare);
        item.position = MuzzleEffectPos.position;
        item.rotation = MuzzleEffectPos.rotation;

        // 이펙트 삭제
        Destroy(item.gameObject, 0.6f);

        item = Instantiate(Projectile);
        item.position = ShotPos.position;
        item.rotation = ShotPos.rotation;

        item.GetComponent<FGunnerBullet>().SpawnPos = item.position;
        item.GetComponent<FGunnerBullet>().y = item.position.y;

        int layermask = 1 << LayerMask.NameToLayer("HitArea");

        if (Physics.Raycast(ShotPos.position, ShotPos.forward, out hit, 30f, layermask))
        {
            Debug.DrawLine(ShotPos.position, hit.point, Color.red, 5f);
            print("hit :: " + hit.collider.gameObject.name);

            // 피격 콜라이더 충돌
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("HitArea"))
            {
                print("피격 콜라이더 충돌");
                PlayerControl playerColtroller = hit.collider.GetComponentInParent<PlayerControl>();

                // 플레이어와 충돌
                if (playerColtroller != null)
                {
                    float shotTime = hit.distance / 60f;
                    //print("지연시간" + shotTime);

                    hit.collider.GetComponent<OnHit>().InstanceBlood(transform.position);
                    hit.collider.GetComponentInParent<Health>().TakeDamage(10f);

                    //AfterShotPos = ShotPos.position;
                    //StartCoroutine(ShotDelay(shotTime, hit));       
                }
            }
            //print(hit.collider.gameObject);
        }
    }

    // 지연시간 후 피격
    IEnumerator ShotDelay(float delayTime, RaycastHit hit)
    {
        //print("딜레이 시작");
        yield return new WaitForSeconds(delayTime);

        Vector3 direction = (hit.point - AfterShotPos).normalized;
        if (Physics.Raycast(AfterShotPos, direction, out hit, 30f))
        {
            PlayerControl playerColtroller = hit.collider.GetComponentInParent<PlayerControl>();

            // 명중
            if (playerColtroller != null)
            {
                Debug.DrawLine(ShotPos.position, hit.point, Color.yellow, 5f);
                hit.collider.GetComponent<OnHit>().InstanceBlood(transform.position);
                hit.collider.GetComponentInParent<Health>().TakeDamage(10f);
            }
        }
        //print("딜레이 종료");
    }
}
