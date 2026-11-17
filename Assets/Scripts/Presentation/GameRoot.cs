using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NexusBreaker
{
    /// <summary>
    /// 씬 진입점. 저장 불러오기 → 오프라인 정산 → 월드·UI·오디오·창 생성 → 매 프레임 모델 진행·입력·자동 저장.
    /// 씬에는 이 컴포넌트가 붙은 오브젝트와 Main Camera(+Global Light 2D)만 있으면 된다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [Tooltip("켜면 저장 파일을 무시하고 새 게임으로 시작(에디터 테스트용)")]
        public bool freshStartInEditor;
        [Tooltip("에디터에서 시간 배속(밸런스 확인용)")]
        [Range(1f, 50f)] public float editorTimeScale = 1f;

        public GameModel Model { get; private set; }
        public WorldDirector World { get; private set; }
        public UiRoot Ui { get; private set; }

        Camera _cam;
        DesktopWindow _window;
        float _autosave;
        const float AutosaveInterval = 30f;
        bool _quitting;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            DebugAutomation.PreLoad();

            var data = freshStartInEditor && Application.isEditor ? SaveData.NewGame() : SaveSystem.Load();
            Model = new GameModel(data, new Rng());
            DebugAutomation.ApplyDemo(Model);

            // 오프라인 정산(연출 생성 전에 — 이벤트 없이 조용히 진행)
            OfflineReport offline = null;
            if (data.lastSaveUnix > 0)
            {
                double away = SaveSystem.NowUnix - data.lastSaveUnix;
                if (away > 60d) offline = Model.SimulateOffline(away);
            }

            SetupCamera();
            World = WorldDirector.Create(Model);
            Ui = UiRoot.Create(Model, _cam);
            AudioManager.Create(Model);
            World.FloatText = (pos, text, color) => Ui.Floating.Spawn(pos, text, color);

            TutorialView.Create(Ui, Model, World, _cam);

            _window = DesktopWindow.Create(Model.Data.settings);
            _window.IsInteractiveAt = IsInteractiveAt;
            Ui.PageChanged += _ => ApplyWindowSettings();
            ApplyWindowSettings();

            if (offline != null) Ui.ShowOfflineReport(offline);
            if (Model.Data.nexusDestroyed) Model.Prestige(); // 파괴된 채 저장됐다면 바로 다음 지역으로
            DebugAutomation.AfterBuild(Ui);
        }

        void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = WorldLayout.HalfH;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Palette.Black;
        }

        public void ApplyWindowSettings()
        {
            var s = Model.Data.settings;
            World.SetTransparentBackground(s.transparentBackground);
            // 투명 모드에서는 순수 검정이 컬러키(=투명)
            _cam.backgroundColor = s.transparentBackground ? new Color(0f, 0f, 0f, 0f) : (Color)Palette.Black;
            _window.Apply(Ui.FrameWidthPx, Ui.FrameHeightPx);
            Ui.UpdateLayout();
        }

        bool IsInteractiveAt(Vector2 screenPos)
        {
            if (Ui.Modal.IsOpen || Ui.PageOpen) return true;
            if (Ui.IsOverUi(screenPos)) return true;
            if (!_cam.pixelRect.Contains(screenPos)) return false;
            Vector2 world = _cam.ScreenToWorldPoint(screenPos);
            return World.Nexus.Contains(world);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Application.isEditor) dt *= editorTimeScale;
            Model.Tick(dt);

            HandleWorldClick();

            _autosave += Time.unscaledDeltaTime;
            if (_autosave >= AutosaveInterval)
            {
                _autosave = 0f;
                SaveSystem.Save(Model.Data);
            }
        }

        void HandleWorldClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (Ui.Modal.IsOpen) return;
            Vector2 sp = mouse.position.ReadValue();
            if (EventSystem.current != null && Ui.IsOverUi(sp)) return;
            if (!_cam.pixelRect.Contains(sp)) return;
            Vector3 world = _cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10f));
            world.z = 0f;
            if (!World.Nexus.Contains(world)) return;
            World.NotifyClickWorld(world);
            Model.Click();
        }

        // ───────────────────────── 저장/종료 ─────────────────────────

        void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(Model.Data); }
        void OnApplicationFocus(bool focus) { if (!focus && Model != null) SaveSystem.Save(Model.Data); }

        void OnApplicationQuit()
        {
            _quitting = true;
            if (Model != null) SaveSystem.Save(Model.Data);
        }

        public void SaveAndQuit()
        {
            SaveSystem.Save(Model.Data);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ResetSave()
        {
            SaveSystem.Delete();
            Model.Data.lastSaveUnix = 0;
            _quitting = true;
            // 씬을 다시 불러와 새 게임으로
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex >= 0 ? gameObject.scene.buildIndex : 0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // 씬 재시작(초기화) 시에는 저장하지 않는다
            if (!_quitting && Model != null && Application.isPlaying) SaveSystem.Save(Model.Data);
        }
    }
}
