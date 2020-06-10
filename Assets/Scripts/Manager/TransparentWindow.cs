using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
// using UnityEngine.UI;

public class TransparentWindow : MonoBehaviour
{
    public bool debugMode = false;
    [SerializeField]
    Material _material;

    [Header("Textures (Unsupported compression!)")]
    [SerializeField]
    Texture2D _enableTexture;
    [SerializeField]
    Texture2D _systemTrayTexture;
    Image _enableImage;
    Icon _systemTrayIcon;
    int _maxWidth = 0, _maxHeight = 0, _xOffset = 0, _yOffset = 0;
    Config _config;
    ObjPoolManager _pool;

    #region Win32

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    public static extern long GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    /// <summary>
    /// uFlags = 1：忽略大小；2：忽略位置；4：忽略Z顺序
    /// </summary>
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern int SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    const string UnityWindowClassName = "UnityWndClass";
    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const uint WS_POPUP = 0x80000000;
    const uint WS_VISIBLE = 0x10000000;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    const uint WS_EX_TOOLWINDOW = 0x00000080;//隐藏图标
    IntPtr HWND_BOTTOM = new IntPtr(1);
    IntPtr HWND_TOP = new IntPtr(0);
    IntPtr HWND_TOPMOST = new IntPtr(-1);
    IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    IntPtr _windowHandle = IntPtr.Zero;
    public IntPtr windowHandle
    {
        get
        {
            if (_windowHandle == IntPtr.Zero)
            {
                uint threadId = GetCurrentThreadId();
                EnumThreadWindows(threadId, (hWnd, lParam) =>
                {
                    var classText = new System.Text.StringBuilder(UnityWindowClassName.Length + 1);
                    GetClassName(hWnd, classText, classText.Capacity);
                    if (classText.ToString() == UnityWindowClassName)
                    {
                        _windowHandle = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            return _windowHandle;
        }
    }
    #endregion

    void Start()
    {
        _config = FindObjectOfType<Config>();
        _pool = FindObjectOfType<ObjPoolManager>();

        GetSystemInfo();
        if (!Application.isEditor)
        {
            Application.targetFrameRate = 60;

            LoadIconFile(Application.persistentDataPath);

            SetWindowStyle();

            AddSystemTray();

            StartCoroutine("AutoUpdate");
        }

        InitRole();



    }

    void GetSystemInfo()
    {
        // 获取多屏幕的总宽度和最高高度，以及任务栏offset
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            _maxWidth += screen.Bounds.Width;
            if (screen.Bounds.Height > _maxHeight)
            {
                _maxHeight = screen.Bounds.Height;
            }
            if (screen.Primary)
            {
                _xOffset = screen.WorkingArea.X;
                _yOffset = screen.WorkingArea.Y;
            }
        }

        if (_maxWidth == 0 || _maxHeight == 0)
            Debug.LogError("获取分辨率失败");
    }

    void SetWindowStyle()
    {
        SetWindowFullScreen();
        Invoke("SetWindowMinAABB", 8);

        if (!debugMode)
        {
            // Set properties of the window
            // See: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633591%28v=vs.85%29.aspx
            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);
            SetWindowLong(windowHandle, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT); // 实现鼠标穿透

            MARGINS margins = new MARGINS() { cxLeftWidth = -1 };
            // Extend the window into the client area
            //See: https://msdn.microsoft.com/en-us/library/windows/desktop/aa969512%28v=vs.85%29.aspx 
            DwmExtendFrameIntoClientArea(windowHandle, ref margins);
        }

        if (DataModel.Instance.Data.isTopMost)
        {
            SetWindowPos(windowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, 1 | 2);
            SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, 1 | 2);
        }
    }

    void InitRole()
    {
        // 实例化已启用的role
        foreach (var item in DataModel.Instance.Data.roles)
        {
            if (item.enable)
                _pool.AddRole(InstantiateRole(item.index), item.index);
        }
    }

    GameObject InstantiateRole(int roleIndex)
    {
        var data = DataModel.Instance.Data.roles[roleIndex];
        var go = GameObject.Instantiate(_config.roles[(int)roleIndex], data.rootPos, Quaternion.identity);
        var role = go.transform.GetComponentInChildren<RoleCtrlBase>();
        Debug.Assert(role != null, $"Role:{go.name} is missing {typeof(RoleCtrlBase)}");
        role.transform.position = data.rolePos;
        role.transform.rotation = data.roleRot;
        return go;
    }

    // 获取从Windows桌面空间转换到Unity屏幕空间的鼠标位置
    public Vector2Int GetMousePosW2U()
    {
        if (Application.isEditor)
            return new Vector2Int((int)Input.mousePosition.x, (int)Input.mousePosition.y);

        RECT rect = new RECT();
        GetWindowRect(windowHandle, ref rect);
        Vector2Int leftBottom = new Vector2Int(rect.Left, rect.Bottom);
        var mousePos = new Vector2Int(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
        leftBottom.y = _maxHeight - leftBottom.y;
        mousePos.y = _maxHeight - mousePos.y;

        return mousePos - leftBottom;
    }

    public void SetMousePenetrate(bool isPenetrate)
    {
        var s = GetWindowLong(windowHandle, GWL_EXSTYLE);
        if (isPenetrate)
        {
            SetWindowLong(windowHandle, GWL_EXSTYLE, (uint)(s | WS_EX_TRANSPARENT));
        }
        else
        {
            SetWindowLong(windowHandle, GWL_EXSTYLE, (uint)(s & ~WS_EX_TRANSPARENT));
        }
    }

    public void SetBottom(bool isBottom)
    {
        if (isBottom)
        {
            SetWindowPos(windowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, 1 | 2);
            SetWindowPos(windowHandle, HWND_BOTTOM, 0, 0, 0, 0, 1 | 2);
        }
        else
        {
            SetWindowPos(windowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, 1 | 2);
            if (DataModel.Instance.Data.isTopMost)
            {
                SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, 1 | 2);
            }
            else
            {
                SetWindowPos(windowHandle, HWND_TOP, 0, 0, 0, 0, 1 | 2);
            }
        }
    }

    public void SetWindowFullScreen()
    {
        SetWindowPos(windowHandle, IntPtr.Zero, _xOffset, _yOffset, _maxWidth, _maxHeight, 4);
    }
    public void SetWindowMinAABB()
    {
        var cam = Camera.main;
        var bound = _pool.GetMinAABB();
        Vector3 min = cam.WorldToScreenPoint(bound.min),// windows max
                max = cam.WorldToScreenPoint(bound.max);// windows min

        max.y = _maxHeight - max.y;
        min.y = _maxHeight - min.y;
        var trueMin = Vector3.Max(max, Vector3.zero);
        var trueMax = Vector3.Max(min, Vector3.one);

        var wh = trueMax - trueMin;
        float widthScale = wh.x / wh.y / cam.aspect;

        var fov = cam.fieldOfView;
        cam.fieldOfView = fov * (wh.y / _maxHeight);

        cam.transform.position = new Vector3(
            bound.center.x,
            bound.center.y,
            cam.transform.position.z
        );

        SetWindowPos(windowHandle, IntPtr.Zero,
        _xOffset + (int)trueMin.x,
        _yOffset + (int)trueMin.y,
        (int)wh.x,
        (int)wh.y,
        4);
    }

    #region 托盘

    SystemTray _icon;
    System.Windows.Forms.ToolStripItem _topmost, _runOnStart;
    System.Windows.Forms.ToolStripItem[] _roleItem;

    // 创建托盘图标、添加选项
    void AddSystemTray()
    {
        _icon = new SystemTray(_systemTrayIcon);
        _topmost = _icon.AddItem("置顶显示", ToggleTopMost);
        _runOnStart = _icon.AddItem("开机自启", ToggleRunOnStartup);
        _icon.AddItem("重置位置", ResetPos);
        _icon.AddSeparator();
        AddRoleItem(_icon);
        _icon.AddSeparator();
        _icon.AddItem("查看文档", OpenDoc);
        _icon.AddItem("检查更新", CheckUpdate);
        _icon.AddSeparator();
        _icon.AddItem("退出", Exit);
        _icon.AddDoubleClickEvent(ToggleTopMost);
        _icon.AddSingleClickEvent(ShowRole);

        _topmost.Image = DataModel.Instance.Data.isTopMost ? _enableImage : null;
        _runOnStart.Image = DataModel.Instance.Data.isRunOnStartup ? _enableImage : null;
    }

    //! 不支持压缩
    void LoadIconFile(string basePath)
    {
        string enableImagePath = basePath + "/Checkmark.png";
        string iconPath = basePath + "/Icon.png";

        File.WriteAllBytes(enableImagePath, _enableTexture.EncodeToPNG());
        _enableImage = Image.FromFile(enableImagePath);

        File.WriteAllBytes(iconPath, _systemTrayTexture.EncodeToPNG());
        _systemTrayIcon = Icon.FromHandle((new Bitmap(iconPath)).GetHicon());
    }

    void ToggleTopMost()
    {
        bool isTop = !DataModel.Instance.Data.isTopMost;
        DataModel.Instance.Data.isTopMost = isTop;
        DataModel.Instance.SaveData();
        if (_pool.IsAnyRoleEnable())
            SetWindowPos(windowHandle, isTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, 1 | 2);
        _topmost.Image = isTop ? _enableImage : null;
    }

    void ToggleRunOnStartup()
    {
        bool isRun = !DataModel.Instance.Data.isRunOnStartup;
        DataModel.Instance.Data.isRunOnStartup = isRun;
        DataModel.Instance.SaveData();
        _runOnStart.Image = isRun ? _enableImage : null;
        if (isRun)
        {
            Rainity.AddToStartup();
        }
        else
        {
            Rainity.RemoveFromStartup();
        }
    }

    void ToggleRole(int roleIndex)
    {
        if (_pool.rootPool[roleIndex] == null)
        {
            _pool.AddRole(InstantiateRole(roleIndex), roleIndex);
        }
        else
        {
            _pool.RemoveRole(roleIndex);
        }
        bool enable = _pool.rootPool[roleIndex] != null;
        _roleItem[roleIndex].Image = enable ? _enableImage : null;
        DataModel.Instance.Data.roles[roleIndex].enable = enable;
        DataModel.Instance.SaveData();
    }

    void AddRoleItem(SystemTray tray)
    {
        _roleItem = new System.Windows.Forms.ToolStripItem[_config.roles.Length];
        foreach (var item in DataModel.Instance.Data.roles)
        {
            var i = item.index;
            _roleItem[i] = tray.AddItem(((Roles)i).ToString(), () =>
             {
                 ToggleRole(i);
             });
            _roleItem[i].Image = item.enable ? _enableImage : null;
        }
    }

    void Exit()
    {
        _icon.Dispose();
        Application.Quit();
    }

    void ResetPos()
    {
        foreach (var role in _pool.rolePool)
        {
            if (role == null)
                continue;

            role.transform.parent.position = Vector3.zero;
            role.transform.position = Vector3.zero;
            role.transform.rotation = Quaternion.identity;
        }
        DataModel.Instance.Data.roles = null;

        DataModel.Instance.SaveData();
        DataModel.Instance.Init();
    }

    void OpenDoc()
    {
        Application.OpenURL("https://github.com/Jason-Ma-233/Sakura_DesktopMascot");
    }

    IEnumerator AutoUpdate()
    {
        yield return new WaitForSeconds(1);
        // 写入版本文件以供py读取
        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Ver.data", Application.version + "\n"
                                                                                   + Application.productName + ".exe");
        // 比较日期，大于一周则调用检查更新
        var lateUpdateDate = DateTime.FromFileTime(DataModel.Instance.Data.updateTime);
        var now = DateTime.Now;
        TimeSpan ts = now - lateUpdateDate;
        if (ts.Days >= 7)
        {
            CheckUpdate();
            DataModel.Instance.Data.updateTime = DateTime.Now.ToFileTime();
            DataModel.Instance.SaveData();
        }
    }

    void CheckUpdate()
    {
        System.Diagnostics.Process p = new System.Diagnostics.Process();
        p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Update.exe";
        p.StartInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory;
        p.Start();
    }

    void ShowRole()
    {
        if (DataModel.Instance.Data.isTopMost || !_pool.IsAnyRoleEnable())
            return;

        SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, 1 | 2);
        SetWindowPos(windowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, 1 | 2);
    }
    #endregion
}