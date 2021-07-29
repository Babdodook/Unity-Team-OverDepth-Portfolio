using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TCP;
using Michsky.UI.Dark;

public enum THydraActionType
{
    Idle = 0,
    Walk_forward,
    Walk_right,
    Walk_left,
    Run,
    FastRun,
    Rotate,

    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Attack5,
    Attack6,

    Evade,
    
    StandToCrawl,
    CrawlToStand,
    StandRoar,

    Groggy,

    Max
}

public enum THydraStance
{
    Stand = 0,
    Crawl,

    Max
}

public class THAttackInfo
{
    public THydraActionType m_ActionType;
    public float m_AttackDistance;

    public THAttackInfo(THydraActionType _ActionType, float _AttackDistance)
    {
        m_ActionType = _ActionType;
        m_AttackDistance = _AttackDistance;
    }
}

public class TitanicHydraController : BaseMonsterController
{
    // 전투 타입
    [HideInInspector]
    public THydraActionType m_THydraActionType;
    THydraActionType m_RecvActionType;

    // 현재 자세
    [HideInInspector] public THydraStance m_CurrentStance;

    [HideInInspector] public Stack<MoveAction> st_MoveAction;
    [HideInInspector] public Stack<THAttackInfo> st_AttackAction;

    bool[] phaseCheck = new bool[2];

    // 체력바 관련
    public TextMeshProUGUI UI_Name;
    public Image HPbar;
    public Image HPBackground;
    float HPbar_width;
    Color hpBgrOrigin;

    [Header("캐스트 시작 위치")]
    public Transform castPosition;
    public LayerMask castLayer;

    public Transform Stone;

    public bool isFindStone = false;
    float originBattleDistance;

    [Header("에이스타 사용 여부")]
    public UseAStar isUseAstar;

    //public Text TestLog;

    #region variables

    [HideInInspector] public bool AfterMove = false;
    [HideInInspector] public bool EndGetWeapon = false;
    [HideInInspector] public bool AfterHit = false;
    [HideInInspector] public bool AfterEvade = false;
    [HideInInspector] public float EvadeDelayTime = 0;
    [HideInInspector] public float evadeTime = 0;
    [HideInInspector] public bool m_NotCheckViewAngle = false;
    [HideInInspector] public bool AfterAttack = false;
    
    [Header("현재 페이즈")] public int m_battlePhase;         // 페이즈
    int RandomAction;
    float OriginAttackDistance;
    [HideInInspector] public int ChangeStanceValue;
    [HideInInspector] public bool toStand = false;

    // 점프 관련
    [HideInInspector] public bool JumpTranslate = false;
    [HideInInspector] public bool SetJumpforce = false;
    [HideInInspector] public float jumpForce;
    float verticalVelocity;
    Vector3 yDirection = Vector3.zero;

    #endregion

    protected override void Awake()
    {
        base.Awake();
        LockRotation = true;
        st_MoveAction = new Stack<MoveAction>();
        st_AttackAction = new Stack<THAttackInfo>();
        OriginAttackDistance = AttackDistance;
        //m_battlePhase = 0;
        jumpForce = 3f;

        for(int i=0;i<phaseCheck.Length;i++)
        {
            phaseCheck[i] = false;
        }

        // hp바 투명
        Color color = HPbar.color;
        color.a = 0;
        HPbar.color = color;

        color = HPBackground.color;
        hpBgrOrigin = HPBackground.color;
        color.a = 0;
        HPBackground.color = color;

        color = UI_Name.color;
        color.a = 0;
        UI_Name.color = color;

        HPbar_width = HPbar.rectTransform.rect.width;

        m_Level = MonsterLevel.Boss;

        originBattleDistance = battleDistance;

        m_desiredPosition = transform.position;
        m_desiredRotation = transform.rotation;

        m_THydraActionType = THydraActionType.Max;
        m_RecvActionType = THydraActionType.Max;

        if (Static_Data.m_number == 1)
        {
            try
            {
                TCPClient.m_Monster.Monster_BeginInfoUpdate(Packing.STATE.TITANICHYDRA, index, sc_Health.maxHP);

                TCP_isConnected = true;
            }
            catch 
            {
                TCP_isConnected = false;
            }
        }
    }

    protected override void Update()
    {
        if (m_nowState == MONSTER_STATE.DEATH)
            return;

        if (Target != null)
            TargetDistance = Vector3.Distance(transform.position, Target.position);

        if (Static_Data.m_number == 1)
        {
            AnimationMovement();
        }

        // 서버 연결되었을때에만 사용
        if (TCP_isConnected)
        {
            RecvUpdateTransform();
        }

        UpdateRotateSpeed();
        UpdateMoveValue();
        OnHitCheck();
        CheckObstacle();
        CheckOtherPlayerExist(Packing.STATE.TITANICHYDRA);
        SetGravity();

        //print("클라이언트 어그로: " + m_Player_aggroValue + " / 아더 어그로: " + m_Other_aggroValue);
    }

