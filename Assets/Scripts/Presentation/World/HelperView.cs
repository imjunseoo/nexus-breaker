using UnityEngine;

namespace NexusBreaker
{
    public class HelperView : MonoBehaviour
    {
        GameModel _model;
        int _index;
        SpriteAnimator _anim;
        bool _hasArt;
        float _bob;

        public static HelperView Create(Transform parent, GameModel model, int index)
        {
            var go = new GameObject($"Helper_{index}");
            go.transform.SetParent(parent, false);
            go.transform.position = WorldLayout.HelperPos(index);
            var v = go.AddComponent<HelperView>();
            v.Init(model, index);
            return v;
        }

        void Init(GameModel model, int index)
        {
            _model = model;
            _index = index;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = WorldLayout.Order.Helper + index;
            // 기본 스프라이트가 왼쪽을 본다고 가정 → 넥서스 왼편에 선 조수는 뒤집는다
            sr.flipX = !WorldLayout.HelperFacesLeft(index);
            _anim = gameObject.AddComponent<SpriteAnimator>();

            var set = GameAssets.Instance.Anim("helper");
            _hasArt = set != null;
            if (_hasArt) _anim.SetSet(set, "idle");
            else sr.sprite = Placeholder.Helper();

            // 조수마다 다른 박자로 보이도록
            _anim.Speed = 1f + 0.07f * index;
            model.SwingStarted += OnSwing;
        }

        void OnDestroy()
        {
            if (_model != null) _model.SwingStarted -= OnSwing;
        }

        void OnSwing(HitSource src, int helper)
        {
            if (src != HitSource.Helper || helper != _index) return;
            if (_hasArt) _anim.Play("swing", loop: false, next: "idle", restart: true);
            else _bob = 0.1f;
        }

        void LateUpdate()
        {
            if (_hasArt) return;
            _bob = Mathf.Max(0f, _bob - Time.deltaTime);
            float dir = WorldLayout.HelperFacesLeft(_index) ? 1f : -1f;
            transform.rotation = Quaternion.Euler(0, 0, _bob > 0f ? 15f * dir : 0f);
        }
    }
}
