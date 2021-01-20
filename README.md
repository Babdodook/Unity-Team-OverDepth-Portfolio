# Unity-Team-OverDepth-Portfolio
**팀 / 게임 명 :** Team OverDepth / OverDepth  
**개발 기간 :** 2020.03.01 ~ 12.03 (기획 30일, 개발 247일)  
**개발 환경 :** Unity 2019.2.21f1 / Visual Studio 2019 / Github Desktop / Git LFS / Trello  
**개발 인원 :** 11명  
**요 약 :** 유니티 엔진을 활용한 최대 2인까지 협동 가능한 3D 액션 게임.  
G-Star 2020 전시, 교내 프로젝트 경진대회 전시, GGC(Global Game Challenge 2020) 전시를 위해 개발하였습니다.  
  
**담당자 / 담당 업무 :** 이재성 / 클라이언트 프로그래머  

# 게임 소개
**[소개영상](https://youtu.be/uYQ6JCqk054)**

OverDepth는 최대 2인 협동 가능한 3D 액션 멀티 게임입니다.  
Soul like를 표방하고 있으며, fromsoftware사의 다크소울과 블러드본에 영향을 많이 받았습니다.

2인이 서로 매칭이되어 세션형식으로 진행됩니다.
플레이어는 동료와 협동하여 몬스터를 처치하고 마지막 보스를 쓰러뜨려야 합니다.  

![어인](https://user-images.githubusercontent.com/48229283/100824961-69d35e80-349a-11eb-9fb0-51db0885c2c0.png) | ![광신도](https://user-images.githubusercontent.com/48229283/100824385-67243980-3499-11eb-97d3-6fbefdb62e7b.png)
:-------------------------:|:-------------------------:
![히드라1](https://user-images.githubusercontent.com/48229283/100952147-4e785a00-3553-11eb-821f-103956c7f84a.png) | ![히드라2](https://user-images.githubusercontent.com/48229283/100824625-e0239100-3499-11eb-856a-77b6e164663a.png)

# 기능  
* 몬스터 AI  
* TCP 클라이언트  
* 캐릭터 Movement
* 카메라 Movement  

# 몬스터 AI
총 5마리의 몬스터가 구성되어 있습니다.  
어인  | 광신도  | 광신도Mace  | 광신도Gunner  | 타이타닉 히드라
:-------------------------:|:-------------------------:|:-------------------------:|:-------------------------:|:-------------------------:
![어인](https://user-images.githubusercontent.com/48229283/100951644-4ec42580-3552-11eb-90a9-d69a94c2222f.jpg) | ![광신도1](https://user-images.githubusercontent.com/48229283/100952361-c181d080-3553-11eb-9a45-e8d1989fafd5.jpg) | ![광신도2](https://user-images.githubusercontent.com/48229283/100952444-ec6c2480-3553-11eb-9dff-fe4e50d1aa87.jpg) | ![광신도3](https://user-images.githubusercontent.com/48229283/100952476-f2fa9c00-3553-11eb-8c78-ccf28c306d73.jpg) | ![히드라](https://user-images.githubusercontent.com/48229283/100952493-f8f07d00-3553-11eb-9d83-55406a6b7ebc.jpg)
느릿한 움직임, 느린 공격 속도 | 저돌적이며, 빠른 공격 속도 | 묵직한 한방, 느린 공격 속도 | 총을 이용한 원거리 공격과 다소 빠른 근접 공격 | 3페이즈로 구성된 공격 패턴, 페이즈가 지날 수록 저돌적인 공격 스타일


몬스터의 상태머신을 정의하는 부모 클래스를 작성하고, 각 몬스터 자식 클래스에서 상속을 받아 재정의합니다.
![몬스터상속구조](https://user-images.githubusercontent.com/48229283/100949531-d3f90b80-354d-11eb-9fbb-55f3987331d5.PNG)

---

## 1. 스택을 이용한 몬스터 행동 패턴 정의
'이동 스택'과 '공격 스택'에 정의된 패턴을 Push하고 Pop하여 사용합니다.

이동 스택 | 공격 스택
:-------------------------:|:-------------------------:
![스택움직임](https://user-images.githubusercontent.com/48229283/100953436-ee36e780-3555-11eb-8dc2-0065696b1698.PNG) | ![스택공격](https://user-images.githubusercontent.com/48229283/100966903-58a95100-3571-11eb-9dc2-31ac2e399d11.PNG)

### 1-1. MoveAction 클래스
몬스터의 움직임(걷기, 뛰기 등)과 실행될 시간을 멤버변수로 가지고 있습니다.

```cs
// 움직임을 지정하고 해당 움직임이 몇초 동안 지속될 것인지 결정
public class MoveAction
{
    public FanaticBattleType Action;    // 움직임
    public float Time;                  // 움직임이 실행될 시간

    public MoveAction(FanaticBattleType _Action, float _Time)
    {
        Action = _Action;
        Time = _Time;
    }
}

// MoveAction 스택 선언
[HideInInspector] public Stack<MoveAction> st_MoveAction;
```

### 1-2. 스택에 행동 할당
스택에 확률과 특정 조건에따라서 움직임과 시간을 Push합니다.

```cs
  // 움직임 세팅
  void SetMovement()
  {
      if(!isMoving)
      {
          int RandomAction = UnityEngine.Random.Range(0, 100);

          // 공격 가능일때, 전력질주 사용
          if (Time.time - prevTime >= RandomActionTime)
          {
              float RandomTime = UnityEngine.Random.Range(3.0f, 4.0f);
              st_MoveAction.Push(new MoveAction(FanaticBattleType.FastRun, RandomTime));
          }
          // 공격 딜레이중, 전력질주 사용 불가
          else
          {
              // 타겟이 가까이 있으면 옆으로만 걷기
              if (TargetDistance <= 3f)
              {
                  // 오른쪽 걷기
                  if (RandomAction >= 50)
                  {
                      float RandomTime = UnityEngine.Random.Range(1.0f, 1.5f);
                      st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_right, RandomTime));
                  }
                  // 왼쪽 걷기
                  else
                  {
                      float RandomTime = UnityEngine.Random.Range(1.0f, 1.5f);
                      st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_left, RandomTime));
                  }
              }
              else
              {
                  // 오른쪽 걷기
                  if (RandomAction >= 90)
                  {
                      float RandomTime = UnityEngine.Random.Range(1.0f, 1.5f);
                      st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_right, RandomTime));
                  }
                  // 왼쪽 걷기
                  else if (RandomAction >= 80)
                  {
                      float RandomTime = UnityEngine.Random.Range(1.0f, 1.5f);
                      st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_left, RandomTime));
                  }
                  // 앞으로 걷기
                  else
                  {
                      float RandomTime = UnityEngine.Random.Range(1.0f, 1.5f);
                      st_MoveAction.Push(new MoveAction(FanaticBattleType.Walk_forward, RandomTime));
                  }
              }
          }

          isMoving = true;
      }
  }
```

## 2. Animator Blendtree 파라미터 업데이트
애니메이터 Blendtree에 사용되는 Forward와 Right 파라미터에 현재 '움직임'에 따른 값을 할당합니다.

![블렌드트리](https://user-images.githubusercontent.com/48229283/100952783-8338e100-3554-11eb-8dd7-29686e5f3477.PNG)

```cs
  // 무브 파라미터 업데이트
  protected virtual void UpdateMoveValue()
  {
      // m_moveType에 따라 Animator에 넘기는 v와 h의 값을 변경
      switch (m_moveType)
      {
          // 제자리
          case MoveType.Stay:
              v = Mathf.Lerp(v, 0, Time.deltaTime * 2);
              h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
              break;
          // 걷기
          case MoveType.WalkForward:
              v = Mathf.Lerp(v, BlendLowValue, Time.deltaTime * 2);
              h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
              break;
          // 달리기
          case MoveType.RunForward:
              v = Mathf.Lerp(v, BlendHighValue, Time.deltaTime * 2);
              h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
              break;
          // 전력질주
          case MoveType.FastRun:
              v = Mathf.Lerp(v, BlendHighValue + 0.5f, Time.deltaTime * 2);
              h = Mathf.Lerp(h, 0, Time.deltaTime * 2);
              break;
          // 오른쪽으로 걷기
          case MoveType.Right:
              v = Mathf.Lerp(v, 0, Time.deltaTime * 2);
              h = Mathf.Lerp(h, BlendLowValue, Time.deltaTime * 2);
              break;
          // 왼쪽으로 걷기
          case MoveType.Left:
              v = Mathf.Lerp(v, 0, Time.deltaTime * 2);
              h = Mathf.Lerp(h, -BlendLowValue, Time.deltaTime * 2);
              break;
          case MoveType.ReSet:
              v = 0;
              h = 0;
              break;
      }
      
      // 애니메이터에 값 할당
      m_Animator.SetFloat("Forward", v);
      m_Animator.SetFloat("Right", h);
  }
```
  
## 3. 기본 몬스터 부모 클래스

몬스터의 뼈대가 되는 클래스입니다.  
기본적인 FSM작동 방식과 프로퍼티를 갖추고 있습니다.  

```cs
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
}
```

# TCP 클라이언트
클라이언트에서 서버에게 패킷을 보내고, 서버로부터 받은 패킷을 처리하여 동기화하는 부분을 작업하였습니다.
  
![서버](https://user-images.githubusercontent.com/48229283/101117733-0462a780-362b-11eb-887e-53df0792a2cc.PNG)  
하나의 클라이언트가 연산을 먼저 처리한 후에 서버에게 보내고, 서버는 다시 클라이언트 모두에게 패킷을 보냅니다.  
클라이언트는 패킷을 받고나서 로직을 실행합니다.
  
## 1. 몬스터 이동 동기화
이동 패킷을 매프레임마다 전송하지 않고, 5프레임 단위로 나눠서 전송합니다.  
5프레임이라는 공백이 있기때문에, 이동하는 위치를 연산할때 Time.deltaTime에 5를 곱해줍니다.  

```cs
// 서버에게 이동할 위치, 회전값 보내기
  public bool TCP_SendMovement(float speed)
  {
      // 서버와 연결되어 있지 않다면, false 리턴
      if (!TCP_isConnected)
          return false;

      // 5프레임 단위로 서버에게 이동할 위치 전송
      ++FrameCheck;
      
      // 5프레임 되었을시,
      if (FrameCheck > 5)
      {
          FrameCheck = -1;
          
          // 몬스터가 이동해야할 예상 position 계산
          // 현재 포지션 + 이동할 방향 * 이동 속도 * 델타타임 * 5
          // 5를 곱하는 이유는 5프레임이라는 공백이 있기 때문.
          Vector3 desiredPosition = transform.position + (m_desiredMoveDirection) * speed * Time.deltaTime * 5);
          
          // 일단 서버에게 패킷전송
          try
          {
              TCPClient.m_Monster.Monster_Movement(
                  Packing.STATE.TITANICHYDRA,         // 몬스터 타입
                  index,                              // 몬스터 id
                  (UInt64)TCPClient.PROTOCOL.M_MOVE,  // 프로토콜
                  desiredPosition,                    // 이동 예상 위치
                  m_desiredMoveType,                  // 이동시 움직임
                  speed);                             // 이동 속도

              TCP_isConnected = true;

              return true;
          }
          // 서버와 연결되어 있지 않다면 false 리턴
          catch
          {
              print("Monster_Movement / No connection found with Server");
              TCP_isConnected = false;
              return false;
          }
      }

      return true;
  }
```

## 2. 몬스터 애니메이션 동기화
애니메이션 패킷은 이동 패킷과 달리 한번만 보내도록 하였습니다.  
애니메이션 번호를 서버에게 보내고 클라이언트에서 받은 번호(int)를 enum으로 캐스팅하여 사용합니다.

```cs
// 서버에게 애니메이션 보내기
  public bool TCP_SendAnimation(int anim)
  {
      // 서버와 연결되어 있지 않다면, false 리턴
      if (!TCP_isConnected)
          return false;
      
      // 한번만 보내기
      if (Animation_FrameCheck == Animation_FrameDelay)
      {
          Animation_FrameCheck = -1;
          
          // 애니메이션 보내기
          try
          {
              TCPClient.m_Monster.Monster_Animation(
                  Packing.STATE.TITANICHYDRA,           // 몬스터 타입
                  index,                                // 몬스터 id
                  (UInt64)TCPClient.PROTOCOL.M_ATTACK,  // 프로토콜
                  transform.position,                   // 현재 위치
                  transform.rotation,                   // 현재 회전값
                  v,                                    // vertical
                  h,                                    // horizontal
                  anim);                                // 애니메이션 번호

              TCP_isConnected = true;
              return true;
          }
          // 서버와 연결되어 있지 않다면 false 리턴
          catch
          {
              print("Monster_Animation / No connection found with Server");

              TCP_isConnected = false;
              return false;
          }
      }

      return true;
  }
```

# 캐릭터 Movement
캐릭터는 카메라의 정면 벡터를 기준으로 움직입니다.
![캐릭터 움직임1](https://user-images.githubusercontent.com/48229283/101126577-2534f800-363f-11eb-86ff-4e717aa97c45.PNG)

```cs
void UpdateMovement()
{
  vertical = Input.GetAxis("Vertical");       // 수직 이동값 z값
  horizontal = Input.GetAxis("Horizontal");   // 수평 이동값 x값
  
  // 카메라 정면과 플레이어의 정면 방향을 구한다
  // normalized를 이용해서 방향값만 구한다.
  // 월드 좌표 기준
  m_CamForward = Vector3.Scale(Cam.forward, new Vector3(1, 0, 1)).normalized;           // 카메라가 바라보는 방향에서 y값을 제외한 방향을 구해온다.
  m_PlayerForward = Vector3.Scale(transform.forward, new Vector3(1, 0, 1)).normalized;  // 캐릭터가 바라보는 방향에서 y값을 제외한 방향을 구해온다.

  // vertical(양수이면 전방, 음수이면 후방)*카메라 전방(월드좌표 기준) + horizontal(양수이면 우측, 음수이면 좌측) * 카메라 우측(월드좌표 기준)
  m_desiredMoveDirection = vertical * m_CamForward + horizontal * Cam.right; // 원하는 방향(가려는 방향) 
  
  // 대각선 무브 값 정규화
  // 가령 W와 S를 동시에 입력시 무브값이 가중되어 곱해짐.
  if (m_desiredMoveDirection.magnitude > 1f)  // 가중된 값을 받아 1f값을 넘기게되면 실행
      m_desiredMoveDirection.Normalize();     // 노멀라이즈화시켜 값을 1로 바꾼다.(방향 값만 가지고 오는것)
  
  // 캐릭터 컨트롤러를 이용한 움직임
  // m_desiredMoveDirection으로 이동한다.
  CC.Move(m_desiredMoveDirection * moveSpeed * Time.deltaTime);
}

```

# 카메라 Movement
![카메라 계층구조2](https://user-images.githubusercontent.com/48229283/101127532-1cddbc80-3641-11eb-8e91-7ab361101a2e.PNG) | ![카메라 계층구조](https://user-images.githubusercontent.com/48229283/101127544-26672480-3641-11eb-8e0f-4d2da5fe8fca.PNG)
:-------------------------:|:-------------------------:

캐릭터를 따라다니는 '카메라 리그'가 있고  
'카메라 리그'의 자식으로 '카메라 피봇'이 있습니다.  
'카메라 피봇'은 마우스 X, Y값을 받아서 회전합니다.  
'메인카메라'는 '카메라 피봇'의 자식으로 있어 '카메라 피봇'이 회전하게되면 '메인카메라'도 같이 회전하게 됩니다.  

### CameraRig.cs
```cs
public class CameraRig : MonoBehaviour
{
    [Header("플레이어 추적 속도")]
    public float m_moveSpeed; // 움직이는 속도
    float m_OriginSpeed;
    float m_LowSpeed;
    float m_HighSpeed;

    [Header("추적 대상")]
    public Transform m_target = null; // 플레이어
    public PlayerControl PlayerController; // 플레이어 컨트롤러 스크립트

    public Vector3 moveVector;

    public CameraEvent sc_CEvent;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked; // 마우스 중앙 고정
        Cursor.visible = false; // 커서 숨기기

        m_OriginSpeed = m_moveSpeed;
        m_LowSpeed = m_moveSpeed - 1.5f;
        m_HighSpeed = m_moveSpeed + 1.5f;
        transform.position = m_target.position; // 플레이어 위치

        moveVector = transform.position;
    }

    void Update()
    {
        if (sc_CEvent != null && sc_CEvent.isPlayEvent)
            return;

        UpdateMoveSpeed();
        UpdatePos();
    }

    void UpdatePos()
    {
        moveVector.x = Mathf.Lerp(moveVector.x, m_target.position.x, 4 * Time.deltaTime);
        moveVector.y = Mathf.Lerp(moveVector.y, m_target.position.y, 4 * Time.deltaTime);
        moveVector.z = Mathf.Lerp(moveVector.z, m_target.position.z, 5 * Time.deltaTime);

        //transform.position = moveVector;
        transform.position = Vector3.Slerp(transform.position, moveVector, m_moveSpeed * Time.deltaTime);
    }

    void UpdateMoveSpeed()
    {
        if(Input.GetKey(PlayerKeyCode.Run))
        {
            if (Input.GetKey(PlayerKeyCode.Forward))
                m_moveSpeed = Mathf.Lerp(m_moveSpeed, m_LowSpeed, 3f * Time.deltaTime);
            else if (Input.GetKey(PlayerKeyCode.Backward))
                m_moveSpeed = Mathf.Lerp(m_moveSpeed, m_HighSpeed, 3f * Time.deltaTime);
        }
        else
            m_moveSpeed = Mathf.Lerp(m_moveSpeed, m_OriginSpeed, Time.deltaTime);

        //print(m_moveSpeed);
    }
}
```

### CameraPivot.cs
```cs
public class CameraPivot : MonoBehaviour
{
    public Transform Player; // 플레이어
    public Transform FocusPos; // 
    public Animator Anim; // Anim 변수를 받아오기 위한 애니메이션 변수
    public Transform CamLookPos;
    PlayerControl PlayerController;

    public CameraEvent sc_CEvent;

    [Header("추적 대상 유지거리")]
    public float m_distance;

    [Header("카메라 회전 속도")]
    public float m_rotateSpeed;

    [Header("회전 보간 속도")]
    public float m_LerpSpeed = 100f;

    [Header("타겟 고정 속도")]
    public float m_focusSpeed;

    // 카메라 피봇의 포지션
    Vector3 m_pivotPos;

    // 메인카메라 위치 계산용
    Vector3 tempCamPos;

    Transform m_MainCam = null;
    
    SearchTarget searchTarget;

    // Rotation Members
    float m_rotateX;        // Mouse Y Value
    float m_rotateY;        // Mouse X Value

    [Header("상하 제한 각도")]
    public float m_LimitUpValue = 80.0f;
    public float m_LimitDownValue = -50f;

    [Header("캐릭터 좌우 기준 회전 값")]
    public float rotateValue = 0;

    // Collision Members
    [Header("충돌 거리 제한")]
    public float m_minDistance = 0.3f;
    public float m_maxDistance;

    [Header("충돌 후 카메라 이동 속도")]
    public float m_smoothSpeed = 10.0f;

    LayerMask PlayerLayer;
    RaycastHit hit;

    [Header("충돌 지점")]
    [SerializeField]
    Vector3 m_CollisionCheckPos;


    private void Awake()
    {
        if (m_MainCam == null)
            m_MainCam = Camera.main.transform;

        searchTarget = Player.GetComponent<SearchTarget>(); // 플레이어가 타겟팅을 했을 때 몬스터를 바라보기 위해서
        Anim = Player.GetComponentInChildren<Animator>(); // 캐릭터에 붙어있는 애니메이션 컴포넌트 가져오기

        PlayerLayer = 0 << LayerMask.NameToLayer("Player"); // 플레이어의 레이어

        m_maxDistance = m_distance; // 추적 상대와의 최대 거리 설정

        PlayerController = Player.GetComponent<PlayerControl>(); // 플레이어 컨트롤 스크립트




    }

    void Update()
    {
        if (sc_CEvent != null && sc_CEvent.isPlayEvent)
            return;

        UpdateCollision();
        UpdateDistance();
        UpdateRotation();
        UpdateFocusState();
    }

    // 대상 거리 유지
    void UpdateDistance()
    {
        // 현재 카메라 피봇의 뒤쪽 방향
        tempCamPos = transform.forward * -1;
        // 거리를 곱해주면 메인카메라가 위치해야할 벡터를 얻는다
        

        if (!side_check) // 벽에 충돌하지 않는 중
        {
            tempCamPos *= m_distance;
            m_distance = m_maxDistance;
            //Debug.Log("기본 처리 수행중");
            // 보간을 이용해서 부드럽게 카메라 무빙이 이루어지도록 ( 카메라피봇 - 메인카메라 사이의 거리를 말하는거임, 회전 보간이 아님)
            m_MainCam.position = Vector3.Slerp(m_MainCam.position, transform.position + tempCamPos, m_smoothSpeed * Time.deltaTime);
        }
        else // 벽에 충돌하는 중
        {
            tempCamPos *= lres;
            //Debug.Log("사이드처리 수행중");
            m_MainCam.position = Vector3.Slerp(m_MainCam.position, transform.position + tempCamPos, m_smoothSpeed * Time.deltaTime);
        }
    }

    bool prevOnce = false;
    // 카메라 회전
    void UpdateRotation()
    {
        if (searchTarget.FocusState || OptionManager.Option_Active) 
            return;      

        m_rotateX = Input.GetAxis("Mouse Y") * m_rotateSpeed * -1;
        m_rotateY = Input.GetAxis("Mouse X") * m_rotateSpeed;

        if (prevOnce)   // 타겟팅 풀린 직후 카메라 위치 초기화
        {
            prevOnce = false;
            transform.rotation = prevRotation;
        }

        
        // 카메라 자동 회전
        Vector3 lookDirection = CamLookPos.position - Camera.main.transform.position; // 캐릭터를 향한 방향(카메라가 바라봐야하는 방향)
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 2 * Time.deltaTime);
        
        
        // 카메라 상하 제한
        float XAxisRot = transform.rotation.eulerAngles.x + m_rotateX;
        if (XAxisRot >= 180)
            XAxisRot -= 360;
        XAxisRot = Mathf.Clamp(XAxisRot, m_LimitDownValue, m_LimitUpValue);

        transform.rotation = Quaternion.Euler(
            XAxisRot, 
            transform.rotation.eulerAngles.y + m_rotateY, 
            0);        
    }


    Quaternion prevRotation;
    Quaternion toRotation;
    Vector3 toDirection;
    void UpdateFocusState()
    {
        if (!searchTarget.FocusState)
            return;
        
        toDirection = searchTarget.FocusedEnemy.GetComponent<BaseMonsterController>().pivot.position;

        toRotation = Quaternion.LookRotation(toDirection - FocusPos.position);

        if (searchTarget.FocusedEnemy != null)
        {
            Transform pivot = searchTarget.FocusedEnemy.GetComponent<BaseMonsterController>().pivot;
            Vector3 focusScreenPoint = Camera.main.WorldToScreenPoint(pivot.position);

            if (Camera.main.pixelWidth / 2 - 450 >= focusScreenPoint.x ||
                focusScreenPoint.x >= Camera.main.pixelWidth / 2 + 450 ||
                Camera.main.pixelHeight / 2 >= focusScreenPoint.y ||
                Camera.main.pixelHeight / 2 + 150 <= focusScreenPoint.y)
                m_focusSpeed = Mathf.Lerp(m_focusSpeed, 4f, 4f * Time.deltaTime);
            else
                m_focusSpeed = Mathf.Lerp(m_focusSpeed, 2f, 4f * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, m_focusSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.y,
            0);
        }

        prevOnce = true;
        prevRotation = transform.rotation;
    }
  }
```

# 그 외
## 맡은 역할
클라이언트 프로그래머 포지션으로  
몬스터 AI와 서버간 동기화 작업을 하였습니다.  
개발 초기에는 캐릭터와 카메라 클래스의 프레임워크 작업을 하였습니다.  

## 프로젝트를 진행하며 어려웠던 점
작업의 어려움보다는 팀원간의 커뮤니케이션, 협업을 함에 있어서 어려움을 겪었습니다.  
다른 팀원이 한 작업물의 퀄리티가 마음에 들지 않는 것.  
일정이 계속 밀리는 것. 등으로 인한 점이 어려웠습니다.  

## 개선된 점
부족한 부분이 있다면 해당 팀원과 그리고 기획자와 회의를 거쳐 부족한 부분을 피드백하고 개선했습니다.  
변동되는 일정에 맞추어서 작업속도를 조절하였습니다.  
  
프로젝트를 진행하며 프로그래밍 능력도 발전하였지만, 팀원을 대하는 태도와 커뮤니케이션 능력도 발전하였습니다.  
