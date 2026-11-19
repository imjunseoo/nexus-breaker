using System.Collections.Generic;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// 월드 오브젝트 생성과 모델 이벤트 → 연출 연결(박스, 손님, 이펙트, 조수 수 변화, 배경).
    /// </summary>
    public class WorldDirector : MonoBehaviour
    {
        GameModel _model;
        public NexusView Nexus { get; private set; }
        public WeakPointView WeakPoint { get; private set; }
        public RepairmanView Repairman { get; private set; }
        readonly List<HelperView> _helpers = new List<HelperView>();
        readonly Dictionary<int, CustomerView> _customers = new Dictionary<int, CustomerView>();
        readonly bool[] _queueUsed = new bool[GameModel.MaxConcurrentVisits];
        Transform _fxRoot, _boxRoot, _customerRoot;
        GameObject _sky, _far, _mid;
        Sprite[] _debrisSprites;

        /// <summary>판매 시 UI 떠오르는 숫자 등에 쓰는 콜백: (월드 위치, 텍스트, 색).</summary>
        public System.Action<Vector3, string, Color32> FloatText;

        public static WorldDirector Create(GameModel model)
        {
            var go = new GameObject("World");
            var d = go.AddComponent<WorldDirector>();
            d.Init(model);
            return d;
        }

        void Init(GameModel model)
        {
            _model = model;
            var assets = GameAssets.Instance;

            BuildEnvironment(assets);

            _fxRoot = new GameObject("FX").transform; _fxRoot.SetParent(transform, false);
            _boxRoot = new GameObject("Boxes").transform; _boxRoot.SetParent(transform, false);
            _customerRoot = new GameObject("Customers").transform; _customerRoot.SetParent(transform, false);

            Nexus = NexusView.Create(transform, model);
            WeakPoint = WeakPointView.Create(Nexus.transform, model);
            Repairman = RepairmanView.Create(transform, model);
            SyncHelpers();

            var debris = new List<Sprite>();
            foreach (var tag in new[] { "a", "b", "c", "d" })
            {
                var set = assets.Anim("fx_debris");
                var clip = set?.Get(tag);
                if (clip != null && clip.frames.Length > 0) debris.Add(clip.frames[0]);
            }
            if (debris.Count == 0) debris.Add(Placeholder.Debris());
            _debrisSprites = debris.ToArray();

            model.Hit += OnHit;
            model.StageChanged += OnStage;
            model.NexusDestroyed += OnNexusDestroyed;
            model.BoxDropped += OnBoxDropped;
            model.CustomerArrived += OnCustomerArrived;
            model.CustomerResolved += OnCustomerResolved;
            model.Upgraded += OnUpgraded;
            model.Prestiged += OnPrestiged;
            model.WeakPointBroken += OnWeakPointBroken;
        }

        void OnDestroy()
        {
            if (_model == null) return;
            _model.Hit -= OnHit;
            _model.StageChanged -= OnStage;
            _model.NexusDestroyed -= OnNexusDestroyed;
            _model.BoxDropped -= OnBoxDropped;
            _model.CustomerArrived -= OnCustomerArrived;
            _model.CustomerResolved -= OnCustomerResolved;
            _model.Upgraded -= OnUpgraded;
            _model.Prestiged -= OnPrestiged;
            _model.WeakPointBroken -= OnWeakPointBroken;
        }

        // ───────────────────────── 배경 ─────────────────────────

        void BuildEnvironment(GameAssets a)
        {
            _sky = MakeLayer("bg_sky", a.Sprite("bg_sky"), new Vector3(0, 0, 0), WorldLayout.Order.Sky, center: true);
            _far = MakeLayer("bg_far", a.Sprite("bg_far"), new Vector3(0, 0, 0), WorldLayout.Order.Far, center: true);
            _mid = MakeLayer("bg_mid", a.Sprite("bg_mid"), new Vector3(0, 0, 0), WorldLayout.Order.Mid, center: true);
            MakeLayer("ground", a.Sprite("ground") ?? Placeholder.Ground(), new Vector3(0, -WorldLayout.HalfH + 0.25f, 0), WorldLayout.Order.Ground, center: true);

            // 소품(props.aseprite 레이어별 스프라이트, 바닥 중앙 피벗). x는 띠 화면 왼쪽 기준 픽셀.
            // 넥서스(56~136px)·수리공(140)·노점(230~358)·손님 줄(242~282)을 가리지 않는 곳에만 둔다.
            PlaceProp(a, "car_wreck", 30);
            PlaceProp(a, "oil_drum", 188);
            PlaceProp(a, "tire_stack", 201);
            PlaceProp(a, "lamp_post", 215);
            PlaceProp(a, "traffic_cone", 228);
            PlaceProp(a, "scrap_pile", 372);

            var stall = new GameObject("ShopStall");
            stall.transform.SetParent(transform, false);
            stall.transform.position = WorldLayout.StallPos;
            var sr = stall.AddComponent<SpriteRenderer>();
            sr.sortingOrder = WorldLayout.Order.Stall;
            var stallSet = a.Anim("shop_stall");
            if (stallSet != null) stall.AddComponent<SpriteAnimator>().SetSet(stallSet, "idle");
            else sr.sprite = Placeholder.Stall();
        }

        void PlaceProp(GameAssets a, string prop, float pxFromLeft)
        {
            var sprite = a.Sprite("props/" + prop, prop);
            if (sprite == null) return;
            var go = new GameObject("prop_" + prop);
            go.transform.SetParent(transform, false);
            go.transform.position = WorldLayout.Snap(new Vector3((pxFromLeft - WorldLayout.StripW / 2f) / WorldLayout.PPU, WorldLayout.GroundY, 0f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = WorldLayout.Order.Props;
        }

        GameObject MakeLayer(string name, Sprite sprite, Vector3 pos, int order, bool center)
        {
            if (sprite == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go;
        }

        /// <summary>투명 배경 모드: 하늘·원경·근경을 숨긴다(지면·노점·소품만 남김).</summary>
        public void SetTransparentBackground(bool transparent)
        {
            if (_sky != null) _sky.SetActive(!transparent);
            if (_far != null) _far.SetActive(!transparent);
            if (_mid != null) _mid.SetActive(!transparent);
        }

        // ───────────────────────── 타격 연출 ─────────────────────────

        void OnHit(HitInfo h)
        {
            Vector3 p;
            if (h.Source == HitSource.Helper)
                p = WorldLayout.HelperPos(h.HelperIndex) + new Vector3(WorldLayout.HelperFacesLeft(h.HelperIndex) ? -0.45f : 0.45f, 0.6f, 0f);
            else if (h.Source == HitSource.Click)
                p = _lastClickWorld;
            else
                p = WorldLayout.NexusPos + new Vector3(0.75f, Random.Range(0.55f, 1.1f), 0f);

            if (h.Crit)
                FxPiece.PlayOnce(_fxRoot, "fx_crit_burst", p, Placeholder.Get(Placeholder.Shape.Circle, 14, 14, Palette.Magenta, Palette.Lamp1, false), 0.18f);
            else
                FxPiece.PlayOnce(_fxRoot, "fx_hit_spark", p, Placeholder.Spark(), 0.1f);

            int pieces = h.Crit ? 6 : (h.Source == HitSource.Helper ? 1 : 2);
            for (int i = 0; i < pieces; i++) SpawnDebris(p, 1f);

            if (h.Source != HitSource.Helper || h.Crit)
                FloatText?.Invoke(p + Vector3.up * 0.3f, BigNum.Format(h.Damage), h.Crit ? Palette.Magenta : Palette.Ash5);
        }

        Vector3 _lastClickWorld;
        public void NotifyClickWorld(Vector3 world) => _lastClickWorld = world;

        void SpawnDebris(Vector3 p, float strength)
        {
            var vel = new Vector3(Random.Range(-1.5f, 2.2f), Random.Range(1.2f, 3.0f), 0f) * strength;
            FxPiece.Debris(_fxRoot, p, vel, _debrisSprites[Random.Range(0, _debrisSprites.Length)], Random.Range(0.6f, 1.1f));
        }

        void OnWeakPointBroken(HitInfo h, Grade boxGrade)
        {
            var p = WeakPoint != null ? WeakPoint.WorldPos : WorldLayout.NexusCenter;
            for (int i = 0; i < 12; i++) SpawnDebris(p + (Vector3)Random.insideUnitCircle * 0.2f, 1.5f);
            FloatText?.Invoke(p + Vector3.up * 0.7f, "약점!", Palette.Cyan);
        }

        void OnStage(int stage)
        {
            for (int i = 0; i < 14; i++) SpawnDebris(WorldLayout.NexusCenter + (Vector3)Random.insideUnitCircle * 0.6f, 1.3f);
        }

        void OnNexusDestroyed()
        {
            for (int i = 0; i < 40; i++) SpawnDebris(WorldLayout.NexusCenter + (Vector3)Random.insideUnitCircle * 0.9f, 1.8f);
        }

        void OnBoxDropped(Grade g, bool milestone)
        {
            BoxDropView.Spawn(_boxRoot, g, milestone);
            if (g >= Grade.Rare || milestone)
                FxPiece.PlayOnce(_fxRoot, "fx_box_poof", WorldLayout.NexusCenter, Placeholder.Spark(), 0.15f);
        }

        // ───────────────────────── 손님 ─────────────────────────

        void OnCustomerArrived(CustomerVisit v)
        {
            int q = 0;
            for (int i = 0; i < _queueUsed.Length; i++) if (!_queueUsed[i]) { q = i; break; }
            _queueUsed[q] = true;
            var view = CustomerView.Spawn(_customerRoot, v, q);
            int slot = q;
            view.Left += _ => _queueUsed[slot] = false;
            _customers[v.Serial] = view;
        }

        void OnCustomerResolved(CustomerVisit v)
        {
            if (!_customers.TryGetValue(v.Serial, out var view) || view == null) return;
            _customers.Remove(v.Serial);
            view.Resolve();
            if (v.Outcome == VisitOutcome.Bought)
            {
                var p = view.transform.position + new Vector3(0f, 1.4f, 0f);
                FxPiece.PlayOnce(_fxRoot, "fx_nut_sparkle", p, Placeholder.Spark(), 0.25f);
                FloatText?.Invoke(p, "+" + BigNum.Price(v.Price), Palette.Lamp0);
            }
        }

        // ───────────────────────── 조수/환생 ─────────────────────────

        void OnUpgraded(UpgradeId id, int level)
        {
            if (id == UpgradeId.Helpers) SyncHelpers();
            FxPiece.PlayOnce(_fxRoot, "fx_level_up", WorldLayout.RepairmanPos + Vector3.up * 0.6f, Placeholder.Spark(), 0.2f);
        }

        void SyncHelpers()
        {
            int want = _model.HelperCount;
            while (_helpers.Count < want) _helpers.Add(HelperView.Create(transform, _model, _helpers.Count));
            while (_helpers.Count > want)
            {
                var last = _helpers[_helpers.Count - 1];
                _helpers.RemoveAt(_helpers.Count - 1);
                if (last != null) Destroy(last.gameObject);
            }
        }

        void OnPrestiged()
        {
            SyncHelpers();
            foreach (var kv in _customers) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _customers.Clear();
            for (int i = 0; i < _queueUsed.Length; i++) _queueUsed[i] = false;
        }
    }
}
