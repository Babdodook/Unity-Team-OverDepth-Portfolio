using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonFogDmg : MonoBehaviour
{
    [Header("데미지 대상")]
    public LayerMask TargetLayer;

    [Header("감지 거리 / 데미지 거리")]
    public float DetectDistance;

    [Header("데미지")]
    public float Damage;

    [Header("데미지 딜레이 시간")]
    public float TakeDmgTime;

    [HideInInspector]
    public bool StartTakeDmg = false;

    // 감지된 대상들
    Collider[] DetectedObj;
    float time = 0;

    private void Update()
    {
        if(StartTakeDmg)
            DetectTarget();
    }

    // 정해진 거리내에 타겟 찾기
    void DetectTarget()
    {
        DetectedObj = Physics.OverlapSphere(this.transform.position, DetectDistance, TargetLayer);
        TakeDamagePerSecond();
    }

    // 정해진 시간마다 데미지 주기
    void TakeDamagePerSecond()
    {
        if(Time.time - time > TakeDmgTime)
        {
            time = Time.time;

            for (int i = 0; i < DetectedObj.Length; i++)
            {
                DetectedObj[i].transform.GetComponent<Bleeding>().isBleeding = true;
                DetectedObj[i].transform.GetComponent<Bleeding>().PoisonFogMonster = this.transform;
                DetectedObj[i].transform.GetComponent<Bleeding>().DmgDistance = DetectDistance;

                //DetectedObj[i].transform.GetComponent<Health>().TakeDamage(Damage);
            }
        }
    }
}
