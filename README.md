# Unity-Team-OverDepth-Portfolio
**팀 / 게임 명 :** Team OverDepth / OverDepth  
**개발 기간 :** 2020.03.01 ~ 12.03 (기획 30일, 개발 247일)  
**개발 환경 :** Unity 2019.2.21f1 / Visual Studio 2019 / Github Desktop / Git LFS / Trello  
**개발 인원 :** 11명  
**요 약 :** 유니티 엔진을 활용한 최대 2인까지 협동 가능한 3D 액션 게임.  
G-Star 2020 전시, 교내 프로젝트 경진대회 전시, GGC(Global Game Challenge 2020) 전시를 위해 개발하였습니다.  
  
**담당자 / 담당 업무 :** 이재성 / 클라이언트 프로그래머  

# ScreenShots


# 기능  
* 몬스터 AI  
* TCP 클라이언트  
* 캐릭터 회전과 이동  
* 카메라 회전과 이동  

# Code

## BaseMonsterController - 몬스터 부모 클래스
몬스터 상태머신의 기초가 되는 클래스입니다.
### 상태머신 정의
부모 클래스에서 함수의 원형을 정의하고 자식 클래스에서 상속받아 재정의합니다.
```swift
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
```swift
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
