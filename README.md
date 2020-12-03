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


몬스터의 상태머신을 정의하는 부모 클래스를 작성하고, 각 몬스터 자식 클래스에서 상속을 받아 재정의합니다.
![몬스터상속구조](https://user-images.githubusercontent.com/48229283/100949531-d3f90b80-354d-11eb-9fbb-55f3987331d5.PNG)

## BaseMonsterController - 몬스터 부모 클래스
몬스터 상태머신의 기초가 되는 클래스입니다.
### 상태머신 정의
부모 클래스에서 함수의 원형을 정의하고 자식 클래스에서 상속받아 재정의합니다.
```cs
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
          // m_nowState에 따라 상태 변경
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
```
  
### Animator Blendtree 파라미터 업데이트
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
  
### 스택을 이용한 몬스터 행동 패턴 정의
'이동 스택'과 '공격 스택'에 정의된 패턴을 Push하고 Pop하여 사용합니다.

![스택움직임](https://user-images.githubusercontent.com/48229283/100953436-ee36e780-3555-11eb-8dc2-0065696b1698.PNG)

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

