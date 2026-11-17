# 넥서스 부수기 — 에셋 스펙 v1

이 문서는 도트 세션 · 사운드 세션 · 게임 코드 세션 사이의 **계약**이다.
파일명·태그명·크기를 바꾸면 코드가 깨지므로, 바꿔야 하면 이 문서를 먼저 고치고 메인(코드) 세션에 알린다.

---

## 0. 작업 영역 (세션별 소유권)

| 세션 | 쓸 수 있는 곳 | 금지 |
|---|---|---|
| 도트 세션 | `Assets/Art/**`, `ArtSource/**`, `Docs/ART_LOG.md` | 그 외 전부 |
| 사운드 세션 | `Assets/Audio/**`, `AudioSource/**`, `Docs/SOUND_LOG.md` | 그 외 전부 |
| 메인(코드) 세션 | 나머지 전부 (`Assets/Scripts`, `Scenes`, `Prefabs`, `Settings`, `Packages`, `ProjectSettings`, `Docs/GDD.md`, 이 문서) | `Assets/Art`, `Assets/Audio` 내용 수정 |

- 서브 세션은 **git commit 금지**, **Unity 에디터 실행 금지**(메인 세션이 에디터를 연다), **`.meta` 파일 생성·수정 금지**(Unity가 만든다).
- 생성 스크립트·원본·미리보기는 `ArtSource/`, `AudioSource/`(Assets 밖)에 둔다.
- 진행 기록은 각자의 LOG 문서에 남긴다(무엇을 만들었는지, 남은 것, 스펙과 다르게 한 점).

---

## 1. 도트 공통 규칙

- **PPU 32** (1 유닛 = 32px). Point 필터, 압축 없음.
- 원본 형식: **`.aseprite`** → Unity `com.unity.2d.aseprite` 임포터가 애니메이션 클립을 자동 생성.
- **Aseprite 태그명 = 애니메이션 상태명** (코드가 `Animator.Play("swing")`처럼 호출). 태그명은 아래 표 그대로, 소문자 snake_case.
- 프레임 시간은 Aseprite 프레임 duration으로 지정(아래 표는 기준값).
- 피벗: 캐릭터·넥서스·손님 = **바닥 중앙**. 아이콘·이펙트 = 중앙.
- 광원: **좌상단**. 외곽선: 캐릭터/아이템은 1px 어두운 외곽선(`#1e1a17`), 셀렉티브 아웃라인 허용.
- 레이어가 여러 개여도 되지만, 최종 결과는 레이어 합성 기준으로 임포트된다(임포터 기본값: 레이어 병합).
- 미리보기: `ArtSource/previews/`에 4배 확대 PNG/GIF.

### 팔레트 (32색, 고정)
모든 스프라이트는 이 팔레트만 쓴다. Aseprite 팔레트 파일: `ArtSource/palette/nexus32.gpl`.

| 그룹 | 색 |
|---|---|
| 외곽/암부 | `#0f0d0c` `#1e1a17` `#2e2620` |
| 녹·갈색 | `#4a3428` `#6b4a35` `#8c5f3f` `#b07a4f` `#d29c6a` |
| 잿빛 | `#3b3d40` `#575a5c` `#7b7d7a` `#a3a39b` `#c9c6b8` `#e8e3d3` |
| 먼지·모래 | `#7a6a4f` `#a08b63` `#c4ad7f` |
| 이끼·독성 | `#2f3a24` `#4c5c32` `#73843f` |
| 녹슨 적색 | `#5c1f1a` `#8e2f22` `#c2482c` `#e07a3c` |
| 불빛 | `#f2b84b` `#ffe08a` |
| 넥서스·외계 네온 | `#1b0f3a` `#4b1f8c` `#8a3ff2` `#2bd9c9` `#8ff7e8` `#f25fd0` |

규칙: 세계(폐허·인물·생필품)는 저채도 그룹만. **넥서스와 외계 물건만** 네온 그룹을 쓴다 → 화면에서 외계 요소가 한눈에 구분돼야 한다.

### 화면 레이아웃 (참고)
- 메인 띠 창 내부 해상도 **384×96** (3배 표시). 바닥선 y=16(아래에서 16px).
- 왼쪽 0~192px: 넥서스(중심 x≈96) + 수리공(넥서스 오른쪽 옆, x≈140).
- 오른쪽 192~384px: 노점(폐버스 개조) + 손님 줄(오른쪽 화면 밖에서 걸어 들어옴).
- 상점 페이지 내부 해상도 **384×216** (uGUI, 패널·버튼은 9-slice).

---

## 2. 도트 에셋 목록

