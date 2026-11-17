using UnityEngine;

namespace NexusBreaker
{
    /// <summary>일회성 이펙트: 애니메이션 1회 재생 후 소멸, 또는 간단한 물리 파편.</summary>
    public class FxPiece : MonoBehaviour
    {
        SpriteRenderer _sr;
        Vector3 _pos, _vel;
        float _life, _maxLife;
        bool _physics;

        public static FxPiece PlayOnce(Transform parent, string animName, Vector3 pos, Sprite fallback, float fallbackLife = 0.2f, int order = WorldLayout.Order.Fx)
        {
            var fx = Make(parent, animName, pos, order);
            var set = GameAssets.Instance.Anim(animName);
            if (set != null && set.Get("play") != null)
            {
                var anim = fx.gameObject.AddComponent<SpriteAnimator>();
                anim.SetSet(set);
                anim.Play("play", loop: false, onDone: () => { if (fx != null) Destroy(fx.gameObject); });
                fx._maxLife = fx._life = set.Get("play").Length + 0.5f; // 안전장치
            }
            else
            {
                fx._sr.sprite = fallback;
                fx._maxLife = fx._life = fallbackLife;
            }
            return fx;
        }

        public static FxPiece Debris(Transform parent, Vector3 pos, Vector3 vel, Sprite sprite, float life)
        {
            var fx = Make(parent, "debris", pos, WorldLayout.Order.Debris);
            fx._sr.sprite = sprite;
            fx._physics = true;
            fx._vel = vel;
            fx._maxLife = fx._life = life;
            return fx;
        }

        static FxPiece Make(Transform parent, string name, Vector3 pos, int order)
        {
            var go = new GameObject("fx_" + name);
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<FxPiece>();
            fx._sr = go.AddComponent<SpriteRenderer>();
            fx._sr.sortingOrder = order;
            fx._pos = pos;
            go.transform.position = WorldLayout.Snap(pos);
            return fx;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_physics)
            {
                _vel.y -= 9.8f * Time.deltaTime;
                _pos += _vel * Time.deltaTime;
                if (_pos.y < WorldLayout.GroundY) { _pos.y = WorldLayout.GroundY; _vel.y = -_vel.y * 0.3f; _vel.x *= 0.6f; }
                transform.position = WorldLayout.Snap(_pos);
                if (_life < 0.3f) { var c = _sr.color; c.a = Mathf.Clamp01(_life / 0.3f); _sr.color = c; }
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
