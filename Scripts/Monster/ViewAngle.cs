using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewAngle : MonoBehaviour
{
    BaseMonsterController bmController;

    [Header("시야각")]
    public float m_ViewAngle;
    [Header("시야 거리")]
    public float m_ViewDistance;

    private void Awake()
    {
        bmController = GetComponent<BaseMonsterController>();
    }

    public bool FindTargetInView()
    {
        if (bmController.Target == null)
            return false;

        // 타겟과 거리 차이로 나오는 단위벡터
        Vector3 subDistance = (bmController.Target.position - transform.position).normalized;

        //print("내적 : " + Vector3.Dot(transform.forward, subDistance) + " / " + (1 + Mathf.Cos(m_ViewAngle / 2) * Mathf.Deg2Rad));

        if (Vector3.Dot(transform.forward, subDistance) > 1 + Mathf.Cos(m_ViewAngle / 2) * Mathf.Deg2Rad)
        {
            return true;
        }
        else
            return false;
    }
}
