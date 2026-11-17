using System.Collections.Generic;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// BGM(크로스페이드) · SFX(풀, 변형 랜덤, 동시재생 제한) · 앰비언스 루프.
    /// 클립 이름은 Docs/ASSET_SPEC.md 4장. 파일이 없으면 조용히 무시한다.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        GameModel _model;
        AudioSource _bgmA, _bgmB, _ambWind, _ambHum;
        readonly List<AudioSource> _sfx = new List<AudioSource>();
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        float _bgmVol = 0.6f, _sfxVol = 0.8f, _ambVol = 0.5f;
        string _bgmName;
        float _fade = 1f;
        bool _aIsActive = true;
        const int SfxVoices = 12;

        public static AudioManager Create(GameModel model)
        {
            var go = new GameObject("Audio");
            var a = go.AddComponent<AudioManager>();
            a.Init(model);
            return a;
        }

        void Init(GameModel model)
        {
            Instance = this;
            _model = model;
            _bgmA = NewSource("BGM_A", loop: true);
            _bgmB = NewSource("BGM_B", loop: true);
            _ambWind = NewSource("Amb_Wind", loop: true);
            _ambHum = NewSource("Amb_Hum", loop: true);
            for (int i = 0; i < SfxVoices; i++) _sfx.Add(NewSource("SFX_" + i, loop: false));

            ApplyVolumes(model.Data.settings);
            StartLoop(_ambWind, "amb_wasteland");
            StartLoop(_ambHum, "amb_nexus_hum");

            model.Hit += h =>
            {
                if (h.Source == HitSource.Helper) Sfx("sfx_helper_hit", 0.55f, 0.06f);
                else if (h.Crit) Sfx("sfx_hit_crit", 1f, 0f);
                else Sfx("sfx_hit", 0.8f, 0.05f);
            };
            model.StageChanged += _ => Sfx("sfx_nexus_crack");
            model.NexusDestroyed += () => Sfx("sfx_nexus_destroy");
            model.BoxDropped += (g, milestone) => Sfx("sfx_box_drop", milestone ? 1f : 0.7f);
            model.BoxOpened += (item, g, isNew) =>
                Sfx(g >= Grade.Alien ? "sfx_box_open_alien" : g >= Grade.Rare ? "sfx_box_open_rare" : "sfx_box_open_common");
            model.CustomerArrived += _ => Sfx("sfx_customer_enter", 0.6f);
            model.CustomerResolved += v =>
            {
                if (v.Outcome == VisitOutcome.Bought)
                    Sfx(v.Market > 0 && v.Price >= v.Market * 2.5 ? "sfx_purchase_big" : "sfx_purchase");
                else Sfx("sfx_customer_reject", 0.6f);
            };
            model.Prestiged += () => { Sfx("sfx_prestige"); Stinger("stinger_prestige"); };
        }

        AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        public void ApplyVolumes(GameSettings s)
        {
            _bgmVol = s.bgmVolume;
            _sfxVol = s.sfxVolume;
            _ambVol = s.ambientVolume;
            _ambWind.volume = _ambVol * 0.8f;
        }

        void StartLoop(AudioSource src, string clipName)
        {
            var clip = GameAssets.Instance.Audio(clipName);
            if (clip == null) return;
            src.clip = clip;
            src.Play();
        }

        /// <summary>효과음. 같은 소리가 너무 촘촘하면(30ms 이내) 건너뛴다.</summary>
        public void Sfx(string name, float volume = 1f, float pitchJitter = 0.03f)
        {
            var clip = GameAssets.Instance.Audio(name);
            if (clip == null) return;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(name, out var t) && now - t < 0.03f) return;
            _lastPlayed[name] = now;

            AudioSource free = null;
            foreach (var s in _sfx) if (!s.isPlaying) { free = s; break; }
            if (free == null) free = _sfx[Random.Range(0, _sfx.Count)];
            free.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            free.PlayOneShot(clip, volume * _sfxVol);
        }

        void Stinger(string name)
        {
            var clip = GameAssets.Instance.Audio(name);
            if (clip == null) return;
            _sfx[0].pitch = 1f;
            _sfx[0].PlayOneShot(clip, _bgmVol);
        }

        void PlayBgm(string name)
        {
            if (_bgmName == name) return;
            var clip = GameAssets.Instance.Audio(name);
            if (clip == null) return;
            _bgmName = name;
            var next = _aIsActive ? _bgmB : _bgmA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            _aIsActive = !_aIsActive;
            _fade = 0f;
        }

        void Update()
        {
            // 상황별 BGM: 상점 페이지 / 코어 지층 / 기본
            var ui = UiRoot.Instance;
            string want = ui != null && ui.Current == UiRoot.Page.Shop ? "bgm_shop"
                        : _model.StratumNow >= 3 && !_model.Data.nexusDestroyed ? "bgm_core"
                        : "bgm_main";
            if (GameAssets.Instance.Audio(want) == null) want = "bgm_main";
            PlayBgm(want);

            _fade = Mathf.MoveTowards(_fade, 1f, Time.unscaledDeltaTime / 2f);
            var active = _aIsActive ? _bgmA : _bgmB;
            var other = _aIsActive ? _bgmB : _bgmA;
            active.volume = _bgmVol * _fade;
            other.volume = _bgmVol * (1f - _fade);
            if (_fade >= 1f && other.isPlaying) other.Stop();

            // 넥서스 웅웅거림: 깊이 팔수록 커진다
            float depth = 1f - (float)_model.HpFraction;
            _ambHum.volume = _model.Data.nexusDestroyed ? 0f : _ambVol * Mathf.Lerp(0.25f, 0.9f, depth);
        }
    }
}
