using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using TCP;
using UnityEngine.AI;

public enum MONSTER_STATE
{
    IDLE = 0,
    TRACE,
    BATTLE,
    RUNAWAY,
    DEFENCE,
    ONHIT,
    DEATH,

    Max,
    None
}

public enum BattleType
{
    LightAttack = 0,
    Roll,
    Follow_walk,
    Follow_Run,
    Stay,

    Max
}

public enum MoveType
{
    Stay = 0,
    WalkForward,
    RunForward,
    Right,
    Left,
    ReSet,
    FastRun,

    Max
}

public enum ActionType
{
    Stay = 0,
    WalkForward,
    RunForward,
    Right,
    Left,
    ReSet,
    FastRun,
    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Attack5,
    Evade,

    Max
}

public enum AnimBattleSTATE
{
    None = -1,
    Move = 0,
    Attack1 = 1,
    Attack2 = 2,
    Attack3 = 3,
    Attack4 = 4,
    Attack5 = 5,
    Attack6 = 6,

    Max
}

public enum MonsterLevel
{
    Normal = 0,
    Boss,

    Max
}

public enum UseAStar
{
    None = 0,
    Use,

    Max
}

public class MonsterInfo
{
    public int m_id;
    public Transform m_target;
    public float m_targetDistancce;
    public MONSTER_STATE m_nowState;
}

// 움직임을 지정하고 해당 무빙이 몇초 동안 지속될 것인지 결정
public class MoveAction
{
    public FanaticBattleType Action;
    public THydraActionType THAction;
    public float Time;

    public MoveAction(FanaticBattleType _Action, float _Time)
    {
        Action = _Action;
        Time = _Time;
    }

    public MoveAction(THydraActionType _Action, float _Time)
    {
        THAction = _Action;
        Time = _Time;
    }
}

public class BaseMonsterController : MonoBehaviour
{
    // Astar Grid
    public Grid grid;
    protected AStarCompleted Astar;
    [HideInInspector] public Vector3 Search_Path;
    [HideInInspector] public Vector3 Look_Path;

    public Transform HitBox;
    public Transform pivot;
    public Transform pivotImg;
    // 스크립트 컴포넌트
    protected ViewAngle sc_ViewAngle; // 시야각 스크립트
    public Health sc_Health;   // 체력
    public MonsterSound sc_FanaticSound = null;
    public MermanSound sc_MermanSound = null;

    protected Animator m_Animator;

    public MONSTER_STATE m_nowState;
    [HideInInspector] public MONSTER_STATE m_prevState;
    [HideInInspector] public BattleType m_battleType;
    [HideInInspector] public MoveType m_moveType;

    [Header("대기 상태 감지 거리")]
    [Header("※타겟 감지에 사용될 거리들※")]
    public float idleDetectDistance;
    [Header("추적 상태 감지 거리")]
    public float traceDetectDistance;

    [Header("공격 사정 거리")]
    public float AttackDistance;
    [Header("전투 유지 거리")]
    public float battleDistance;

    [Header("타겟")]
    public Transform Target = null;
    public float TargetDistance;

    LayerMask DetectLayer;
    Collider[] DetectedObj;
    float minDistanceTarget;

    [Header("회전속도")]
    public float extraRotationSpeed;

    [Header("달리기 속도")]
    public float RunSpeed;
    [Header("걷기 속도")]
    public float WalkSpeed;
    [HideInInspector] public float OriginWalkSpeed; 

    protected int m_prevHp;

    [HideInInspector] public CharacterController CC;
    public float gravity;

    [Header("몬스터 등급")]
    public MonsterLevel m_Level;

    #region variables

