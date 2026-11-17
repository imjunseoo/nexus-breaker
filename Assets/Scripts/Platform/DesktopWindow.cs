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

            // 현재 창의 아래쪽 가장자리를 기준으로 위로 늘이기/줄이기
            GetWindowRect(_hwnd, out RECT cur);
            RECT work = WorkArea();
            int x, y;
            if (_settings.windowX == int.MinValue || _settings.windowY == int.MinValue)
            {
                x = work.Right - w - 8;
                y = work.Bottom - h;
            }
            else
            {
                x = _settings.windowX;
                int bottom = cur.Bottom > cur.Top ? cur.Bottom : _settings.windowY + h;
                y = bottom - h;
            }
            // 작업 영역 안으로 클램프
            x = Mathf.Clamp(x, work.Left, Mathf.Max(work.Left, work.Right - w));
            y = Mathf.Clamp(y, work.Top, Mathf.Max(work.Top, work.Bottom - h));

            SetWindowLongPtr(_hwnd, GWL_STYLE, new IntPtr(WS_POPUP | WS_VISIBLE));
            ApplyExStyle(_settings.clickThrough && _clickThroughActive);

            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            if (_settings.transparentBackground) DwmExtendFrameIntoClientArea(_hwnd, ref margins);
            else { var zero = new MARGINS(); DwmExtendFrameIntoClientArea(_hwnd, ref zero); }

            IntPtr after = _settings.alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
            SetWindowPos(_hwnd, after, x, y, w, h, SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            _settings.windowX = x;
            _settings.windowY = y;
#endif
        }

        void Update()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (_pendingApply) Apply(_frameW, _frameH); // 시작 직후 창 핸들을 못 얻었으면 재시도
            if (_hwnd == IntPtr.Zero) return;

            // 시작 직후 Unity가 저장된 해상도로 창 크기를 다시 덮어쓰는 경우가 있어, 크기가 다르면 몇 번 재적용
            int scale = Mathf.Clamp(_settings.windowScale, 1, 6);
            if (!_dragging && (Screen.width != _frameW * scale || Screen.height != _frameH * scale)
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
                }
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
            if (_hwnd != IntPtr.Zero && GetWindowRect(_hwnd, out RECT r))
            {
                _settings.windowX = r.Left;
                _settings.windowY = r.Top;
            }
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

        static RECT WorkArea()
        {
            var r = new RECT();
            SystemParametersInfo(SPI_GETWORKAREA, 0, ref r, 0);
            return r;
        }

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