### 2-1. 캐릭터 — `Assets/Art/Characters/`
| 파일 | 캔버스 | 태그 (프레임 수 · 프레임당 ms) |
|---|---|---|
| `repairman.aseprite` | 48×48 | `idle` (4 · 150), `swing` (6 · 70, 4번째 프레임이 타격 순간), `swing_crit` (8 · 60, 큰 동작), `cheer` (4 · 120) |
| `helper.aseprite` | 32×32 | `idle` (4 · 160), `swing` (4 · 90, 3번째 프레임이 타격 순간) |

- **방향: 수리공·조수 모두 왼쪽(넥서스 쪽)을 보는 옆모습**(수리공은 넥서스 오른편에 선다). 코드가 필요하면 flipX로 뒤집는다.
- 수리공: 작업복, 안전모(찌그러짐), 방진 마스크 목에 걸침, 큰 망치. 실루엣이 32px 안팎 키로 또렷하게.
- 조수: 수리공보다 작고 단순한 생존자 일꾼, 렌치나 쇠파이프.

#### 수리공 비교 테스트 (Claude 손도트 vs PixelLab)
사용자가 두 방식의 품질 차이를 보고 싶어 한다. 수리공의 `idle` + `swing` 만 두 버전으로 만든다.
- Claude 버전: `ArtSource/compare/claude/` (도트 세션이 Aseprite로 직접 찍음)
- PixelLab 버전: `ArtSource/compare/pixellab/` (PixelLab MCP가 연결된 별도 세션이 생성 후 팔레트·크기만 맞춤)
- 비교 시트: `ArtSource/compare/compare_sheet.png` — 같은 배율(4배)로 나란히, 애니메이션은 GIF 2개.
- 사용자가 고른 쪽을 `Assets/Art/Characters/repairman.aseprite`로 확정한다. 확정 전까지는 Claude 버전을 임시로 넣어둔다.

### 2-2. 넥서스 — `Assets/Art/Nexus/`
| 파일 | 캔버스 | 태그 |
|---|---|---|
| `nexus.aseprite` | 80×96 | `s0_idle` `s1_idle` `s2_idle` `s3_idle` `s4_idle` (각 4 · 200, 맥동 발광 루프), `hit` (2 · 50, 흰 번쩍임 — s0 기준 하나면 됨), `destroy` (12 · 80, 산산조각 + 코어 섬광) |

- 외계 결정 기둥 형태, 지면에 비스듬히 박힘, 주변에 흡수된 잡동사니가 박혀 있음.
- s0 → s4로 갈수록 균열·파편 탈락이 늘고, s4는 내부 코어(청록·자홍)가 드러남.

### 2-3. 이펙트 — `Assets/Art/FX/`
| 파일 | 캔버스 | 태그 |
|---|---|---|
| `fx_hit_spark.aseprite` | 16×16 | `play` (4 · 50) |
| `fx_crit_burst.aseprite` | 32×32 | `play` (5 · 50) |
| `fx_debris.aseprite` | 8×8 | `a` `b` `c` `d` (각 1프레임, 파편 4종) |
| `fx_box_poof.aseprite` | 24×24 | `play` (5 · 60) |
| `fx_nut_sparkle.aseprite` | 8×8 | `play` (4 · 80) |
| `fx_level_up.aseprite` | 32×32 | `play` (6 · 60) |

### 2-4. 박스 — `Assets/Art/Boxes/`
캔버스 16×16, 각 파일 태그 `idle` (2 · 300, 미세한 흔들림/반짝임), `open` (5 · 70).

| 파일 | 등급 | 모양 |
|---|---|---|
| `box_common.aseprite` | 일반 | 테이프 붙은 골판지 상자 |
| `box_uncommon.aseprite` | 고급 | 나무 크레이트 |
| `box_rare.aseprite` | 희귀 | 금속 케이스 |
| `box_alien.aseprite` | 외계 | 발광하는 유기체 포드(네온) |
| `box_unknown.aseprite` | ??? | 글리치처럼 일렁이는 검은 큐브 |

### 2-5. 아이템 아이콘 — `Assets/Art/Items/`
캔버스 **16×16**, 파일명 = `item_<id>.aseprite`. 기본 1프레임(태그 없음 가능). 외계·??? 등급은 `idle` 태그로 2~4프레임 발광 루프 권장.
상표·실존 브랜드 디자인 금지(일반화된 모양으로).

