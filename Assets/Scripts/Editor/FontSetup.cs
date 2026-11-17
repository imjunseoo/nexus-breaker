using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace NexusBreaker.EditorTools
{
    /// <summary>
    /// Galmuri(OFL) TTF → 픽셀 정렬 TMP 폰트 에셋(Raster Hinted, Point 필터, 동적 아틀라스).
    /// 각 폰트는 설계 크기로만 쓴다: Galmuri11=12px, Galmuri9=10px, Galmuri7=8px. (UI 캔버스 단위, 화면에서는 ×배율)
    /// </summary>
    public static class FontSetup
    {
        const string Dir = "Assets/Fonts/Galmuri";

        static readonly (string ttf, string asset, int size)[] Fonts =
        {
            ("Galmuri11.ttf", "Galmuri11_12px", 12),
            ("Galmuri9.ttf", "Galmuri9_10px", 10),
            ("Galmuri7.ttf", "Galmuri7_8px", 8),
        };

        // 미리 아틀라스에 넣어둘 글자(런타임 첫 표시 끊김 감소). 나머지 한글은 필요할 때 동적으로 추가된다.
        const string Prewarm =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "×→▲▼·…%" +
            "만억조경해너트박스창고진열대상점강화설정시장가판매구매손님생존자떠돌이상인약탈군잔당넥서스교단수집변장한외계인" +
            "망치손놀림조수고용장비급소찾기잔해뒤지감별사의눈확간판달야경비타격스윙치명등급업칸초마다명시간" +
            "외피압축층내핵코어경도폐허도시일반고급희귀획득비싼데살게없네즉빠름보통느림매우기적예상약분하나모두열기내리같은물건";

        [MenuItem("Nexus Breaker/Create Pixel Fonts (Galmuri)")]
        public static void CreateMenu()
        {
            Create();
            Debug.Log(AssetWiring.Wire());
        }

        public static void RunBatch()
        {
            try
            {
                Create();
                Debug.Log(AssetWiring.Wire());
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Create()
        {
            foreach (var (ttf, assetName, size) in Fonts)
            {
                string ttfPath = $"{Dir}/{ttf}";
                string assetPath = $"{Dir}/{assetName}.asset";
                var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
                if (font == null) { Debug.LogWarning($"[Font] {ttfPath} 없음"); continue; }
                if (File.Exists(assetPath)) AssetDatabase.DeleteAsset(assetPath);

                var fa = TMP_FontAsset.CreateFontAsset(font, size, 1, GlyphRenderMode.RASTER_HINTED, 2048, 2048,
                                                       AtlasPopulationMode.Dynamic, true);
                if (fa == null) { Debug.LogError($"[Font] {ttf} 폰트 에셋 생성 실패"); continue; }
                fa.name = assetName;
                AssetDatabase.CreateAsset(fa, assetPath);

                var tex = fa.atlasTextures[0];
                tex.name = assetName + " Atlas";
                tex.filterMode = FilterMode.Point;
                AssetDatabase.AddObjectToAsset(tex, fa);
                fa.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);

                fa.TryAddCharacters(Prewarm, out string missing);
                foreach (var t in fa.atlasTextures) if (t != null) t.filterMode = FilterMode.Point;
                if (!string.IsNullOrEmpty(missing)) Debug.Log($"[Font] {assetName} 없는 글자: {missing}");

                EditorUtility.SetDirty(fa);
                Debug.Log($"[Font] {assetName} 생성 (샘플링 {size}px, Raster Hinted, Point)");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
