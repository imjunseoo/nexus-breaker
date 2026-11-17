using UnityEngine;

namespace NexusBreaker
{
    public class NexusView : MonoBehaviour
    {
        GameModel _model;
        SpriteRenderer _sr;
        SpriteAnimator _anim;
        BoxCollider2D _col;
        Vector3 _home;
        float _shakeTime;
        float _flashTime;
        bool _hasArt;

        public static NexusView Create(Transform parent, GameModel model)
        {
            var go = new GameObject("Nexus");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<NexusView>();
            v.Init(model);
            return v;
        }

        void Init(GameModel model)
        {
            _model = model;
            _home = WorldLayout.NexusPos;
            transform.position = _home;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = WorldLayout.Order.Nexus;
            _anim = gameObject.AddComponent<SpriteAnimator>();

            var set = GameAssets.Instance.Anim("nexus");
            _hasArt = set != null;
            if (_hasArt) _anim.SetSet(set);
            else _sr.sprite = Placeholder.Nexus();

            _col = gameObject.AddComponent<BoxCollider2D>();
            _col.size = new Vector2(2.2f, 2.8f);
            _col.offset = new Vector2(0f, 1.4f);

            model.Hit += OnHit;
            model.StageChanged += OnStage;
            model.NexusDestroyed += OnDestroyed;
            model.Prestiged += OnPrestiged;
            ShowStage(model.DamageStageNow);
            gameObject.SetActive(!model.Data.nexusDestroyed);
        }

        void OnDestroy()
        {
            if (_model == null) return;
            _model.Hit -= OnHit;
            _model.StageChanged -= OnStage;
            _model.NexusDestroyed -= OnDestroyed;
            _model.Prestiged -= OnPrestiged;
        }

        Color _baseColor = Color.white;

        void ShowStage(int stage)
        {
            if (_hasArt)
            {
                _anim.Play($"s{stage}_idle", loop: true, restart: true);
                _baseColor = Color.white;
            }
            else _baseColor = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.8f), stage / 4f);
            _sr.color = _baseColor;
        }

        void OnHit(HitInfo h)
        {
            _shakeTime = h.Crit ? 0.16f : 0.07f;
            _flashTime = 0.05f;
        }

        void OnStage(int stage)
        {
            ShowStage(stage);
            _shakeTime = 0.35f;
        }

        void OnDestroyed()
        {
            if (_hasArt && _anim.Has("destroy"))
                _anim.Play("destroy", loop: false, onDone: () => gameObject.SetActive(false));
            else gameObject.SetActive(false);
        }

        void OnPrestiged()
        {
            gameObject.SetActive(true);
            ShowStage(_model.DamageStageNow);
        }

        void LateUpdate()
        {
            Vector3 p = _home;
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                // 1~2px 단위 흔들림
                p.x += Random.Range(-2, 3) / WorldLayout.PPU;
                p.y += Random.Range(0, 2) / WorldLayout.PPU;
            }
            transform.position = WorldLayout.Snap(p);

            if (_flashTime > 0f)
            {
                _flashTime -= Time.deltaTime;
                _sr.color = _flashTime > 0f ? new Color(1.6f, 1.6f, 1.6f, 1f) : _baseColor;
            }
        }

        public bool Contains(Vector2 world) => _col != null && gameObject.activeInHierarchy && _col.OverlapPoint(world);
    }
}
