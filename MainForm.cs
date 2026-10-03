using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

namespace Metin2Macro;

public sealed class MainForm : Form
{
    private NotifyIcon tray = null!;
    private System.Windows.Forms.Timer f3Timer = null!;
    private System.Windows.Forms.Timer f4Timer = null!;

    private NumericUpDown f3Interval = null!;
    private NumericUpDown f4Interval = null!;

    private Label statusLabel = null!;
    private Label statusDot = null!;

    private NativeMethods.LowLevelKeyboardProc? keyboardProc;
    private IntPtr keyboardHook = IntPtr.Zero;

    private bool enabled;
    private bool exiting;
    private bool rWasPressed;

    private const int HeaderHeight = 48;

    // ============================================================
    // KOLORY
    // ============================================================

    private readonly Color BackgroundColor =
        Color.FromArgb(18, 19, 24);

    private readonly Color HeaderColor =
        Color.FromArgb(22, 23, 29);

    private readonly Color PanelColor =
        Color.FromArgb(27, 29, 36);

    private readonly Color PanelLightColor =
        Color.FromArgb(34, 36, 45);

    private readonly Color TextColor =
        Color.FromArgb(235, 237, 242);

    private readonly Color MutedColor =
        Color.FromArgb(145, 149, 160);

    private readonly Color AccentColor =
        Color.FromArgb(83, 214, 137);

    private readonly Color RedColor =
        Color.FromArgb(210, 65, 65);

    private readonly Color BlueColor =
        Color.FromArgb(45, 125, 220);

    public MainForm()
    {
        Text = "Metin2 Macro";

        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        
        // ========================================================
        // ROZMIAR OKNA
        // ========================================================

        ClientSize = new Size(520, 384);

        MinimumSize = new Size(520, 384);
        MaximumSize = new Size(520, 384);

        StartPosition =
            FormStartPosition.CenterScreen;

        FormBorderStyle =
            FormBorderStyle.None;

        BackColor =
            BackgroundColor;

        DoubleBuffered = true;

        // Normalna minimalizacja -> pasek Windows
        ShowInTaskbar = true;

        // ========================================================
        // INTERFEJS
        // ========================================================

        BuildInterface();

        // ========================================================
        // TIMER F3
        // ========================================================

        f3Timer =
            new System.Windows.Forms.Timer();

        f3Timer.Tick += (_, _) =>
        {
            NativeMethods.TapKey(
                NativeMethods.VK_F3);
        };

        // ========================================================
        // TIMER F4
        // ========================================================

        f4Timer =
            new System.Windows.Forms.Timer();

        f4Timer.Tick += (_, _) =>
        {
            NativeMethods.TapKey(
                NativeMethods.VK_F4);
        };

        // ========================================================
        // TRAY
        // ========================================================

        tray = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),

