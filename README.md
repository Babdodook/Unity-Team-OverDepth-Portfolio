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
* 캐릭터 회전과 이동  
* 카메라 회전과 이동  

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

  // m_moveType 변경
  public void SetMoveType(MoveType type)
  {
      if (TCP_isConnected)
          m_desiredMoveType = type;
      else
          m_moveType = type;
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

# 캐릭터 회전과 이동
# 카메라 회전과 이동
