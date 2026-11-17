"""Build AudioSource/preview.html from the files in Assets/Audio + report/metrics.json.

Open the page directly from disk (file://). It uses plain <audio> elements so no server is needed.
"""
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
AUD = ROOT / "Assets" / "Audio"

# recommended mixer group levels (dB) - mirrored in Docs/SOUND_LOG.md
GROUPS = {"BGM": -8, "SFX": 0, "Ambient": -6}

LONG_DESC = {
    "bgm_main": ("메인", "84 BPM · D단조 · 36마디. 카플러스-스트롱 기타 아르페지오 + 디튠 패드 + NES풍 삼각파 베이스 + 붐뱁 드럼. "
                 "B1·B2 섹션 25% 펄스 칩 리드 멜로디, 브리지는 오카리나풍 휘슬, 아웃트로는 말렛 응답. 테이프 와우·새츄레이션·로우패스로 로파이 마감."),
    "bgm_shop": ("상점", "98 BPM · F장조 · 셔플 · 32마디. FM 일렉피아노 컴핑 + 워킹 베이스 + 브러시·셰이커. "
                 "I-vi-ii-V 턴어라운드, 칩 리드(50% 펄스) 멜로디 → 브리지는 칼림바풍 말렛 → 마지막은 칩+말렛 옥타브."),
    "bgm_core": ("코어 지층", "90 BPM · C# 프리지안 · 28마디. 8분 펄스 베이스 + 심장박동 + 가이거 틱 + 링모듈레이션 외계 패드. "
                 "FM 벨(비정수 배음 3.5) 모티프, P3에서 테레민풍 글라이드 리드와 톰 드라이브, P4는 숨 고르기."),
    "stinger_prestige": ("환생 스팅어", "상승 칩 셋잇단 + 가속 톰 + 라이저 → B♭maj9 → C6/9 → D(add9) 해결(♭VI-♭VII-I). "
                         "메인 테마의 D단조가 D장조로 밝아지는 연출. 루프 아님."),
    "amb_wasteland": ("황무지", "60초 루프. 원형(circular) 노이즈로 만든 3밴드 바람 + 돌풍 따라 움직이는 휘파람 공명 + "
                      "먼 금속 삐걱임·쾅·간판 덜컹·자갈 굴러감(긴 잔향). 마지막 삐걱임 꼬리가 루프 시작으로 이어짐."),
    "amb_nexus_hum": ("넥서스 웅웅", "16초 루프. 55 Hz 드론+배음(맥놀이 8초 주기) + 결정질 비정수 배음 시머 + FM 그르렁 + 전기 지직. "
                      "모든 주파수가 1/16 Hz 격자 → 수학적으로 완전 루프. 코드에서 거리/HP로 볼륨 조절."),
}
LOOPS = {"bgm_main", "bgm_shop", "bgm_core", "amb_wasteland", "amb_nexus_hum"}


def main():
    metrics = json.loads((HERE / "report" / "metrics.json").read_text(encoding="utf-8"))
    sfx_meta = json.loads((HERE / "build" / "sfx_meta.json").read_text(encoding="utf-8"))
    items = []
    for f in sorted(AUD.rglob("*")):
        if f.suffix.lower() not in (".wav", ".ogg"):
            continue
        name = f.stem
        cat = f.parent.name
        m = metrics.get(name, {})
        if cat == "SFX":
            title, desc = "", sfx_meta.get(name, {}).get("desc", "")
        else:
            title, desc = LONG_DESC.get(name, ("", ""))
        items.append({
            "name": name, "cat": cat, "src": "../" + str(f.relative_to(ROOT)).replace("\\", "/"),
            "title": title, "desc": desc, "loop": name in LOOPS,
            "dur": m.get("dur_s"), "tp": m.get("true_peak_dbtp"), "lufs_i": m.get("lufs_i"),
            "lufs_m": m.get("lufs_m_max"), "ch": m.get("channels"),
        })
    reports = sorted(p.name for p in (HERE / "report").glob("*.png"))
    html = TEMPLATE.replace("__DATA__", json.dumps(items, ensure_ascii=False)) \
        .replace("__GROUPS__", json.dumps(GROUPS)) \
        .replace("__REPORTS__", json.dumps(reports))
    (HERE / "preview.html").write_text(html, encoding="utf-8")
    print("preview.html:", len(items), "sounds")


