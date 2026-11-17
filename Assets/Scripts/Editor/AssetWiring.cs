using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace NexusBreaker.EditorTools
{
    /// <summary>
    /// Assets/Art · Assets/Audio · Assets/Fonts를 이름 규칙으로 훑어 Resources/GameAssets.asset을 채운다.
    /// 아트/사운드가 추가·교체될 때마다 다시 실행하면 된다(멱등).
    /// - .aseprite: 애니메이션 클립 → 태그별 프레임·시간 추출(AnimSet). 태그 첫 프레임은 "파일/태그" 이름의 스프라이트로도 등록.
    /// - .png: 스프라이트(멀티 스프라이트면 "파일/스프라이트명"도 등록)
    /// - .wav/.ogg/.mp3: 파일명으로 오디오 등록
    /// - Assets/Fonts/*.asset(TMP_FontAsset) 중 첫 번째를 UI 폰트로
    /// </summary>
    public static class AssetWiring
    {
        public const string AssetPath = "Assets/Resources/GameAssets.asset";

        [MenuItem("Nexus Breaker/2. Wire Assets (아트·사운드 연결)")]
        public static void WireMenu()
        {
            var report = Wire();
            Debug.Log(report);
        }

        public static string Wire()
        {
            Directory.CreateDirectory("Assets/Resources");
            var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetPath);
            if (ga == null)
            {
                ga = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(ga, AssetPath);
            }

            ga.animSets.Clear();
            ga.sprites.Clear();
            ga.audio.Clear();

            int files = 0, clips = 0;
            var spriteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddSprite(string name, Sprite s)
            {
                if (s == null || string.IsNullOrEmpty(name) || !spriteNames.Add(name)) return;
                ga.sprites.Add(new NamedSprite { name = name, sprite = s });
            }

            if (AssetDatabase.IsValidFolder("Assets/Art"))
            {
                foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets/Art" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext != ".aseprite" && ext != ".ase" && ext != ".png") continue;
                    string file = Path.GetFileNameWithoutExtension(path);
                    var all = AssetDatabase.LoadAllAssetsAtPath(path);
                    var sprites = all.OfType<Sprite>().OrderBy(s => s.name, StringComparer.Ordinal).ToList();
                    var animClips = all.OfType<AnimationClip>().ToList();
                    files++;
                    if (path.StartsWith("Assets/Art/UI/"))
                        Debug.Log($"[Wire] {file}: 스프라이트 {sprites.Count}개({string.Join(",", sprites.Take(4).Select(s => s.name))}…), 클립 {animClips.Count}개({string.Join(",", animClips.Take(4).Select(c => c.name))}…), 하위 에셋 {all.Length}개");

                    var set = new AnimSet { name = file, defaultSprite = sprites.FirstOrDefault() };
                    foreach (var clip in animClips)
                    {
                        var data = Extract(clip, file);
                        if (data == null) continue;
                        set.clips.Add(data);
                        clips++;
                        AddSprite(file + "/" + data.tag, data.frames[0]);
                    }
                    if (set.clips.Count > 0)
                    {
                        // 기본 스프라이트: idle 계열 첫 프레임 우선
                        var idle = set.Get("idle") ?? set.Get("s0_idle") ?? set.Get("walk") ?? set.clips[0];
                        set.defaultSprite = idle.frames[0];
                        ga.animSets.Add(set);
                    }

                    AddSprite(file, set.defaultSprite);
                    if (sprites.Count > 1)
                        foreach (var s in sprites) { AddSprite(file + "/" + s.name, s); AddSprite(s.name, s); }
                }
            }

            int sounds = 0;
            if (AssetDatabase.IsValidFolder("Assets/Audio"))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip == null) continue;
                    ga.audio.Add(new NamedClip { name = Path.GetFileNameWithoutExtension(path), clip = clip });
                    sounds++;
                }
            }

            ga.font = ga.fontSmall = ga.fontTiny = null;
            if (AssetDatabase.IsValidFolder("Assets/Fonts"))
            {
                var fonts = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Fonts" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(f => f != null).ToList();
                ga.font = fonts.FirstOrDefault(f => f.name.StartsWith("Galmuri11")) ?? fonts.FirstOrDefault();
                ga.fontSmall = fonts.FirstOrDefault(f => f.name.StartsWith("Galmuri9"));
                ga.fontTiny = fonts.FirstOrDefault(f => f.name.StartsWith("Galmuri7"));
            }

            ga.InvalidateCaches();
            EditorUtility.SetDirty(ga);
            AssetDatabase.SaveAssets();
            return $"[Wire] 아트 파일 {files}개 · 애니메이션 세트 {ga.animSets.Count}개 · 클립 {clips}개 · 스프라이트 이름 {ga.sprites.Count}개 · 사운드 {sounds}개 · 폰트 {(ga.font != null ? ga.font.name : "없음(개발용 OS 폰트 사용)")}";
        }

        /// <summary>스프라이트 키프레임 → 프레임/시간 배열. 클립 이름이 "파일_태그" 형식이면 태그만 남긴다.</summary>
        static AnimClipData Extract(AnimationClip clip, string file)
        {
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .FirstOrDefault(b => b.propertyName == "m_Sprite");
            if (binding.propertyName != "m_Sprite") return null;
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys == null || keys.Length == 0) return null;

            var frames = new List<Sprite>();
            var times = new List<float>();
            foreach (var k in keys.OrderBy(k => k.time))
            {
                if (!(k.value is Sprite s)) continue;
                frames.Add(s);
                times.Add(k.time);
            }
            if (frames.Count == 0) return null;

            var durations = new List<float>();
            for (int i = 0; i < frames.Count; i++)
            {
                float d = i + 1 < frames.Count ? times[i + 1] - times[i] : clip.length - times[i];
                durations.Add(d);
            }
            // 마지막 키가 길이 0짜리 "끝 표시" 중복 키면 제거
            if (frames.Count > 1 && durations[durations.Count - 1] <= 1e-4f)
            {
                frames.RemoveAt(frames.Count - 1);
                durations.RemoveAt(durations.Count - 1);
            }
            for (int i = 0; i < durations.Count; i++) if (durations[i] <= 1e-4f) durations[i] = 0.1f;

            string tag = clip.name;
            if (tag.StartsWith(file + "_", StringComparison.OrdinalIgnoreCase)) tag = tag.Substring(file.Length + 1);
            tag = tag.Trim().ToLowerInvariant();

            return new AnimClipData { tag = tag, frames = frames.ToArray(), durations = durations.ToArray() };
        }
    }
}
