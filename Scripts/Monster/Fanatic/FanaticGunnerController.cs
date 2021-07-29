using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Sockets;
using TCP;

public enum FGunnerAnimBattleType
{
    Move = 0,
    Attack0102 = 1,
    RunAttack = 2,
    Kick = 3,
    GunAttack = 4,

    Max
}

public enum FGunnerActionType
{
    Idle = 0,
    Walk_forward,
    Walk_right,
    Walk_left,
    Run,
    Attack0102,
    RunAttack,
    Kick,
    GunAttack,
    Rotate,
    Evade,

    Max
}

public class FanaticGunnerController : BaseMonsterController
{
    // 전투 타입
    [HideInInspector]
    public FGunnerActionType m_FGunnerActionType;
    FanaticBattleType m_RecvActionType;

    [HideInInspector]
    public Stack<MoveAction> st_MoveAction;

    [Header("캐스트 시작 위치")]
    public Transform castPosition;
    public LayerMask castLayer;

    [Header("에이스타 사용 여부")]
    public UseAStar isUseAstar;

    // 체력바 관련
    public TextMeshProUGUI UI_Name;
    public Image HPbar;
    public Image HPBackground;
    float HPbar_width;
    Color hpBgrOrigin;

    #region variables

    [HideInInspector] public bool AfterMove = false;
    [HideInInspector] public bool EndGetWeapon = false;
    [HideInInspector] public bool AfterHit = false;
    [HideInInspector] public bool AfterEvade = false;
    [HideInInspector] public float EvadeDelayTime = 0;
    [HideInInspector] public float evadeTime = 0;
    [HideInInspector] public bool m_NotCheckViewAngle = false;
    [HideInInspector] public bool AfterAttack = false;
    bool GunAttackOnce = false;

    #endregion

    protected override void Awake()
    {
        base.Awake();
        LockRotation = true;
        st_MoveAction = new Stack<MoveAction>();
        //m_Level = MonsterLevel.Boss;

        isObstacleExist = true;

        if (m_Level == MonsterLevel.Boss)
        {
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
        }
        //StartCoroutine(ShowHPbar());
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        if (Static_Data.m_number == 1)
        {
            try
            {
                TCPClient.m_Monster.Monster_BeginInfoUpdate(Packing.STATE.FANATIC_GUN, index, sc_Health.maxHP);
            }
            catch { }
        }
    }

    protected override void Update()
    {
        if (m_nowState == MONSTER_STATE.DEATH)
            return;

        EvadeDelayTime += Time.deltaTime;

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
        CheckOtherPlayerExist(Packing.STATE.FANATIC_GUN);
        SetGravity();
    }
    
    // 중력 적용
    // 이동 방향 벡터 y값에 중력값을 계속 더해준다
    void SetGravity()
    {
        // 중력 적용
        Vector3 yDirection = new Vector3(0, -gravity * Time.deltaTime, 0);
        CC.Move(yDirection * 10 * Time.deltaTime);
    }