| id | 이름 | 등급 | 기준가(너트) |
|---|---|---|---|
| snack_shrimp | 새우맛 과자 | 일반 | 15 |
| choco_bun | 눅눅한 초코빵 | 일반 | 18 |
| water_bottle | 생수 | 일반 | 20 |
| lighter | 라이터 | 일반 | 22 |
| cup_ramen | 컵라면 | 일반 | 25 |
| bandage | 붕대 | 일반 | 28 |
| battery | 건전지 | 일반 | 30 |
| canned_food | 통조림 | 일반 | 35 |
| cigarettes | 담배 한 갑 | 일반 | 40 |
| mask_filter | 방독면 필터 | 일반 | 60 |
| diary | 누군가의 일기장 | 고급 | 90 |
| teddy_bear | 곰인형 | 고급 | 110 |
| broken_radio | 고장난 라디오 | 고급 | 120 |
| melted_watch | 반쯤 녹은 손목시계 | 고급 | 150 |
| rusty_toolbox | 녹슨 공구함 | 고급 | 180 |
| lp_record | LP 레코드 | 고급 | 200 |
| gas_mask | 방독면 | 고급 | 220 |
| medkit | 구급상자 | 희귀 | 500 |
| gold_tooth | 금니 | 희귀 | 600 |
| film_camera | 필름 카메라 | 희귀 | 700 |
| generator_part | 발전기 부품 | 희귀 | 800 |
| glow_jelly | 발광 젤리 | 외계 | 2200 |
| warm_stone | 계속 따뜻한 돌 | 외계 | 2500 |
| zero_g_spoon | 무중력 숟가락 | 외계 | 2800 |
| humming_cube | 웅웅거리는 큐브 | 외계 | 3200 |
| alien_remote | 해석불가 리모컨 | 외계 | 4000 |
| crystal_shard | 넥서스 결정 조각 | 외계 | 5000 |
| core_fragment | 넥서스 코어 파편 | ??? | 25000 |
| singing_skull | 노래하는 두개골 | ??? | 30000 |
| star_map | 별 지도 | ??? | 40000 |

### 2-6. 손님 — `Assets/Art/Customers/`
캔버스 32×32, 파일명 `customer_<id>.aseprite`, 태그: `walk` (6 · 100), `idle` (4 · 180), `buy` (4 · 120, 만족), `reject` (4 · 120, 고개 젓기).
id: `survivor_a`, `survivor_b`, `merchant`, `raider`, `soldier`, `cultist`, `collector`, `alien`.

- 오른쪽에서 왼쪽으로 걸어오는 **왼쪽을 보는 옆모습** 기준(코드가 flipX로 반대 방향 처리).
- 수집가: 낡았지만 화려한 코트·외알 안경. 외계인: 사람 옷을 어색하게 걸친 실루엣 + 눈만 네온.

### 2-7. 환경 — `Assets/Art/Environment/`
| 파일 | 크기 | 내용 |
|---|---|---|
| `bg_sky.aseprite` | 384×96 | 스모그 낀 황혼 하늘 그라데이션(디더링) |
| `bg_far.aseprite` | 384×96 | 먼 폐허 스카이라인 실루엣 (투명 배경, 패럴랙스용) |
| `bg_mid.aseprite` | 384×96 | 가까운 폐허·전신주·잔해 (투명 배경) |
| `ground.aseprite` | 384×16 | 지면 띠 (투명 모드에서도 표시됨) |
| `shop_stall.aseprite` | 128×80 | 폐버스 개조 노점(카운터, 차양, 랜턴). 태그 `idle` (4 · 200, 랜턴 깜빡임) |
| `props.aseprite` | 자유 | 부서진 자동차, 드럼통, 타이어 더미 등. 레이어/슬라이스별로 1개씩 |

투명 배경 모드에서는 하늘·원경을 끄고 `ground` + `shop_stall` + `props`만 보인다 → 이 셋은 단독으로도 어색하지 않게.

### 2-8. UI — `Assets/Art/UI/`
| 파일 | 크기 | 비고 |
|---|---|---|
| `ui_panel.aseprite` | 24×24 | 녹슨 철판 패널, 9-slice 테두리 6px |
| `ui_panel_dark.aseprite` | 24×24 | 안쪽 패널(어두운 톤), 9-slice 6px |
| `ui_button.aseprite` | 24×16 | 태그 대신 레이어/슬라이스 3종: normal / hover / pressed, 9-slice 5px |
| `ui_slot.aseprite` | 20×20 | 진열대·창고 칸 프레임 (빈 칸 / 선택됨 2종) |
| `ui_bar.aseprite` | 64×8 | HP 바 프레임 + 채움(네온 청록) 별도 레이어 |
| `ui_icons.aseprite` | 16×16 × N | 아이콘 시트: nut(화폐), hammer, helper, speed, crit, box, warehouse, shelf, customer, shop, settings, prestige_core, close, pin(항상 위), ghost(클릭 통과) |
| `ui_nut_small.aseprite` | 8×8 | 텍스트 옆 작은 너트 아이콘 |

