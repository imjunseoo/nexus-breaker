using UnityEngine;

namespace NexusBreaker
{
    /// <summary>넥서스에서 튀어나와 땅에 떨어진 뒤 노점 쪽으로 날아가는 박스(연출 전용 — 모델에는 이미 창고에 들어가 있다).</summary>
    public class BoxDropView : MonoBehaviour
    {
        SpriteRenderer _sr;
        SpriteAnimator _anim;
        Vector3 _pos, _vel;
        float _groundY;
        float _restTimer;
        int _phase; // 0 = 튀어나옴, 1 = 바닥 대기, 2 = 노점으로 이동
        float _flyT;
        Vector3 _flyFrom;

        public static BoxDropView Spawn(Transform parent, Grade grade, bool milestone)
        {
            var go = new GameObject($"Box_{grade}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<BoxDropView>();
            v.Init(grade, milestone);
            return v;
        }

        void Init(Grade grade, bool milestone)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = WorldLayout.Order.Box;
            _anim = gameObject.AddComponent<SpriteAnimator>();
            var set = GameAssets.Instance.Anim(GradeInfo.BoxId(grade));
            if (set != null) _anim.SetSet(set, "idle");
            else _sr.sprite = Placeholder.Box(grade);

            _pos = WorldLayout.NexusCenter + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.4f), 0f);
            float dir = Random.value < 0.75f ? 1f : -1f; // 대부분 수리공·노점 쪽으로
            _vel = new Vector3(dir * Random.Range(1.2f, 2.6f), Random.Range(2.2f, 3.4f) * (milestone ? 1.2f : 1f), 0f);
            _groundY = WorldLayout.GroundY;
            _restTimer = milestone ? 1.6f : 0.9f;
            transform.position = WorldLayout.Snap(_pos);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (_phase)
            {
                case 0:
                    _vel.y -= 9.8f * dt;
                    _pos += _vel * dt;
                    if (_pos.x < -WorldLayout.HalfW + 0.3f) { _pos.x = -WorldLayout.HalfW + 0.3f; _vel.x = -_vel.x * 0.5f; }
                    if (_pos.y <= _groundY)
                    {
                        _pos.y = _groundY;
                        if (Mathf.Abs(_vel.y) > 1.2f) { _vel.y = -_vel.y * 0.35f; _vel.x *= 0.5f; }
                        else { _phase = 1; }
                    }
                    break;
                case 1:
                    _restTimer -= dt;
                    if (_restTimer <= 0f) { _phase = 2; _flyFrom = _pos; _flyT = 0f; }
                    break;
                case 2:
                    _flyT += dt / 0.55f;
                    float t = Mathf.Clamp01(_flyT);
                    var to = WorldLayout.BoxCollectPos;
                    _pos = Vector3.Lerp(_flyFrom, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
                    var c = _sr.color; c.a = 1f - Mathf.Clamp01((t - 0.7f) / 0.3f); _sr.color = c;
                    if (t >= 1f) Destroy(gameObject);
                    break;
            }
            transform.position = WorldLayout.Snap(_pos);
        }
    }
}
