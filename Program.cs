using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioPlaybackApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private ComboBox? comboInput;
        private ComboBox? comboOutput;
        private Button? btnStartStop;
        private TrackBar? volumeSlider;
        private Label? lblVolume;
        private Label? lblInput;
        private Label? lblOutput;
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;

        private WaveIn? _waveIn;
        private WaveOut? _waveOut;
        private bool _isMonitoring = false;
        private float _volume = 1.0f;

        public MainForm()
        {
            InitializeComponent();
            LoadDevices();
        }

        private void InitializeComponent()
        {
            this.Text = "Audio Monitor (Low Latency)";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            lblInput = new Label { Text = "Input Device:", Location = new Point(20, 20), Size = new Size(100, 20) };
            comboInput = new ComboBox { Location = new Point(20, 40), Size = new Size(340, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            lblOutput = new Label { Text = "Output Device:", Location = new Point(20, 80), Size = new Size(100, 20) };
            comboOutput = new ComboBox { Location = new Point(20, 100), Size = new Size(340, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            lblVolume = new Label { Text = "Volume: 100%", Location = new Point(20, 140), Size = new Size(100, 20) };
            volumeSlider = new TrackBar { Location = new Point(20, 160), Size = new Size(340, 45), Minimum = 0, Maximum = 100, Value = 100 };
            volumeSlider.ValueChanged += (s, e) => {
                _volume = volumeSlider.Value / 100f;
                lblVolume.Text = $"Volume: {volumeSlider.Value}%";
            };

            btnStartStop = new Button { Text = "Start Monitoring", Location = new Point(20, 210), Size = new Size(340, 40) };
            btnStartStop.Click += BtnStartStop_Click;

            // System Tray Setup
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Restore", null, (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            });
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            trayIcon = new NotifyIcon
            {
                Text = "Audio Monitor",
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };

            this.Controls.AddRange(new Control[] { lblInput, comboInput, lblOutput, comboOutput, lblVolume, volumeSlider, btnStartStop });
        }

        protected override void OnResize(EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon?.ShowBalloonTip(2000, "Audio Monitor", "Running in background", ToolTipIcon.Info);
            }
            base.OnResize(e);
        }

        private void LoadDevices()
        {
            if (comboInput == null || comboOutput == null) return;
            comboInput.Items.Clear();
            int inDevs = WinMM.waveInGetNumDevs();
            for (int i = 0; i < inDevs; i++)
            {
                WinMM.WAVEINCAPS caps = new WinMM.WAVEINCAPS();
                WinMM.waveInGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
                comboInput.Items.Add(caps.szPname);
            }
            if (comboInput.Items.Count > 0) comboInput.SelectedIndex = 0;

            comboOutput.Items.Clear();
            int outDevs = WinMM.waveOutGetNumDevs();
            for (int i = 0; i < outDevs; i++)
            {
                WinMM.WAVEOUTCAPS caps = new WinMM.WAVEOUTCAPS();
                WinMM.waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
                comboOutput.Items.Add(caps.szPname);
            }
            if (comboOutput.Items.Count > 0) comboOutput.SelectedIndex = 0;
        }

        private void BtnStartStop_Click(object? sender, EventArgs e)
        {
            if (!_isMonitoring)
            {
                StartMonitoring();
            }
            else
            {
                StopMonitoring();
            }
        }

        private void StartMonitoring()
        {
            try
            {
                if (comboInput == null || comboOutput == null || btnStartStop == null) return;
                int inputIndex = comboInput.SelectedIndex;
                int outputIndex = comboOutput.SelectedIndex;

                if (inputIndex < 0 || outputIndex < 0) return;

                _waveOut = new WaveOut(outputIndex, 44100, 1);
                _waveIn = new WaveIn(inputIndex, 44100, 1);
                
                _waveIn.DataAvailable += (data) => {
                    // Apply volume
                    for (int i = 0; i < data.Length / 2; i++)
                    {
                        short sample = BitConverter.ToInt16(data, i * 2);
                        short adjusted = (short)Math.Clamp(sample * _volume, short.MinValue, short.MaxValue);
                        byte[] bytes = BitConverter.GetBytes(adjusted);
                        data[i * 2] = bytes[0];
                        data[i * 2 + 1] = bytes[1];
                    }
                    _waveOut?.Play(data);
                };

                _waveIn.Start();
                _isMonitoring = true;
                btnStartStop.Text = "Stop Monitoring";
                comboInput.Enabled = false;
                comboOutput.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting monitoring: " + ex.Message);
                StopMonitoring();
            }
        }

        private void StopMonitoring()
        {
            _waveIn?.Stop();
            _waveIn?.Dispose();
            _waveIn = null;

            _waveOut?.Dispose();
            _waveOut = null;

            _isMonitoring = false;
            if (btnStartStop != null) btnStartStop.Text = "Start Monitoring";
            if (comboInput != null) comboInput.Enabled = true;
            if (comboOutput != null) comboOutput.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopMonitoring();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// A simple wrapper for Windows waveIn (audio capture) APIs.
    /// </summary>
    public class WaveIn : IDisposable
    {
        private IntPtr _hWaveIn;
        private WinMM.WaveDelegate _callback;
        private List<IntPtr> _headers = new List<IntPtr>();
        private bool _running;
        public event Action<byte[]>? DataAvailable;

        public WaveIn(int deviceId, int sampleRate, int channels)
        {
            _callback = WaveInProc;
            WinMM.WAVEFORMATEX format = new WinMM.WAVEFORMATEX
            {
                wFormatTag = (ushort)1, // PCM
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)sampleRate,
                wBitsPerSample = (ushort)16,
                nBlockAlign = (ushort)(channels * 2),
                nAvgBytesPerSec = (uint)(sampleRate * channels * 2)
            };

            // Open the audio capture device
            int result = WinMM.waveInOpen(out _hWaveIn, deviceId, ref format, _callback, IntPtr.Zero, WinMM.CALLBACK_FUNCTION);
            if (result != 0) throw new Exception("waveInOpen failed: " + result);

            // Prepare multiple buffers to ensure continuous capture (low latency)
            int bufferSize = (int)(format.nAvgBytesPerSec / 50); // ~20ms per buffer
            for (int i = 0; i < 4; i++)
            {
                AddBuffer(bufferSize);
            }
        }

        private void AddBuffer(int size)
        {
            IntPtr buffer = Marshal.AllocHGlobal(size);
            WinMM.WAVEHDR header = new WinMM.WAVEHDR
            {
                lpData = buffer,
                dwBufferLength = (uint)size
            };
            IntPtr headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(header));
            Marshal.StructureToPtr(header, headerPtr, false);
            _headers.Add(headerPtr);

            WinMM.waveInPrepareHeader(_hWaveIn, headerPtr, Marshal.SizeOf(typeof(WinMM.WAVEHDR)));
            WinMM.waveInAddBuffer(_hWaveIn, headerPtr, Marshal.SizeOf(typeof(WinMM.WAVEHDR)));
        }

        private void WaveInProc(IntPtr hwi, int msg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            // WIM_DATA is sent when a buffer is filled with audio data
            if (msg == WinMM.WIM_DATA && _running)
            {
                WinMM.WAVEHDR header = Marshal.PtrToStructure<WinMM.WAVEHDR>(dwParam1);
                byte[] data = new byte[header.dwBytesRecorded];
                Marshal.Copy(header.lpData, data, 0, (int)header.dwBytesRecorded);
                
                DataAvailable?.Invoke(data);

                // Re-queue the buffer for reuse
                if (_running)
                {
                    WinMM.waveInAddBuffer(_hWaveIn, dwParam1, Marshal.SizeOf(typeof(WinMM.WAVEHDR)));
                }
            }
        }

        public void Start()
        {
            _running = true;
            WinMM.waveInStart(_hWaveIn);
        }

        public void Stop()
        {
            _running = false;
            WinMM.waveInStop(_hWaveIn);
            WinMM.waveInReset(_hWaveIn);
        }

        public void Dispose()
        {
            Stop();
            foreach (var h in _headers)
            {
                WinMM.WAVEHDR header = Marshal.PtrToStructure<WinMM.WAVEHDR>(h);
                WinMM.waveInUnprepareHeader(_hWaveIn, h, Marshal.SizeOf(typeof(WinMM.WAVEHDR)));
                Marshal.FreeHGlobal(header.lpData);
                Marshal.FreeHGlobal(h);
            }
            WinMM.waveInClose(_hWaveIn);
        }
    }

    /// <summary>
    /// A simple wrapper for Windows waveOut (audio playback) APIs.
    /// </summary>
    public class WaveOut : IDisposable
    {
        private IntPtr _hWaveOut;
        private WinMM.WaveDelegate _callback;
        private List<IntPtr> _headers = new List<IntPtr>();

        public WaveOut(int deviceId, int sampleRate, int channels)
        {
            _callback = WaveOutProc;
            WinMM.WAVEFORMATEX format = new WinMM.WAVEFORMATEX
            {
                wFormatTag = (ushort)1, // PCM
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)sampleRate,
                wBitsPerSample = (ushort)16,
                nBlockAlign = (ushort)(channels * 2),
                nAvgBytesPerSec = (uint)(sampleRate * channels * 2)
            };

            // Open the audio playback device
            int result = WinMM.waveOutOpen(out _hWaveOut, deviceId, ref format, _callback, IntPtr.Zero, WinMM.CALLBACK_FUNCTION);
            if (result != 0) throw new Exception("waveOutOpen failed: " + result);
        }

        public void Play(byte[] data)
        {
            // Allocate memory for the audio chunk
            IntPtr buffer = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buffer, data.Length);

            WinMM.WAVEHDR header = new WinMM.WAVEHDR
            {
                lpData = buffer,
                dwBufferLength = (uint)data.Length
            };
            IntPtr headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(header));
            Marshal.StructureToPtr(header, headerPtr, false);

            lock (_headers) { _headers.Add(headerPtr); }

            // Send the chunk to the output device
            WinMM.waveOutPrepareHeader(_hWaveOut, headerPtr, Marshal.SizeOf(typeof(WinMM.WAVEHDR)));
            WinMM.waveOutWrite(_hWaveOut, headerPtr, Marshal.SizeOf(typeof(WinMM.WAVEHDR)));
        }

        private void WaveOutProc(IntPtr hwo, int msg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            // WOM_DONE is sent when a buffer has finished playing
            if (msg == WinMM.WOM_DONE)
            {
                WinMM.WAVEHDR header = Marshal.PtrToStructure<WinMM.WAVEHDR>(dwParam1);
                WinMM.waveOutUnprepareHeader(_hWaveOut, dwParam1, Marshal.SizeOf(typeof(WinMM.WAVEHDR)));
                
                // Free the allocated memory for this chunk
                Marshal.FreeHGlobal(header.lpData);
                Marshal.FreeHGlobal(dwParam1);
                lock (_headers) { _headers.Remove(dwParam1); }
            }
        }

        public void Dispose()
        {
            WinMM.waveOutReset(_hWaveOut);
            WinMM.waveOutClose(_hWaveOut);
        }
    }

    public static class WinMM
    {
        public const int CALLBACK_FUNCTION = 0x00030000;
        public const int WIM_DATA = 0x3C0;
        public const int WOM_DONE = 0x3BD;

        public delegate void WaveDelegate(IntPtr hWave, int msg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
            public uint dwSupport;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [DllImport("winmm.dll")]
        public static extern int waveInGetNumDevs();
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern int waveInGetDevCaps(int uDeviceID, ref WAVEINCAPS pwic, int cbwic);
        [DllImport("winmm.dll")]
        public static extern int waveInOpen(out IntPtr phwi, int uDeviceID, ref WAVEFORMATEX pwfx, WaveDelegate dwCallback, IntPtr dwInstance, int fdwOpen);
        [DllImport("winmm.dll")]
        public static extern int waveInPrepareHeader(IntPtr hwi, IntPtr pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern int waveInUnprepareHeader(IntPtr hwi, IntPtr pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern int waveInAddBuffer(IntPtr hwi, IntPtr pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern int waveInStart(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern int waveInStop(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern int waveInReset(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern int waveInClose(IntPtr hwi);

        [DllImport("winmm.dll")]
        public static extern int waveOutGetNumDevs();
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern int waveOutGetDevCaps(int uDeviceID, ref WAVEOUTCAPS pwoc, int cbwoc);
        [DllImport("winmm.dll")]
        public static extern int waveOutOpen(out IntPtr phwo, int uDeviceID, ref WAVEFORMATEX pwfx, WaveDelegate dwCallback, IntPtr dwInstance, int fdwOpen);
        [DllImport("winmm.dll")]
        public static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern int waveOutUnprepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern int waveOutWrite(IntPtr hwo, IntPtr pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern int waveOutReset(IntPtr hwo);
        [DllImport("winmm.dll")]
        public static extern int waveOutClose(IntPtr hwo);
    }
}
