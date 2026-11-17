using UnityEngine;

namespace NexusBreaker
{
    public class RepairmanView : MonoBehaviour
    {
        GameModel _model;
        SpriteAnimator _anim;
        SpriteRenderer _sr;
        bool _hasArt;
        float _bob;

        public static RepairmanView Create(Transform parent, GameModel model)
        {
            var go = new GameObject("Repairman");
            go.transform.SetParent(parent, false);
            go.transform.position = WorldLayout.RepairmanPos;
            var v = go.AddComponent<RepairmanView>();
            v.Init(model);
            return v;
        }

        void Init(GameModel model)
        {
            _model = model;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = WorldLayout.Order.Repairman;
            _sr.flipX = false; // 스프라이트 기본 방향: 왼쪽(넥서스 쪽)을 본다고 가정. 아트 확정 후 조정.
            _anim = gameObject.AddComponent<SpriteAnimator>();

            var set = GameAssets.Instance.Anim("repairman");
            _hasArt = set != null;
            if (_hasArt) _anim.SetSet(set, "idle");
            else _sr.sprite = Placeholder.Repairman();

            model.SwingStarted += OnSwing;
            model.BoxDropped += OnBox;
            model.Prestiged += OnCheer;
        }

        void OnDestroy()
        {
            if (_model == null) return;
            _model.SwingStarted -= OnSwing;
            _model.BoxDropped -= OnBox;
            _model.Prestiged -= OnCheer;
        }

        void OnSwing(HitSource src, int helper)
        {
            if (src != HitSource.Swing) return;
            if (!_hasArt) { _bob = 0.12f; return; }

            // 스윙 간격이 애니메이션보다 짧아지면 재생 속도를 올려 따라간다
            var clip = _anim.Set.Get("swing");
            float len = clip != null ? clip.Length : 0.42f;
            _anim.Speed = Mathf.Max(1f, len / (float)_model.SwingInterval * 0.95f);
            _anim.Play("swing", loop: false, next: "idle", restart: true);
        }

        void OnBox(Grade g, bool milestone)
        {
            if (!milestone || !_hasArt || !_anim.Has("cheer")) return;
            if (_anim.CurrentTag == "swing" && _anim.IsPlaying) return;
            _anim.Speed = 1f;
            _anim.Play("cheer", loop: false, next: "idle", restart: true);
        }

        void OnCheer()
        {
            if (_hasArt && _anim.Has("cheer")) _anim.Play("cheer", loop: false, next: "idle", restart: true);
        }

        void LateUpdate()
        {
            if (_hasArt) return;
            // 플레이스홀더: 스윙 때 살짝 기울기
            _bob = Mathf.Max(0f, _bob - Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, _bob > 0f ? 18f : 0f);
        }
    }
}
