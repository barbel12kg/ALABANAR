using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KlikAuto;

public sealed class MainForm : Form
{
    private const int HOTKEY_ID = 0x4A11;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_NOREPEAT = 0x4000;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020, MOUSEEVENTF_MIDDLEUP = 0x0040;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private readonly System.Windows.Forms.Timer clickTimer = new();
    private readonly ComboBox clickType = new();
    private readonly ComboBox positionMode = new();
    private readonly NumericUpDown interval = new();
    private readonly NumericUpDown clickLimit = new();
    private readonly CheckBox unlimited = new();
    private readonly TextBox hotkeyBox = new();
    private readonly Button hotkeyButton = new();
    private readonly Button startButton = new();
    private readonly Button stopButton = new();
    private readonly Label statusLabel = new();
    private readonly Label countLabel = new();
    private readonly Label coordLabel = new();
    private readonly System.Windows.Forms.Timer cursorTimer = new();
    private Keys toggleKey = Keys.F6;
    private bool capturingKey;
    private bool running;
    private long clickCount;
    private int fixedX, fixedY;

    public MainForm()
    {
        Text = "KlikAuto — Windows Auto Clicker";
        MinimumSize = new Size(540, 610);
        Size = new Size(600, 690);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 20, 27);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 0, AutoScroll = true };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var heading = new Label { Text = "KLIK AUTO", Font = new Font("Segoe UI", 21, FontStyle.Bold), ForeColor = Color.White, AutoSize = true };
        root.Controls.Add(heading);
        var subtitle = new Label { Text = "Simple • configurable • system-wide hotkey", ForeColor = Color.Silver, AutoSize = true, Margin = new Padding(0, 2, 0, 18) };
        root.Controls.Add(subtitle);

        AddSection(root, "CLICK SETTINGS");
        AddField(root, "Mouse button", clickType);
        clickType.DropDownStyle = ComboBoxStyle.DropDownList;
        clickType.Items.AddRange(new object[] { "Left click", "Right click", "Middle click" });
        clickType.SelectedIndex = 0;

        AddField(root, "Click position", positionMode);
        positionMode.DropDownStyle = ComboBoxStyle.DropDownList;
        positionMode.Items.AddRange(new object[] { "Follow cursor (current position)", "Fixed position (capture X/Y)" });
        positionMode.SelectedIndex = 0;
        positionMode.SelectedIndexChanged += (_, _) => UpdateCoordinateControls();

        AddField(root, "Interval (milliseconds)", interval);
        interval.Minimum = 1; interval.Maximum = 600000; interval.Value = 100; interval.Increment = 10;
        interval.Width = 160;

        AddField(root, "Maximum clicks (0 = unlimited)", clickLimit);
        clickLimit.Minimum = 0; clickLimit.Maximum = 10000000; clickLimit.Value = 0; clickLimit.Width = 160;

        unlimited.Text = "Unlimited clicks";
        unlimited.Checked = true;
        unlimited.AutoSize = true;
        unlimited.ForeColor = Color.Gainsboro;
        unlimited.CheckedChanged += (_, _) => clickLimit.Enabled = !unlimited.Checked;
        root.Controls.Add(unlimited);

        var coordPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 8) };
        coordLabel.Text = "Fixed position: not captured";
        coordLabel.ForeColor = Color.Silver;
        coordLabel.AutoSize = true;
        var capture = MakeButton("Capture current cursor", Color.FromArgb(55, 70, 94));
        capture.Click += (_, _) => CaptureCoordinates();
        coordPanel.Controls.Add(capture);
        coordPanel.Controls.Add(coordLabel);
        root.Controls.Add(coordPanel);

        AddSection(root, "TOGGLE HOTKEY");
        var hotkeyPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        hotkeyBox.ReadOnly = true; hotkeyBox.Text = "F6"; hotkeyBox.Width = 110; hotkeyBox.BackColor = Color.FromArgb(34, 38, 49); hotkeyBox.ForeColor = Color.White; hotkeyBox.BorderStyle = BorderStyle.FixedSingle;
        hotkeyButton.Text = "Change key";
        StyleButton(hotkeyButton, Color.FromArgb(55, 70, 94));
        hotkeyButton.Click += (_, _) => BeginHotkeyCapture();
        hotkeyPanel.Controls.Add(hotkeyBox); hotkeyPanel.Controls.Add(hotkeyButton);
        root.Controls.Add(hotkeyPanel);
        var hint = new Label { Text = "Press the chosen key anywhere in Windows to start/stop. Default: F6. F7 is always an emergency stop.", ForeColor = Color.Silver, AutoSize = true, MaximumSize = new Size(500, 0), Margin = new Padding(0, 7, 0, 12) };
        root.Controls.Add(hint);

        var buttons = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Top, Height = 52, Margin = new Padding(0, 12, 0, 10) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        startButton.Text = "START  (F6)";
        stopButton.Text = "STOP  (F7)";
        StyleButton(startButton, Color.FromArgb(27, 145, 91));
        StyleButton(stopButton, Color.FromArgb(174, 58, 65));
        startButton.Click += (_, _) => StartClicking();
        stopButton.Click += (_, _) => StopClicking();
        buttons.Controls.Add(startButton, 0, 0); buttons.Controls.Add(stopButton, 1, 0);
        root.Controls.Add(buttons);

        statusLabel.Text = "● STOPPED";
        statusLabel.ForeColor = Color.FromArgb(255, 130, 130);
        statusLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        statusLabel.AutoSize = true;
        root.Controls.Add(statusLabel);
        countLabel.Text = "Total clicks: 0";
        countLabel.ForeColor = Color.Gainsboro; countLabel.AutoSize = true;
        root.Controls.Add(countLabel);

        clickTimer.Tick += (_, _) => PerformClick();
        cursorTimer.Interval = 100;
        cursorTimer.Tick += (_, _) => RefreshHotkeyLabel();
        cursorTimer.Start();

        Shown += (_, _) => RegisterToggleHotkey();
        FormClosing += (_, _) => { StopClicking(); UnregisterHotKey(Handle, HOTKEY_ID); };
        UpdateCoordinateControls();
        UpdateButtonLabels();
    }

    private void AddSection(TableLayoutPanel root, string text)
    {
        var label = new Label { Text = text, ForeColor = Color.FromArgb(110, 175, 255), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 16, 0, 8) };
        root.Controls.Add(label);
    }

    private void AddField(TableLayoutPanel root, string labelText, Control control)
    {
        var panel = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 6) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        var label = new Label { Text = labelText, ForeColor = Color.Gainsboro, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 7, 0, 0) };
        control.Dock = DockStyle.Fill;
        if (control is ComboBox cb) { cb.BackColor = Color.FromArgb(34, 38, 49); cb.ForeColor = Color.White; cb.FlatStyle = FlatStyle.Flat; }
        if (control is NumericUpDown num) { num.BackColor = Color.FromArgb(34, 38, 49); num.ForeColor = Color.White; num.BorderStyle = BorderStyle.FixedSingle; }
        panel.Controls.Add(label, 0, 0); panel.Controls.Add(control, 1, 0);
        root.Controls.Add(panel);
    }

    private Button MakeButton(string text, Color color)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 34, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = color, Padding = new Padding(10, 4, 10, 4) };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private void StyleButton(Button b, Color color)
    {
        b.Dock = DockStyle.Fill; b.BackColor = color; b.ForeColor = Color.White; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Font = new Font("Segoe UI", 10, FontStyle.Bold); b.Margin = new Padding(4);
    }

    private void UpdateCoordinateControls()
    {
        bool fixedMode = positionMode.SelectedIndex == 1;
        coordLabel.Visible = fixedMode;
        foreach (Control c in coordLabel.Parent!.Controls)
            if (c is Button) c.Visible = fixedMode;
    }

    private void CaptureCoordinates()
    {
        if (GetCursorPos(out POINT p))
        {
            fixedX = p.X; fixedY = p.Y;
            coordLabel.Text = $"Captured: X={fixedX}, Y={fixedY}";
        }
    }

    private void BeginHotkeyCapture()
    {
        StopClicking();
        capturingKey = true;
        hotkeyBox.Text = "Press a key...";
        hotkeyButton.Text = "Waiting...";
        ActiveControl = null;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (capturingKey)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key != Keys.None && key != Keys.ControlKey && key != Keys.ShiftKey && key != Keys.Menu)
            {
                toggleKey = key;
                capturingKey = false;
                hotkeyBox.Text = toggleKey.ToString();
                hotkeyButton.Text = "Change key";
                UpdateButtonLabels();
                RegisterToggleHotkey();
            }
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void RegisterToggleHotkey()
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        if (!RegisterHotKey(Handle, HOTKEY_ID, MOD_NOREPEAT, (uint)toggleKey))
            MessageBox.Show($"Tidak bisa memakai {toggleKey}; mungkin sedang dipakai aplikasi lain. Pilih tombol lain.", "Hotkey tidak tersedia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            if (running) StopClicking(); else StartClicking();
        }
        base.WndProc(ref m);
    }

    private void UpdateButtonLabels() => startButton.Text = $"START  ({toggleKey})";

    private void StartClicking()
    {
        if (capturingKey) return;
        if (positionMode.SelectedIndex == 1 && fixedX == 0 && fixedY == 0)
        {
            MessageBox.Show("Klik 'Capture current cursor' terlebih dahulu untuk menyimpan posisi X/Y.", "Posisi belum diatur", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!unlimited.Checked && clickLimit.Value == 0) { StopClicking(); return; }
        clickCount = 0;
        countLabel.Text = "Total clicks: 0";
        clickTimer.Interval = (int)interval.Value;
        running = true;
        clickTimer.Start();
        statusLabel.Text = "● RUNNING";
        statusLabel.ForeColor = Color.FromArgb(95, 220, 155);
    }

    private void StopClicking()
    {
        clickTimer.Stop();
        running = false;
        statusLabel.Text = "● STOPPED";
        statusLabel.ForeColor = Color.FromArgb(255, 130, 130);
    }

    private void PerformClick()
    {
        int x, y;
        if (positionMode.SelectedIndex == 1) { x = fixedX; y = fixedY; }
        else if (GetCursorPos(out POINT p)) { x = p.X; y = p.Y; }
        else return;

        Cursor.Position = new Point(x, y);
        uint down, up;
        switch (clickType.SelectedIndex)
        {
            case 1: down = MOUSEEVENTF_RIGHTDOWN; up = MOUSEEVENTF_RIGHTUP; break;
            case 2: down = MOUSEEVENTF_MIDDLEDOWN; up = MOUSEEVENTF_MIDDLEUP; break;
            default: down = MOUSEEVENTF_LEFTDOWN; up = MOUSEEVENTF_LEFTUP; break;
        }
        mouse_event(down, 0, 0, 0, UIntPtr.Zero);
        mouse_event(up, 0, 0, 0, UIntPtr.Zero);
        clickCount++;
        countLabel.Text = $"Total clicks: {clickCount:N0}";
        if (!unlimited.Checked && clickCount >= (long)clickLimit.Value) StopClicking();
    }

    private void RefreshHotkeyLabel()
    {
        if (!running) return;
        // UI status remains responsive while the click timer runs.
    }
}
