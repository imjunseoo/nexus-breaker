using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

namespace NexusBreaker.EditorTools
{
    /// <summary>
    /// Assets/Art 아래 .aseprite 파일이 처음 임포트될 때(메타 파일이 없을 때) 프로젝트 규칙을 적용한다.
    /// PPU 32, Point 필터, 무압축, 프레임 병합, 애니메이션 클립 생성, 피벗(캐릭터류는 바닥 중앙).
    /// </summary>
    public class AsepriteImportDefaults : AssetPostprocessor
    {
        static readonly string[] BottomPivotFolders =
        {
            "Assets/Art/Characters/", "Assets/Art/Customers/", "Assets/Art/Nexus/", "Assets/Art/Boxes/",
        };

        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith("Assets/Art/") || !assetPath.EndsWith(".aseprite", System.StringComparison.OrdinalIgnoreCase)) return;
            if (!(assetImporter is AsepriteImporter imp)) return;
            if (!imp.importSettingsMissing) return; // 사람이 바꾼 설정은 존중

            Apply(imp, assetPath);
        }

        public static void Apply(AsepriteImporter imp, string path)
        {
            imp.importMode = FileImportModes.AnimatedSprite;
            imp.layerImportMode = LayerImportModes.MergeFrame;
            imp.spritePixelsPerUnit = 32f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.generateAnimationClips = true;
            imp.generateModelPrefab = true;
            imp.pivotSpace = PivotSpaces.Canvas;

            bool bottom = false;
            foreach (var f in BottomPivotFolders) if (path.StartsWith(f)) bottom = true;
            if (path.Contains("/Environment/shop_stall")) bottom = true;
            imp.pivotAlignment = bottom ? SpriteAlignment.BottomCenter : SpriteAlignment.Center;

            // 소품 시트: 레이어 1장 = 소품 1개(레이어 이름 = 소품 이름), 각자 바닥 중앙이 지면 접점
            if (path.EndsWith("/Environment/props.aseprite"))
            {
                imp.layerImportMode = LayerImportModes.IndividualLayers;
                imp.pivotSpace = PivotSpaces.Local;
                imp.pivotAlignment = SpriteAlignment.BottomCenter;
            }

            var ps = imp.GetImporterPlatformSettings(BuildTarget.NoTarget);
            if (ps != null)
            {
                ps.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SetImporterPlatformSettings(ps);
            }
        }

        /// <summary>배치 모드: props.aseprite에만 규칙 재적용 → 재임포트 → 에셋 연결.</summary>
        public static void ReapplyPropsBatch()
        {
            try
            {
                const string p = "Assets/Art/Environment/props.aseprite";
                if (AssetImporter.GetAtPath(p) is AsepriteImporter imp)
                {
                    Apply(imp, p);
                    imp.SaveAndReimport();
                    foreach (var s in AssetDatabase.LoadAllAssetsAtPath(p))
                        if (s is Sprite sp) Debug.Log($"[Aseprite] props 스프라이트: {sp.name} {sp.rect.width}x{sp.rect.height} pivot {sp.pivot}");
                }
                Debug.Log(AssetWiring.Wire());
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Nexus Breaker/Re-apply Aseprite Import Defaults")]
        static void ReapplyAll()
        {
            var guids = AssetDatabase.FindAssets("", new[] { "Assets/Art" });
            int n = 0;
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(".aseprite", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!(AssetImporter.GetAtPath(p) is AsepriteImporter imp)) continue;
                Apply(imp, p);
                imp.SaveAndReimport();
                n++;
            }
            Debug.Log($"[Aseprite] 임포트 기본값 재적용: {n}개");
        }
    }
}
