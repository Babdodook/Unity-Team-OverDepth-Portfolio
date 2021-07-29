using Inven;
using System;
using System.Collections;
using System.Collections.Generic;
using TCP;
using UnityEngine;

public enum AnimIdle
{
    Idle = 0,
    Crawl_Idle,
    Crawl,
    Eating,
    Crawl_to_State,
    State_to_Crawl,
    Jump,

    Max
}

public enum IdleType
{
    HangAround = 0,
    Hiding,

    Max
}

public enum MermanBattleType
{
    Idle = 0,
    Walk,
    Run,
    ComboAttack,
    RunAttack,
    RunBite,
    Rotate,
    Walk_left,
    Walk_right,

    Max
}

public enum MermanType
{
    Normal=0,
    Leader,

    Max
}

public enum AttackValue
{
    ComboAttack = 0,
    RunAttack = 3,

    Max
}

public class MermanController : BaseMonsterController
{
    [Header("아이들 타입 설정")]
    public IdleType m_IdleType;

    [Header("어인 타입")]
    public MermanType m_MermanType;
    public MermanBattleType m_MermanBattleType;    // 전투 타입

    // 배회중에 아이들이 지속될 시간
    float IdleTime;
    float IdleCheckTime;
    // 포효
    LayerMask AllyLayer;
    Collider[] allys;
    Collider[] mermans;

    [Header("독안개")]
    public Transform PoisonFog;

    [Header("캐스트 시작 위치")]
    public Transform castPosition;
    public LayerMask castLayer;

    #region variables

    bool Coroutine_HangAround = false;
    [HideInInspector] public bool isRoar = false;
    [HideInInspector] public bool DefenceOnce = false;
    [HideInInspector] public bool DefenceFlag = false;
    [HideInInspector] public bool isDeath = false;
    [HideInInspector] public bool EndJump = false;

    #endregion

    // 배회 구역 피벗
    public Transform m_SectorPos;

    List<Node> AroundList = new List<Node>();
    Vector3 TargetPostion = Vector3.zero;
    AroundSectorSize m_Sector;

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
        AllyLayer = 1 << LayerMask.NameToLayer("Enemy");
        mermans = new Collider[50];

        if (m_IdleType != IdleType.Hiding)
            EndJump = true;

        if (PoisonFog != null)
            PoisonFog.gameObject.SetActive(false);

