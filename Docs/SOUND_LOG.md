# 사운드 작업 로그 (사운드 세션)

최종 갱신: 2026-11-20 · 담당: 사운드 세션 · 스펙: `Docs/ASSET_SPEC.md` 3~4장

## 상태 요약
- **스펙의 41개 파일을 모두 만들었다**: SFX 35개(WAV 모노, v0.2 추가 4개 포함), BGM 4개·앰비언스 2개(OGG Vorbis 스테레오). 전부 `AudioSource/`의 Python 스크립트(numpy/scipy)로 직접 합성했고, 샘플·실존 곡은 쓰지 않았다.
- 측정 검사를 **모두 통과**했다(`AudioSource/report/analyze_log.txt`). 트루 피크 ≤ −1 dBTP, 클리핑 0, DC < 0.001, SFX 라우드니스는 목표 ±1 LU 이내, 루프 이음새에 클릭이 없다.
- 미리듣기: `AudioSource/preview.html`(브라우저로 열기)에서 전체 재생, 루프 이음새 듣기, 망치 연타 테스트, 인게임 믹스 시뮬레이션을 할 수 있다.

## BGM·앰비언스 형식: OGG Vorbis (스펙대로)
- 2026-11-18, 사용자 허락을 받아 `soundfile` 0.14.0(PyPI, libsndfile 1.2.2 포함)을 설치하고 **`.ogg`(Vorbis, 44.1 kHz, 스테레오)**로 전환했다. 한때 있던 `.wav` 버전은 지웠다.
  - **메인 세션 주의**: 이 클립들을 WAV일 때 이미 연결했다면 GUID가 바뀌었으니 `.ogg`로 다시 연결해야 한다. `Assets/Audio/Ambient/*.wav.meta`는 원본이 사라진 meta라서 Unity가 다음 새로고침 때 스스로 지운다.
- 품질: 기본 `compression_level 0.4`(≈ oggenc q6). `amb_nexus_hum`만 0.2(≈ q8)다.
- 디코드한 뒤 다시 검사했다: 길이가 샘플 단위로 같고, TP ≤ −3.7 dBTP, 루프 이음새 점프 비율 0.01~0.57이다.
- `amb_nexus_hum`은 원본 그대로 인코딩하면 파일 경계에서 Vorbis 오차 때문에 이음새가 나빠졌다(점프 비율 1.67). 그래서 **루프 시작점을 14.496초 지점으로 옮긴 뒤(양 채널이 0에 가까운 곳) 인코딩**했다. 드론이라 듣기에는 차이가 없다.
- 무손실 원본 마스터(WAV, 약 58 MB)는 `AudioSource/build/masters/`에 있다. **메인 세션에 요청: `.gitignore`에 `AudioSource/build/`를 추가해 주세요.** 스크립트로 언제든 똑같이 다시 만들 수 있다.
- 자산 용량: BGM 4.8 MB, 앰비언스 1.4 MB.

## 권장 Unity 임포트 설정 (메인 세션)
| 대상 | Load Type | Compression | 기타 |
|---|---|---|---|
| SFX (짧은 모노) | Decompress On Load | ADPCM (UI·틱은 PCM도 OK) | Force To Mono 불필요(이미 모노), Preload |
| BGM | Streaming | Vorbis, Quality 60 | Load In Background |
| `amb_wasteland` | Compressed In Memory | Vorbis, Quality 60 | |
| `amb_nexus_hum` | Decompress On Load | **PCM** | 16초 스테레오라 메모리 2.8 MB. 다시 Vorbis로 압축하면 루프 이음새 오차가 새로 생길 수 있다(위 참고) |

- 루프 파일은 AudioSource `loop = true`만 켜면 된다. 파일 자체가 샘플 단위로 정확한 길이이고 이음새가 연속이다.
- Vorbis는 Unity에서 끊김 없이 루프된다. **MP3로 바꾸지 말 것**(앞뒤 패딩 때문에 이음새가 생긴다).

## 라우드니스 기준과 권장 믹서 볼륨
라우드니스는 ITU BS.1770(K-weighting)으로 쟀다. 모노 SFX는 Unity에서 가운데 패닝(양 채널 동일)으로 재생된다고 보고 **듀얼 모노(+3 dB)로 계산**했다.