    protected override void UpdateRotateSpeed()
    {
        if (!LockRotation)
        {
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
                
                //print("아이들 -> 추적 상태로 넘어가기");
            }
        }
    }

    protected override void Trace_state()
    {
        //if (!EndGetWeapon)
        //return;

        if (Static_Data.m_number == 1)
        {
            SetMoveDirection(WalkSpeed);
            SetMoveType(MoveType.WalkForward);
            //m_moveType = MoveType.WalkForward;

            if (TargetDistance <= battleDistance)
            {
                if (ChangeState_FrameCheck == -1)
                    ChangeState_FrameCheck = 0;

                TCP_SendChangeState(MONSTER_STATE.BATTLE);
            }

            //DetectEnemy(traceDetectDistance);
        }
    }

    protected override void CheckAttackDistance()
    {
        // 공격 실행 아닐때에만
        if (m_isAttack || TCP_setAnimation) 
            return;

        // 전투 유지거리 벗어나면 추적 상태로
        if (TargetDistance > battleDistance)
        {
            if (m_Animator.GetInteger("BattleSTATE") != (int)FanaticBattleType.Idle)
            {
                m_Animator.SetInteger("BattleSTATE", (int)FanaticBattleType.Idle);
            }

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
            int RandomAction = UnityEngine.Random.Range(0, 100);

            // 공격 쿨타임 돌아올 때에만 공격
            if (Time.time - prevTime >= RandomActionTime)
            {
                // 근접공격 사거리면 근접 공격
                if (TargetDistance <= 5f)
                {
                    if (RandomAction >= 50)
                        m_FanaticBattleType = FanaticBattleType.Attack1;
                    else
                        m_FanaticBattleType = FanaticBattleType.Attack2;
                }
                else
                    m_FanaticBattleType = FanaticBattleType.Attack3;

                Animation_FrameCheck = Animation_FrameDelay;

                if (TCP_isConnected)
                    TCP_setAnimation = true;
                return;
            }

            // 움직임 세팅
            SetMovement();
            ChangeMovement();

        }
    }

    // 움직임 세팅
    void SetMovement()
    {
        if (!isMoving)
        {
            int RandomAction = UnityEngine.Random.Range(0, 100);

            
            // 타겟이 가까이 있으면 옆으로만 걷기
            if (TargetDistance <= 3f)
            {
                // 오른쪽 걷기
                if (RandomAction >= 65)
                {
                    float RandomTime = UnityEngine.Random.Range(6.0f, 7.5f);
                    st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_right, RandomTime));
                }
                // 왼쪽 걷기
                else if(RandomAction > 35)
                {
                    float RandomTime = UnityEngine.Random.Range(6.0f, 7.5f);
                    st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_left, RandomTime));
                }
                else
                {
                    float RandomTime = UnityEngine.Random.Range(4.0f, 5.5f);
                    st_MoveAction.Push(new MoveAction(FanaticBattleType.Idle, RandomTime));
                }
            }
            else
            {
                // 오른쪽 걷기
                if (RandomAction >= 90)
                {
                    float RandomTime = UnityEngine.Random.Range(6.0f, 7.5f);
                    st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_right, RandomTime));
                }
                // 왼쪽 걷기
                else if (RandomAction >= 80)
                {
                    float RandomTime = UnityEngine.Random.Range(6.0f, 7.5f);
                    st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_left, RandomTime));
                }
                // 앞으로 걷기
                else if(RandomAction > 60)
                {
                    float RandomTime = UnityEngine.Random.Range(3.0f, 4.5f);
                    st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_forward, RandomTime));
                }
                else
                {
                    float RandomTime = UnityEngine.Random.Range(4.0f, 5.5f);
                    st_MoveAction.Push(new MoveAction(FanaticBattleType.Idle, RandomTime));
                }
            }
            

            isMoving = true;
        }
    }

    // 지정된 움직임대로 변경하기
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
                m_FanaticBattleType = tempMoveAction.Action;
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

    protected override void Battle_state()
    {
        if (m_isEvade)
            return;

        if (Static_Data.m_number == 1)
        {
            // 피격당함
            if (OnHitFlag)
            {
                LockRotation = true;
                m_nowState = MONSTER_STATE.ONHIT;
                return;
            }

            CheckAttackDistance();

            if (!m_isAttack && m_nowState == MONSTER_STATE.BATTLE)
            {
                PlayMovementAction(m_FanaticBattleType);
                Check_PlayBattleAction(m_FanaticBattleType);
            }
        }

        // 서버 연결된 상태일때만 실행
        if (TCP_isConnected)
        {
            //print("전투상태, 서버연결됨");
            if (!m_isAttack && m_nowState == MONSTER_STATE.BATTLE)
                PlayBattleAction(m_RecvActionType);
        }
    }

    // 서버와 연결 체크 후, 서버에 애니메이션 보내기
    // 서버연결 안되있는 경우엔 공격 애니메이션 실행
    void Check_PlayBattleAction(FanaticBattleType actionType)
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
    void PlayMovementAction(FanaticBattleType actionType)
    {
        float speed = 0;
        switch (actionType)
        {
            case FanaticBattleType.Idle:
                SetMoveType(MoveType.Stay);
                m_desiredSpeed = 0;

                return;
            case FanaticBattleType.Walk_forward:
                SetMoveType(MoveType.WalkForward);
                //m_moveType = MoveType.WalkForward;
                LockRotation = false;
                speed = WalkSpeed;

                break;
            case FanaticBattleType.Walk_left:
                SetMoveType(MoveType.Left);
                //m_moveType = MoveType.Left;
                //transform.LookAt(Target);
                
                m_desiredMoveDirection = transform.right * -1;
                if (!TCP_SendMovement(WalkSpeed))
                    CC.Move(m_desiredMoveDirection * WalkSpeed * Time.deltaTime);

                m_isMovementAction = true;
                return;
            case FanaticBattleType.Walk_right:
                SetMoveType(MoveType.Right);
                //m_moveType = MoveType.Right;
                //transform.LookAt(Target);

                m_desiredMoveDirection = transform.right;
                if (!TCP_SendMovement(WalkSpeed))
                    CC.Move(m_desiredMoveDirection * WalkSpeed * Time.deltaTime);

                m_isMovementAction = true;
                return;
            case FanaticBattleType.Run:
                SetMoveType(MoveType.WalkForward);
                //m_moveType = MoveType.RunForward;
                LockRotation = false;
                speed = RunSpeed;

                break;
            default:
                m_isMovementAction = false;
                return;
        }

        SetMoveDirection(speed);
        m_isMovementAction = true;
    }

    // actionType에 따른 공격 애니메이션 실행
    void PlayBattleAction(FanaticBattleType actionType)
    {
        switch (actionType)
        {
            case FanaticBattleType.Evade:
                m_isEvade = true;
                isMoving = false;
                TCP_setAnimation = false;
                SetMoveType(MoveType.Stay);
                m_desiredSpeed = 0;

                HitBox.GetComponent<BoxCollider>().enabled = false;
                m_Animator.SetInteger("Evade", 0);
                break;
            case FanaticBattleType.Attack1:
                m_isAttack = true;
                isMoving = false;
                TCP_setAnimation = false;
                SetMoveType(MoveType.Stay);
                m_desiredSpeed = 0;

                currentDamageValue = 25f;
                currentAttackType = 0;
                CreateRandomActionTime(5f, 6f);

                m_Animator.SetInteger("BattleSTATE", (int)AnimBattleSTATE.Attack1);
                break;
            case FanaticBattleType.Attack2:
                m_isAttack = true;
                isMoving = false;
                TCP_setAnimation = false;
                SetMoveType(MoveType.Stay);
                m_desiredSpeed = 0;

                currentDamageValue = 20f;
                currentAttackType = 0;
                CreateRandomActionTime(5f, 6f);

                m_Animator.SetInteger("BattleSTATE", (int)AnimBattleSTATE.Attack2);
                break;
            case FanaticBattleType.Attack3:
                m_isAttack = true;
                isMoving = false;
                TCP_setAnimation = false;
                SetMoveType(MoveType.Stay);
                m_desiredSpeed = 0;

                currentDamageValue = 15f;
                currentAttackType = 0;
                CreateRandomActionTime(5f, 6f);

                m_Animator.SetInteger("BattleSTATE", (int)AnimBattleSTATE.Attack3);
                break;
            case FanaticBattleType.Max:
                break;
        }

        //print("공격 실행: " + actionType);
        m_RecvActionType = FanaticBattleType.Max;
    }

    bool hideHpBarOnce = false;
    [HideInInspector] public int onHitRandomAction;
    int damage = 0;

    // 현재 체력에따라 피격 체크
    public void OnHitCheck()
    {
        // 체력 닳았을때
        if (m_prevHp > sc_Health.curHP)
        {
            if (m_Level == MonsterLevel.Boss)
            {
                // UI 갱신
                ImageWidthSlider(sc_Health.curHP, sc_Health.maxHP, HPbar.rectTransform, HPbar_width);
            }

            // 죽음
            if (sc_Health.curHP <= 0)
            {
                try
                {
                    TCPClient.m_Monster.Monster_Die(Packing.STATE.FANATIC_GUN, index, transform.position, transform.rotation);
                }
                catch { }

                m_nowState = MONSTER_STATE.DEATH;

                // 체력바 감추기
                if (!hideHpBarOnce && m_Level == MonsterLevel.Boss)
                {
                    hideHpBarOnce = true;
                    StartCoroutine(HideHPbar());
                }
            }
            else if (sc_Health.curHP <= sc_Health.maxHP)
            {
                //print("체력 닳음");
                OnHitFlag = true;
                OnHitOnce = false;

                damage = m_prevHp - sc_Health.curHP;
                onHitRandomAction = UnityEngine.Random.Range(0, 100);
                try
                {
                    TCPClient.m_Monster.Monster_Hit(Packing.STATE.FANATIC_GUN, index, transform.position, transform.rotation, onHitRandomAction, damage);
                }
                catch { }

                // TCP Hit Send
                if (Static_Data.m_number != 1)
                {
                    OnHit_state();
                }
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

    public void OnHit_State(Vector3 pos, Quaternion rot, int anim, int hp)
    {
        transform.position = pos;
        transform.rotation = rot;
        onHitRandomAction = anim;

        m_prevHp = hp;
        sc_Health.curHP = hp;
        if(m_Level == MonsterLevel.Boss)
            ImageWidthSlider(sc_Health.curHP, sc_Health.maxHP, HPbar.rectTransform, HPbar_width);

        m_Animator.SetInteger("OnHit", 0);
    }

    protected override void Death_state() 
    {
        if (!DeathOnce)
        {
            Target = null;
            DeathOnce = true;
            m_Animator.SetInteger("Death", 0);

            grid.ClearMonsterGrid();
            grid.m_State = GRID_STATE.Death;
        }
    }

    // TCP Death State
    public void Death_State(Vector3 pos, Quaternion rot)
    {
        if (!DeathOnce)
        {
            print("죽음 받음");

            Target = null;
            DeathOnce = true;
            m_nowState = MONSTER_STATE.DEATH;

            grid.ClearMonsterGrid();
            grid.m_State = GRID_STATE.Death;

            sc_Health.curHP = 0;

            // 체력바 감추기
            if (!hideHpBarOnce && m_Level == MonsterLevel.Boss)
            {
                hideHpBarOnce = true;
                StartCoroutine(HideHPbar());
            }

            transform.position = pos;
            transform.rotation = rot;

            print("죽음 애니메이션 실행 전");
            m_Animator.SetInteger("Death", 0);
            print("죽음 애니메이션 실행");
            //print(transform.position + "," + pos);
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
            //switch(m_nowState)
            //{
            //    case MONSTER_STATE.IDLE:
            //        Idle_state();
            //        break;
            //}

            if (Static_Data.m_number == 1)
            {
                switch (m_nowState)
                {
                    case MONSTER_STATE.ONHIT:
                        OnHit_state();
                        break;
                    case MONSTER_STATE.DEATH:
                        Death_state();
                        yield break;
                }             
            }

            switch(m_nowState)
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
            }

            yield return null;
        }
    }

    // HP UI 관련 스크립트 ----------------------------------------------------------------

    IEnumerator ShowHPbar()
    {
        print("체력바 보이기");
        Color color;
        while (true)
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

    // 서버 통신 관련

    // 타겟이 있는 방향(m_desiredMoveDirection) 으로 이동
    void SetMoveDirection(float speed)
    {
        if (isUseAstar == UseAStar.None)
        {
            pathPos = Target.position - transform.position;
            distance = pathPos.magnitude;
            direction = pathPos / distance;
            m_desiredMoveDirection = Vector3.Scale(direction, new Vector3(1, 0, 1)).normalized;

            //CC.Move(m_desiredMoveDirection * speed * Time.deltaTime);
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
                print("Empty");
                SetMoveType(MoveType.Stay);
                //m_moveType = MoveType.Stay;
                grid.pathfind_active = false;
                return;
            }

            Search_Path.y = transform.position.y;

            // 장애물 존재, 에이스타 사용 노드를 따라감
            if (isObstacleExist)
            {
                pathPos = Search_Path - transform.position;
                print(" 장애물 존재, 에이스타 사용중 ");
            }
            // 장애물 없음, 직진
            else
            {
                pathPos = Target.position - transform.position;
                print(" 장애물 없음 ");
            }

            distance = pathPos.magnitude;
            direction = pathPos / distance;
            m_desiredMoveDirection = Vector3.Scale(direction, new Vector3(1, 0, 1)).normalized;

            //CC.Move(m_desiredMoveDirection * speed * Time.deltaTime);
            grid.pathfind_active = false;
        }

        // 서버 연결 없으면 바로 움직임
        if (!TCP_SendMovement(speed))
        {
            CC.Move(m_desiredMoveDirection * speed * Time.deltaTime);
            //print("서버연결 없음");
        }
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
                    CC.Move(m_desiredMoveDirection * AnimTranslateSpeed * Time.deltaTime);
            }
        }
        else if (OnHitTranlsate)
        {
            m_desiredMoveDirection = transform.forward * -1;
            if (!TCP_SendMovement(AnimTranslateSpeed))
                CC.Move(m_desiredMoveDirection * AnimTranslateSpeed * Time.deltaTime);
        }
        else if (EvadeTranslate)
        {
            m_desiredMoveDirection = transform.forward * -1;
            if (!TCP_SendMovement(AnimTranslateSpeed))
                CC.Move(m_desiredMoveDirection * AnimTranslateSpeed * Time.deltaTime);
        }
    }

    // -------------------------------------- Send Management -------------------------------------

    // 상태 변경하기
    public void TCP_SendChangeState(MONSTER_STATE desiredState)
    {
        if (ChangeState_FrameCheck == 0)
        {
            switch (m_nowState)
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
                        Packing.STATE.FANATIC_GUN,
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
                        //m_moveType = MoveType.WalkForward;

                        //print("추적 상태로");

                        break;
                    case MONSTER_STATE.BATTLE:
                        m_isAttack = false;
                        isMoving = false;
                        MoveTime = 0;
                        GunAttackOnce = false;

                        //print("전투 상태로");
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
                    classNumber = 1;
                else
                    classNumber = 2;

                TCPClient.m_Monster.Monster_SetTarget(
                    Packing.STATE.FANATIC_GUN,
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

                switch (m_nowState)
                {
                    case MONSTER_STATE.IDLE:
                        m_Animator.SetInteger("BattleSTATE", (int)FanaticBattleType.Idle);
                        SetMoveType(MoveType.WalkForward);
                        m_nowState = MONSTER_STATE.TRACE;

                        if (m_Level == MonsterLevel.Boss)
                            StartCoroutine(ShowHPbar());

                        prevTime = Time.time;
                        LockRotation = false;
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
            var desiredPosition = transform.position + (m_desiredMoveDirection);// * speed * Time.deltaTime * 10);

            try
            {
                TCPClient.m_Monster.Monster_Movement(
                    Packing.STATE.FANATIC_GUN,
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
                    Packing.STATE.FANATIC_GUN,
                    index,
                    (UInt64)TCPClient.PROTOCOL.M_ATTACK,
                    transform.position,
                    transform.rotation,
                    v,
                    h,
                    anim);

                print("공격 애니메이션 보내기: " + (FanaticBattleType)anim);

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

    // --------------------------------------------------------------------------------------------
    // -------------------------------------- Recv Management -------------------------------------

    // 서버로부터 타겟 받음
    public void TCP_RecvTarget(int classNumber)
    {
        if (Static_Data.m_number == classNumber)
        {
            Target = GameManager.Instance.ClientPlayer;
        }
        else
            Target = GameManager.Instance.OtherPlayer;

        m_Animator.SetInteger("BattleSTATE", (int)FanaticBattleType.Idle);
        SetMoveType(MoveType.WalkForward);
        //m_moveType = MoveType.WalkForward;
        m_nowState = MONSTER_STATE.TRACE;

        if (m_Level == MonsterLevel.Boss)
            StartCoroutine(ShowHPbar());

        prevTime = Time.time;
        LockRotation = false;

        print("타겟 받음 : " + Target.name);
    }

    // 상태 받아서 변경하기
    public void TCP_RecvChangeState(int _nowState, int _desiredState)
    {
        switch ((MONSTER_STATE)_nowState)
        {
            case MONSTER_STATE.TRACE:       // 현재 추적 상태일때
                m_isAttack = false;
                isMoving = false;
                MoveTime = 0;

                print("TCP_RecvChangeState " + (MONSTER_STATE)_nowState + " -> " + (MONSTER_STATE)_desiredState + " 상태로");
                break;
            case MONSTER_STATE.BATTLE:      // 현재 배틀 상태일때
                switch ((MONSTER_STATE)_desiredState)
                {
                    case MONSTER_STATE.TRACE:
                        SetMoveType(MoveType.WalkForward);
                        //m_moveType = MoveType.WalkForward;

                        print("TCP_RecvChangeState 전투 -> 추적상태로");
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
        m_RecvActionType = (FanaticBattleType)anim;
        print("공격 애니메이션 받음: " + m_RecvActionType);
    }

    // 캐릭터 번호에 따라서, 데미지 누적
    public void TCP_RecvDamageType(int curHP, int serial)
    {
        if (serial == 2)
        {
            int Damage = m_prevHp - curHP;
            m_Other_aggroValue += Damage;
        }
    }
}
