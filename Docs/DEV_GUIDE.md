# 넥서스 부수기 — 개발 가이드

## 실행
1. Unity Hub에서 `C:\Users\imjun\NexusBreaker`를 **Unity 6000.3.5f2**로 연다.
2. `Assets/Scenes/Main.unity`를 열고 Play.
   - Game 뷰 크기는 `1152x288`(3배) 또는 `1152x648`(상점 열었을 때)이 가장 깔끔하다. 다른 크기면 정수 배율로 가운데 정렬된다.
   - `GameRoot` 오브젝트의 **Fresh Start In Editor**를 켜면 세이브를 무시하고 새 게임, **Editor Time Scale**로 배속 확인.

## 조작
- 넥서스 클릭: 직접 타격(수리공·조수는 자동으로 때림)
- 오른쪽 위 버튼: 상점 / 강화 / 설정 / ≡(끌어서 창 이동 — 빌드에서만)
- 상점: 왼쪽 창고(박스 클릭 = 하나 열기, "모두 열기"), 아이템 클릭 → 오른쪽에서 가격 정하고 "진열하기"
  (또는 아이템 선택 후 빈 진열대 칸 클릭). 진열된 칸 클릭 → 가격 바로 변경 / 내리기.
  칸 왼쪽 위 점 색 = 예상 판매 속도(청록 즉시 · 초록 빠름 · 노랑 보통 · 주황 느림 · 빨강 매우 느림 · 자홍 기적)

## 에디터 메뉴 (Nexus Breaker)
| 메뉴 | 하는 일 |
|---|---|
| 1. Setup Project | TMP 리소스, 플레이어 설정(1152×288 창·백그라운드 실행·플립 모델 끔), Main 씬, 에셋 연결 |
| 2. Wire Assets | `Assets/Art`·`Assets/Audio`·`Assets/Fonts`를 이름 규칙으로 훑어 `Resources/GameAssets.asset` 갱신. **아트·사운드가 추가/교체될 때마다 실행** |
| Re-apply Aseprite Import Defaults | 모든 .aseprite에 PPU 32·Point·무압축·피벗 규칙 재적용 |
| Run Balance Simulation | 방치 봇으로 첫 넥서스 파괴 시간 추정 → 콘솔 |

배치 모드(에디터를 닫은 상태):
```
Unity.exe -batchmode -projectPath . -executeMethod NexusBreaker.EditorTools.ProjectSetup.RunBatch
Unity.exe -batchmode -projectPath . -executeMethod NexusBreaker.EditorTools.BalanceSim.RunBatch   (→ Docs/BALANCE_SIM.md)
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## 자동 스크린샷(빌드 검증용)
```
NexusBreaker.exe -nbSaveDir <임시폴더> -nbDemo -nbPage shop -nbShot shot.png -nbDelay 5
```
`-nbSaveDir`로 실제 세이브를 건드리지 않는다. `-nbDemo`는 보여주기용 상태로 시작한다.

## 코드 구조 (`Assets/Scripts`)
| 폴더 | 내용 |
|---|---|
| `Core/` | 큰 수 표시(만·억·조), 난수·분포, 세이브 데이터·저장 |
| `Data/` | 아이템 30종·손님 8종·강화 11종·시장 이벤트·지역 카탈로그, **`Stats.cs`(밸런스 공식 전부)** |
| `Model/` | `GameModel` — 게임 규칙 전체(순수 C#, Unity 의존 없음). 타격·박스·상점·시세·환생·오프라인 |
| `Presentation/` | `GameRoot`(진입점), `GameAssets`(에셋 참조), `SpriteAnimator`(Aseprite 태그 재생), 플레이스홀더, 오디오 |
| `Presentation/World/` | 넥서스·수리공·조수·박스·손님·이펙트 연출 |
| `Presentation/UI/` | 코드로 생성하는 uGUI: HUD·상점·강화·설정·툴팁·알림·팝업 |
| `Platform/` | `DesktopWindow` — 테두리 없는 창·항상 위·투명(컬러키)·클릭 통과·창 끌기 (Windows 빌드 전용) |
| `Editor/` | 셋업·에셋 자동 연결·Aseprite 임포트 기본값·밸런스 시뮬레이터 |
| `Tests/EditMode/` | 모델 테스트 |

## 규칙
- 입력은 Input System + `InputSystemUIInputModule`만 사용(구식 StandaloneInputModule 금지).
- 아트가 없으면 플레이스홀더로 돈다. 아트 파일명·태그명은 `Docs/ASSET_SPEC.md`가 계약.
- 밸런스는 `Data/Stats.cs`에서만 바꾸고 시뮬레이션으로 확인.
- 한글 폰트: 지금은 개발용으로 OS의 맑은 고딕을 런타임에 읽는다(빌드에 포함 안 됨). 정식 폰트(TMP 폰트 에셋)를 `Assets/Fonts/`에 넣고 Wire Assets를 실행하면 교체된다.
