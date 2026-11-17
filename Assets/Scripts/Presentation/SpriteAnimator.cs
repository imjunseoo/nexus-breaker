using System;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>
    /// Aseprite 태그 단위 프레임 애니메이션 재생기. SpriteRenderer 또는 UI Image에 붙인다.
    /// Animator 대신 직접 재생 → 클립 바인딩 경로와 무관하게 동작하고 UI에서도 쓸 수 있다.
    /// </summary>
    public class SpriteAnimator : MonoBehaviour
    {
        public AnimSet Set { get; private set; }
        public string CurrentTag { get; private set; }
        public bool IsPlaying => _clip != null && !_finished;

        SpriteRenderer _sr;
        Image _img;
        AnimClipData _clip;
        int _frame;
        float _timer;
        bool _loop;
        bool _finished;
        string _next;
        Action _onDone;
        public float Speed = 1f;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _img = GetComponent<Image>();
        }

        public void SetSet(AnimSet set, string startTag = null)
        {
            Set = set;
            _clip = null;
            if (set == null) return;
            if (startTag != null && set.Get(startTag) != null) Play(startTag, loop: true);
            else if (set.defaultSprite != null) Apply(set.defaultSprite);
        }

        public bool Has(string tag) => Set != null && Set.Get(tag) != null;

        /// <summary>
        /// 태그 재생. loop=false면 끝난 뒤 next 태그로 넘어가고(없으면 마지막 프레임 유지) onDone 호출.
        /// 같은 루프 태그를 다시 부르면 처음부터 다시 시작하지 않는다.
        /// </summary>
        public void Play(string tag, bool loop = true, string next = null, Action onDone = null, bool restart = false)
        {
            if (Set == null) { onDone?.Invoke(); return; }
            var clip = Set.Get(tag);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) { onDone?.Invoke(); return; }
            if (!restart && loop && _loop && CurrentTag == tag && !_finished) return;

            _clip = clip;
            CurrentTag = tag;
            _loop = loop;
            _next = next;
            _onDone = onDone;
            _frame = 0;
            _timer = 0f;
            _finished = false;
            Apply(clip.frames[0]);
        }

        void Update()
        {
            if (_clip == null || _finished) return;
            _timer += Time.deltaTime * Speed;
            float dur = FrameDuration(_frame);
            while (_timer >= dur)
            {
                _timer -= dur;
                _frame++;
                if (_frame >= _clip.frames.Length)
                {
                    if (_loop) _frame = 0;
                    else
                    {
                        _frame = _clip.frames.Length - 1;
                        _finished = true;
                        var done = _onDone; _onDone = null;
                        string next = _next;
                        done?.Invoke();
                        if (next != null && _finished) Play(next, loop: true);
                        return;
                    }
                }
                Apply(_clip.frames[_frame]);
                dur = FrameDuration(_frame);
            }
        }

        float FrameDuration(int i)
        {
            if (_clip.durations == null || i >= _clip.durations.Length) return 0.1f;
            return Mathf.Max(0.016f, _clip.durations[i]);
        }

        void Apply(Sprite s)
        {
            if (_sr != null) _sr.sprite = s;
            if (_img != null) { _img.sprite = s; _img.enabled = s != null; }
        }
    }
}