    [HideInInspector] public bool m_isEvade = false;
    [HideInInspector] public bool m_TargetInBattleZone; // 타겟 트리거 인 플래그
    [HideInInspector] public bool m_isAttack = false;
    [HideInInspector] public bool m_canHit = false;
    [HideInInspector] public bool LockRotation = false;   // 회전 잠금
    [HideInInspector] public bool m_isAttackDelay = false;
    [HideInInspector] public bool OnHitFlag = false;
    [HideInInspector] public bool OnHitOnce = false;
    [HideInInspector] public bool DeathOnce = false;
    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public float MoveTime = 0;
    [HideInInspector] public float AttackDelayTime = 0;
    [HideInInspector] public float EndHitDelayTime = 0;
    [HideInInspector] public float RandomActionTime = 0;
    [HideInInspector] public float prevTime = 0;
    [Header("피격 딜레이 시간")]public float HitDelayTime;
    [HideInInspector] public float currentDamageValue = 0;
    [HideInInspector] public int currentAttackType = 0;     // 공격 타입 (약공격, 강공격)
    [HideInInspector] public int currentOnHitType = 0;      // 피격당했을때, 약, 강인지 판별
    [HideInInspector] public float startMoveTime = 0;
    [HideInInspector] public int m_Player_aggroValue = 0;        // 어그로 수치
    [HideInInspector] public int m_Other_aggroValue = 0;
    // 전투 타입
    [HideInInspector]
    public FanaticBattleType m_FanaticBattleType;

    #endregion

    [HideInInspector] public float v = 0;    // Vertical;
    [HideInInspector] public float h = 0;    // Horizontal;

    protected float BlendLowValue = 0.5f;
    protected float BlendHighValue = 1.0f;

    // 스폰한 지점
    Vector3 spawnPosition;
    protected Vector3 lookrotation;
    [HideInInspector] public Vector3 OriginPosition;

    // TCP 통신 관련
    [HideInInspector] public int FrameCheck;
    [HideInInspector] public int Animation_FrameCheck;
    [HideInInspector] public int Animation_FrameDelay = 0;
    [HideInInspector] public int SetTarget_FrameCheck;
    [HideInInspector] public int ChangeState_FrameCheck;
    [HideInInspector] public int index;
    [HideInInspector] public Vector3 m_desiredPosition;
    [HideInInspector] public Quaternion m_desiredRotation;
    [HideInInspector] public Quaternion SendRotation;
    [HideInInspector] public MoveType m_desiredMoveType;
    [HideInInspector] public float m_desiredV;
    [HideInInspector] public float m_desiredH;
    [HideInInspector] public float m_desiredSpeed = 0;
    [HideInInspector] public bool TCP_isConnected;      // 서버 연결 여부
    [HideInInspector] public bool TCP_setAnimation = false;
    [HideInInspector] public bool m_isMovementAction = false;
    [HideInInspector] public float recv_v = 0;
    [HideInInspector] public float recv_h = 0;

    [HideInInspector] public Vector3 pathPos = Vector3.zero;
    [HideInInspector] public float distance;
    [HideInInspector] public Vector3 direction;
    [HideInInspector] public Vector3 m_desiredMoveDirection;

    // 애니메이션 이동 관련
    [HideInInspector] public bool AttackTranlsate = false;
    [HideInInspector] public bool OnHitTranlsate = false;
    [HideInInspector] public bool EvadeTranslate = false;
    [HideInInspector] public float AnimTranslateSpeed = 0;
    [HideInInspector] public EvadeType m_evadeType;

    protected virtual void Awake()
    {
        spawnPosition = transform.position;
        m_nowState = MONSTER_STATE.IDLE;
        m_moveType = MoveType.Stay;
        m_Animator = GetComponentInChildren<Animator>();
        DetectLayer = 1 << LayerMask.NameToLayer("Player");
        StartCoroutine(ChangeState());

        sc_ViewAngle = GetComponent<ViewAngle>();
        sc_Health = GetComponent<Health>();

        m_prevHp = sc_Health.curHP;

        grid = GetComponent<Grid>();
        Astar = GetComponent<AStarCompleted>();

        CC = GetComponent<CharacterController>();

        OriginPosition = transform.position;
        OriginWalkSpeed = WalkSpeed;
        FrameCheck = 5;
        Animation_FrameCheck = -1;
        ChangeState_FrameCheck = -1;
        TCP_isConnected = false;
        SendRotation = transform.rotation;
    }

