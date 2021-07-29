using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FGunnerBullet : MonoBehaviour
{
    public Transform Impact;

    [Header("속도")]
    public float Speed;

    [HideInInspector]
    public Vector3 SpawnPos;
    int DestroyDistance;
    Vector3 pos;
    public float y;

    private void Awake()
    {
        //y = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        DestroyDistance = (int)Vector3.Distance(SpawnPos, transform.position);

        // 30미터 경과시 제거
        if (DestroyDistance >= 30f)
            Destroy(this.gameObject);

        transform.Translate(Vector3.forward * Speed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        pos = transform.position;
        pos.y = y + (1.0f - Mathf.Pow(3, (0.02f * DestroyDistance)));
        transform.position = pos;
        //print("PosY: " + (1.0f - Mathf.Pow(3, 0.03f * DestroyDistance)));
        //print("Distance: " + DestroyDistance);
        //print("PosY: " + (1.0f - Mathf.Pow(3, (0.02f * DestroyDistance))));
        //print(pos.y);
    }

    
    private void OnTriggerEnter(Collider other)
    {
        PlayerControl P_Controller = other.GetComponentInParent<PlayerControl>();
        BaseMonsterController M_Controller = other.GetComponentInParent<BaseMonsterController>();

        // 플레이어와 몬스터 제외하고 충돌 시
        if(P_Controller == null && M_Controller == null)
        {
            var item = Instantiate(Impact);
            item.position = transform.position;
            Quaternion rot = transform.rotation;
            rot.y += 180;
            item.rotation = rot;

            Destroy(item.gameObject, 1);
            Destroy(this.gameObject);
        }
        // 플레이어 충돌 시
        else if(P_Controller != null)
        {
            Destroy(this.gameObject);
        }
    }
    
}
