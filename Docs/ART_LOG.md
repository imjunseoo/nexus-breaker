# ART LOG — 도트 세션

## 도구 / 파이프라인
- Aseprite 1.3.18.6 소스 빌드(개인용): `C:\Users\imjun\Tools\aseprite\src\build\bin\aseprite.exe`
- 팔레트: `ArtSource/palette/nexus32.gpl` (스펙 32색, 키 문자 매핑은 `ArtSource/tools/px.py` 상단)
- 원본: `ArtSource/src/<분류>/<에셋>.py` — 픽셀을 ASCII 그리드(1글자=1픽셀)로 직접 찍고, 파츠 조합 + 손 보정.
- 빌드: `python ArtSource/src/<분류>/<에셋>.py --export` → 레이어 PNG + Lua 생성 → Aseprite CLI로 `.aseprite` 저장 + `ArtSource/previews/<분류>/`에 4배 PNG 시트·태그별 GIF.
- 검증: `python ArtSource/tools/verify.py` — 캔버스 크기·태그·프레임 수·프레임 시간·팔레트(32색) 위반 검사.
- 색 모드 RGB(팔레트는 파일에 내장), 반투명 픽셀 없음, 외곽선 `#1e1a17`.

## 진행 상황
| 분류 | 상태 |
|---|---|
| 캐릭터 repairman | ✅ 임시 확정(Claude 버전) — PixelLab 비교 대기 |
| **전체** | **65/65 파일, verify.py 전부 OK (크기·태그·프레임 수·ms·32색 팔레트)** |
| 캐릭터 helper | ✅ idle 4×160, swing 4×90(3번째=타격) |
| 넥서스 | ✅ s0~s4_idle, hit, destroy |
| 아이템 30 | ✅ (외계 6·??? 3종은 `idle` 4×150 발광 루프, 나머지 1프레임·태그 없음) |
| 박스 5 | ✅ idle 2×300 / open 5×70 |
| FX 6 | ✅ |
| 손님 8 | ✅ walk 6×100 / idle 4×180 / buy 4×120 / reject 4×120 |
| 환경 6 | ✅ bg_sky/far/mid/ground(가로 무한 타일), shop_stall(idle 4×200), props(9종) |
| UI 7 (+ui_chat 추가 요청분) | ✅ |

## 애니메이션 메모 (코드 참고)
- repairman (48×48, 피벗 바닥 중앙, 왼쪽을 봄)
  - `idle` 4×150: 망치를 땅에 세우고 기대어 숨쉬기.
  - `swing` 6×70: 1 들어올림(예비) → 2 머리 위 → 3 내려침(스미어) → **4 타격** → 5 반동 → 6 내려놓기.
  - `swing_crit` 8×60: 1 깊게 웅크림 → 2 도약 → 3 정점(뒤로 젖힘) → 4 내려침(큰 스미어) → **5 타격** → 6 타격 흔들림(1px) → 7 반동 → 8 복귀.
  - `cheer` 4×120: 웅크림 → 점프하며 망치 들어올림 ×2 → 착지.
  - 타격 순간 망치 머리 왼쪽 끝 ≈ 캔버스 x=2~4, y=28~40 (넥서스 오른쪽 면에 닿는 위치).
- nexus (80×96, 피벗 바닥 중앙)
  - 캔버스 바닥 = 바닥선. 결정 본체 꼭대기는 y≈17 → 바닥선 기준 배치 시 96px 창 안에 들어옴(위 16px 줄은 떠다니는 입자만).
  - `s0~s4_idle` 4×200: 코어·맥 맥동 + 떠오르는 입자. 단계마다 균열/파편 탈락 누적, s3 꼭대기 부러짐(잔해가 왼쪽 바닥에), s4 코어(청록·자홍) 노출.
  - `hit` 2×50: s0 기준 흰 번쩍임(1=전체, 2=반). s1~s4에서는 실루엣이 달라서, 단계별 번쩍임은 셰이더(스프라이트 흰색 틴트)로 하는 걸 권장.
  - `destroy` 12×80: 1~2 과열 → 3 백색 섬광 → 4~11 파편 비산·낙하 + 충격 링 + 먼지 → 12 잔해(마지막 프레임 유지 권장, Loop 끄기).