| 카테고리 | 파일에 구운 레벨 | 권장 믹서 그룹 볼륨 | 실효 레벨 |
|---|---|---|---|
| BGM | −18 LUFS (integrated) | **BGM −8 dB** | ≈ −26 LUFS |
| 앰비언스 | −24 LUFS (integrated) | **Ambient −6 dB** | ≈ −30 LUFS |
| SFX | 소리별 목표(아래 표, 순간 최대 LUFS-M) | **SFX 0 dB** | 망치 −22 LUFS-M → 음악보다 약 4 LU 위 |
| Master | – | **0 dB** | |

- 레벨을 파일에 미리 맞춰 두었으므로 **각 AudioSource 볼륨은 기본 1.0(0 dB)**이다. 예외는 아래 표의 "권장 볼륨"을 보면 된다.
- 하루 종일 켜두는 게임이라 음악은 SFX보다 낮게 잡았다. 사용자 설정 슬라이더의 기본값도 위 값을 권장한다.

## 파일 목록 — 길이 · 루프 · 권장 볼륨
### BGM — `Assets/Audio/BGM/`
| 파일 | 길이 | 루프 | 측정 | 권장 볼륨(클립) | 내용 |
|---|---:|:---:|---|---:|---|
| `bgm_main.ogg` | 102.857 s (4,536,000 샘플) | ○ | −18.0 LUFS, TP −3.9 | 0 dB | 84 BPM · D단조 · 36마디. KS 기타 아르페지오, 패드, 삼각파 베이스, 붐뱁 드럼, 25% 펄스 칩 리드(B1·B2), 오카리나 브리지, 말렛 아웃트로 |
| `bgm_shop.ogg` | 78.367 s (3,456,000 샘플) | ○ | −18.0 LUFS, TP −4.6 | 0 dB | 98 BPM · F장조 · 셔플 · 32마디. FM 일렉피아노, 워킹 베이스, 브러시·셰이커, 칩 리드 + 칼림바풍 말렛 |
| `bgm_core.ogg` | 74.667 s (3,292,800 샘플) | ○ | −18.0 LUFS, TP −3.7 | 0 dB | 90 BPM · C# 프리지안 · 28마디. 펄스 베이스, 심장박동, 가이거 틱, 링모듈레이션 외계 패드, FM 벨, 테레민풍 리드 |
| `stinger_prestige.ogg` | 8.571 s | × | −17.0 LUFS, TP −5.0 | 0 dB | ♭VI–♭VII–I(B♭→C→D장조) 상승 연출. 재생 전 현재 BGM을 0.5초 페이드아웃하고, 끝나면 새 지역 BGM을 페이드인 |

- 음악 전환 권장: main ↔ core 2초 크로스페이드, main ↔ shop 1초 크로스페이드. 세 곡의 레벨이 같아서 전환할 때 볼륨이 튀지 않는다.

### 앰비언스 — `Assets/Audio/Ambient/`
| 파일 | 길이 | 루프 | 측정 | 권장 볼륨(클립) | 내용 |
|---|---:|:---:|---|---:|---|
| `amb_wasteland.ogg` | 60.000 s | ○ | −24.1 LUFS, TP −7.8 | 0 dB | 3밴드 바람(돌풍), 휘파람 공명, 먼 금속 삐걱임·쾅·간판 덜컹·자갈 |
| `amb_nexus_hum.ogg` | 16.000 s | ○ | −23.9 LUFS, TP −15.1 | **코드 제어: 0 ~ −18 dB, 기본 −10 dB** | 55 Hz 드론+배음, 결정질 시머, FM 그르렁, 전기 지직. HP 10% 이하이거나 가까울수록 0 dB 쪽. 인코딩할 때 시작점을 회전했다 |

### SFX — `Assets/Audio/SFX/` (전부 모노)
목표 LUFS-M = 400 ms 창의 순간 최대 라우드니스(듀얼 모노). 측정값은 모두 목표 ±0.1 이내다.