> **여러 아이콘·상태가 든 UI 파일은 "1프레임짜리 Aseprite 태그"로 구분한다** (코드가 `파일/태그`로 자동 연결):
> - `ui_button`: 태그 `normal` `hover` `pressed`
> - `ui_slot`: 태그 `empty` `selected`
> - `ui_bar`: 태그 `frame` `fill` (fill은 64×8 캔버스 안에서 프레임 안쪽 영역만 칠함)
> - `ui_icons`: 아이콘 이름 그대로 태그 (`nut` `hammer` `helper` `speed` `crit` `box` `warehouse` `shelf` `customer` `shop` `settings` `prestige_core` `close` `pin` `ghost`)
> - 9-slice 테두리는 코드가 런타임에 붙인다: panel·panel_dark 6px, button 5px, slot 4px, bar 2px. 이 값에 맞춰 테두리 장식을 그릴 것.

---

## 3. 사운드 공통 규칙

- SFX: **WAV, 44.1kHz, 16-bit, 모노**, 피크 -1 dBFS, 앞뒤 무음 제거, 짧게.
- BGM/앰비언스: **OGG(Vorbis, q≈6), 44.1kHz, 스테레오, 끊김 없는 루프**(루프 지점에서 클릭 노이즈 없음).
- 톤: 포스트 아포칼립스 · 로파이 · 약간의 칩튠. 날카로운 고음 금지(하루 종일 켜두는 데스크톱 게임 → **귀가 피곤하지 않게**).
- 자주 나는 소리(타격 등)는 변형 3종 이상(`_01` `_02` `_03`) — 코드가 랜덤 재생.
- 실존 곡·효과음 모방 금지.

## 4. 사운드 목록

### SFX — `Assets/Audio/SFX/`
| 파일 | 설명 |
|---|---|
| `sfx_hit_01..05.wav` | 망치가 결정체를 때리는 둔탁한 금속+유리 소리 (가장 자주 들림 → 가장 부드럽게) |
| `sfx_hit_crit.wav` | 치명타: 더 무겁고 반짝임 추가 |
| `sfx_helper_hit_01..03.wav` | 조수 타격: 작고 가벼운 렌치 소리 |
| `sfx_nexus_crack.wav` | 파손 단계 변경: 갈라지는 소리 |
| `sfx_nexus_destroy.wav` | 넥서스 파괴: 크게 부서짐 + 저음 붐 (2~3초) |
| `sfx_box_drop.wav` | 박스가 땅에 떨어짐 |
| `sfx_box_open_common.wav` | 골판지 뜯기 |
| `sfx_box_open_rare.wav` | 금속 걸쇠 + 짧은 반짝임 |
| `sfx_box_open_alien.wav` | 외계 포드 열림: 신스 시머 |
| `sfx_item_place.wav` | 진열대에 물건 놓기 |
| `sfx_price_tick.wav` | 가격 조절 틱(아주 작게) |
| `sfx_customer_enter.wav` | 손님 도착(깡통 풍경/종 딸랑) |
| `sfx_purchase_01..03.wav` | 판매 성공: 너트 짤랑 |
| `sfx_purchase_big.wav` | 고가 판매(수집가 등): 더 화려하게 |
| `sfx_customer_reject.wav` | 손님 거절: 비언어적 투덜거림/낮은 부저 |
| `sfx_upgrade.wav` | 업그레이드 구매 |
| `sfx_ui_click.wav` `sfx_ui_hover.wav` `sfx_ui_open.wav` `sfx_ui_close.wav` | UI |
| `sfx_prestige.wav` | 환생: 상승하는 화음 + 휘익 |
| `sfx_offline_reward.wav` | 오프라인 정산 팝업 |
| `sfx_error.wav` | 너트 부족 등 |

### BGM — `Assets/Audio/BGM/`
| 파일 | 길이 | 설명 |
|---|---|---|
| `bgm_main.ogg` | 90~120초 루프 | 메인: 느린 로파이 아포칼립스, 약 80~90 BPM, 기타/패드/부드러운 칩 리드 |
| `bgm_shop.ogg` | 60~90초 루프 | 상점 페이지: 조금 더 따뜻하고 경쾌 |
| `bgm_core.ogg` | 60~90초 루프 | 코어 지층(HP 10% 이하): 긴장감, 외계 신스 |
| `stinger_prestige.ogg` | 6~10초 | 환생 연출 |

### 앰비언스 — `Assets/Audio/Ambient/`
| 파일 | 길이 | 설명 |
|---|---|---|
| `amb_wasteland.ogg` | 30~60초 루프 | 바람, 먼 금속 삐걱임 |
| `amb_nexus_hum.ogg` | 10~20초 루프 | 넥서스 저음 웅웅거림 (코드가 거리/HP에 따라 볼륨 조절) |

믹서 그룹(메인 세션이 만듦): `Master` > `BGM`, `SFX`, `Ambient`.