            Text = "Metin2 Macro — OFF",
            Visible = true
        };

        var menu =
            new ContextMenuStrip();

        menu.Items.Add(
            "Pokaż",
            null,
            (_, _) => ShowFromTray());

        menu.Items.Add(
            "ON / OFF",
            null,
            (_, _) => Toggle());

        menu.Items.Add(
            "Wyjście",
            null,
            (_, _) => ExitApplication());

        tray.ContextMenuStrip = menu;

        tray.DoubleClick += (_, _) =>
        {
            ShowFromTray();
        };

        // ========================================================
        // ZAMKNIĘCIE
        // ========================================================

        FormClosing += (_, _) =>
        {
            if (!exiting)
            {
                exiting = true;

                StopMacro();

                RemoveKeyboardHook();

                tray.Visible = false;
                tray.Dispose();
            }
        };

        // ========================================================
        // START GLOBALNEGO HOOKA
        // ========================================================

        Shown += (_, _) =>
        {
            InstallKeyboardHook();
        };
    }

    // ============================================================
    // BUDOWANIE INTERFEJSU
    // ============================================================

    private void BuildInterface()
    {
        // ========================================================
        // HEADER
        // ========================================================

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = HeaderHeight,
            BackColor = HeaderColor
        };

        // LOGO

        var logo = new Label
        {
            Text = "M2",

            ForeColor = AccentColor,

            Font = new Font(
                "Segoe UI",
                11,
                FontStyle.Bold),

            Location =
                new Point(20, 14),

            AutoSize = true
        };

        // NAZWA

        var title = new Label
        {
            Text = "METIN2 MACRO",

            ForeColor = TextColor,

            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Bold),

            Location =
                new Point(58, 15),

            AutoSize = true
        };

        // ========================================================
        // MINIMALIZACJA
        // ========================================================

        var minimizeButton =
            CreateHeaderButton(
                "—",
                BlueColor,
                8f);

        minimizeButton.Location = new Point(424, 0);
        minimizeButton.Size = new Size(48, 48);

        minimizeButton.Click += (_, _) =>
        {
            // Normalna minimalizacja Windows.
            //
            // Timery NIE są zatrzymywane.
            // Makro nadal działa.

            WindowState =
                FormWindowState.Minimized;
        };

        // ========================================================
        // ZAMKNIĘCIE
        // ========================================================

        var closeButton =
            CreateHeaderButton(
                "×",
                RedColor,
                14f);

        closeButton.Location = new Point(472, 0);
        closeButton.Size = new Size(48, 48);

        closeButton.Click += (_, _) =>
        {
            ExitApplication();
        };

        header.Controls.Add(logo);
        header.Controls.Add(title);
        header.Controls.Add(minimizeButton);
        header.Controls.Add(closeButton);

        header.MouseDown += HeaderMouseDown;
        logo.MouseDown += HeaderMouseDown;
        title.MouseDown += HeaderMouseDown;

        Controls.Add(header);

        // ========================================================
        // CONTENT
        // ========================================================

        var content = new Panel
        {
            Location = new Point(0, HeaderHeight),
            Size = new Size(520, 336),
            BackColor = BackgroundColor
        };

        // ========================================================
        // STATUS
        // ========================================================

        var statusPanel = new Panel
        {
            Location =
                new Point(35, 22),

            Size =
                new Size(450, 68),

            BackColor =
                PanelColor
        };

        statusDot = new Label
        {
            Text = "●",

            ForeColor =
                RedColor,

            Font = new Font(
                "Segoe UI",
                21,
                FontStyle.Bold),

            Location =
                new Point(20, 17),

            AutoSize = true
        };

        statusLabel = new Label
        {
            Text = "MACRO INACTIVE",

            ForeColor =
                TextColor,

            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Bold),

            Location =
                new Point(58, 14),

            AutoSize = true
        };

        var statusHint = new Label
        {
            Text = "Press R to toggle",

            ForeColor =
                MutedColor,

            Font = new Font(
                "Segoe UI",
                8),

            Location =
                new Point(59, 37),

            AutoSize = true
        };

        statusPanel.Controls.Add(statusDot);
        statusPanel.Controls.Add(statusLabel);
        statusPanel.Controls.Add(statusHint);

        // ========================================================
        // F3
        // ========================================================

        var f3Card =
            CreateIntervalCard(
                "F3",
                1500,
                out f3Interval);

        f3Card.Location =
            new Point(35, 108);

        // ========================================================
        // F4
        // ========================================================

        var f4Card =
            CreateIntervalCard(
                "F4",
                500,
                out f4Interval);

        f4Card.Location =
            new Point(267, 108);

        // ========================================================
        // HOTKEY
        // ========================================================

        var hotkeyLabel = new Label
        {
            Text = "HOTKEY",

            ForeColor = MutedColor,

            Font = new Font(
                "Segoe UI",
                8,
                FontStyle.Bold),

            Location = new Point(35, 193),

            Size = new Size(450, 18),

            TextAlign =
                ContentAlignment.MiddleCenter
        };

        var hotkeyBox = new Panel
        {
            Location = new Point(35, 215),

            Size =
                new Size(450, 44),

            BackColor =
                PanelColor
        };

        var hotkeyName = new Label
        {
            Text = "Toggle macro",

            ForeColor =
                TextColor,

            Font = new Font(
                "Segoe UI",
                9),

            Location =
                new Point(15, 12),

            AutoSize = true
        };

        var hotkey = new Label
        {
            Text = "R",

            ForeColor =
                AccentColor,

            BackColor =
                PanelLightColor,

            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold),

            TextAlign =
                ContentAlignment.MiddleCenter,

            Location =
                new Point(390, 6),

            Size =
                new Size(45, 32)
        };

        hotkeyBox.Controls.Add(hotkeyName);
        hotkeyBox.Controls.Add(hotkey);

        // ========================================================
        // START / STOP
        // ========================================================

        var startButton = new Button
        {
            Text = "START / STOP",

            Location = new Point(35, 269),

            Size =
                new Size(218, 45),

            FlatStyle =
                FlatStyle.Flat,

            BackColor =
                PanelLightColor,

            ForeColor =
                TextColor,

            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold),

            Cursor =
                Cursors.Hand
        };

        startButton.FlatAppearance.BorderSize = 0;

        startButton.MouseEnter += (_, _) =>
        {
            startButton.BackColor =
                Color.FromArgb(42, 45, 55);
        };

        startButton.MouseLeave += (_, _) =>
        {
            startButton.BackColor =
                PanelLightColor;
        };

        startButton.Click += (_, _) =>
        {
            Toggle();
        };

        // ========================================================
        // MINIMIZE TO TRAY
        // ========================================================

        var trayButton = new Button
        {
            Text = "MINIMIZE TO TRAY",

            Location = new Point(267, 269),

            Size =
                new Size(218, 45),

            FlatStyle =
                FlatStyle.Flat,

            BackColor =
                PanelLightColor,

            ForeColor =
                TextColor,

            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold),

            Cursor =
                Cursors.Hand
        };

        trayButton.FlatAppearance.BorderSize = 0;

        trayButton.MouseEnter += (_, _) =>
        {
            trayButton.BackColor =
                Color.FromArgb(42, 45, 55);
        };

        trayButton.MouseLeave += (_, _) =>
        {
            trayButton.BackColor =
                PanelLightColor;
        };

        trayButton.Click += (_, _) =>
        {
            HideToTray();
        };

        // ========================================================
        // DODANIE KONTROLEK
        // ========================================================

        content.Controls.Add(statusPanel);

        content.Controls.Add(f3Card);
        content.Controls.Add(f4Card);

        content.Controls.Add(hotkeyLabel);
        content.Controls.Add(hotkeyBox);

        content.Controls.Add(startButton);
        content.Controls.Add(trayButton);

        Controls.Add(content);
        content.BringToFront();
        header.BringToFront();
    }

    // ============================================================
    // KARTA F3 / F4
    // ============================================================

    private Panel CreateIntervalCard(
        string key,
        int value,
        out NumericUpDown numeric)
    {
        var card = new Panel
        {
            Size = new Size(218, 75),

            BackColor =
                PanelColor
        };

        var keyLabel = new Label
        {
            Text = key,
            ForeColor = AccentColor,
            Font = new Font(
                "Segoe UI",
                15,
                FontStyle.Bold),
            Location = new Point(15, 8),
            AutoSize = true
        };

        var captionLabel = new Label
        {
            Text = "INTERVAL",
            ForeColor = MutedColor,
            Font = new Font(
                "Segoe UI",
                7,
                FontStyle.Bold),
            Location = new Point(18, 38),
            AutoSize = true
        };

        numeric = new NumericUpDown
        {
            Minimum = 50,
            Maximum = 60000,
            Value = value,

            Location = new Point(106, 17),

            Size = new Size(95, 32),

            BackColor = PanelLightColor,
            ForeColor = TextColor,

            BorderStyle = BorderStyle.FixedSingle,

            Font = new Font(
                "Segoe UI",
                9),

            TextAlign =
                HorizontalAlignment.Center
        };

        card.Controls.Add(keyLabel);
        card.Controls.Add(captionLabel);
        card.Controls.Add(numeric);

        return card;
    }

    // ============================================================
    // PRZYCISK HEADER
    // ============================================================

    private Button CreateHeaderButton(
        string textValue,
        Color backgroundColor,
        float fontSize)
    {
        var button = new Button
        {
            FlatStyle = FlatStyle.Flat,
            BackColor = backgroundColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Text = ""
        };

        button.FlatAppearance.BorderSize = 0;

        var symbol = new Label
        {
            Text = textValue,

            ForeColor = Color.White,
            BackColor = Color.Transparent,

            Font = new Font(
                "Segoe UI",
                fontSize,
                FontStyle.Regular),

            TextAlign =
                ContentAlignment.MiddleCenter,

            Dock = DockStyle.Fill,

            AutoSize = false,

            Cursor = Cursors.Hand
        };
        
        symbol.Click += (_, _) =>
        {
            button.PerformClick();
        };

        symbol.MouseEnter += (_, _) =>
        {
            button.BackColor = backgroundColor;
        };

        button.Controls.Add(symbol);

        return button;
    }
    // ============================================================
    // PRZESUWANIE OKNA
    // ============================================================

    private void HeaderMouseDown(
        object? sender,
        MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();

        SendMessage(
            Handle,
            WM_NCLBUTTONDOWN,
            HTCAPTION,
            0);
    }

    private const int WM_NCLBUTTONDOWN =
        0x00A1;

    private const int HTCAPTION =
        0x0002;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr hWnd,
        int msg,
        int wParam,
        int lParam);

    // ============================================================
    // GLOBALNY HOOK KLAWIATURY
    // ============================================================

    private void InstallKeyboardHook()
    {
        keyboardProc =
            KeyboardHookCallback;

        using var process =
            System.Diagnostics.Process
                .GetCurrentProcess();

        using var module =
            process.MainModule;

        keyboardHook =
            NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                keyboardProc,
                NativeMethods.GetModuleHandle(
                    module?.ModuleName),
                0);

        if (keyboardHook ==
            IntPtr.Zero)
        {
            MessageBox.Show(
                "Nie udało się uruchomić globalnego hooka klawiatury.",
                "Metin2 Macro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // ============================================================
    // OBSŁUGA R
    // ============================================================

    private IntPtr KeyboardHookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode >= 0)
        {
            bool keyDown =
                wParam ==
                    (IntPtr)
                    NativeMethods.WM_KEYDOWN
                ||
                wParam ==
                    (IntPtr)
                    NativeMethods.WM_SYSKEYDOWN;

            bool keyUp =
                wParam ==
                    (IntPtr)
                    NativeMethods.WM_KEYUP
                ||
                wParam ==
                    (IntPtr)
                    NativeMethods.WM_SYSKEYUP;

            var data =
                Marshal.PtrToStructure<
                    NativeMethods.KBDLLHOOKSTRUCT>(
                    lParam);

            if (keyDown &&
                data.vkCode ==
                    NativeMethods.VK_R)
            {
                if (!rWasPressed)
                {
                    rWasPressed = true;

                    BeginInvoke(
                        Toggle);
                }
            }

            if (keyUp &&
                data.vkCode ==
                    NativeMethods.VK_R)
            {
                rWasPressed = false;
            }
        }

        // R nie jest blokowany.
        return NativeMethods.CallNextHookEx(
            keyboardHook,
            nCode,
            wParam,
            lParam);
    }

    // ============================================================
    // TOGGLE
    // ============================================================

    private void Toggle()
    {
        if (IsDisposed ||
            !IsHandleCreated)
        {
            return;
        }

        enabled = !enabled;

        f3Timer.Interval =
            (int)f3Interval.Value;

        f4Timer.Interval =
            (int)f4Interval.Value;

        if (enabled)
        {
            f3Timer.Start();
            f4Timer.Start();

            statusDot.ForeColor =
                AccentColor;

            statusLabel.Text =
                "MACRO ACTIVE";

            tray.Text =
                "Metin2 Macro — ON";
        }
        else
        {
            StopMacro();
        }
    }

    // ============================================================
    // STOP
    // ============================================================

    private void StopMacro()
    {
        enabled = false;

        f3Timer.Stop();
        f4Timer.Stop();

        statusDot.ForeColor =
            RedColor;

        statusLabel.Text =
            "MACRO INACTIVE";

        tray.Text =
            "Metin2 Macro — OFF";
    }

    // ============================================================
    // MINIMALIZACJA DO TRAYA
    // ============================================================

    private void HideToTray()
    {
        // Tylko dedykowany przycisk
        // ukrywa aplikację z paska.

        Hide();

        // Makro nadal działa.
    }

    private void ShowFromTray()
    {
        Show();

        WindowState =
            FormWindowState.Normal;

        Activate();
    }

    // ============================================================
    // USUNIĘCIE HOOKA
    // ============================================================

    private void RemoveKeyboardHook()
    {
        if (keyboardHook !=
            IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(
                keyboardHook);

            keyboardHook =
                IntPtr.Zero;
        }
    }

    // ============================================================
    // ZAMKNIĘCIE APLIKACJI
    // ============================================================

    private void ExitApplication()
    {
        exiting = true;

        StopMacro();

        RemoveKeyboardHook();

        tray.Visible = false;

        tray.Dispose();

        Application.Exit();
    }

    // ============================================================
    // DISPOSE
    // ============================================================

    protected override void Dispose(
        bool disposing)
    {
        if (disposing)
        {
            RemoveKeyboardHook();

            tray?.Dispose();

            f3Timer?.Dispose();
            f4Timer?.Dispose();
        }

        base.Dispose(disposing);
    }

    // ============================================================
    // RAMKA OKNA
    // ============================================================

    protected override void OnPaint(
        PaintEventArgs e)
    {
        base.OnPaint(e);

        using var pen =
            new Pen(
                Color.FromArgb(
                    45,
                    47,
                    56),
                1);

        e.Graphics.DrawRectangle(
            pen,
            0,
            0,
            ClientSize.Width - 1,
            ClientSize.Height - 1);
    }
}