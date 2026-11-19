using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// 약점(v0.2) 연출: 넥서스 결정 몸통 위에 빛나는 균열(fx_weakpoint)을 띄우고, 클릭 판정을 맡는다.
    /// 넥서스의 자식이라 넥서스가 흔들리면 같이 흔들린다.
    /// </summary>
    public class WeakPointView : MonoBehaviour
    {
        /// <summary>
        /// 후보 지점(넥서스 80×96 캔버스, 왼쪽 위 기준 px). s0~s4 모든 단계에서 반경 4px가 결정(네온 색)인 곳만 골랐다.
        /// (ArtSource/previews/nexus 미리보기에서 자동 추출) 결정 윗부분(y&lt;50)은 HUD 글자(HP·시장 이벤트 줄)와 겹쳐서 뺐다.
        /// </summary>
        static readonly Vector2Int[] Spots =
        {
            new Vector2Int(24, 54), new Vector2Int(34, 55), new Vector2Int(44, 59), new Vector2Int(25, 64),
            new Vector2Int(35, 65), new Vector2Int(50, 67), new Vector2Int(41, 73), new Vector2Int(23, 74),
            new Vector2Int(32, 79),
        };

        const float ClickRadiusPx = 9f;

        GameModel _model;
        SpriteRenderer _sr;
        SpriteAnimator _anim;
        bool _hasArt;
        bool _live;       // 클릭 가능한 상태
        float _pulse;

        public static WeakPointView Create(Transform nexus, GameModel model)
        {
            var go = new GameObject("WeakPoint");
            go.transform.SetParent(nexus, false);
            var v = go.AddComponent<WeakPointView>();
            v.Init(model);
            return v;
        }

        void Init(GameModel model)
        {
            _model = model;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = WorldLayout.Order.Nexus + 1;
            _anim = gameObject.AddComponent<SpriteAnimator>();
            var set = GameAssets.Instance.Anim("fx_weakpoint");
            _hasArt = set != null;
            if (_hasArt) _anim.SetSet(set);
            else _sr.sprite = Placeholder.Get(Placeholder.Shape.Circle, 12, 12, Palette.Cyan, Palette.Magenta, false);
            _sr.enabled = false;

            model.WeakPointSpawned += OnSpawned;
            model.WeakPointBroken += OnBroken;
            model.WeakPointExpired += OnExpired;
            model.Prestiged += Hide;
            if (model.WeakPointActive) OnSpawned();
        }

        void OnDestroy()
        {
            if (_model == null) return;
            _model.WeakPointSpawned -= OnSpawned;
            _model.WeakPointBroken -= OnBroken;
            _model.WeakPointExpired -= OnExpired;
            _model.Prestiged -= Hide;
        }

        void OnSpawned()
        {
            var p = Spots[Mathf.Clamp((int)(_model.WeakPointRoll * Spots.Length), 0, Spots.Length - 1)];
            // 캔버스 바닥 중앙 = 넥서스 피벗. 24px(짝수) 스프라이트의 중심이 픽셀 경계에 오도록 정수 px로
            transform.localPosition = new Vector3((p.x - 40) / WorldLayout.PPU, (96 - p.y) / WorldLayout.PPU, 0f);
            _sr.enabled = true;
            _sr.color = Color.white;
            transform.localScale = Vector3.one;
            _live = true;
            _pulse = 0f;
            if (_hasArt)
            {
                if (_anim.Has("spawn")) _anim.Play("spawn", loop: false, next: "loop", restart: true);
                else _anim.Play("loop", loop: true, restart: true);
            }
        }

        void OnBroken(HitInfo h, Grade g)
        {
            _live = false;
            if (_hasArt && _anim.Has("break")) _anim.Play("break", loop: false, onDone: Hide, restart: true);
            else Hide();
        }

        void OnExpired()
        {
            if (!_live) return;
            _live = false;
            if (_hasArt && _anim.Has("fade")) _anim.Play("fade", loop: false, onDone: Hide, restart: true);
            else Hide();
        }

        void Hide()
        {
            _live = false;
            _sr.enabled = false;
        }

        void Update()
        {
            if (!_live || _hasArt) return;
            // 임시 그림: 맥동으로 눈에 띄게
            _pulse += Time.deltaTime * 6f;
            float s = 1f + 0.18f * Mathf.Sin(_pulse);
            transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>월드 좌표가 약점 위인가(클릭 판정).</summary>
        public bool Contains(Vector2 world)
        {
            if (!_live || !_model.WeakPointActive) return false;
            return Vector2.Distance(world, transform.position) <= ClickRadiusPx / WorldLayout.PPU;
        }

        public Vector3 WorldPos => transform.position;
    }
}