| 파일 | 길이(s) | 목표 LUFS-M | TP(dBTP) | 권장 볼륨 | 재생 팁 |
|---|---:|---:|---:|---:|---|
| `sfx_hit_01..05.wav` | 0.92–0.99 | −22 | −6.9 ~ −8.5 | 0 dB | 5종 랜덤 + pitch 0.97–1.03 + 볼륨 −1.5~0 dB 랜덤. 동시 재생 최대 4개, 50 ms 안에 재트리거하면 무시 |
| `sfx_hit_crit.wav` | 1.46 | −17 | −5.7 | 0 dB | pitch 0.98–1.02 |
| `sfx_helper_hit_01..03.wav` | 0.41–0.45 | −26 | −8.6 ~ −10.8 | 0 dB | 3종 랜덤 + pitch 0.96–1.04. 조수가 여럿이면 동시 재생 최대 3개 |
| `sfx_nexus_crack.wav` | 1.77 | −18 | −8.6 | 0 dB | |
| `sfx_nexus_destroy.wav` | 2.90 | −14 | −4.5 | 0 dB | 재생하는 동안 BGM −6 dB 덕킹 권장 |
| `sfx_box_drop.wav` | 0.46 | −21 | −6.3 | 0 dB | pitch 0.95–1.05 |
| `sfx_box_open_common.wav` | 0.86 | −21 | −9.8 | 0 dB | 고급 등급 박스(나무 크레이트) 전용 소리가 없으니 이 소리를 pitch 0.9로 재생 권장 |
| `sfx_box_open_rare.wav` | 1.78 | −20 | −12.6 | 0 dB | |
| `sfx_box_open_alien.wav` | 2.11 | −19 | −10.0 | 0 dB | ??? 박스는 이 소리를 pitch 0.85로 재생 권장 |
| `sfx_item_place.wav` | 0.20 | −24 | −7.8 | 0 dB | pitch 0.95–1.05 |
| `sfx_price_tick.wav` | 0.07 | −32 | −13.9 | 0 dB | 슬라이더 드래그 중에는 최대 25회/초로 제한 |
| `sfx_customer_enter.wav` | 1.68 | −22 | −12.8 | 0 dB | |
| `sfx_purchase_01..03.wav` | 0.46–0.53 | −21 | −8.0 ~ −10.3 | 0 dB | 3종 랜덤 + pitch 0.98–1.02 |
| `sfx_purchase_big.wav` | 2.07 | −18 | −8.4 | 0 dB | |
| `sfx_customer_reject.wav` | 0.69 | −22 | −14.7 | 0 dB | |
| `sfx_upgrade.wav` | 1.45 | −19 | −8.9 | 0 dB | |
| `sfx_ui_click.wav` | 0.10 | −26 | −4.9 | 0 dB | |
| `sfx_ui_hover.wav` | 0.06 | −33 | −13.9 | 0 dB | 같은 버튼에서는 다시 재생하지 않기. 최소 간격 60 ms |
| `sfx_ui_open.wav` | 0.34 | −25 | −14.6 | 0 dB | |
| `sfx_ui_close.wav` | 0.32 | −25 | −14.3 | 0 dB | |
| `sfx_prestige.wav` | 2.80 | −15 | −7.6 | 0 dB | 환생 버튼을 눌렀을 때. 그 뒤 연출에서 `stinger_prestige` 재생 |
| `sfx_offline_reward.wav` | 2.31 | −19 | −12.0 | 0 dB | |
| `sfx_error.wav` | 0.34 | −22 | −11.5 | 0 dB | |
| `sfx_weakpoint_spawn.wav` (v0.2) | 1.44 | −26 | −19.7 | 0 dB | 약점 생성. 일반 타격보다 4 LU 작다. 여러 개가 동시에 생겨도 한 번만 재생 |
| `sfx_weakpoint_break.wav` (v0.2) | 1.60 | −15.5 | −6.0 | 0 dB | 약점 파괴. 치명타(−17)보다 1.5 LU 크고 꼬리가 길다. 같은 클릭의 일반 타격음·치명타음은 생략 권장 |
| `sfx_contract_done.wav` (v0.2) | 1.35 | −22 | −15.2 | 0 dB | 의뢰 달성. 여러 개가 동시에 달성돼도 한 번만 재생 |
| `sfx_contract_claim.wav` (v0.2) | 0.62 | −21 | −6.1 | 0 dB | 의뢰 보상 받기. '모두 받기'일 때도 한 번만 재생 |