- 박스 `open` 5×70: 1 눌림 → 2 뚜껑 튐 → 3~4 빛기둥 → 5 잔광. 마지막 프레임에서 fx_box_poof와 아이템 아이콘 등장 연출 권장.
- helper (32×32, 피벗 바닥 중앙, 왼쪽을 봄): 렌치. `swing` 1 들어올림 → 2 내려침 → **3 타격**(렌치 수평, 캔버스 x≈3~10, y≈16~19) → 4 복귀.
- customers (32×32, 피벗 바닥 중앙, 왼쪽을 봄 — 오른쪽으로 갈 땐 flipX): 발바닥 외곽선이 y=31(캔버스 맨 아래)에 고정.
  - `walk` 6×100: 접지→하강→교차 ×2(양발). 이동 속도는 프레임당 약 2px(=초당 20px) 기준으로 맞추면 발 미끄러짐이 적음.
  - `idle` 4×180 숨쉬기 + 4번째 눈 깜빡임 / `buy` 4×120: 끄덕 → 너트 동전 들어올림 → 반짝 → 끄덕 / `reject` 4×120: 고개 젓기 + 손바닥 들어 거절, 찡그림.
  - alien: 사람 옷(페도라·트렌치코트)을 어색하게 걸친 검은 실루엣, 눈만 네온 청록. cultist: 네온은 가슴의 넥서스 결정 펜던트만.
- FX: `play` 태그는 1회 재생용(Loop 끄기). fx_debris는 태그 a~d 각 1프레임(파편 4종, 코드가 랜덤 선택).

## Unity 임포트 설정 제안 (메인 세션용)
- 공통: PPU 32, Filter Point, Compression None. 색 모드 RGB, 반투명 픽셀 없음.
- **Import Mode = Merge Frame**(기본): 캐릭터·손님·넥서스·박스·FX·아이템·bg_*·ground·shop_stall, ui_panel/ui_panel_dark/ui_nut_small.
- UI 다중 상태 파일(`ui_button` `ui_slot` `ui_bar` `ui_icons`)은 스펙 2-8 갱신에 따라 **레이어 1장 + 1프레임짜리 태그**로 구분(태그명 = 상태/아이콘 이름, 소문자). Merge Frame 그대로 쓰고 코드가 `파일/태그`로 연결.
- **Import Mode = Individual Layers**(레이어 = 스프라이트 1개)가 필요한 건 이제 `props`(소품 9종)뿐.
- 방향: repairman·helper·customer_* 모두 **왼쪽을 보는 옆모습**(스펙 2-1·2-6). 오른쪽을 볼 땐 flipX.
- 피벗: repairman/helper/customer_*/nexus = Bottom Center. 아이템·FX·UI = Center. bg_*·ground = Bottom Left 권장(타일링).
- 루프: idle/walk/s*_idle = 루프, swing/swing_crit/cheer(1회), hit/destroy/open/play = 1회(Loop 끄기).