    // 중력 적용
    // 이동 방향 벡터 y값에 중력값을 계속 더해준다
    void SetGravity()
    {
        if (CC.isGrounded)
        {
            verticalVelocity = -gravity * Time.deltaTime;
            if (SetJumpforce)
            {
                SetJumpforce = false;
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        yDirection = new Vector3(0, verticalVelocity, 0);
        CC.Move(yDirection * Time.deltaTime);
    }

    protected override void UpdateRotateSpeed()
    {
        if (!LockRotation)
        {
            if (isFindStone)
            {
                lookrotation = Stone.position - transform.position;
                lookrotation.y = 0;

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookrotation), extraRotationSpeed * Time.deltaTime);

                return;
            }

            if (Target != null)
            {
                lookrotation = Target.position - transform.position;
                lookrotation.y = 0;

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookrotation), extraRotationSpeed * Time.deltaTime);
            }
        }
        
    }

    bool sendOnce = false;
    protected override void Idle_state()
    {
        // 플레이어 1일때에만 검사
        if (Static_Data.m_number == 1)
        {
            DetectEnemy(idleDetectDistance);

            if (Target != null)
            {
                if (!sendOnce)
                {
                    sendOnce = true;
                    // 지정한 타겟 서버에게 보내기
                    TCP_SendTarget();
                }
            }
        }
    }

    protected override void Trace_state()
    {
        if (m_CurrentStance == THydraStance.Crawl)
            return;

        // 첫번째 클라이언트만 연산, 서버에게 패킷 보내기
        if (Static_Data.m_number == 1)
        {
            if (m_battlePhase == 1)
            {
                SetMoveDirection(WalkSpeed);
                SetMoveType(MoveType.WalkForward);
            }
            else
            {
                SetMoveDirection(RunSpeed);
                SetMoveType(MoveType.RunForward);
            }

            if (TargetDistance <= battleDistance)
            {
                if (ChangeState_FrameCheck == -1)
                    ChangeState_FrameCheck = 0;

                TCP_SendChangeState(MONSTER_STATE.BATTLE);
                //print("전투 상태로");
            }
        }
    }

    // 타겟과 거리에 따라서 동작 변경
    protected override void CheckAttackDistance()
    {
        // 공격 실행 아닐때에만
        if (m_isAttack || TCP_setAnimation)
            return;

        // 전투 유지거리 벗어나면 추적 상태로
        if (TargetDistance > battleDistance)
        {
            if (ChangeState_FrameCheck == -2)
                ChangeState_FrameCheck = 0;

            // 어그로 수치에 따라 타겟 변경
            if (TCP_isConnected)
            {
                if (m_Player_aggroValue > m_Other_aggroValue)
                {
                    Target = GameManager.Instance.ClientPlayer;
                    //print("타겟 클라이언트 플레이어로 지정");
                }
                else if (m_Player_aggroValue < m_Other_aggroValue)
                {
                    Target = GameManager.Instance.OtherPlayer;
                    //print("타겟 아더 플레이어로 지정");
                }

                SetTarget_FrameCheck = 0;
                TCP_SendTarget();
                m_Player_aggroValue = 0;
                m_Other_aggroValue = 0;
                //print("타겟 변경 실행");
            }

            TCP_SendChangeState(MONSTER_STATE.TRACE);
            //print("추적 상태로");
        }
        // 전투 거리 내에 타겟 존재
        else if (TargetDistance <= battleDistance)
        {
            // 페이즈 변경 시 특정 동작 실행
            if (PhaseCheck())
                return;

            // 4족 또는 2족으로 자세 변경
            if (ChangeStance())
                return;

            // 공격 동작 세팅
            SetAttackAction();
            // 돌던지는 공격이면, 먼저 돌을 찾으러 이동
            if (st_AttackAction.Peek().m_ActionType == THydraActionType.Attack4)
            {
                battleDistance = 50f;

                if (FindStone())
                {
                    m_THydraActionType = THydraActionType.Run;
                    if (Vector3.Distance(Stone.position, transform.position) <= 3)
                    {
                        Stone.GetComponent<StoneMove>().OffCollider();
                        if (Vector3.Distance(Stone.position, transform.position) <= 2)
                        {
                            // 서버연결되있는 경우, 돌 찾음.
                            if (TCP_isConnected)
                            {
                                TCPClient.m_Monster.Monster_BattleInfo(
                                    Packing.STATE.TITANICHYDRA,
                                    index,
                                    (UInt64)TCPClient.PROTOCOL.M_FINDSTONE,
                                    0);
                            }
                            else
                            {
                                isFindStone = false;
                            }
                        }
                        else
                            return;
                    }
                    else
                        return;
                }
            }

            // 공격 사정거리 지정하기
            AttackDistance = st_AttackAction.Peek().m_AttackDistance;

            // 사정거리 안에 들어오면 공격 동작 수행
            if(TargetDistance <= AttackDistance)
            {
                // 공격 동작 수행하도록 현재 액션타입에 공격 지정
                m_THydraActionType = st_AttackAction.Pop().m_ActionType;
                Animation_FrameCheck = Animation_FrameDelay;

                if(TCP_isConnected)
                    TCP_setAnimation = true;
                return;
            }

            // 움직임 세팅
            SetMovement();
            ChangeMovement();
        }
    }

    Collider[] Stones;
    public LayerMask StoneLayer;
    bool FindStone()
    {
        if (isFindStone)
            return true;

        Stones = Physics.OverlapSphere(this.transform.position, 30f, StoneLayer);
        if (Stones.Length != 0)
        {
            //print(Stones[0].gameObject.name);
            float minDistance = Vector3.Distance(transform.position, Stones[0].transform.position);

            Transform _target = null;
            for (int i = 0; i < Stones.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, Stones[i].transform.position);
                if (distance <= minDistance)
                {
                    minDistance = distance;
                    _target = Stones[i].transform;
                }
            }

            Stone = _target;
            Stone.GetComponent<StoneMove>().Target = Target;
            int SendStoneIndex = GameManager.Instance.GetCurrentStoneIndex(Stone);

            // 서버연결되있는 경우, 돌 찾으러 가라고 보내기
            if (TCP_isConnected)
            {
                TCPClient.m_Monster.Monster_BattleInfo(
                    Packing.STATE.TITANICHYDRA,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_SETSTONE,
                    SendStoneIndex);

                TCPClient.m_Monster.Monster_BattleInfo(
                    Packing.STATE.TITANICHYDRA,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_FINDSTONE,
                    1);
            }
            else
                isFindStone = true;

            return true;
        }

        return false;
    }

    int prevAction;
    // 공격 동작 지정
    // 공격 스택에 공격 동작을 세팅
    public void SetAttackAction()
    {
        if (st_AttackAction.Count == 0)
        {
            RandomAction = UnityEngine.Random.Range(0, 100);

            // 4족 자세일 경우
            // 4족 자세에 맞는 공격만 허용
            if (m_CurrentStance == THydraStance.Crawl)
            {
                if(m_battlePhase == 2)
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack5, 8f));
                }
                else if(m_battlePhase == 3)
                {
                    if(TargetDistance >= 8f)
                        st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack6, 14f));
                    else
                        st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack5, 8f));
                }
                
                return;
            }

            // 1 페이즈 공격 설정
            if (m_battlePhase == 1)
            {
                // 스매쉬어택 -> 콤보어택
                if (RandomAction > 50)
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack1, 5f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack2, 3.5f));
                }
                // 콤보어택 -> 스매쉬어택
                else
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack2, 3.5f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack1, 5f));
                }
            }
            // 2 페이즈 공격 설정
            else if (m_battlePhase == 2)
            {
                // 콤보어택 -> 콤보어택 -> 스매쉬어택
                if (RandomAction > 65)
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack2, 20f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack1, 20f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack1, 5f));
                }
                // 콤보어택 -> 스매쉬 어택 -> 러쉬어택
                else if(RandomAction > 35)
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack3, 20f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack2, 20f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack1, 5f));
                }
                // 러쉬어택 -> 스매쉬어택
                else
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack2, 20f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack3, 20f));
                }
            }
            // 3 페이즈 공격 설정
            else if (m_battlePhase == 3)
            {
                // 돌던지기, 러쉬어택
                if (RandomAction > 60)
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack4, 30f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack3, 20f));
                }
                // 스매쉬 어택
                else if(RandomAction > 20)
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack2, 3.5f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack1, 5f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack2, 3.5f));
                }
                else
                {
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack4, 30f));
                    //st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack3, 20f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack3, 20f));
                    st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack3, 20f));
                }
            }
            // 테스트용
            else if(m_battlePhase == 0)
            {
                st_AttackAction.Push(new THAttackInfo(THydraActionType.Attack4, 30f));
            }
        }
        
    }

    // 지정된 전투 동작 수행
    protected override void Battle_state()
    {
        if(Static_Data.m_number == 1)
        {
            //애니메이션 지정 -> try { 서버한테 먼저 보내기 } catch { 애니메이션 실행 }
            //                                            -> 서버로부터 받은 애니메이션 실행
            //                                                                     -> 원래 로직대로 실행

            // 거리에 따라 애니메이션 스택에 쌓기
            CheckAttackDistance();

            if (!m_isAttack && m_nowState == MONSTER_STATE.BATTLE)
            {
                PlayMovementAction(m_THydraActionType);
                Check_PlayBattleAction(m_THydraActionType);
            }
        }

        // 서버연결 되있는 경우, 서버에서 받은 애니메이션 실행
        if(TCP_isConnected)
        {
            if(!m_isAttack && m_nowState == MONSTER_STATE.BATTLE)
                PlayBattleAction(m_RecvActionType);
        }
    }

    // 서버와 연결 체크 후, 서버에 애니메이션 보내기
    // 서버연결 안되있는 경우엔 공격 애니메이션 실행
    void Check_PlayBattleAction(THydraActionType actionType)
    {
        // 이동 애니메이션 실행이면, 공격 애니메이션은 실행 안하도록 종료
        if (m_isMovementAction)
            return;

        // 서버 연결되있으면, 애니메이션 보내고 밑에 실행문은 실행안함.
        if (TCP_SendAnimation((int)actionType))
        {
            return;
        }

        // 공격 애니메이션 실행
        PlayBattleAction(actionType);
    }

    // actionType에 따른 이동 애니메이션 실행 또는 서버에게 보내기
    void PlayMovementAction(THydraActionType actionType)
    {
        float speed = 0;
        switch (actionType)
        {
            case THydraActionType.Walk_forward:
                SetMoveType(MoveType.WalkForward);
                LockRotation = false;

                if (m_CurrentStance == THydraStance.Crawl)
                    speed = WalkSpeed + 1;
                else
                    speed = WalkSpeed;

                break;
            case THydraActionType.Run:
                SetMoveType(MoveType.RunForward);
                LockRotation = false;

                if (isFindStone)
                {
                    speed = RunSpeed;
                }
                else
                {
                    if (m_CurrentStance == THydraStance.Crawl)
                        speed = RunSpeed + 2;
                    else
                        speed = RunSpeed;
                }

                break;
            default:
                m_isMovementAction = false;
                return;
        }

        if (isFindStone)
        {
            MoveToStone(speed);
        }
        else
        {
            SetMoveDirection(speed);
        }

        m_isMovementAction = true;
    }

    // actionType에 따른 공격 애니메이션 실행
    void PlayBattleAction(THydraActionType actionType)
    {
        switch (actionType)
        {
            case THydraActionType.StandToCrawl:     // 2족 -> 4족 변경
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;

                m_Animator.SetTrigger("StandToCrawl_Roar");

                break;
            case THydraActionType.CrawlToStand:     // 4족 -> 2족 변경
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;

                m_Animator.SetTrigger("CrawlToStand");

                break;
            case THydraActionType.StandRoar:
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;

                m_Animator.SetTrigger("StandRoar");

                break;
            case THydraActionType.Groggy:
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;
                LockRotation = true;

                m_Animator.SetTrigger("Groggy");
                break;
            case THydraActionType.Attack1:          // 두팔 번갈아가며 휘두르기
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;
                LockRotation = false;

                currentDamageValue = 25f;
                currentAttackType = 1;

                m_Animator.SetInteger("Attack", (int)AnimBattleSTATE.Attack1);

                break;
            case THydraActionType.Attack2:          // 두팔 모아서 아래로 찍기
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;
                LockRotation = false;

                currentDamageValue = 25f;
                currentAttackType = 2;

                m_Animator.SetInteger("Attack", (int)AnimBattleSTATE.Attack2);

                break;
            case THydraActionType.Attack3:          // 러쉬 어택, 몸통박치기
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;
                LockRotation = false;

                currentDamageValue = 25f;
                currentAttackType = 2;

                m_Animator.SetInteger("Attack", (int)AnimBattleSTATE.Attack3);

                break;
            case THydraActionType.Attack4:          // 돌던지기
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;
                LockRotation = false;

                battleDistance = originBattleDistance;

                currentDamageValue = 20f;
                currentAttackType = 1;

                m_Animator.SetInteger("Attack", (int)AnimBattleSTATE.Attack4);

                break;
            case THydraActionType.Attack5:          // 4족 , 짧게 점프해서 때리기
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;
                LockRotation = false;

                currentDamageValue = 30f;
                currentAttackType = 2;

                m_Animator.SetInteger("Attack", (int)AnimBattleSTATE.Attack5);

                break;
            case THydraActionType.Attack6:          // 4족 , 길게 점프해서 때리기
                m_isAttack = true;
                isMoving = false;
                SetMoveType(MoveType.Stay);
                TCP_setAnimation = false;
                m_desiredSpeed = 0;
                LockRotation = false;

                currentDamageValue = 30f;
                currentAttackType = 2;

                m_Animator.SetInteger("Attack", (int)AnimBattleSTATE.Attack6);

                break;
            case THydraActionType.Max:
                break;
        }

        m_RecvActionType = THydraActionType.Max;
    }

    float RandomTime;
    // 이동 애니메이션 지정
    void SetMovement()
    {
        if (!isMoving)
        {
            RandomAction = UnityEngine.Random.Range(0, 100);

            if(m_battlePhase == 1)
            {
                RandomTime = UnityEngine.Random.Range(3.0f, 4.0f);
                st_MoveAction.Push(new MoveAction(THydraActionType.Walk_forward, RandomTime));
            }
            else if(m_battlePhase == 2)
            {
                if (TargetDistance >= 7f)
                {
                    RandomTime = UnityEngine.Random.Range(2.0f, 3.0f);
                    st_MoveAction.Push(new MoveAction(THydraActionType.Run, RandomTime));
                }
                else
                {
                    RandomTime = UnityEngine.Random.Range(3.0f, 5.0f);
                    st_MoveAction.Push(new MoveAction(THydraActionType.Walk_forward, RandomTime));
                }
            }
            else
            {
                if(RandomAction > 80)
                {
                    RandomTime = UnityEngine.Random.Range(3.0f, 5.0f);
                    st_MoveAction.Push(new MoveAction(THydraActionType.Walk_forward, RandomTime));
                }
                else
                {
                    RandomTime = UnityEngine.Random.Range(3.0f, 5.0f);
                    st_MoveAction.Push(new MoveAction(THydraActionType.Run, RandomTime));
                }
            }

            isMoving = true;
        }
    }

    // 지정된 이동 애니메이션으로 변경하기
    void ChangeMovement()
    {
        if (isMoving)
        {
            // 움직임 스택 Pop하고 지정된 시간 세팅
            if (MoveTime < 0)
            {
                MoveAction tempMoveAction = st_MoveAction.Pop();
                MoveTime = tempMoveAction.Time;
                startMoveTime = Time.time;
                m_THydraActionType = tempMoveAction.THAction;
            }

            // 지정된 시간 지나면 MoveTime 초기화
            if (Time.time - startMoveTime >= MoveTime)
            {
                MoveTime = -1;

                // 지정된 움직임 끝나면 다시 재설정하도록
                if (st_MoveAction.Count == 0)
                {
                    isMoving = false;
                    //print("재설정");
                }
            }
        }
    }

    // 다음 페이즈로 변경 시킴
    bool PhaseCheck()
    {
        if (m_battlePhase == 2 && !phaseCheck[0] && m_CurrentStance == THydraStance.Stand)
        {
            phaseCheck[0] = true;
            m_THydraActionType = THydraActionType.StandRoar;

            Animation_FrameCheck = Animation_FrameDelay;
            if (TCP_isConnected)
                TCP_setAnimation = true;

            TCP_SendBattleInfo();
            //print("페이즈2 시작");
            return true;
        }
        else if (m_battlePhase == 3 && !phaseCheck[1] && m_CurrentStance == THydraStance.Stand)
        {
            phaseCheck[1] = true;

            st_AttackAction.Clear();
            //st_AttackAction.Push(new THAttackInfo(THydraActionType.StandRoar, 50f));
            //m_THydraActionType = THydraActionType.StandRoar;

            m_THydraActionType = THydraActionType.Groggy;

            Animation_FrameCheck = Animation_FrameDelay;
            if (TCP_isConnected)
                TCP_setAnimation = true;

            TCP_SendBattleInfo();
            //print("페이즈3 시작");
            return true;
        }

        return false;
    }

    // 현재 자세 변경 2족, 4족
    bool ChangeStance()
    {
        if (toStand && m_CurrentStance == THydraStance.Crawl)
        {
            m_THydraActionType = THydraActionType.CrawlToStand;
            m_CurrentStance = THydraStance.Stand;
            st_AttackAction.Clear();

            ChangeStanceValue = 0;
            toStand = false;

            Animation_FrameCheck = Animation_FrameDelay;
            if (TCP_isConnected)
                TCP_setAnimation = true;

            return true;
        }
        else if (ChangeStanceValue >= 7 && m_CurrentStance == THydraStance.Stand)
        {
            m_THydraActionType = THydraActionType.StandToCrawl; // 4족 변경 액션
            m_CurrentStance = THydraStance.Crawl;               // 현재 자세 4족으로 변경
            st_AttackAction.Clear();                            // 액션스택을 비운다. 지정된 공격 패턴 초기화

            Animation_FrameCheck = Animation_FrameDelay;
            if (TCP_isConnected)
                TCP_setAnimation = true;

            return true;
        }
        
        return false;
    }

    bool hideHpBarOnce = false;
    // 현재 체력에따라 피격 체크
    public void OnHitCheck()
    {
        // 체력 닳았을때
        if (m_prevHp > sc_Health.curHP)
        {
            // UI 갱신
            ImageWidthSlider(sc_Health.curHP, sc_Health.maxHP, HPbar.rectTransform, HPbar_width);

            // 죽음
            if (sc_Health.curHP <= 0)
            {
                if (Static_Data.m_number == 1)
                {
                    try
                    {
                        TCPClient.m_Monster.Monster_Die(Packing.STATE.TITANICHYDRA, index, transform.position, transform.rotation);
                    }
                    catch { }
                }

                m_nowState = MONSTER_STATE.DEATH;

                // 체력바 감추기
                if(!hideHpBarOnce)
                {
                    hideHpBarOnce = true;
                    StartCoroutine(HideHPbar());
                }
            }
            else if (sc_Health.curHP <= sc_Health.maxHP / 100 * 50)
            {
                m_battlePhase = 3;
            }
            else if(sc_Health.curHP <= sc_Health.maxHP / 100 * 80)
            {
                m_battlePhase = 2;
            }
            else if (sc_Health.curHP <= sc_Health.maxHP)
            {
                m_battlePhase = 1;
                //print("체력 닳음");
                //OnHitFlag = true;
                //OnHitOnce = false;
            }

            // 피격 누적 포인트
            if (m_battlePhase > 1 && !isFindStone)
            {
                // 일반, 강공격 맞으면 1 상승
                if (currentOnHitType < 2)
                    ChangeStanceValue += 1;
                // 블러드 포인트 공격 맞으면 2 상승
                else
                    ChangeStanceValue += 2;
            }
        }

        m_prevHp = sc_Health.curHP;
    }

    protected override void OnHit_state()
    {
        if (!OnHitOnce)
        {
            //print("피격 실행");
            OnHitOnce = true;
            m_Animator.SetInteger("OnHit", 0);
        }
    }

