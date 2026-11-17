using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// 자동 검증용 명령줄 옵션(일반 실행에는 영향 없음).
    ///   -nbSaveDir &lt;폴더&gt;   저장 위치를 바꾼다(실제 세이브 보호)
    ///   -nbDemo               보여주기용 상태(너트·박스·창고·진열대·강화·HP 85%)로 시작
    ///   -nbPage shop|upgrade|settings   시작 시 페이지 열기
    ///   -nbShot &lt;파일.png&gt;  -nbDelay 초   지정 시간 뒤 스크린샷을 찍고 종료
    /// </summary>
    public class DebugAutomation : MonoBehaviour
    {
        public static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        public static bool Flag(string name)
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>GameRoot.Awake 맨 앞에서 호출(저장 위치 변경).</summary>
        public static void PreLoad()
        {
            var dir = Arg("-nbSaveDir");
            if (!string.IsNullOrEmpty(dir)) SaveSystem.OverrideDir = dir;
        }

        /// <summary>모델 생성 직후 호출.</summary>
        public static void ApplyDemo(GameModel m)
        {
            ApplyTutorialStep(m);
            if (Flag("-nbNoChat")) m.Data.settings.showChat = false; // 방명록 끈 상태 확인용
            // -nbOffline 초: 그만큼 꺼져 있었던 것처럼 → 오프라인 정산 팝업 확인용
            if (double.TryParse(Arg("-nbOffline"), NumberStyles.Float, CultureInfo.InvariantCulture, out double away))
                m.Data.lastSaveUnix = SaveSystem.NowUnix - (long)away;
            if (!Flag("-nbDemo")) return;
            var d = m.Data;
            if (Arg("-nbTutorialStep") == null) d.tutorialStep = -1; // 보여주기용 화면에는 튜토리얼을 띄우지 않음
            d.nuts = 12345;
            d.nexusHp = d.nexusMaxHp * 0.853;
            d.nextMilestonePercent = 85;
            d.boxes[0] = 4; d.boxes[1] = 2; d.boxes[2] = 1; d.boxes[3] = 1;
            foreach (var id in new[] { "cup_ramen", "cigarettes", "battery", "diary", "melted_watch", "medkit", "warm_stone", "snack_shrimp" })
                d.warehouse.Add(new ItemStack { id = id, count = id == "cup_ramen" ? 5 : 1 });
            d.upgrades.Add(new UpgradeLevel { id = UpgradeId.Hammer.ToString(), level = 12 });
            d.upgrades.Add(new UpgradeLevel { id = UpgradeId.Helpers.ToString(), level = Flag("-nbLively") ? 4 : 2 });
            d.upgrades.Add(new UpgradeLevel { id = UpgradeId.SwingSpeed.ToString(), level = Flag("-nbLively") ? 9 : 4 });
            if (Flag("-nbLively"))
            {
                // 보여주기용: 손님·박스가 자주 나오게
                d.upgrades.Add(new UpgradeLevel { id = UpgradeId.Traffic.ToString(), level = 22 });
                d.upgrades.Add(new UpgradeLevel { id = UpgradeId.BoxRate.ToString(), level = 12 });
                d.upgrades.Add(new UpgradeLevel { id = UpgradeId.Warehouse.ToString(), level = 10 });
            }
            m.PlaceOnShelf("cup_ramen", 25);
            m.PlaceOnShelf("cup_ramen", 40);
            m.PlaceOnShelf("cigarettes", 60);
            m.PlaceOnShelf("melted_watch", 600);
            foreach (var id in new[] { "cup_ramen", "cigarettes", "battery", "diary", "melted_watch", "medkit", "warm_stone" })
                if (!d.codex.Contains(id)) d.codex.Add(id);
        }

        /// <summary>-nbTutorialStep n: 튜토리얼 n단계 화면을 바로 확인하기 위한 상태 준비.</summary>
        static void ApplyTutorialStep(GameModel m)
        {
            if (!int.TryParse(Arg("-nbTutorialStep"), out int step)) return;
            var d = m.Data;
            d.tutorialStep = step;
            if (step >= 2 && step <= 3 && m.BoxCount == 0) d.boxes[0] = 2;
            if (step >= 4 && step <= 6 && m.ItemCountInWarehouse == 0) d.warehouse.Add(new ItemStack { id = "cup_ramen", count = 2 });
            if (step >= 6 && m.OccupiedShelfCount == 0) m.PlaceOnShelf("cup_ramen", 25);
            if (step == 7) d.nuts = Math.Max(d.nuts, 40);
        }

        static UiRoot.Page ParsePage(string s) => (s ?? "").ToLowerInvariant() switch
        {
            "shop" => UiRoot.Page.Shop,
            "upgrade" => UiRoot.Page.Upgrade,
            "settings" => UiRoot.Page.Settings,
            _ => UiRoot.Page.None,
        };

        public static void AfterBuild(UiRoot ui)
        {
            var page = ParsePage(Arg("-nbPage"));
            // -nbPageAfter 초: 창을 옮긴 뒤 페이지를 여는 상황 재현용(창 위치 유지 검증)
            if (page != UiRoot.Page.None && float.TryParse(Arg("-nbPageAfter"), NumberStyles.Float, CultureInfo.InvariantCulture, out float after))
            {
                var pg = new GameObject("DebugAutomationPage");
                pg.AddComponent<DebugAutomation>().StartCoroutine(OpenPageLater(ui, page, after));
            }
            else if (page != UiRoot.Page.None) ui.OpenPage(page);
            float delay = 4f;
            if (float.TryParse(Arg("-nbDelay"), NumberStyles.Float, CultureInfo.InvariantCulture, out var dl)) delay = dl;

            var seq = Arg("-nbSeq");
            if (!string.IsNullOrEmpty(seq))
            {
                int count = int.TryParse(Arg("-nbSeqCount"), out var c) ? c : 40;
                float interval = float.TryParse(Arg("-nbSeqInterval"), NumberStyles.Float, CultureInfo.InvariantCulture, out var iv) ? iv : 0.1f;
                var g = new GameObject("DebugAutomation");
                g.AddComponent<DebugAutomation>().StartCoroutine(SequenceAndQuit(seq, delay, count, interval));
                return;
            }

            var shot = Arg("-nbShot");
            if (string.IsNullOrEmpty(shot)) return;
            var go = new GameObject("DebugAutomation");
            go.AddComponent<DebugAutomation>().StartCoroutine(ShotAndQuit(shot, delay));
        }

        static IEnumerator OpenPageLater(UiRoot ui, UiRoot.Page page, float after)
        {
            yield return new WaitForSecondsRealtime(after);
            ui.OpenPage(page);
            Debug.Log($"[DebugAutomation] 페이지 열기: {page}");
        }

        /// <summary>GIF용 연속 캡처: folder/frame_000.png … 를 저장하고 종료.</summary>
        static IEnumerator SequenceAndQuit(string folder, float delay, int count, float interval)
        {
            System.IO.Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(delay);
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, $"frame_{i:000}.png"));
                yield return new WaitForSecondsRealtime(interval);
            }
            yield return new WaitForSecondsRealtime(1.5f);
            Debug.Log($"[DebugAutomation] 연속 캡처 {count}장: {folder}");
            Application.Quit();
        }

        static IEnumerator ShotAndQuit(string path, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(1.5f);
            Debug.Log($"[DebugAutomation] 스크린샷 저장: {path} ({Screen.width}x{Screen.height})");
            Application.Quit();
        }
    }
}
