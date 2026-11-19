using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NexusBreaker.EditorTools
{
    /// <summary>
    /// 배포용(릴리스) Windows 빌드. 개발 빌드 옵션 없이 Builds/Release/NexusBreaker_v{버전}/ 에 만든다.
    /// 배치: Unity -batchmode -quit -projectPath . -executeMethod NexusBreaker.EditorTools.BuildTools.BuildReleaseBatch -nbVersion 0.1.0-beta
    /// (zip 패키징은 빌드 후 별도 단계에서 한다 — *_DoNotShip 폴더 제외)
    /// </summary>
    public static class BuildTools
    {
        public const string DefaultVersion = "0.2.0-beta";

        [MenuItem("Nexus Breaker/Build Release (Windows)")]
        public static void BuildMenu() => Build(DefaultVersion);

        public static void BuildReleaseBatch()
        {
            string version = DebugAutomation.Arg("-nbVersion") ?? DefaultVersion;
            var ok = Build(version);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Build(string version)
        {
            PlayerSettings.bundleVersion = version;
            string dir = $"Builds/Release/NexusBreaker_v{version}";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = $"{dir}/NexusBreaker.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None, // 개발 빌드 아님(콘솔·프로파일러 연결 없음)
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[Release] {s.result} · {s.totalSize / (1024f * 1024f):0.0}MB · {s.totalTime.TotalSeconds:0}s · {dir}");
            return s.result == BuildResult.Succeeded;
        }
    }
}