        m_Level = MonsterLevel.Normal;
        isObstacleExist = true;
    }

    private void Start()
    {
        // 섹터포스 오브젝트 연결된 놈만 컴포넌트 가져오기
        if(m_SectorPos != null)
            m_Sector = m_SectorPos.GetComponent<AroundSectorSize>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (CC == null)
            return;

        UpdateRotateSpeed();
        UpdateMoveValue();
        OnHitCheck();
        //CheckObstacle();

        AttackDelayTime += Time.deltaTime;
        EndHitDelayTime += Time.deltaTime;

        if (Target != null && Target.gameObject.activeInHierarchy)
            TargetDistance = Vector3.Distance(transform.position, Target.position);

        // 중력 적용
        if (m_nowState != MONSTER_STATE.DEATH && EndJump)
        {
            Vector3 yDirection = new Vector3(0, -gravity * Time.deltaTime, 0);
            CC.Move(yDirection * 10 * Time.deltaTime);
        }
    }

    // 무브 파라미터 업데이트
    protected override void UpdateMoveValue()
    {
        switch (m_moveType)
        {
            case MoveType.Stay:
                v = Mathf.Lerp(v, 0, Time.deltaTime * 2);
                h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
                break;
            case MoveType.WalkForward:
                v = Mathf.Lerp(v, BlendLowValue, Time.deltaTime);
                h = Mathf.Lerp(h, 0, Time.deltaTime);
                break;
            case MoveType.RunForward:
                v = Mathf.Lerp(v, 0.85f, Time.deltaTime * 3);
                h = Mathf.Lerp(h, 0, Time.deltaTime * 3);
                break;
            case MoveType.FastRun:
                v = Mathf.Lerp(v, BlendHighValue + 0.5f, Time.deltaTime * 4);
                h = Mathf.Lerp(h, 0, Time.deltaTime * 4);
                break;
            case MoveType.Right:
                v = Mathf.Lerp(v, 0, Time.deltaTime * 4);
                h = Mathf.Lerp(h, BlendLowValue, Time.deltaTime * 4);
                break;
            case MoveType.Left:
                v = Mathf.Lerp(v, 0, Time.deltaTime * 4);
                h = Mathf.Lerp(h, -BlendLowValue, Time.deltaTime * 4);
                break;
            case MoveType.ReSet:
                v = 0;
                h = 0;
                break;
        }

        m_Animator.SetFloat("Forward", v);
        m_Animator.SetFloat("Right", h);
    }

    protected override void UpdateRotateSpeed()
    {
        if (!LockRotation)
        {
            if (Target != null || Coroutine_HangAround)
            {
                var look = Look_Path - transform.position;

                //var look = Target.position - transform.position;
                if (m_nowState == MONSTER_STATE.BATTLE)
                    look = Target.position - transform.position;
                //if(Target == null)
                //    look = TargetPostion - transform.position;

                look.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), extraRotationSpeed * Time.deltaTime);
            }
        }
    }

    // 배회 코루틴 탈출 조건
    bool CheckDetectEnemy()
    {
        // 타겟잡히면 다음 상태로 넘어가도록
        if (Target)
        {
            return true;
        }

        if (IdleCheckTime >= IdleTime)
            return true;
        else
            return false;
    }

    //float distance = 0;
    int IdleAction = 0;
    float EatingTime = 0;

    // 배회중
    protected IEnumerator HangAround()
    {
        Coroutine_HangAround = true;

        // 배회에 필요한 데이터
        Vector3 OriginPoint = transform.position;

        float v = UnityEngine.Random.Range(-1f, 1f);
        float h = UnityEngine.Random.Range(-1f, 1f);

        // 목표 지점 설정
        //TargetPostion = v * Vector3.forward + h * Vector3.right;
        //TargetPostion *= 3f;
        //TargetPostion += OriginPoint;

        while (true)
        {
            // 타겟 잡히면 종료
            if (Target != null)
            {
                Astar.FindPathStop();
                grid.pathfind_active = false;

                m_Animator.SetInteger("IdleSTATE", (int)AnimIdle.Crawl_to_State);
                m_Animator.SetBool("TraceSTATE", true);
                LockRotation = false;
                m_IdleType = IdleType.Max;
                //m_nowState = STATE.TRACE;

                break;
            }

            // 거리가 1미만일때, 먹는시간 n초 이상일때
            if (distance <= 1.5f/* || EatingTime >= 3f*/)
            {
                LockRotation = true;
                EatingTime = 0;
                // 아이들상태
                m_Animator.SetInteger("IdleSTATE", (int)AnimIdle.Crawl_Idle);

                //다음 행동 결정
                //IdleAction = 5;
                IdleAction = UnityEngine.Random.Range(0, 10);
                IdleTime = UnityEngine.Random.Range(2f, 3f);
                IdleCheckTime = 0;
                // 랜덤 시간 후에~ 행동
                yield return new WaitUntil(() => CheckDetectEnemy() == true);

                // 기어가기
                if (IdleAction >= 5)
                {
                    distance = 3f;
                    // 기어가는 애니메이션 출력
                    //m_Animator.SetInteger("IdleSTATE", (int)AnimIdle.Crawl);

                    // 임시로 사용할 목적지 설정 코드
                    //OriginPoint = transform.position;

                    //v = UnityEngine.Random.Range(-1f, 1f);
                    //h = UnityEngine.Random.Range(-1f, 1f);

                    // 목표 지점 설정
                    //TargetPostion = v * Vector3.forward + h * Vector3.right;
                    //TargetPostion *= 3f;
                    //TargetPostion += OriginPoint;

                    //// 기존 에이스타 코드
                    //// 목적지 설정
                    var Neighbours = grid.GetNeighbours(grid.NodeFromWorldPoint(m_SectorPos.position, GRIDTYPE.Detailed), m_Sector.m_SectorSize.x, m_Sector.m_SectorSize.y, GRIDTYPE.Detailed);
                    AroundList.Clear();

                    Node node;

                    for (int i = 0; i < Neighbours.Count; i++)
                    {
                        node = Neighbours[i];
                        if (node.walkable && Astar.MonsterCheak(node, OriginPosition).Equals(-1))
                        {
                            var node2 = grid.GetNeighboursToCross(node);
                            var flag = false;

                            for (int j = 0; j < node2.Count; j++)
                            {
                                if (!node2[j].walkable || Astar.MonsterCheak(node2[j], OriginPoint).Equals(1))
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (!flag)
                                AroundList.Add(node);
                        }
                    }

                    //for (var i = 0; i < AroundList.Count; i++)
                    //{
                    //    if (i > AroundList.Count * 0.25)
                    //        AroundList.RemoveAt(i);
                    //    if (i > (AroundList.Count * 0.25 + AroundList.Count * 0.5))
                    //        break;
                    //}

                    TargetPostion = AroundList[UnityEngine.Random.Range(0, AroundList.Count - 1)].worldPosition;
                    LockRotation = false;

                    yield return new WaitForSeconds(0.1f);
                }
                // 먹기
                else
                {
                    // 먹는 애니메이션 출력
                    m_Animator.SetInteger("IdleSTATE", (int)AnimIdle.Eating);
                    distance = 1.5f;
                    yield return new WaitForSeconds(3f);
                }
            }

            // 걷기상태
            if (IdleAction >= 5)
            {
                // 기존 에이스타 코드
                // 목적지로 이동
                int index = MoveToTarget(WalkSpeed, TargetPostion);

                if (index.Equals(1))
                {
                    m_Animator.SetInteger("IdleSTATE", (int)AnimIdle.Crawl);
                    distance = Vector3.Distance(transform.position, TargetPostion);
                }
                else if (index.Equals(0))
                {
                    distance = 1.5f;
                }

                // 임시 사용 코드
                //distance = Vector3.Distance(transform.position, TargetPostion);
                //CC.Move(transform.forward * OriginWalkSpeed * Time.deltaTime);
            }
            yield return null;
        }
        Coroutine_HangAround = false;
    }

    // 매복 중
    void Hiding()
    {
        if (Target != null)
        {
            m_IdleType = IdleType.Max;
            LockRotation = true;
            m_Animator.SetInteger("IdleSTATE", (int)AnimIdle.Crawl_to_State);
            m_Animator.SetInteger("IdleSTATE", (int)AnimIdle.Jump);
        }
    }

    protected override void Idle_state()
    {
        if (OnHitFlag)
        {
            LockRotation = true;
            m_nowState = MONSTER_STATE.ONHIT;
            return;
        }

        switch (m_IdleType)
        {
            // 배회중
            case IdleType.HangAround:
                IdleCheckTime += Time.deltaTime;
                if (!Coroutine_HangAround)
                    StartCoroutine(HangAround());

                DetectEnemy(idleDetectDistance);    // 타겟 찾기
                break;
            // 매복중
            case IdleType.Hiding:
                LockRotation = true;
                Hiding();
                break;
        }
    }

    protected override void Trace_state()
    {
        if (OnHitFlag)
        {
            LockRotation = true;
            m_nowState = MONSTER_STATE.ONHIT;
            return;
        }

        m_moveType = MoveType.WalkForward;
        WalkSpeed = Mathf.Lerp(WalkSpeed, OriginWalkSpeed, 1.5f * Time.deltaTime);
        MoveToTarget(WalkSpeed);
        //MoveToTarget(WalkSpeed, Target.position);

        // 처음 잡은 타겟 대상으로 추적하면서
        // 추적 거리 반경내의 적 감지
        DetectEnemy(traceDetectDistance);

        // 전투 유지 거리안으로 들어가면 전투상태로 변환
        if (TargetDistance <= battleDistance)
        {
            m_isAttack=false;
            m_nowState = MONSTER_STATE.BATTLE;
        }
    }

    protected override void CheckAttackDistance()
    {
        // 공격 실행 아닐때에만
        if (m_isAttack)
            return;

        // 전투 유지거리 벗어나면 추적 상태로
        if (TargetDistance > battleDistance)
        {
            //m_moveType = MoveType.RunForward;
            m_moveType = MoveType.WalkForward;
            m_nowState = MONSTER_STATE.TRACE;

            //print("추적 상태로");
        }
        // 전투 거리 내에 타겟 존재
        else if (TargetDistance <= battleDistance)
        {
            int RandomAction = UnityEngine.Random.Range(0, 100);

            if (Time.time - prevTime >= RandomActionTime)
            {
                // 사정거리 안이면 콤보어택 출력
                if (TargetDistance <= AttackDistance)
                {
                    // 50 퍼센트 확률로 콤보 공격 또는 런어택
                    if (RandomAction >= 30)
                        m_MermanBattleType = MermanBattleType.ComboAttack;
                    else
                        m_MermanBattleType = MermanBattleType.RunAttack;

                    return;
                }
            }
            
            m_MermanBattleType = MermanBattleType.Walk;
        }
    }

    // 전투 상태
    protected override void Battle_state()
    {
        if (OnHitFlag)
        {
            LockRotation = true;
            m_nowState = MONSTER_STATE.ONHIT;
            return;
        }
        if(DefenceFlag && !DefenceOnce)
        {
            LockRotation = true;
            m_nowState = MONSTER_STATE.DEFENCE;
            return;
        }

        // 거리 체크
        CheckAttackDistance();

        //print(m_MermanBattleType);

        if(!m_isAttack && m_nowState == MONSTER_STATE.BATTLE)
        {
            switch (m_MermanBattleType)
            {
                case MermanBattleType.Rotate:
                    LockRotation = false;
                    m_moveType = MoveType.WalkForward;
                    break;
                case MermanBattleType.Walk:
                    LockRotation = false;
                    m_moveType = MoveType.WalkForward;
                    WalkSpeed = Mathf.Lerp(WalkSpeed, OriginWalkSpeed, 1.5f * Time.deltaTime);
                    MoveToTarget(WalkSpeed);
                    //Walk();
                    break;
                case MermanBattleType.Run:
                    LockRotation = false;
                    m_moveType = MoveType.RunForward;
                    MoveToTarget(RunSpeed);
                    //Run();
                    break;
                case MermanBattleType.Walk_left:
                    //m_moveType = MoveType.Left;
                    m_moveType = MoveType.WalkForward;
                    transform.LookAt(Target);
                    CC.Move((transform.right * -1) * WalkSpeed * Time.deltaTime);
                    break;
                case MermanBattleType.Walk_right:
                    //m_moveType = MoveType.Right;
                    m_moveType = MoveType.WalkForward;
                    transform.LookAt(Target);
                    CC.Move(transform.right * WalkSpeed * Time.deltaTime);
                    break;
                case MermanBattleType.ComboAttack:
                    m_isAttack = true;
                    isMoving = false;

                    currentDamageValue = 20f;
                    currentAttackType = 0;
                    CreateRandomActionTime(3f, 6f);

                    //m_moveType = MoveType.RunForward;
                    m_moveType = MoveType.WalkForward;
                    GetComponentInChildren<MermanAnimEvent>().AttackTranlsate = true;
                    GetComponentInChildren<MermanAnimEvent>().TranslateSpeed = WalkSpeed;
                    m_Animator.SetInteger("AttackValue", (int)AttackValue.ComboAttack);
                    break;
                case MermanBattleType.RunAttack:
                    //print("런어택 실행");
                    m_isAttack = true;
                    isMoving = false;

                    currentDamageValue = 25f;
                    currentAttackType = 1;
                    CreateRandomActionTime(3f, 6f);

                    //m_moveType = MoveType.RunForward;
                    m_moveType = MoveType.WalkForward;
                    GetComponentInChildren<MermanAnimEvent>().AttackTranlsate = true;
                    GetComponentInChildren<MermanAnimEvent>().TranslateSpeed = WalkSpeed;
                    m_Animator.SetInteger("AttackValue", (int)AttackValue.RunAttack);
                    break;
            }
        }
    }

    Queue<Vector3> PreviousPositionStack = new Queue<Vector3>();

    int MoveToTarget(float speed, Vector3 pos)
    {
        if (!grid.pathfind_active)
            Astar.ReQuestFindPath(pos, transform.position);

        if (grid.path.Count >= 2)
        {
            if (!SearchPath())
            {
                print("Error");
                grid.pathfind_active = false;
                PreviousPositionStack.Clear();
                return 0;
            }
            else
                PreviousPositionStack.Enqueue(transform.position);
        }
        else
        {
            //print("Grid Empty");
            grid.pathfind_active = false;
            PreviousPositionStack.Clear();
            return 0;
        }

        if (PreviousPositionStack.Count >= 10)
        {            
            if (Vector3.Distance(transform.position, PreviousPositionStack.Dequeue()) <= 0.01f)
            {
                grid.pathfind_active = false;
                PreviousPositionStack.Clear();
                return 0;
            }
            PreviousPositionStack.Clear();
        }

        Search_Path.y = transform.position.y;

        var pathPos = Search_Path - transform.position;
        float distance = pathPos.magnitude;
        Vector3 direction = pathPos / distance;
        var m_desiredMoveDirection = Vector3.Scale(direction, new Vector3(1, 0, 1)).normalized;

        CC.Move(m_desiredMoveDirection * speed * Time.deltaTime);

        grid.pathfind_active = false;
        return 1;
    }

    void MoveToTarget(float speed)
    {
        // 플레이어가 도중 나갔을 경우 예외처리 해야됨
        if (!Target.gameObject.activeInHierarchy || DeathOnce)
        {
            //Target = null;
            //m_nowState = STATE.DEATH;
            return;
        }

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
            m_moveType = MoveType.Stay;
            grid.pathfind_active = false;
            return;
        }

        Search_Path.y = transform.position.y;

        Vector3 pathPos = Vector3.zero;

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

        float distance = pathPos.magnitude;
        Vector3 direction = pathPos / distance;
        var m_desiredMoveDirection = Vector3.Scale(direction, new Vector3(1, 0, 1)).normalized;

        CC.Move(m_desiredMoveDirection * speed * Time.deltaTime);
        grid.pathfind_active = false;
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

    void Defence_state()
    {
        if (!DefenceOnce)
        {
            DefenceOnce = true;

            if (m_MermanType == MermanType.Normal)
                m_Animator.SetInteger("DefenceSTATE", 0);
            else if (m_MermanType == MermanType.Leader)
            {
                Roar_DetectMermans(30f);
                if(TargetDistance <= 5f && Target.name == "Player")
                    TestCameraScript.mPthis.Camera_Shake(1.5f,0.15f,5,1);
                //UsePoisonFog();
                m_Animator.SetInteger("DefenceSTATE", 1);

                //print("포효 실행");
            }
        }
    }



    void UsePoisonFog()
    {
        PoisonFog.gameObject.SetActive(true);
        PoisonFog.GetComponent<ParticleSystem>().Play();
        GetComponent<PoisonFogDmg>().StartTakeDmg = true;

        StartCoroutine(StopPoisonFog());
    }

    IEnumerator StopPoisonFog()
    {
        EndPoisonFog = Time.time;
        yield return new WaitUntil(() => PoisonFogDelay() == true);

        PoisonFog.GetComponent<ParticleSystem>().Stop(true);
        GetComponent<PoisonFogDmg>().StartTakeDmg = false;

        print("독데미지 끝");
    }

    float EndPoisonFog = 0;
    bool PoisonFogDelay()
    {
        if (Time.time - EndPoisonFog > 20f)
        {
            return true;
        }
        else if (DeathOnce)
            return true;
        else
            return false;
    }

    MermanController mermanController;
    void Roar_DetectMermans(float DetectDistance)
    {
        // 레이어가 Enemy인 오브젝트 검출
        allys = Physics.OverlapSphere(this.transform.position, DetectDistance, AllyLayer);

        int mermanIndex = 0;
        if(allys.Length != 0)
        {
            // 그중에서 어인몬스터만 검출
            for (int i = 0; i < allys.Length; i++)
            {
                mermanController = allys[i].gameObject.GetComponent<MermanController>();

                // 매복중인 애들은 제외
                if (mermanController != null &&
                    mermanController.m_IdleType != IdleType.Hiding)
                {
                    mermans[mermanIndex++] = allys[i];
                }
            }
        }

        if (mermans.Length != 0)
        {
            for (int i = 0; i < mermanIndex; i++)
            {
                mermans[i].gameObject.GetComponent<MermanController>().Target = Target;
            }
        }
    }

    protected override void OnHit_state()
    {
        if(!OnHitOnce)
        {
            OnHitOnce = true;
            if(currentOnHitType == 0)
                m_Animator.SetTrigger("OnHit0");
            else if(currentOnHitType == 1)
                m_Animator.SetTrigger("OnHit1");

            //m_Animator.SetInteger("OnHit", currentOnHitType);

            m_Animator.SetTrigger("OnHitTrigger");
            //print(currentOnHitType);
        }
    }

    protected override void Death_state()
    {
        if (!DeathOnce)
        {
            int Randnum = UnityEngine.Random.Range(0, 2);
            // send
            try
            {
                print("Send");
                TCPClient.m_Monster.Monster_Die(Packing.STATE.MERMAN, index, transform.position, transform.rotation, Randnum);
            }
            catch { }

            grid.ClearMonsterGrid();            
            grid.m_State = GRID_STATE.Death;

            Target = null;
            DeathOnce = true;
            m_Animator.SetInteger("Death", Randnum);
            m_Animator.SetTrigger("DeathTrigger");

            isDeath = true;
        }
    }

    Vector3 die_position = Vector3.zero;

    public void Death_State(Vector3 position, Quaternion rotation, int anim)
    {
        DeathOnce = true;
        sc_Health.curHP = 0;
        m_nowState = MONSTER_STATE.DEATH;

        try
        {
            grid.m_State = GRID_STATE.Death;
            grid.ClearMonsterGrid();
        }
        catch { }

        transform.position = position;
        die_position = position;
        transform.rotation = rotation;

        Target = null;
        print("Mermen Death Anim : " + anim);
        m_Animator.SetInteger("Death", anim);
        m_Animator.SetTrigger("DeathTrigger");
        isDeath = true;

        //print(transform.position + "," + position);
    }

    public void OnHitCheck()
    {
        // 체력 닳았을때
        if (m_prevHp > sc_Health.curHP)
        {
            m_prevState = m_nowState;

            // 죽음
            if (sc_Health.curHP <= 0)
            {
                m_nowState = MONSTER_STATE.DEATH;
            }
            // 몬스터 체력 <= 30%
            else if (!DefenceFlag && (sc_Health.curHP <= sc_Health.maxHP / 100 * 30) && m_MermanType == MermanType.Leader)
            {
                DefenceFlag = true;
            }
            else if (sc_Health.curHP <= sc_Health.maxHP)
            {
                OnHitFlag = true;
                OnHitOnce = false;
            }
        }

        m_prevHp = sc_Health.curHP;
    }

    public bool CheckHitDelay()
    {
        if (EndHitDelayTime >= HitDelayTime || !OnHitOnce || (DefenceFlag && !DefenceOnce))
        {
            return true;
        }
        else
            return false;
    }

    public bool CheckAttackDelay()
    {
        if (AttackDelayTime >= 1f || OnHitFlag || (DefenceFlag && !DefenceOnce))
        {
            return true;
        }
        else
            return false;
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
                    Defence_state();
                    break;
                case MONSTER_STATE.ONHIT:
                    OnHit_state();
                    break;
                case MONSTER_STATE.DEATH:
                    Death_state();
                    if (die_position != Vector3.zero && transform.position != die_position)
                        transform.position = die_position;
                    //print(transform.position);
                    yield break;                    
            }

            yield return null;
        }
    }
}