## 9-slice / 슬라이스
9-slice 테두리는 코드가 런타임에 붙임(스펙 2-8). 그 폭에 맞춰 장식은 모서리 칸에만, 변은 늘어나는 방향으로 균일, 가운데는 단색으로 그림. 9-slice 늘림 시뮬레이션으로 확인함.
Unity Sprite Border 순서는 L, B, R, T (px). 파일 안에도 같은 값을 center로 가진 슬라이스 1개를 참고용으로 넣어 둠.
| 파일 | 태그(1프레임씩) | Border L,B,R,T |
|---|---|---|
| ui_panel | (없음, 1프레임) | 6, 6, 6, 6 |
| ui_panel_dark | (없음, 1프레임) | 6, 6, 6, 6 |
| ui_chat (32×32) | (없음, 1프레임) | 8, 8, 8, 8 — 손님 채팅/방명록 말풍선(160×96 등으로 늘려 씀). 왼쪽 3px는 투명 여백, 꼬리는 왼쪽-아래 모서리 칸(y 25~29)에서 왼쪽을 가리킴. 헤더 띠(녹슨 철판 y 1~6, 구분선 y 7) + 오른쪽 위 모서리에 타이핑 점 3개. 안쪽 단색 `#2e2620`(ui_panel_dark와 같음) → 밝은 텍스트용 |
| ui_button | `normal` `hover` `pressed` | 5, 5, 5, 5 |
| ui_slot | `empty` `selected` | 4, 4, 4, 4 |
| ui_bar | `frame` `fill` | 2, 2, 2, 2 — `fill`은 프레임 안쪽(x 2~61, y 2~5)만 칠함 |
| ui_icons (16×16) | `nut` `hammer` `helper` `speed` `crit` `box` `warehouse` `shelf` `customer` `shop` `settings` `prestige_core` `close` `pin` `ghost` | 없음 |
| ui_nut_small (8×8) | (없음) | 없음 |
| props (160×72) | 레이어·슬라이스 `car_wreck tire_stack oil_drum oil_drum_fallen sandbags road_sign traffic_cone scrap_pile lamp_post` (각 슬라이스 맨 아래 줄 = 지면 접점) | 없음 |
- ui_button `pressed`는 면이 1px 내려앉은 모양(위쪽 그림자 줄) — 텍스트도 1px 내려주면 자연스러움.

## 환경 메모
- bg_sky / bg_far / bg_mid / ground: 가로 384px **이음새 없이 타일링**(x=0과 x=383이 이어짐) → 패럴랙스 스크롤 가능.
- 바닥선: 배경 3장은 y=80(창 아래에서 16px)을 지면으로 그림. ground(384×16)는 그 아래 16px 띠.
- bg_far/bg_mid는 투명 배경. 투명 모드에서는 ground + shop_stall + props만 보여도 어색하지 않도록 ground 위쪽 가장자리를 불규칙하게 처리.
- shop_stall(128×80): 맨 아래 줄 = 지면 접점(앞바퀴·블록). 카운터 상판은 y=52~55, 진열 선반 y=40. 랜턴(x=60,y=35~39) 깜빡임 + 지붕 깃발 펄럭임.

## 미리보기
- 에셋별: `ArtSource/previews/<분류>/<에셋>.png`(태그별 행, 4배 안팎) + 애니메이션 태그마다 `<에셋>__<태그>.gif`.
- 전체: `ArtSource/previews/contact_sheet.png` — **실제 Assets/Art/*.aseprite를 Aseprite CLI로 다시 읽어서** 만든 시트(재생성: `python ArtSource/tools/contact_sheet.py`).
- 장면 목업: `ArtSource/previews/scene_mockup.png`(384×96, 3배 — 하늘·원경·중경·지면·넥서스·수리공 타격·노점·손님), `scene_mockup_transparent.png`(투명 모드 가정).

## 스펙과 다르게 한 점 / 제안 (스펙 문서는 수정하지 않음)
- (해결) UI 다중 상태 파일은 스펙 2-8 갱신대로 1프레임 태그 구조로 바꿈. ui_icons 캔버스는 16×16(아이콘 15프레임).
- props 캔버스: 160×72(스펙 "자유"), 레이어/슬라이스 구조 유지(스펙 2-7 그대로).
- 넥서스 `hit`은 스펙대로 s0 기준 1세트. 실제 게임에선 현재 단계 스프라이트에 흰색 틴트(셰이더/Material)로 번쩍이는 편이 실루엣이 안 튐 — 제안.
- 넥서스 결정 꼭대기를 y≈17로 낮춰서, 캔버스 바닥을 바닥선에 맞추면 96px 창 안에 다 들어옴(스펙 크기 80×96 그대로).

## 남은 것
- PixelLab 비교: `ArtSource/compare/pixellab/`이 아직 없음. 도착하면 `python ArtSource/tools/compare_sheet.py` → `ArtSource/compare/compare_sheet.png` + `compare_idle.gif` / `compare_swing.gif` 생성. 사용자가 고른 쪽을 repairman으로 확정.

## 비교 테스트
- Claude 버전: `ArtSource/compare/claude/` (repairman.aseprite, idle/swing 4배 GIF, 전체 시트)
- PixelLab 버전 대기 중 → 도착하면 `ArtSource/compare/compare_sheet.png` 제작.
