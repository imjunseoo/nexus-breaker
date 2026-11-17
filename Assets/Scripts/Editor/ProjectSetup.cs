using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NexusBreaker.EditorTools
{
    /// <summary>
    /// 한 번 실행하는 프로젝트 셋업: TMP 필수 리소스, 플레이어 설정(데스크톱 띠 창), Main 씬, 빌드 씬 목록, 에셋 연결.
    /// 배치 모드: Unity -batchmode -quit -projectPath . -executeMethod NexusBreaker.EditorTools.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        const string MainScene = "Assets/Scenes/Main.unity";

        [MenuItem("Nexus Breaker/1. Setup Project (처음 한 번)")]
        public static void RunMenu()
        {
            Run();
            EditorSceneManager.OpenScene(MainScene);
        }

        static bool _batchDone;

        /// <summary>배치 모드: TMP 패키지 임포트는 비동기라 완료 콜백을 기다린 뒤 나머지를 실행하고 종료.</summary>
        public static void RunBatch()
        {
            try
            {
                if (TmpMissing() && StartTmpImport())
                {
                    AssetDatabase.importPackageCompleted += _ => FinishBatch();
                    AssetDatabase.importPackageFailed += (_, err) => { Debug.LogError("[Setup] TMP 임포트 실패: " + err); FinishBatch(); };
                    double start = EditorApplication.timeSinceStartup;
                    EditorApplication.update += () => { if (EditorApplication.timeSinceStartup - start > 180) FinishBatch(); };
                    return;
                }
                FinishBatch();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        static void FinishBatch()
        {
            if (_batchDone) return;
            _batchDone = true;
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Run()
        {
            if (TmpMissing()) StartTmpImport();
            ConfigurePlayer();
            CreateMainScene();
            Debug.Log(AssetWiring.Wire());
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] 완료");
        }

        static bool TmpMissing() => AssetDatabase.FindAssets("t:TMP_Settings").Length == 0;

        static bool StartTmpImport()
        {
            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            if (pkg == null) { Debug.LogWarning("[Setup] com.unity.ugui 패키지를 찾지 못함"); return false; }
            string path = Path.Combine(pkg.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(path)) { Debug.LogWarning("[Setup] TMP Essential Resources 없음: " + path); return false; }
            AssetDatabase.ImportPackage(path, false);
            Debug.Log("[Setup] TMP Essential Resources 임포트 시작");
            return true;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "NexusBreaker";
            PlayerSettings.productName = "Nexus Breaker";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = WorldLayout.FrameW * 3;
            PlayerSettings.defaultScreenHeight = WorldLayout.StripH * 3;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.allowFullscreenSwitch = false;
            // 투명·레이어드 창(DWM/컬러키)은 D3D11 + 비플립 스왑체인 전제.
            // D3D12는 플립 모델만 지원해서 레이어드 창과 충돌 → 창 크기 변경 시 장치 제거(887a0006) 크래시가 재현됨.
            PlayerSettings.useFlipModelSwapchain = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("[Setup] 플레이어 설정: 1152×288 창, 백그라운드 실행, D3D11 고정, 플립 모델 끔");
        }

        static void CreateMainScene()
        {
            if (!File.Exists(MainScene))
            {
                // 템플릿 샘플 씬(Main Camera + Global Light 2D)을 복사해서 시작
                if (File.Exists("Assets/Scenes/SampleScene.unity"))
                    AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", MainScene);
                else
                {
                    var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(s, MainScene);
                }
            }

            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<GameRoot>() == null)
            {
                var go = new GameObject("GameRoot");
                go.AddComponent<GameRoot>();
            }
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = WorldLayout.HalfH;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palette.Black;
                cam.transform.position = new Vector3(0, 0, -10);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
            Debug.Log("[Setup] Main 씬 생성·빌드 목록 등록");
        }
    }
}