<<<<<<< Updated upstream
    IEnumerator Death_state()
=======
    protected override void Death_state()
>>>>>>> Stashed changes
    {
        if (!DeathOnce)
        {
            Target = null;
            DeathOnce = true;
            m_Animator.SetTrigger("Death");

            yield return new WaitForSeconds(3f);

            var Complete = GameObject.Find("CreaterManager").transform.GetChild(2).gameObject;
            Complete.SetActive(true);

            yield return new WaitForSeconds(5f);

            Complete.SetActive(false);
            var Creadit = GameObject.Find("CreaterManager").transform.GetChild(3).gameObject;
            Creadit.SetActive(true);            
        }
    }

    // 타겟이 있는 방향(m_desiredMoveDirection) 으로 이동
    void SetMoveDirection(float speed)
    {
        if (isUseAstar == UseAStar.None)
        {
            pathPos = Target.position - transform.position;
            distance = pathPos.magnitude;
            direction = pathPos / distance;
            m_desiredMoveDirection = Vector3.Scale(direction, new Vector3(1, 0, 1)).normalized;
        }
        else
        {
            if (!grid.pathfind_active)
                Astar.ReQuestFindPath(Target.position, transform.position);

            if (grid.path.Count >= 2)
            {
                if (!SearchPath())
                {
                    grid.pathfind_active = false;
                    return;
                }
            }
            else
            {
                //print("Empty");
                SetMoveType(MoveType.Stay);
                grid.pathfind_active = false;
                return;
            }

            Search_Path.y = transform.position.y;

            // 장애물 존재, 에이스타 사용 노드를 따라감
            if (isObstacleExist)
            {
                pathPos = Search_Path - transform.position;
                //print(" 장애물 존재, 에이스타 사용중 ");
            }
            // 장애물 없음, 직진
            else
            {
                pathPos = Target.position - transform.position;
                //print(" 장애물 없음 ");
            }

            distance = pathPos.magnitude;
            direction = pathPos / distance;
            m_desiredMoveDirection = Vector3.Scale(direction, new Vector3(1, 0, 1)).normalized;
            grid.pathfind_active = false;
        }

        // 서버 연결 없으면 바로 움직임
        if(!TCP_SendMovement(speed))
        {
            CC.Move(m_desiredMoveDirection * speed * Time.deltaTime);
            //print("서버연결 없음");
        }
    }

    // 돌이 있는 방향(m_desiredMoveDirection) 으로 이동
    void MoveToStone(float speed)
    {
        pathPos = Stone.position - transform.position;
        distance = pathPos.magnitude;
        direction = pathPos / distance;
        m_desiredMoveDirection = Vector3.Scale(direction, new Vector3(1, 0, 1)).normalized;

        if(!TCP_SendMovement(speed))
        {
            CC.Move(m_desiredMoveDirection * speed * Time.deltaTime);
        }
    }

    RaycastHit hit;
    bool isObstacleExist;
    void CheckObstacle()
    {
        if (Target != null)
        {
            float maxDistance = TargetDistance;
            Vector3 direction = Target.position - transform.position;
            direction.Normalize();

            if (Physics.SphereCast(castPosition.position, castPosition.position.y / 2 - 0.1f, direction, out hit, maxDistance, castLayer))
            {
                Debug.DrawRay(castPosition.position, direction * hit.distance, Color.red);
                isObstacleExist = true;
                /*
                // 타겟이 10미터 안에 있을때, 장애물 검출되면 에이스타 사용하도록
                if (TargetDistance <= 10)
                {
                    Debug.DrawRay(castPosition.position, direction * hit.distance, Color.red);
                    isObstacleExist = true;
                }
                // 멀리 있을땐 상관없이 직진
                else
                {
                    Debug.DrawRay(castPosition.position, direction * hit.distance, Color.white);
                    isObstacleExist = false;
                }
                */
                //Gizmos.DrawWireSphere(castPosition.position + direction * hit.distance, castPosition.position.y / 2 - 0.1f);
            }
            else
            {
                Gizmos.color = Color.white;
                Debug.DrawRay(castPosition.position, direction * hit.distance, Color.white);

                isObstacleExist = false;
            }
        }
    }

    protected override IEnumerator ChangeState()
    {
        while (true)
        {
            switch (m_nowState)
            {
                case MONSTER_STATE.IDLE:
                    Idle_state();
                    break;
                case MONSTER_STATE.TRACE:
                    Trace_state();
                    break;
                case MONSTER_STATE.BATTLE:
                    Battle_state();
                    break;
                case MONSTER_STATE.DEFENCE:
                    //Defence_state();
                    break;
                case MONSTER_STATE.ONHIT:
                    OnHit_state();
                    break;
                case MONSTER_STATE.DEATH:
                    StartCoroutine(Death_state());
                    yield break;
            }

            yield return null;
        }
    }

    IEnumerator ShowHPbar()
    {
        //print("체력바 보이기");
        Color color;
        while(true)
        {
            color = HPbar.color;
            color.a += 0.01f;
            HPbar.color = color;

            color = UI_Name.color;
            color.a += 0.01f;
            UI_Name.color = color;

            if (HPBackground.color.a <= hpBgrOrigin.a)
            {
                color = HPBackground.color;
                color.a += 0.01f;
                HPBackground.color = color;
            }

            if (color.a >= 1)
                break;

            yield return new WaitForSeconds(0.01f);
        }
    }

    IEnumerator HideHPbar()
    {
        Color color;
        while (true)
        {
            color = HPBackground.color;
            color.a -= 0.01f;
            HPBackground.color = color;

            color = HPbar.color;
            color.a -= 0.01f;
            HPbar.color = color;

            color = UI_Name.color;
            color.a -= 0.01f;
            UI_Name.color = color;

            if (color.a <= 0)
                break;

            yield return new WaitForSeconds(0.01f);
        }
    }

    // 현재 수치값
    // 최대값
    // 체력 이미지
    // 체력 이미지 width값
    void ImageWidthSlider(float current, float size, RectTransform Image, float size2)
    {
        float per = current / size * 100;
        //print("per: " + per + " // width: " + (size2 * per / 100));
        Image.sizeDelta = new Vector2((size2 * per / 100f), Image.rect.height);
    }

    // 애니메이션 실행시, 이동하기
    void AnimationMovement()
    {
        if (AttackTranlsate)
        {
            if (TargetDistance > 1f)
            {
                m_desiredMoveDirection = transform.forward;
                if(!TCP_SendMovement(AnimTranslateSpeed))
                    CC.Move(transform.forward * AnimTranslateSpeed * Time.deltaTime);
            }
        }

        if (OnHitTranlsate)
        {
            CC.Move((transform.forward * -1) * AnimTranslateSpeed * Time.deltaTime);
        }

        if (JumpTranslate)
        {
            AnimTranslateSpeed = Mathf.Lerp(AnimTranslateSpeed, 0, 2f * Time.deltaTime);

            m_desiredMoveDirection = transform.forward;
            if (!TCP_SendMovement(AnimTranslateSpeed))
                CC.Move(transform.forward * AnimTranslateSpeed * Time.deltaTime);
            //print("점프 이동중");
        }
    }

    // -------------------------------------- Send Management -------------------------------------

    // 상태 변경하기
    public void TCP_SendChangeState(MONSTER_STATE desiredState)
    {
        if (ChangeState_FrameCheck == 0)
        {
            switch(m_nowState)
            {
                case MONSTER_STATE.TRACE:
                    ChangeState_FrameCheck = -2;
                    break;
                case MONSTER_STATE.BATTLE:
                    ChangeState_FrameCheck = -1;
                    break;
            }

            try
            {
                TCPClient.m_Monster.Monster_ChangeState(
                        Packing.STATE.TITANICHYDRA,
                        index,
                        (UInt64)TCPClient.PROTOCOL.M_CHANGE_STATE,
                        m_nowState,
                        desiredState);

                TCP_isConnected = true;
            }
            catch
            {
                print("Monster_ChangeState / No connection found with Server");
                TCP_isConnected = false;

                switch (desiredState)
                {
                    case MONSTER_STATE.TRACE:
                        SetMoveType(MoveType.WalkForward);
                        //print("추적 상태로");
                        break;
                    case MONSTER_STATE.BATTLE:
                        //print("전투 상태로");

                        m_isAttack = false;
                        isMoving = false;
                        MoveTime = 0;
                        break;
                }

                m_nowState = desiredState;
            }
        }
    }

    // 지정한 타겟 보내기
    public void TCP_SendTarget()
    {
        // 한번만 보내기
        if (SetTarget_FrameCheck == 0)
        {
            SetTarget_FrameCheck = -1;
            // 서버에게 플레이어 식별코드 보내기
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
                    Packing.STATE.TITANICHYDRA,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_SET_TARGET,
                    m_nowState,
                    classNumber);

                TCP_isConnected = true;
            }
            catch
            {
                print("Monster_SetTarget / No connection found with Server");
                TCP_isConnected = false;

                switch(m_nowState)
                {
                    case MONSTER_STATE.IDLE:
                        m_nowState = MONSTER_STATE.TRACE;
                        LockRotation = false;

                        StartCoroutine(ShowHPbar());
                        //print("추적상태로");
                        break;
                }
                
            }
        }
    }

    // 이동할 위치, 회전값 보내기
    public bool TCP_SendMovement(float speed)
    {
        if (!TCP_isConnected)
            return false;

        // 5프레임 단위로 서버에게 이동할 위치 전송
        ++FrameCheck;
        if (FrameCheck > 5)
        {
            FrameCheck = -1;
            Vector3 desiredPosition = transform.position + (m_desiredMoveDirection); // * speed * Time.deltaTime * 5);

            try
            {
                TCPClient.m_Monster.Monster_Movement(
                    Packing.STATE.TITANICHYDRA,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_MOVE,
                    desiredPosition,
                    m_desiredMoveType,
                    speed);

                TCP_isConnected = true;

                return true;
            }
            catch
            {
                print("Monster_Movement / No connection found with Server");
                TCP_isConnected = false;
                return false;
            }
        }

        return true;
    }

    // 서버에게 애니메이션 보내기
    public bool TCP_SendAnimation(int anim)
    {
        if (!TCP_isConnected)
            return false;

        if (Animation_FrameCheck == Animation_FrameDelay)
        {
            Animation_FrameCheck = -1;
            // 애니메이션 보내기
            try
            {
                TCPClient.m_Monster.Monster_Animation(
                    Packing.STATE.TITANICHYDRA,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_ATTACK,
                    transform.position,
                    transform.rotation,
                    v,
                    h,
                    anim);

                TCP_isConnected = true;
                return true;
            }
            catch
            {
                print("Monster_Animation / No connection found with Server");

                TCP_isConnected = false;
                return false;
            }
        }

        return true;
    }

    // 서버에게 현재 속도 보내기
    public void TCP_SendSpeed(float speed)
    {
        try
        {
            TCPClient.m_Monster.Monster_SetSpeed(
                    Packing.STATE.TITANICHYDRA,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_SET_SPEED,
                    speed);

            TCP_isConnected = true;
        }
        catch
        {
            print("Monster_SetSpeed / No connection found with Server");

            TCP_isConnected = false;
        }
    }

    // 서버에게 현재 페이즈 보내기
    public bool TCP_SendBattleInfo()
    {
        if (!TCP_isConnected)
            return false;

        try
        {
            TCPClient.m_Monster.Monster_BattleInfo(
                    Packing.STATE.TITANICHYDRA,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_BATTLEINFO,
                    m_battlePhase);

            TCP_isConnected = true;
            return true;
        }
        catch
        {
            print("Monster_BattleInfo / No connection found with Server");

            TCP_isConnected = false;
            return false;
        }
    }

    // 서버에게 받은 데미지 보내기
    public void TCP_SendDamageInfo(int damage)
    {
        if (!TCP_isConnected)
            return;

        try
        {
            TCPClient.m_Monster.Monster_Hit(
                    Packing.STATE.TITANICHYDRA,
                    index,
                    transform.position,
                    transform.rotation,
                    0,
                    damage);

            TCP_isConnected = true; 
        }
        catch
        {
            print("Monster_Hit / No connection found with Server");

            TCP_isConnected = false;
        }
    }

    // --------------------------------------------------------------------------------------------
    // -------------------------------------- Recv Management -------------------------------------

    // 서버로부터 타겟 받음
    public void TCP_RecvTarget(MONSTER_STATE state, int classNumber)
    {
        if (Static_Data.m_number == classNumber)
        {
            Target = GameManager.Instance.ClientPlayer;
        }
        else
            Target = GameManager.Instance.OtherPlayer;

        //print("Target: " + Target.gameObject.name);

        switch (state)
        {
            case MONSTER_STATE.IDLE:        // 아이들상태일때, 추적상태로 넘어가도록
                m_nowState = MONSTER_STATE.TRACE;
                LockRotation = false;

                StartCoroutine(ShowHPbar());

                break;
            case MONSTER_STATE.TRACE:       // 추적상태일때, 타겟 재설정하고 전투로 넘어감
                m_nowState = MONSTER_STATE.BATTLE;
                break;
        }
    }

    // 상태 받아서 변경하기
    public void TCP_RecvChangeState(int _nowState, int _desiredState)
    {
        switch((MONSTER_STATE)_nowState)
        {
            case MONSTER_STATE.TRACE:       // 현재 추적 상태일때
                m_isAttack = false;
                isMoving = false;
                MoveTime = 0;

                break;
            case MONSTER_STATE.BATTLE:      // 현재 배틀 상태일때
                switch((MONSTER_STATE)_desiredState)
                {
                    case MONSTER_STATE.TRACE:
                        SetMoveType(MoveType.WalkForward);
                        //m_moveType = MoveType.WalkForward;
                        break;
                    case MONSTER_STATE.DEATH:
                        break;
                }
                break;
        }

        m_nowState = (MONSTER_STATE)_desiredState;
    }

    // 받은 애니메이션 m_THydraActionType에 지정
    public void TCP_RecvAnimation(int anim)
    {
        m_RecvActionType = (THydraActionType)anim;
        //print("공격 애니메이션 받음: " + m_RecvActionType);
    }

    // 받은 페이즈 지정
    public void TCP_RecvBattleInfo(int _battlePhase)
    {
        m_battlePhase = _battlePhase;
    }

    // 계산된 HP 받음
    public void TCP_RecvHPInfo(int curHP)
    {
        sc_Health.curHP = curHP;
    }

    // 캐릭터 번호에 따라서, 데미지 누적
    public void TCP_RecvDamageType(int curHP, int serial)
    {
        if(serial == 2)
        {
            int Damage = m_prevHp - curHP;
            m_Other_aggroValue += Damage;
        }
    }

    // 돌 찾으러가라는 bool값 받기
    public void TCP_RecvFindStone(int check)
    {
        isFindStone = Convert.ToBoolean(check);
    }

    // 던지려는 돌 인덱스 가져와서 Stone에 할당
    public void TCP_RecvSetStone(int stoneIndex)
    {
        Stone = GameManager.Instance.GetCurrentStoneTransform(stoneIndex);
        Stone.GetComponent<StoneMove>().Target = Target;
    }

    public void TCP_RecvDeathPosition(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }
}