## 소리 설계 메모
- **망치 타격**: 둔탁한 트랜지언트(165→68 Hz 사인 스윕 + 160–1000 Hz 노크 노이즈), 금속 바 모드(1 : 2.756 : 5.404), 유리질 결정 공진(변형마다 다른 비정수 배음 세트, 미세 디튠으로 반짝임), 0.32 s 룸 잔향. 5개 변형은 음높이 ±7%, 결정 배음, 노이즈 시드가 다르다. 스펙트럼 중심은 약 300 Hz로 가장 부드럽게 잡았다.
- **박스 개봉**: 재질마다 노이즈 텍스처가 다르다. 골판지는 스틱슬립 테이프 찢김, 금속 케이스는 걸쇠 모드와 판금 공진, 외계 포드는 질척한 SVF 스윕·거품·FM 시머다.
- **외계 계열**: FM(비정수비 √2, 3.5)과 링모듈레이션.
- **v0.2 약점·의뢰 (2026-11-20 추가)**
  - `sfx_weakpoint_spawn`: 맑은 결정 '팅'(A5 880 Hz, 어택 4 ms로 부드럽게)에 A5 한 옥타브 아래 사인을 살짝 깔고, D6·E6·A6 작은 핑 3개와 1.1→1.7 kHz 상승 시머를 아주 작게 얹었다. 1~2분마다 하루 종일 들리니 −26 LUFS-M으로 작게 잡았고, 날카로운 트랜지언트는 없다(TP −19.7).
  - `sfx_weakpoint_break`: 낮은 쿵(120→42 Hz + 45 Hz 서브, 새츄레이션), 시원한 크랙(600–4500 Hz 노이즈 12 ms), 깨진 코어 공진(F5·C6 결정), 파편 110알(0.9–4 kHz)을 겹쳤다. 꼬리는 D장조 펜타토닉 벨 상승 6음과 흩어지는 반짝임 14개로 약 1.2초까지 이어지고, 옅은 외계 FM 시머(√2)를 깔았다. 치명타보다 5–10 kHz 대역이 10 dB 밝다(−33 vs −43 dB). 그래도 9 kHz 로우패스는 적용했다.
  - `sfx_contract_done`: 부드러운 두 음 차임 E5→A5(4도 상승 = 완료 느낌). 배음은 옥타브·12도만 약하게 넣었고, 한 옥타브 아래 언더톤으로 따뜻하게 했다. 2–5 kHz 대역은 −37 dB로 아주 부드럽다.
  - `sfx_contract_claim`: 고무 도장 '탁'(240→140 Hz 쿵 + 나무 책상 모드 190/410/820 Hz + 종이 슬랩 노이즈 5 ms) 뒤에 26 ms 후 작은 재접촉음, 0.12초 뒤 너트 4개 짤랑.
- **귀 피로 대책**: 모든 SFX에 9 kHz 4차 로우패스를 걸었다. 자주 나는 소리는 2–5 kHz 에너지를 낮췄고(판매 너트 소리의 음역도 낮춤), 음악은 하이셸프 −2 dB와 11 kHz 로우패스로 마감했다.
- **루프 방식**: 루프 길이 뒤로 넘어간 음표 꼬리와 잔향을 시작 부분에 겹쳐 더했다(wrap). 마스터 체인(컴프레서, 테이프, 와우)은 [루프, 루프]에 돌리고 두 번째 사본만 썼다. LFO는 루프 안에서 정수 주기로 맞췄고, 노이즈는 원형(FFT)으로 만들었다. 이음새 검사 결과 경계에서의 샘플 점프가 파일 내부 99.9퍼센타일 점프의 0.03~0.36배에 그친다.
- **튜닝 검증**: 기타(KS)·칩·EP·베이스·말렛 음정이 ±0.5센트 이내다.

## 측정 방법 · 재생성
- 전체 재생성: `cd AudioSource` → `python render_all.py` (약 1~2분). 순서: SFX → BGM·앰비언스 마스터(`build/masters/*.wav`) → `encode_ogg.py`(OGG 변환·검증 후 Assets에 넣음) → 측정 → 미리듣기
  - 개별 실행: `python make_sfx.py [이름필터]`, `python make_bgm.py [main|shop|core|stinger]`, `python make_amb.py [wasteland|hum]`
  - 측정: `python analyze.py` → `report/metrics.json`, 스펙트로그램 `report/spec_*.png`, 이음새 `report/seam_*.png`, 평균 스펙트럼 `report/ltas_*.png`
  - 미리듣기 갱신: `python build_preview.py`
- 필요한 것: Python 3.14 + numpy, scipy, matplotlib, soundfile(모두 설치됨).
- 참고: libsndfile 1.2.2는 긴 Vorbis 파일을 한 번에 쓰면 이 PC에서 조용히 죽는다(종료 코드 127). 그래서 `encode_ogg.py`는 8192프레임 단위로 나눠서 쓴다.

## 남은 것 / 다음
- [ ] 사용자 청취 피드백 반영(미리듣기 페이지로 듣는 중)
- [x] OGG 인코딩으로 전환(2026-11-18)
- [ ] 메인 세션: `.gitignore`에 `AudioSource/build/` 추가, BGM·앰비언스 클립을 `.ogg`로 다시 연결
