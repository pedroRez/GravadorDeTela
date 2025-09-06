using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Collections.Generic;
using LoloRecorder.Services;
using ScreenRecorderLib;

namespace LoloRecorder.Views
{
    public partial class MainWindow : Window
    {
        private readonly ScreenRecorderService _recorderService;
        private HwndSource? _hwndSource;
        private IntPtr _windowHandle;
        private bool _isPaused;

        private const int HOTKEY_START_PAUSE_ID = 1;
        private const int HOTKEY_STOP_ID = 2;
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gravacao.mp4");
            _recorderService = new ScreenRecorderService(outputPath);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(HwndHook);
            }
            RegisterHotKey(_windowHandle, HOTKEY_START_PAUSE_ID, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F8));
            RegisterHotKey(_windowHandle, HOTKEY_STOP_ID, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F9));
        }

        private async void RecordToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var mode = ModeCombo.SelectedIndex switch
                {
                    1 => RecordingMode.Janela,
                    2 => RecordingMode.Regiao,
                    _ => RecordingMode.TelaInteira
                };

                string? webcamDevice = null;
                if (WebcamToggle.IsChecked == true && WebcamDevicesCombo.SelectedItem is RecordableCamera cam)
                {
                    webcamDevice = cam.DeviceName;
                }
                int? splitSeconds = null;
                if (SplitCheck.IsChecked == true)
                {
                    if (!int.TryParse(SplitSecondsBox.Text, out var sec) || sec < 1)
                    {
                        StatusLabel.Content = "Valor inválido";
                        RecordToggle.IsChecked = false;
                        MessageBox.Show("Informe segundos válidos para divisão.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    splitSeconds = sec;
                }

                int? stopMinutes = null;
                if (StopCheck.IsChecked == true)
                {
                    if (!int.TryParse(StopMinutesBox.Text, out var min) || min < 1)
                    {
                        StatusLabel.Content = "Valor inválido";
                        RecordToggle.IsChecked = false;
                        MessageBox.Show("Informe minutos válidos para parada automática.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    stopMinutes = min;
                }
                var (success, error) = await _recorderService.StartAsync(mode, webcamDevice, splitSeconds, stopMinutes);
                if (!success)
                {
                    StatusLabel.Content = "Erro ao iniciar";
                    RecordToggle.IsChecked = false;
                    MessageBox.Show(error ?? "Falha desconhecida ao iniciar gravação.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                StatusLabel.Content = "Gravando...";
                _isPaused = false;
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Erro ao iniciar";
                RecordToggle.IsChecked = false;
                MessageBox.Show($"Falha inesperada ao iniciar a gravação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RecordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _recorderService.StopAsync();
                StatusLabel.Content = "Parado";
                _isPaused = false;
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Erro ao parar";
                MessageBox.Show($"Falha inesperada ao encerrar a gravação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            if (_hwndSource != null)
                _hwndSource.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_START_PAUSE_ID);
            UnregisterHotKey(_windowHandle, HOTKEY_STOP_ID);
            await _recorderService.DisposeAsync();
            base.OnClosed(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (id == HOTKEY_START_PAUSE_ID)
                {
                    ToggleRecordingHotkey();
                    handled = true;
                }
                else if (id == HOTKEY_STOP_ID)
                {
                    StopRecordingHotkey();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ToggleRecordingHotkey()
        {
            if (RecordToggle.IsChecked != true)
            {
                RecordToggle.IsChecked = true;
                _isPaused = false;
            }
            else if (!_isPaused)
            {
                _recorderService.Pause();
                _isPaused = true;
                StatusLabel.Content = "Pausado";
            }
            else
            {
                _recorderService.Resume();
                _isPaused = false;
                StatusLabel.Content = "Gravando...";
            }
        }

        private void StopRecordingHotkey()
        {
            if (RecordToggle.IsChecked == true || _isPaused)
            {
                RecordToggle.IsChecked = false;
                _isPaused = false;
            }
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Configurações ainda não implementadas.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WebcamToggle_Checked(object sender, RoutedEventArgs e)
        {
            var devices = Recorder.GetSystemVideoCaptureDevices()?.ToList() ?? new List<RecordableCamera>();
            WebcamDevicesCombo.ItemsSource = devices;
            WebcamDevicesCombo.Visibility = Visibility.Visible;
            if (devices.Count > 0)
                WebcamDevicesCombo.SelectedIndex = 0;
        }

        private void WebcamToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            WebcamDevicesCombo.Visibility = Visibility.Collapsed;
        }

        private void SplitCheck_Checked(object sender, RoutedEventArgs e)
        {
            SplitSecondsBox.IsEnabled = true;
        }

        private void SplitCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            SplitSecondsBox.IsEnabled = false;
        }

        private void StopCheck_Checked(object sender, RoutedEventArgs e)
        {
            StopMinutesBox.IsEnabled = true;
        }

        private void StopCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            StopMinutesBox.IsEnabled = false;
        }
    }
}
