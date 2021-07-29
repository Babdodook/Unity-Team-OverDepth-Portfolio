using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EvadeType
{
    backward = 0,
    right,
    left,

    Max
}

public class BaseMonsterAnimEvent : MonoBehaviour
{
    protected Animator m_Animator;
    BaseMonsterController Controller;
    public Transform pTransform;

    protected Vector3 backward;
    protected bool m_bEvade = false;

    [HideInInspector] public bool AttackTranlsate = false;
    [HideInInspector] public bool OnHitTranlsate = false;
    
    public float TranslateSpeed;

    protected virtual void Awake()
    {
        m_Animator = GetComponent<Animator>();
        Controller = GetComponentInParent<BaseMonsterController>();
    }
    
    public virtual void StartEvade()
    {
    }

    public void EvadeTranslateStart()
    {
        m_bEvade = true;
    }

    public void EvadeTranslateEnd()
    {
        m_bEvade = false;
    }

    public virtual void EndEvade()
    {
    }

    public virtual void CanHitOn()
    {
    }

    public virtual void CanHitOff()
    {
    }

    public virtual void EndAttack()
    {
        StartCoroutine(AttackDelay());
    }

    public virtual IEnumerator AttackDelay()
    {
        yield return null;
    }

    public virtual void OnHitStart()
    {
    }

    public virtual IEnumerator HitStopDelay()
    {
        yield return null;
    }

    public virtual void OnHitEnd()
    {
    }

    public virtual IEnumerator HitDelay()
    {
        yield return null;
    }

    public virtual void AfterHitAction()
    {
    }

    public virtual void Death()
    {
    }
}
