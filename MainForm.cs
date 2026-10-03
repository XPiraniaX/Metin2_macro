using System;
using System.Drawing;
using System.Windows.Forms;

namespace Metin2Macro;

public sealed class MainForm : Form
{
    private const int HotkeyId = 9001;

    private readonly NotifyIcon tray;
    private readonly System.Windows.Forms.Timer f3Timer;
    private readonly System.Windows.Forms.Timer f4Timer;
    private readonly NumericUpDown f3Interval;
    private readonly NumericUpDown f4Interval;
    private readonly Label status;
    private bool enabled;
    private bool exiting;

    public MainForm()
    {
        Text = "Metin2 Macro";
        ClientSize = new Size(420, 270);
        MinimumSize = new Size(420, 270);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        status = new Label
        {
            Text = "● OFF",
            Dock = DockStyle.Top,
            Height = 55,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 18, FontStyle.Bold)
        };

        var info = new Label
        {
            Text = "R — globalne ON / OFF   |   działa w tle",
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var settings = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 80,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(35, 5, 35, 5)
        };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        settings.Controls.Add(new Label
        {
            Text = "F3 — interwał (ms)",
            Anchor = AnchorStyles.Left,
            AutoSize = true
        }, 0, 0);

        f3Interval = new NumericUpDown
        {
            Minimum = 50,
            Maximum = 60000,
            Value = 1500,
            Dock = DockStyle.Fill
        };
        settings.Controls.Add(f3Interval, 1, 0);

        settings.Controls.Add(new Label
        {
            Text = "F4 — interwał (ms)",
            Anchor = AnchorStyles.Left,
            AutoSize = true
        }, 0, 1);

        f4Interval = new NumericUpDown
        {
            Minimum = 50,
            Maximum = 60000,
            Value = 500,
            Dock = DockStyle.Fill
        };
        settings.Controls.Add(f4Interval, 1, 1);

        var button = new Button
        {
            Text = "START / STOP",
            Dock = DockStyle.Top,
            Height = 42
        };
        button.Click += (_, _) => Toggle();

        var trayButton = new Button
        {
            Text = "Minimalizuj do traya",
            Dock = DockStyle.Top,
            Height = 34
        };
        trayButton.Click += (_, _) => HideToTray();

        Controls.Add(trayButton);
        Controls.Add(button);
        Controls.Add(settings);
        Controls.Add(info);
        Controls.Add(status);

        f3Timer = new System.Windows.Forms.Timer();
        f3Timer.Tick += (_, _) => NativeMethods.TapKey(NativeMethods.VK_F3);

        f4Timer = new System.Windows.Forms.Timer();
        f4Timer.Tick += (_, _) => NativeMethods.TapKey(NativeMethods.VK_F4);

        tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Metin2 Macro — OFF",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Pokaż", null, (_, _) => ShowFromTray());
        menu.Items.Add("ON / OFF", null, (_, _) => Toggle());
        menu.Items.Add("Wyjście", null, (_, _) => ExitApplication());
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => ShowFromTray();

        FormClosing += (_, e) =>
        {
            if (!exiting)
            {
                e.Cancel = true;
                HideToTray();
            }
        };

        Shown += (_, _) =>
        {
            if (!NativeMethods.RegisterHotKey(
                    Handle,
                    HotkeyId,
                    NativeMethods.MOD_NOREPEAT,
                    NativeMethods.VK_R))
            {
                MessageBox.Show(
                    "Nie udało się przejąć globalnego R. Inna aplikacja może używać tego skrótu.",
                    "Metin2 Macro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY &&
            m.WParam.ToInt32() == HotkeyId)
        {
            Toggle();
        }

        base.WndProc(ref m);
    }

    private void Toggle()
    {
        enabled = !enabled;

        f3Timer.Interval = (int)f3Interval.Value;
        f4Timer.Interval = (int)f4Interval.Value;

        if (enabled)
        {
            f3Timer.Start();
            f4Timer.Start();
            status.Text = "● ON";
            tray.Text = "Metin2 Macro — ON";
        }
        else
        {
            f3Timer.Stop();
            f4Timer.Stop();
            status.Text = "● OFF";
            tray.Text = "Metin2 Macro — OFF";
        }
    }

    private void HideToTray()
    {
        Hide();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        exiting = true;
        f3Timer.Stop();
        f4Timer.Stop();
        NativeMethods.UnregisterHotKey(Handle, HotkeyId);
        tray.Visible = false;
        tray.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (IsHandleCreated)
                NativeMethods.UnregisterHotKey(Handle, HotkeyId);

            tray?.Dispose();
            f3Timer?.Dispose();
            f4Timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