    protected virtual void Update()
    {
        UpdateRotateSpeed();
    }

    public void CreateRandomActionTime(float min, float max)
    {
        prevTime = Time.time;
        RandomActionTime = UnityEngine.Random.Range(min, max);
    }

    // 회전속도 조정
    protected virtual void UpdateRotateSpeed()
    {
        if (Target != null)
        {
            Vector3 lookrotation = Look_Path - transform.position;
            lookrotation.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookrotation), extraRotationSpeed * Time.deltaTime);
        }
    }

    // 가장 가까운 타겟 검출하기
    protected void DetectEnemy(float DetectDistance)
    {
        DetectedObj = Physics.OverlapSphere(this.transform.position, DetectDistance, DetectLayer);

        if (DetectedObj.Length != 0)
        {
            //print(DetectedObj[0].gameObject.name);
            minDistanceTarget = Vector3.Distance(transform.position, DetectedObj[0].transform.position);

            Transform _target = null;
            for (int i = 0; i < DetectedObj.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, DetectedObj[i].transform.position);
                if (distance <= minDistanceTarget)
                {
                    minDistanceTarget = distance;
                    _target = DetectedObj[i].transform;
                }
            }

            Target = _target;
        }
    }
        
    // 무브 파라미터 업데이트
    protected virtual void UpdateMoveValue()
    {
        switch (m_moveType)
        {
            case MoveType.Stay:
                v = Mathf.Lerp(v, 0, Time.deltaTime * 2);
                h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
                break;
            case MoveType.WalkForward:
                v = Mathf.Lerp(v, BlendLowValue, Time.deltaTime * 2);
                h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
                break;
            case MoveType.RunForward:
                v = Mathf.Lerp(v, BlendHighValue, Time.deltaTime * 2);
                h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
                break;
            case MoveType.FastRun:
                v = Mathf.Lerp(v, BlendHighValue+0.5f, Time.deltaTime * 2);
                h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
                break;
            case MoveType.Right:
                v = Mathf.Lerp(v, 0, Time.deltaTime * 2);
                h = Mathf.Lerp(h, BlendLowValue, Time.deltaTime * 2);
                break;
            case MoveType.Left:
                v = Mathf.Lerp(v, 0, Time.deltaTime * 2);
                h = Mathf.Lerp(h, -BlendLowValue, Time.deltaTime * 2);
                break;
            case MoveType.ReSet:
                v = 0;
                h = 0;
                break;
        }

        m_Animator.SetFloat("Forward", v);
        m_Animator.SetFloat("Right", h);
    }

    // 대기 상태
    protected virtual void Idle_state()
    {
    }

    // 추적 상태
    protected virtual void Trace_state()
    {
    }

    // 공격 거리 감지하기
    // 사정거리안에 들어오면 공격 실행
    protected virtual void CheckAttackDistance()
    {
    }

    // 전투 상태
    protected virtual void Battle_state()
    {
    }

    // 도주 상태
    protected virtual void Runaway_state()
    {
    }

    // 피격 상태
    protected virtual void OnHit_state()
    {

    }

    // 죽음 상태
    protected virtual void Death_state()
    {

    }

    // 상태 변경 코루틴
    protected virtual IEnumerator ChangeState()
    {
        while (true)
        {
            switch (m_nowState)
            {
                case MONSTER_STATE.ONHIT:
                    OnHit_state();
                    break;
                case MONSTER_STATE.DEATH:
                    Death_state();
                    yield break;        // 죽은 경우 코루틴 종료
                case MONSTER_STATE.IDLE:
                    Idle_state();
                    break;
                case MONSTER_STATE.TRACE:
                    Trace_state();
                    break;
                case MONSTER_STATE.BATTLE:
                    Battle_state();
                    break;
            }

            yield return null;
        }
    }

    // A* 길 탐색
    public bool SearchPath()
    {
        try
        {
            var n = grid.path[0];
            Look_Path = n.worldPosition;

            n = grid.path[1];
            Search_Path = n.worldPosition;

            return true;
        }
        catch (Exception e)
        {
            print(e);
            return false;
        }
    }

    // 서버로부터 위치, 회전값, 속도 받음
    public void TCP_RecvTransform(Vector3 _position, Quaternion _rotation, float _speed)
    {
        m_desiredPosition = _position;
        //m_desiredRotation = _rotation;
        m_desiredSpeed = _speed;
    }

    // 서버로부터 위치, 회전값 받음
    public void TCP_RecvTransform(Vector3 _position, Quaternion _rotation)
    {
        m_desiredPosition = _position;
        m_desiredRotation = _rotation;
    }

    // vertical, horizontal 값 받음
    public void TCP_RecvMoveValue(float forward, float right)
    {
        m_desiredV = forward;
        m_desiredH = right;
    }

    // 속도값 받음
    public void TCP_RecvSpeed(float speed)
    {
        m_desiredSpeed = speed;
    }

    // 포지션, 로테이션 업데이트
    protected void RecvUpdateTransform()
    {
        Vector3 moveDirection = m_desiredPosition - transform.position;
        moveDirection = Vector3.Scale(moveDirection, new Vector3(1, 0, 1)).normalized;

        //if (Vector3.Distance(transform.position, m_desiredPosition) > 0.1f)
        CC.Move(moveDirection * (m_desiredSpeed) * Time.deltaTime);

        //transform.position = Vector3.Lerp(transform.position, m_desiredPosition, 0.1f);
        //transform.rotation = Quaternion.Lerp(transform.rotation, m_desiredRotation, extraRotationSpeed * Time.deltaTime);
    }

    // vertical, horizontal 업데이트
    protected void RecvUpdateMoveValue()
    {
        if (m_isAttack)
        {
            m_desiredV = m_desiredH = 0;
        }

        recv_v = Mathf.Lerp(recv_v, m_desiredV, 2 * Time.deltaTime);
        recv_h = Mathf.Lerp(recv_h, m_desiredH, 2 * Time.deltaTime);

        m_Animator.SetFloat("Forward", recv_v);
        m_Animator.SetFloat("Right", recv_h);
    }

    // 서버로부터 받은 moveType으로 변경
    public void TCP_RecvMoveType(int _moveType)
    {
        m_moveType = (MoveType)_moveType;
    }

    // m_moveType 변경
    public void SetMoveType(MoveType type)
    {
        if (TCP_isConnected)
            m_desiredMoveType = type;
        else
            m_moveType = type;
    }

    // Other 플레이어가 꺼져있다면. 클라이언트 플레이어로 타겟 변경
    protected void CheckOtherPlayerExist(Packing.STATE monsterType)
    {
        // 타겟 지정된 플레이어 나가면 다른 플레이어로 타겟 변경
        if (Target != null)
        {
            if (!GameManager.Instance.OtherPlayer.gameObject.activeSelf)
            {
                Target = GameManager.Instance.ClientPlayer;
                return;
            }

            // 클라이언트 넘버가 1일때, 타겟 플레이어가 죽으면 다른 플레이어로 타겟 변경 패킷 보내기
            if (TCP_isConnected && Static_Data.m_number == 1)
            {
                if (Target.GetComponentInChildren<PlayerAnimControl>().isDie)
                {
                    if (Target == GameManager.Instance.ClientPlayer)
                    {
                        Target = GameManager.Instance.OtherPlayer;
                    }
                    else
                        Target = GameManager.Instance.ClientPlayer;

                    try
                    {
                        int classNumber;
                        if (Target == GameManager.Instance.ClientPlayer)
                        {
                            classNumber = 1;
                        }
                        else
                            classNumber = 2;

                        TCPClient.m_Monster.Monster_SetTarget(
                            monsterType,
                            index,
                            (UInt64)TCPClient.PROTOCOL.M_SET_TARGET,
                            m_nowState,
                            classNumber);

                        TCP_isConnected = true;
                    }
                    catch
                    {

                    }
                }
            }
        }
    }// End CheckOtherPlayerExist


}
