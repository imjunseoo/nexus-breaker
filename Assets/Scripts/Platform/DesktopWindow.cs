using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// 데스크톱 아이들러용 창 제어(Windows 스탠드얼론 전용, 에디터에서는 아무것도 하지 않음).
    /// - 테두리 없는 창, 작업 영역 오른쪽 아래에 붙이기, 페이지 열림에 따라 위로 늘이기
    /// - 항상 위
    /// - 투명 배경(검정 컬러키 + DWM) / 빈 곳 클릭 통과(UI·넥서스 위에서만 클릭 받기)
    /// - 손잡이로 창 끌기
    /// </summary>
    public class DesktopWindow : MonoBehaviour
    {
        public static DesktopWindow Instance { get; private set; }

        /// <summary>커서가 "상호작용 가능한 것" 위에 있는지(화면 좌표, 왼쪽 아래 원점). GameRoot가 채운다.</summary>
        public Func<Vector2, bool> IsInteractiveAt;

        public bool Supported =>
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            true;
#else
            false;
#endif

        GameSettings _settings;
        int _frameW, _frameH;
        bool _pendingApply;
        float _lastApplyTime;
        int _reapplies;
        bool _placed; // 한 번 자리를 잡은 뒤에는 현재 창 위치를 기준으로 크기만 바꾼다
        bool _clickThroughActive;
        bool _dragging;
        Vector2Int _dragStartCursor, _dragStartWindow;

        public static DesktopWindow Create(GameSettings settings)
        {
            var go = new GameObject("DesktopWindow");
            var w = go.AddComponent<DesktopWindow>();
            w._settings = settings;
            Instance = w;
            return w;
        }

        /// <summary>프레임(내부 픽셀) 크기와 설정을 창에 반영.</summary>
        public void Apply(int frameW, int frameH)
        {
            if (frameW != _frameW || frameH != _frameH) _reapplies = 0;
            _frameW = frameW;
            _frameH = frameH;
            _lastApplyTime = Time.unscaledTime;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            EnsureHandle();
            if (_hwnd == IntPtr.Zero) { _pendingApply = true; return; }
            _pendingApply = false;

            int scale = Mathf.Clamp(_settings.windowScale, 1, 6);
            int w = frameW * scale, h = frameH * scale;

            // 위치 기준:
            //  - 한 번 자리를 잡은 뒤에는 "지금 창이 실제로 있는 곳"(왼쪽·아래쪽 가장자리)을 기준으로 크기만 바꾼다
            //    → 끌어서 옮긴 뒤 상점/설정을 열어도 제자리에서 위로 늘어난다
            //  - 처음 실행 때만 저장된 위치(없으면 작업 영역 오른쪽 아래)를 쓴다
            GetWindowRect(_hwnd, out RECT cur);
            int x, bottom;
            if (_placed && cur.Right > cur.Left && cur.Bottom > cur.Top)
            {
                x = cur.Left;
                bottom = cur.Bottom;
            }
            else if (_settings.windowX != int.MinValue && (_settings.windowBottom != int.MinValue || _settings.windowY != int.MinValue))
            {
                x = _settings.windowX;
                bottom = _settings.windowBottom != int.MinValue ? _settings.windowBottom : _settings.windowY + h;
            }
            else
            {
                RECT start = WorkAreaNear(cur.Left + (cur.Right - cur.Left) / 2, cur.Top + (cur.Bottom - cur.Top) / 2);
                x = start.Right - w - 8;
                bottom = start.Bottom;
            }
            int y = bottom - h;

            // 창이 있는 모니터(여러 대면 가장 가까운 모니터)의 작업 영역 안으로 클램프
            RECT work = WorkAreaNear(x + w / 2, bottom - 1);
            x = Mathf.Clamp(x, work.Left, Mathf.Max(work.Left, work.Right - w));
            y = Mathf.Clamp(y, work.Top, Mathf.Max(work.Top, work.Bottom - h));

            SetWindowLongPtr(_hwnd, GWL_STYLE, new IntPtr(WS_POPUP | WS_VISIBLE));
            ApplyExStyle(_settings.clickThrough && _clickThroughActive);

            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            if (_settings.transparentBackground) DwmExtendFrameIntoClientArea(_hwnd, ref margins);
            else { var zero = new MARGINS(); DwmExtendFrameIntoClientArea(_hwnd, ref zero); }

            IntPtr after = _settings.alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
            bool ok = SetWindowPos(_hwnd, after, x, y, w, h, SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            GetWindowRect(_hwnd, out RECT res);
            Debug.Log($"[Window] t={Time.unscaledTime:0.00} apply {w}x{h} @({x},{y}) placed={_placed} ok={ok} → rect ({res.Left},{res.Top})-({res.Right},{res.Bottom}) screen {Screen.width}x{Screen.height}");
            SavePosition(x, y, y + h);
#endif
        }

        void SavePosition(int left, int top, int bottom)
        {
            _settings.windowX = left;
            _settings.windowY = top;
            _settings.windowBottom = bottom;
        }

        void Update()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (_pendingApply) Apply(_frameW, _frameH); // 시작 직후 창 핸들을 못 얻었으면 재시도
            if (_hwnd == IntPtr.Zero) return;

            // 시작 직후 Unity가 저장된 해상도로 창 크기를 다시 덮어쓰는 경우가 있어, 크기가 다르면 몇 번 재적용
            int scale = Mathf.Clamp(_settings.windowScale, 1, 6);
            bool sizeOk = Screen.width == _frameW * scale && Screen.height == _frameH * scale;
            // 시작 직후 Unity가 창을 다시 잡는 동안은 저장 위치를 고수하고, 크기가 맞춰진 뒤부터 "현재 위치" 기준으로 전환
            if (sizeOk && !_placed && Time.unscaledTime - _lastApplyTime > 0.3f) _placed = true;
            if (!_dragging && !sizeOk
                && Time.unscaledTime - _lastApplyTime > 0.5f && _reapplies < 20)
            {
                _reapplies++;
                Apply(_frameW, _frameH);
            }

            if (_dragging)
            {
                if (GetCursorPos(out POINT p))
                {
                    int nx = _dragStartWindow.x + (p.X - _dragStartCursor.x);
                    int ny = _dragStartWindow.y + (p.Y - _dragStartCursor.y);
                    SetWindowPos(_hwnd, IntPtr.Zero, nx, ny, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                    // 끌기 끝 이벤트를 놓쳐도 위치가 남도록 끄는 동안 계속 저장
                    if (GetWindowRect(_hwnd, out RECT dr)) SavePosition(dr.Left, dr.Top, dr.Bottom);
                }
                // 마우스 버튼을 놓았는데 끌기 끝 이벤트가 안 왔으면 여기서 끝낸다
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null && !mouse.leftButton.isPressed) EndDrag();
                return;
            }

            // 클릭 통과: 커서가 UI/넥서스 위면 클릭을 받고, 빈 곳이면 아래 창으로 넘긴다
            if (_settings.clickThrough && IsInteractiveAt != null && GetCursorPos(out POINT cp))
            {
                var client = cp;
                ScreenToClient(_hwnd, ref client);
                var unityPos = new Vector2(client.X, Screen.height - client.Y);
                bool inside = client.X >= 0 && client.Y >= 0 && client.X < Screen.width && client.Y < Screen.height;
                bool wantThrough = !(inside && IsInteractiveAt(unityPos));
                if (wantThrough != _clickThroughActive)
                {
                    _clickThroughActive = wantThrough;
                    ApplyExStyle(wantThrough);
                }
            }
            else if (_clickThroughActive)
            {
                _clickThroughActive = false;
                ApplyExStyle(false);
            }
#endif
        }

        public void BeginDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (_hwnd == IntPtr.Zero || !GetCursorPos(out POINT p)) return;
            GetWindowRect(_hwnd, out RECT r);
            _dragStartCursor = new Vector2Int(p.X, p.Y);
            _dragStartWindow = new Vector2Int(r.Left, r.Top);
            _dragging = true;
#endif
        }

        public void UpdateDrag() { /* Update()에서 커서를 직접 추적한다 */ }

        public void EndDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            _dragging = false;
            if (_hwnd != IntPtr.Zero && GetWindowRect(_hwnd, out RECT r)) SavePosition(r.Left, r.Top, r.Bottom);
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr _hwnd;

        /// <summary>
        /// 이 프로세스의 Unity 창 핸들. GetActiveWindow는 창이 포커스를 잃으면 0을 돌려주므로
        /// 프로세스 ID로 최상위 창을 찾는다(클래스 UnityWndClass 우선).
        /// </summary>
        void EnsureHandle()
        {
            if (_hwnd != IntPtr.Zero) return;
            s_pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            s_found = s_fallback = IntPtr.Zero;
            EnumWindows(EnumCallback, IntPtr.Zero);
            _hwnd = s_found != IntPtr.Zero ? s_found : s_fallback;
            if (_hwnd == IntPtr.Zero) _hwnd = GetActiveWindow();
        }

        // IL2CPP에서도 동작하도록 정적 콜백 + 정적 상태
        static uint s_pid;
        static IntPtr s_found, s_fallback;

        [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
        static bool EnumCallback(IntPtr h, IntPtr lParam)
        {
            GetWindowThreadProcessId(h, out uint wpid);
            if (wpid != s_pid || !IsWindowVisible(h)) return true;
            var cls = new System.Text.StringBuilder(64);
            GetClassName(h, cls, cls.Capacity);
            if (cls.ToString() == "UnityWndClass") { s_found = h; return false; }
            if (s_fallback == IntPtr.Zero) s_fallback = h;
            return true;
        }

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder name, int max);

        void ApplyExStyle(bool clickThrough)
        {
            long ex = WS_EX_LAYERED;
            if (clickThrough) ex |= WS_EX_TRANSPARENT;
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(ex));
            // 투명 모드: 순수 검정(0,0,0)을 컬러키로 → 투명 + 클릭 통과. 아니면 불투명.
            if (_settings.transparentBackground) SetLayeredWindowAttributes(_hwnd, 0, 255, LWA_COLORKEY);
            else SetLayeredWindowAttributes(_hwnd, 0, 255, LWA_ALPHA);
        }

        /// <summary>점(화면 좌표)이 있는 모니터의 작업 영역(작업 표시줄 제외). 모니터 밖이면 가장 가까운 모니터.</summary>
        static RECT WorkAreaNear(int x, int y)
        {
            IntPtr mon = MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (mon != IntPtr.Zero && GetMonitorInfo(mon, ref mi)) return mi.rcWork;
            var r = new RECT();
            SystemParametersInfo(SPI_GETWORKAREA, 0, ref r, 0); // 실패 시 주 모니터
            return r;
        }

        const uint MONITOR_DEFAULTTONEAREST = 2;
        [StructLayout(LayoutKind.Sequential)] struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
        [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO mi);

        const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
        const long WS_POPUP = 0x80000000L, WS_VISIBLE = 0x10000000L;
        const long WS_EX_LAYERED = 0x00080000L, WS_EX_TRANSPARENT = 0x00000020L;
        const uint LWA_COLORKEY = 0x1, LWA_ALPHA = 0x2;
        const uint SWP_NOSIZE = 0x1, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10, SWP_FRAMECHANGED = 0x20, SWP_SHOWWINDOW = 0x40;
        const uint SPI_GETWORKAREA = 0x30;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1), HWND_NOTOPMOST = new IntPtr(-2);

        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct MARGINS { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight; }

        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action, uint param, ref RECT r, uint winIni);
        [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
#endif
    }
}
