using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NexusBreaker
{
    [Serializable]
    public class AnimClipData
    {
        public string tag;
        public Sprite[] frames;
        public float[] durations; // 초 단위
        public float Length
        {
            get { float t = 0f; if (durations != null) foreach (var d in durations) t += d; return t; }
        }
    }

    [Serializable]
    public class AnimSet
    {
        public string name;               // 파일명(확장자 제외), 예: "repairman"
        public Sprite defaultSprite;
        public List<AnimClipData> clips = new List<AnimClipData>();

        public AnimClipData Get(string tag)
        {
            foreach (var c in clips) if (c.tag == tag) return c;
            return null;
        }
    }

    [Serializable]
    public class NamedSprite
    {
        public string name;
        public Sprite sprite;
    }

    [Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
    }

    /// <summary>
    /// 런타임이 쓰는 모든 아트·사운드 참조. 에디터 메뉴 "Nexus Breaker/Wire Assets"가 이름 규칙으로 자동 채운다.
    /// 비어 있으면 Placeholder가 대신 그린다(아트가 오기 전에도 게임이 돈다).
    /// </summary>
    [CreateAssetMenu(menuName = "Nexus Breaker/Game Assets", fileName = "GameAssets")]
    public class GameAssets : ScriptableObject
    {
        public List<AnimSet> animSets = new List<AnimSet>();
        public List<NamedSprite> sprites = new List<NamedSprite>();
        public List<NamedClip> audio = new List<NamedClip>();
        [Tooltip("기본 UI 폰트(Galmuri11, 12px)")] public TMP_FontAsset font;
        [Tooltip("작은 글자(Galmuri9, 10px)")] public TMP_FontAsset fontSmall;
        [Tooltip("아주 작은 글자(Galmuri7, 8px)")] public TMP_FontAsset fontTiny;

        Dictionary<string, AnimSet> _anim;
        Dictionary<string, Sprite> _sprite;
        Dictionary<string, AudioClip> _audio;
        Dictionary<string, List<AudioClip>> _variants;

        static GameAssets _instance;

        public static GameAssets Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameAssets>("GameAssets");
                    if (_instance == null) _instance = CreateInstance<GameAssets>(); // 아직 연결 전: 전부 플레이스홀더
                }
                return _instance;
            }
        }

        void BuildCaches()
        {
            if (_anim != null) return;
            _anim = new Dictionary<string, AnimSet>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in animSets) if (a != null && !string.IsNullOrEmpty(a.name)) _anim[a.name] = a;

            _sprite = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in sprites) if (s != null && s.sprite != null && !string.IsNullOrEmpty(s.name)) _sprite[s.name] = s.sprite;

            _audio = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            _variants = new Dictionary<string, List<AudioClip>>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in audio)
            {
                if (a == null || a.clip == null || string.IsNullOrEmpty(a.name)) continue;
                _audio[a.name] = a.clip;
                // "sfx_hit_03" → 변형 그룹 "sfx_hit"
                int us = a.name.LastIndexOf('_');
                if (us > 0 && int.TryParse(a.name.Substring(us + 1), out _))
                {
                    string root = a.name.Substring(0, us);
                    if (!_variants.TryGetValue(root, out var list)) _variants[root] = list = new List<AudioClip>();
                    list.Add(a.clip);
                }
            }
        }

        public AnimSet Anim(string name)
        {
            BuildCaches();
            return name != null && _anim.TryGetValue(name, out var a) ? a : null;
        }

        /// <summary>이름 후보를 순서대로 찾아 첫 번째로 있는 스프라이트.</summary>
        public Sprite Sprite(params string[] names)
        {
            BuildCaches();
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n)) continue;
                if (_sprite.TryGetValue(n, out var s)) return s;
                var a = Anim(n);
                if (a != null && a.defaultSprite != null) return a.defaultSprite;
            }
            return null;
        }

        public Sprite ItemIcon(string itemId) => Sprite("item_" + itemId);
        public Sprite UiIcon(string icon) => Sprite("ui_icons/" + icon, "icon_" + icon, icon);

        /// <summary>정확한 이름 → 없으면 "_01.._NN" 변형 중 랜덤.</summary>
        public AudioClip Audio(string name)
        {
            BuildCaches();
            if (_audio.TryGetValue(name, out var c)) return c;
            if (_variants.TryGetValue(name, out var list) && list.Count > 0)
                return list[UnityEngine.Random.Range(0, list.Count)];
            return null;
        }

        public void InvalidateCaches() { _anim = null; _sprite = null; _audio = null; _variants = null; _bordered.Clear(); }

        readonly Dictionary<Sprite, Sprite> _bordered = new Dictionary<Sprite, Sprite>();

        /// <summary>
        /// 9-slice 테두리가 없는 스프라이트에 테두리를 붙인 복제본(런타임 생성, 캐시).
        /// Aseprite 임포터 스프라이트에 Sprite Editor로 테두리를 따로 지정하지 않아도 되게 한다.
        /// </summary>
        public Sprite Bordered(Sprite s, int border)
        {
            if (s == null || border <= 0 || s.border != Vector4.zero) return s;
            if (_bordered.TryGetValue(s, out var c) && c != null) return c;
            c = UnityEngine.Sprite.Create(s.texture, s.rect, new Vector2(0.5f, 0.5f), s.pixelsPerUnit, 0,
                                          SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            c.name = s.name + "_9s";
            _bordered[s] = c;
            return c;
        }

        /// <summary>Docs/ASSET_SPEC.md 2-8의 9-slice 테두리 값.</summary>
        public static int BorderFor(string uiName) => uiName switch
        {
            "ui_panel" => 6,
            "ui_panel_dark" => 6,
            "ui_button" => 5,
            "ui_slot" => 4,
            "ui_bar" => 2,
            "ui_chat" => 8,
            _ => 0,
        };

        /// <summary>UI 스프라이트를 이름 후보로 찾고 스펙의 9-slice 테두리를 적용.</summary>
        public Sprite UiSprite(string baseName, params string[] names)
        {
            var s = Sprite(names.Length > 0 ? names : new[] { baseName });
            return Bordered(s, BorderFor(baseName));
        }
    }
}
