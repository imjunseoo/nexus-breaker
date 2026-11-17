using System;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>손님 한 명: 오른쪽에서 걸어와 카운터 앞에서 구매/거절 후 다시 오른쪽으로 나간다.</summary>
    public class CustomerView : MonoBehaviour
    {
        public CustomerVisit Visit { get; private set; }
        public event Action<CustomerView> Left;

        SpriteRenderer _sr;
        SpriteAnimator _anim;
        bool _hasArt;
        Vector3 _from, _to;
        float _walkT, _walkDur;
        int _phase; // 0 = 입장, 1 = 대기(결정 기다림), 2 = 반응, 3 = 퇴장
        float _reactTimer;
        SpriteRenderer _bubble;

        static readonly Color32[] PlaceholderColors =
        {
            Palette.Dust1, Palette.Ash2, Palette.Rust3, Palette.Red1, Palette.Moss1, Palette.Violet, Palette.Lamp0, Palette.Cyan,
        };

        public static CustomerView Spawn(Transform parent, CustomerVisit visit, int queueIndex)
        {
            var go = new GameObject($"Customer_{visit.Customer.Id}_{visit.Serial}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<CustomerView>();
            v.Init(visit, queueIndex);
            return v;
        }

        void Init(CustomerVisit visit, int queueIndex)
        {
            Visit = visit;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = WorldLayout.Order.Customer + queueIndex;
            _anim = gameObject.AddComponent<SpriteAnimator>();
            var set = GameAssets.Instance.Anim("customer_" + visit.Customer.Id);
            _hasArt = set != null;
            if (_hasArt) _anim.SetSet(set, "walk");
            else
            {
                int idx = 0;
                for (int i = 0; i < CustomerCatalog.All.Count; i++) if (CustomerCatalog.All[i] == visit.Customer) idx = i;
                _sr.sprite = Placeholder.Customer(PlaceholderColors[idx % PlaceholderColors.Length]);
            }

            _from = new Vector3(WorldLayout.CustomerSpawnX, WorldLayout.GroundY, 0f);
            _to = WorldLayout.QueuePos(queueIndex);
            _walkDur = (float)GameModel.CustomerWalkTime;
            transform.position = WorldLayout.Snap(_from);
        }

        /// <summary>모델이 구매/거절을 결정했을 때 호출.</summary>
        public void Resolve()
        {
            _phase = 2;
            transform.position = WorldLayout.Snap(_to);
            bool bought = Visit.Outcome == VisitOutcome.Bought;
            if (_hasArt)
            {
                string tag = bought ? "buy" : "reject";
                if (_anim.Has(tag)) _anim.Play(tag, loop: false, restart: true);
                else _anim.Play("idle", loop: true);
            }
            ShowBubble(bought);
            _reactTimer = 1.1f;
        }

        void ShowBubble(bool bought)
        {
            var go = new GameObject("Bubble");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            _bubble = go.AddComponent<SpriteRenderer>();
            _bubble.sortingOrder = WorldLayout.Order.Fx;
            Sprite icon = null;
            if (bought && Visit.Item != null)
                icon = GameAssets.Instance.ItemIcon(Visit.Item.Id) ?? Placeholder.Item(Visit.Item.Grade);
            else
                icon = GameAssets.Instance.UiIcon("close") ?? Placeholder.Get(Placeholder.Shape.Circle, 8, 8, Palette.Red2, Palette.Outline, false);
            _bubble.sprite = icon;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (_phase)
            {
                case 0:
                    _walkT += dt / _walkDur;
                    transform.position = WorldLayout.Snap(Vector3.Lerp(_from, _to, Mathf.Clamp01(_walkT)));
                    if (_walkT >= 1f)
                    {
                        _phase = 1;
                        if (_hasArt) _anim.Play("idle", loop: true);
                    }
                    break;
                case 2:
                    _reactTimer -= dt;
                    if (_bubble != null) _bubble.transform.localPosition = new Vector3(0f, 1.15f + Mathf.Sin(Time.time * 8f) * 0.03f, 0f);
                    if (_reactTimer <= 0f)
                    {
                        _phase = 3;
                        if (_bubble != null) Destroy(_bubble.gameObject);
                        _sr.flipX = true; // 오른쪽으로 걸어 나감
                        if (_hasArt) _anim.Play("walk", loop: true, restart: true);
                        _from = transform.position;
                        _to = new Vector3(WorldLayout.CustomerSpawnX + 0.5f, WorldLayout.GroundY, 0f);
                        _walkT = 0f;
                        _walkDur = Mathf.Max(0.8f, (_to.x - _from.x) / 1.6f);
                    }
                    break;
                case 3:
                    _walkT += dt / _walkDur;
                    transform.position = WorldLayout.Snap(Vector3.Lerp(_from, _to, Mathf.Clamp01(_walkT)));
                    if (_walkT >= 1f)
                    {
                        Left?.Invoke(this);
                        Destroy(gameObject);
                    }
                    break;
            }
        }
    }
}
