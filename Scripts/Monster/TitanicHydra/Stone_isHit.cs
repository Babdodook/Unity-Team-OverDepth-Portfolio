using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stone_isHit : MonoBehaviour
{
    bool take_damage_once = false;
    float Damage; // 피해량
    public bool m_canHit = false;

    private void OnTriggerEnter(Collider other)
    {
        Health otherHealth = other.gameObject.GetComponentInParent<Health>(); // 충돌한 오브젝트의 Health스크립트 가져오기
        PlayerControl PlayerController = other.gameObject.GetComponentInParent<PlayerControl>();

        if( otherHealth != null &&
            PlayerController != null &&
            m_canHit)
        {
            take_damage_once = true;

            PlayerController.currentOnHitType = 2;
            Damage = 30f;

            StartCoroutine(DamageDelay(otherHealth));
            m_canHit = false;
        }
    }

    IEnumerator DamageDelay(Health otherHealth)
    {
        otherHealth.TakeDamage(Damage);
        yield return new WaitForSeconds(0.3f);
        take_damage_once = false;

        //print("때렸다");
    }
}