TEMPLATE = r"""<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>넥서스 부수기 사운드</title>
<style>
:root{
  --bg:#16130f; --panel:#221d18; --panel2:#2c251f; --line:#3d342b; --text:#e8e3d3; --muted:#a3a39b;
  --rust:#c2482c; --amber:#f2b84b; --neon:#2bd9c9; --violet:#8a3ff2;
}
@media (prefers-color-scheme: light){
  :root:not([data-theme="dark"]){
    --bg:#efe9da; --panel:#fbf7ec; --panel2:#f3ecdb; --line:#d8ccb2; --text:#2e2620; --muted:#6b5f50;
    --rust:#8e2f22; --amber:#9a6a12; --neon:#127a72; --violet:#5b2aa8;
  }
}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);font:14px/1.5 "Segoe UI","Malgun Gothic",system-ui,sans-serif}
header{padding:20px 16px 8px;max-width:1100px;margin:auto}
h1{margin:0;font-size:22px;letter-spacing:.5px}
h1 small{color:var(--muted);font-weight:400;font-size:13px;margin-left:8px}
.lead{color:var(--muted);margin:6px 0 0}
main{max-width:1100px;margin:auto;padding:0 16px 40px}
.bar{position:sticky;top:0;z-index:5;background:var(--bg);border-bottom:1px solid var(--line);padding:10px 0;display:flex;flex-wrap:wrap;gap:10px 18px;align-items:center}
.bar label{display:flex;gap:6px;align-items:center;color:var(--muted);font-size:13px}
.bar input[type=range]{width:110px;accent-color:var(--amber)}
.bar output{min-width:44px;color:var(--text);font-variant-numeric:tabular-nums}
button{font:inherit;border:1px solid var(--line);background:var(--panel2);color:var(--text);border-radius:6px;padding:5px 10px;cursor:pointer}
button:hover{border-color:var(--amber)}
button.on{background:var(--amber);color:#1e1a17;border-color:var(--amber)}
button.stop{border-color:var(--rust);color:var(--rust)}
h2{font-size:16px;margin:26px 0 8px;display:flex;gap:10px;align-items:baseline}
h2 span{color:var(--muted);font-size:12px;font-weight:400}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(320px,1fr));gap:10px}
.card{background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:10px 12px;display:flex;flex-direction:column;gap:6px}
.card.playing{border-color:var(--neon)}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.name{font-family:Consolas,monospace;font-size:13px;font-weight:600;word-break:break-all}
.tag{font-size:11px;padding:1px 6px;border-radius:10px;border:1px solid var(--line);color:var(--muted)}
.tag.loop{color:var(--neon);border-color:var(--neon)}
.desc{color:var(--muted);font-size:12.5px}
.meta{font-size:11.5px;color:var(--muted);font-variant-numeric:tabular-nums}
.tests{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:10px}
.test{background:var(--panel2);border:1px dashed var(--line);border-radius:8px;padding:10px 12px}
.test p{margin:4px 0 8px;color:var(--muted);font-size:12.5px}
progress{width:100%;height:6px;accent-color:var(--neon)}
.reports{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:10px}
.reports a{display:block;background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:6px;color:var(--muted);font-size:12px;text-decoration:none}
.reports img{width:100%;display:block;border-radius:4px;margin-bottom:4px;background:#000}
</style>
</head>
<body>
<header>
  <h1>넥서스 부수기 — 사운드 미리듣기 <small>모두 코드로 합성 · 44.1kHz 16-bit</small></h1>
  <p class="lead">재생 버튼으로 하나씩 들어보세요. 아래 볼륨은 <b>권장 믹서 그룹 값</b>(BGM −8 dB, 앰비언스 −6 dB, SFX 0 dB)으로 시작합니다. 맨 아래 측정 리포트(스펙트로그램)도 있습니다.</p>
</header>
<main>
  <div class="bar">
    <label>BGM <input type="range" min="-30" max="6" step="1" data-g="BGM"><output></output></label>
    <label>SFX <input type="range" min="-30" max="6" step="1" data-g="SFX"><output></output></label>
    <label>앰비언스 <input type="range" min="-30" max="6" step="1" data-g="Ambient"><output></output></label>
    <button class="stop" id="stopAll">■ 전부 정지</button>
  </div>

  <h2>시뮬레이션 테스트 <span>실제 게임처럼 겹쳐 들어보기</span></h2>
  <div class="tests">
    <div class="test"><b>망치 연타</b><p>sfx_hit_01~05를 무작위로 약 3.3회/초(±음높이 3%) — 오래 들어도 피곤하지 않은지.</p>
      <button data-test="hits">▶ 시작</button></div>
    <div class="test"><b>조수 + 망치</b><p>망치 2회/초 + 조수 렌치 1.5회/초 + 5% 확률 치명타.</p>
      <button data-test="helpers">▶ 시작</button></div>
    <div class="test"><b>인게임 믹스</b><p>bgm_main + amb_wasteland + 망치 연타 + 가끔 박스·손님·판매. 권장 그룹 볼륨 적용.</p>
      <button data-test="mix">▶ 시작</button></div>
    <div class="test"><b>코어 지층 믹스</b><p>bgm_core + amb_nexus_hum + 망치 연타.</p>
      <button data-test="core">▶ 시작</button></div>
  </div>

  <div id="lists"></div>

  <h2>측정 리포트 <span>AudioSource/report/*.png · 가로선 = 5 kHz / 10 kHz</span></h2>
  <div class="reports" id="reports"></div>
</main>
<script>
const DATA = __DATA__;
const GROUPS = __GROUPS__;
const REPORTS = __REPORTS__;
const GROUP_OF = {SFX:"SFX", BGM:"BGM", Ambient:"Ambient"};
const gainDb = Object.assign({}, GROUPS);
const live = new Set();
const lin = db => Math.pow(10, db/20);
const vol = (cat, extraDb=0) => Math.min(1, lin(gainDb[GROUP_OF[cat]] + extraDb));
const byName = Object.fromEntries(DATA.map(d => [d.name, d]));

function fmt(v, d=1){ return (v===null||v===undefined) ? "–" : Number(v).toFixed(d); }

function playOnce(name, extraDb=0, rate=1){
  const d = byName[name]; if(!d) return null;
  const a = new Audio(d.src); a.volume = vol(d.cat, extraDb); a.playbackRate = rate;
  a.preservesPitch = false; a.mozPreservesPitch = false; a.webkitPreservesPitch = false;
  a.dataset.cat = d.cat; live.add(a); a.onended = () => live.delete(a); a.play(); return a;
}
function stopAll(){
  live.forEach(a => { a.pause(); a.currentTime = 0; }); live.clear();
  Object.values(tests).forEach(t => t.stop && t.stop());
  document.querySelectorAll(".card.playing").forEach(c => c.classList.remove("playing"));
  document.querySelectorAll("button.on").forEach(b => { b.classList.remove("on"); if(b.dataset.label) b.textContent = b.dataset.label; });
}
document.getElementById("stopAll").onclick = stopAll;

document.querySelectorAll(".bar input[type=range]").forEach(r => {
  const g = r.dataset.g; r.value = gainDb[g]; const o = r.nextElementSibling; o.textContent = r.value + " dB";
  r.oninput = () => { gainDb[g] = +r.value; o.textContent = r.value + " dB";
    live.forEach(a => { if (GROUP_OF[a.dataset.cat] === g) a.volume = vol(a.dataset.cat, +(a.dataset.extra||0)); }); };
});

// ---------- cards
const sections = [["SFX","효과음 (모노)"],["BGM","배경음악"],["Ambient","앰비언스"]];
const lists = document.getElementById("lists");
for (const [cat, label] of sections){
  const rows = DATA.filter(d => d.cat === cat); if(!rows.length) continue;
  const h = document.createElement("h2"); h.innerHTML = `${label} <span>${rows.length}개 · Assets/Audio/${cat}/</span>`; lists.appendChild(h);
  const grid = document.createElement("div"); grid.className = "grid"; lists.appendChild(grid);
  for (const d of rows){
    const c = document.createElement("div"); c.className = "card";
    const loud = d.cat === "SFX" ? `LUFS-M ${fmt(d.lufs_m)}` : `LUFS-I ${fmt(d.lufs_i)}`;
    c.innerHTML = `<div class="row"><span class="name">${d.name}</span>${d.title?`<span class="tag">${d.title}</span>`:""}${d.loop?'<span class="tag loop">루프</span>':""}</div>
      <div class="desc">${d.desc||""}</div>
      <div class="meta">${fmt(d.dur,2)} s · ${d.ch===2?"스테레오":"모노"} · ${loud} · TP ${fmt(d.tp)} dBTP</div>`;
    const ctr = document.createElement("div"); ctr.className = "row";
    const audio = new Audio(d.src); audio.preload = "none"; audio.dataset.cat = d.cat;
    const prog = document.createElement("progress"); prog.max = 1; prog.value = 0;
    const b = document.createElement("button"); b.textContent = "▶ 재생"; b.dataset.label = "▶ 재생";
    b.onclick = () => {
      if (!audio.paused){ audio.pause(); audio.currentTime = 0; b.classList.remove("on"); b.textContent = b.dataset.label; c.classList.remove("playing"); live.delete(audio); return; }
      audio.loop = d.loop; audio.volume = vol(d.cat); live.add(audio); audio.currentTime = 0; audio.play();
      b.classList.add("on"); b.textContent = "■ 정지"; c.classList.add("playing");
    };
    audio.onended = () => { b.classList.remove("on"); b.textContent = b.dataset.label; c.classList.remove("playing"); live.delete(audio); };
    audio.ontimeupdate = () => { if (audio.duration) prog.value = audio.currentTime / audio.duration; };
    ctr.appendChild(b);
    if (d.loop){
      const s = document.createElement("button"); s.textContent = "↻ 이음새 듣기";
      s.title = "끝 4초 전부터 재생 → 루프 지점을 넘어 처음으로 이어지는지 확인";
      s.onclick = () => { audio.loop = true; audio.volume = vol(d.cat); live.add(audio);
        const go = () => { audio.currentTime = Math.max(0, audio.duration - 4); audio.play(); };
        if (isNaN(audio.duration)) { audio.preload = "auto"; audio.addEventListener("loadedmetadata", go, {once:true}); audio.load(); } else go();
        b.classList.add("on"); b.textContent = "■ 정지"; c.classList.add("playing"); };
      ctr.appendChild(s);
    }
    c.appendChild(ctr); c.appendChild(prog); grid.appendChild(c);
  }
}

// ---------- simulation tests
const pick = arr => arr[Math.floor(Math.random()*arr.length)];
const HITS = ["sfx_hit_01","sfx_hit_02","sfx_hit_03","sfx_hit_04","sfx_hit_05"];
const HELP = ["sfx_helper_hit_01","sfx_helper_hit_02","sfx_helper_hit_03"];
function loopBed(names){
  const els = names.map(([n, extra]) => { const a = playOnce(n, extra||0); if (a){ a.loop = true; a.dataset.extra = extra||0; } return a; });
  return () => els.forEach(a => { if(a){ a.pause(); live.delete(a); } });
}
function every(msLo, msHi, fn){
  let t = null, alive = true;
  const tick = () => { if(!alive) return; fn(); t = setTimeout(tick, msLo + Math.random()*(msHi-msLo)); };
  tick(); return () => { alive = false; clearTimeout(t); };
}
const rate = () => 0.97 + Math.random()*0.06;
const tests = {
  hits: {start(){ this.s = [every(250, 360, () => playOnce(pick(HITS), -Math.random()*1.5, rate()))]; }},
  helpers: {start(){ this.s = [
      every(430, 560, () => Math.random() < 0.05 ? playOnce("sfx_hit_crit") : playOnce(pick(HITS), -Math.random()*1.5, rate())),
      every(560, 760, () => playOnce(pick(HELP), -Math.random()*2, rate()))]; }},
  mix: {start(){ this.s = [loopBed([["bgm_main"],["amb_wasteland"]]),
      every(300, 420, () => playOnce(pick(HITS), -Math.random()*1.5, rate())),
      every(5000, 9000, () => playOnce(pick(["sfx_box_drop","sfx_customer_enter","sfx_purchase_01","sfx_purchase_02","sfx_purchase_03","sfx_item_place"])))]; }},
  core: {start(){ this.s = [loopBed([["bgm_core"],["amb_nexus_hum"]]),
      every(300, 420, () => Math.random() < 0.05 ? playOnce("sfx_hit_crit") : playOnce(pick(HITS), -Math.random()*1.5, rate()))]; }},
};
Object.values(tests).forEach(t => t.stop = function(){ (this.s||[]).forEach(f => f()); this.s = []; });
document.querySelectorAll("button[data-test]").forEach(b => {
  b.dataset.label = b.textContent;
  b.onclick = () => { const t = tests[b.dataset.test];
    if (b.classList.contains("on")){ t.stop(); b.classList.remove("on"); b.textContent = b.dataset.label; return; }
    t.start(); b.classList.add("on"); b.textContent = "■ 정지"; };
});

// ---------- reports
const rep = document.getElementById("reports");
for (const r of REPORTS){
  const a = document.createElement("a"); a.href = "report/" + r; a.target = "_blank";
  a.innerHTML = `<img loading="lazy" src="report/${r}" alt="${r}">${r}`; rep.appendChild(a);
}
</script>
</body>
</html>
"""

if __name__ == "__main__":
    main()
