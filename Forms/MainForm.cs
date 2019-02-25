namespace StudioAT.Utilitites.SnippingOCR
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;


    public partial class MainForm : Form
    {
        private bool _capturing;
        private IntPtr _clipboardViewerNext;
        private readonly Hotkey _hotkey = new Hotkey();
        private bool _isSnipping = false;

        public MainForm()
        {
            InitializeComponent();
            SnippingTool.AreaSelected += SnippingToolOnAreaSelected;
            SnippingTool.Cancel += SnippingToolOnCancel;

            WindowState = FormWindowState.Minimized;
            notifyIcon.Visible = true;


            mnuLanguageCombo.Items.AddRange((object[])Enum.GetNames(typeof(OcrLanguages)));
            mnuLanguageCombo.SelectedIndex = 0;

            mnuFormatImageCombo.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RegisterClipboardViewer();
            RegisterHotKey();
            ShowBaloonMessage("Double-click the systray icon or press CTRL+WIN+C to start a new snip...", "Snipping OCR");
            Hide();
        }

        private void RegisterClipboardViewer()
        {
            _clipboardViewerNext = Win32.User32.SetClipboardViewer(this.Handle);
        }

        private void UnregisterClipboardViewer()
        {
            Win32.User32.ChangeClipboardChain(this.Handle, _clipboardViewerNext);
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            StartSnipping();
        }

        private void RegisterHotKey()
        {
            _hotkey.KeyCode = Keys.C;
            _hotkey.Windows = true;
            _hotkey.Control = true;
            _hotkey.Pressed += Hk_Win_C_OnPressed;

            if (!_hotkey.GetCanRegister(this))
            {
                Console.WriteLine("Already registered");
            }
            else
            {
                _hotkey.Register(this);
            }
        }

        private void Hk_Win_C_OnPressed(object sender, HandledEventArgs handledEventArgs)
        {
            StartSnipping();
        }

        private void UnregisterHotkey()
        {
            if (_hotkey.Registered)
            {
                _hotkey.Unregister();
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch ((Win32.Msgs)m.Msg)
            {
                case Win32.Msgs.WM_DRAWCLIPBOARD:
                    OnClipboardData();
                    Win32.User32.SendMessage(_clipboardViewerNext, m.Msg, m.WParam, m.LParam);
                    break;
                case Win32.Msgs.WM_CHANGECBCHAIN:
                    Debug.WriteLine("WM_CHANGECBCHAIN: lParam: " + m.LParam, "WndProc");
                    if (m.WParam == _clipboardViewerNext)
                    {
                        _clipboardViewerNext = m.LParam;
                    }
                    else
                    {
                        Win32.User32.SendMessage(_clipboardViewerNext, m.Msg, m.WParam, m.LParam);
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        // Called when there is new data in clipboard
        public void OnClipboardData()
        {
            if (_capturing)
            {
                ProcessClipboardData();
            }
        }

        private void ProcessClipboardData()
        {
            if (Clipboard.ContainsImage())
            {
                ShowBaloonMessage("Processing clipboard image...", "OCR");
                int retries = 2;
                while (retries-- > 0)
                {
                    var img = Clipboard.GetImage();
                    if (img != null)
                    {
                        ProcessOcrImage(img);
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        private async void ProcessOcrImage(Image image)
        {
            var lang = (string)mnuLanguageCombo.SelectedItem;
            var imageFormat = (string)mnuFormatImageCombo.SelectedItem; 
            var result = await OCR.Process(image, (OcrLanguages)Enum.Parse(typeof(OcrLanguages), lang), (FormatImageCognitiveService)Enum.Parse(typeof(FormatImageCognitiveService), imageFormat));
            notifyIcon.Visible = true;
            if (this.mnuVisualizeResult.Checked)
            {
                OcrResultForm.ShowOcr(result);
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void Exit()
        {
            notifyIcon.Visible = false;
            notifyIcon = null;
            UnregisterClipboardViewer();
            UnregisterHotkey();
            Application.Exit();
            Environment.Exit(0);
        }

        private void mnuMonitorClipboard_Click(object sender, EventArgs e)
        {
            mnuMonitorClipboard.Checked = !mnuMonitorClipboard.Checked;
            if (mnuMonitorClipboard.Checked)
            {
                _capturing = true;
                OnClipboardData();
            }
            else
            {
                _capturing = false;
            }
        }

        private void ShowBaloonMessage(string text, string title)
        {
            notifyIcon.BalloonTipText = text;
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.ShowBalloonTip(1000);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            if (notifyIcon != null)
            {
                notifyIcon.Visible = true;
            }
        }

        private void mnuClipboardNow_Click(object sender, EventArgs e)
        {
            ProcessClipboardData();
        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
        }

        private void mnuSnip_Click(object sender, EventArgs e)
        {
            StartSnipping();
        }

        private void StartSnipping()
        {
            if (!_isSnipping)
            {
                _isSnipping = true;
                SnippingTool.Snip();
            }
        }

        private void SnippingToolOnCancel(object sender, EventArgs e)
        {
            _isSnipping = false;
        }

        private void SnippingToolOnAreaSelected(object sender, EventArgs e)
        {
            _isSnipping = false;
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                Clipboard.SetImage(SnippingTool.Image);
                ShowBaloonMessage("Copied to clipboard...", "OCR");
            }
            else
            {
                ShowBaloonMessage("Processing image...", "OCR");
                ProcessOcrImage(SnippingTool.Image);
            }
        }
    }
}
