using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowSerumButton : MonoBehaviour
{
    public Transform TargetPlayer;
    public Transform GetSerumImg;
    public Transform pivot;
    public Material OriginMat;
    public Material OutlineMat;

    [Header("플레이어 판별용")]
    public int ClassNumber;

    public SkinnedMeshRenderer MermanSkin;

    MermanController sc_Controller;

    [Header("감지 거리 (반지름)")]
    public float DetectDistance;

    bool isCreateSerum;

    private void Awake()
    {
        sc_Controller = GetComponent<MermanController>();

        if (sc_Controller.m_MermanType == MermanType.Leader)
        {
            //TargetPlayer = GameManager.Instance.ClientPlayer;
            isCreateSerum = false;
        }
    }

    private void Update()
    {
        if (ClassNumber == Static_Data.m_number)
        {
            if (sc_Controller.isDeath && sc_Controller.m_MermanType == MermanType.Leader && !isCreateSerum)
                CheckPlayerExist();
        }
    }

    void CheckPlayerExist()
    {
        if (CheckCreatedSerum())
            return;

        if (TargetPlayer.GetComponent<PlayerControl>() != null)
        {
            float distance = Vector3.Distance(transform.position, TargetPlayer.position);
            ShowOutline(distance);

            if (distance <= DetectDistance)
            {
                print("제작 가능 상태");
                GetSerumImg.gameObject.SetActive(true);
                Vector3 focusScreenPoint = Camera.main.WorldToScreenPoint(pivot.position);
                GetSerumImg.position = focusScreenPoint;

                TargetPlayer.GetComponent<GetSerum>().TargetMonster = this.transform;
                TargetPlayer.GetComponent<GetSerum>().CanCreateSerum = true;

                //print("실행");
            }
            else
            {
                print("제작 불가능 상태");
                GetSerumImg.gameObject.SetActive(false);
                TargetPlayer.GetComponent<GetSerum>().TargetMonster = null;
                TargetPlayer.GetComponent<GetSerum>().CanCreateSerum = false;
            }
        }
    }

    // 외곽선 그리기
    void ShowOutline(float distance)
    {
        if(distance <= DetectDistance)
        {
            MermanSkin.material = OutlineMat;
        }
        else
            MermanSkin.material = OriginMat;
    }

    bool CheckCreatedSerum()
    {
        if(TargetPlayer.GetComponent<GetSerum>().CheckList(this.transform))
        {
            GetSerumImg.gameObject.SetActive(false);
            MermanSkin.material = OriginMat;
            isCreateSerum = true;
            return true;
        }

        return false;
    }
}
