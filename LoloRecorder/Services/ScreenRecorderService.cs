using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using ScreenRecorderLib;

namespace LoloRecorder.Services
{
    /// <summary>
    /// Serviço simples para gravação de tela utilizando ScreenRecorderLib
    /// e processamento opcional com ffmpeg.
    /// </summary>
    public class ScreenRecorderService : IAsyncDisposable
    {
        private readonly RecorderOptions _options;
        private readonly string _outputPath;
        private readonly string _ffmpegPath;
        private Recorder? _recorder;
        private TaskCompletionSource<bool>? _recordingTcs;
        private Timer? _splitTimer;
        private CancellationTokenSource? _stopAfterCts;
        private int _segmentIndex = 1;
        private int _splitSeconds;
        private string _currentOutputPath = string.Empty;
        public bool IsPaused { get; private set; }
        public bool IsRecording => _recorder != null;

        /// <summary>
        /// Cria uma nova instância do serviço.
        /// </summary>
        /// <param name="outputPath">Arquivo de saída desejado.</param>
        /// <param name="options">Opções de áudio/vídeo ou <c>null</c> para valores padrão.</param>
        /// <param name="ffmpegPath">Caminho do executável ffmpeg (padrão: "ffmpeg" no PATH).</param>
        public ScreenRecorderService(string outputPath, RecorderOptions? options = null, string ffmpegPath = "ffmpeg")
        {
            _outputPath = outputPath;
            _ffmpegPath = ffmpegPath;
            _options = options ?? new RecorderOptions
            {
                AudioOptions = new AudioOptions
                {
                    IsAudioEnabled = true
                },
                VideoEncoderOptions = new VideoEncoderOptions
                {
                    Framerate = 30,
                    Quality = 60
                },
                OverlayOptions = new OverLayOptions()
            };
        }

        /// <summary>
        /// Inicia a gravação de tela.
        /// </summary>
        /// <returns>
        /// Tupla indicando sucesso e mensagem de erro (quando houver).
        /// </returns>
        public Task<(bool Success, string? ErrorMessage)> StartAsync(RecordingMode mode, string? webcamDevice = null, int? splitSeconds = null, int? stopAfterMinutes = null)
        {
            if (_recorder != null)
                return Task.FromResult<(bool, string?)>((false, "Gravação já iniciada."));

            try
            {
                var directory = Path.GetDirectoryName(_outputPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = _options;
                options.OverlayOptions ??= new OverLayOptions();
                options.OverlayOptions.Overlays.Clear();
                if (!string.IsNullOrWhiteSpace(webcamDevice))
                {
                    options.OverlayOptions.Overlays.Add(new VideoCaptureOverlay
                    {
                        DeviceName = webcamDevice,
                        AnchorPoint = Anchor.BottomRight,
                        Offset = new ScreenSize(10, 10),
                        Size = new ScreenSize(0, 200)
                    });
                }
                switch (mode)
                {
                    case RecordingMode.Janela:
                        options.RecordingSources = new RecordingSourceBase[]
                        {
                            new WindowRecordingSource { Handle = GetForegroundWindow() }
                        };
                        break;
                    case RecordingMode.Regiao:
                        options.RecordingSources = new RecordingSourceBase[]
                        {
                            new DisplayRecordingSource
                            {
                                SourceRect = new ScreenRect { Left = 0, Top = 0, Right = 800, Bottom = 600 }
                            }
                        };
                        break;
                    default:
                        options.RecordingSources = new RecordingSourceBase[]
                        {
                            DisplayRecordingSource.MainMonitor
                        };
                        break;
                }

                _splitSeconds = splitSeconds ?? 0;
                _segmentIndex = 1;
                _currentOutputPath = _splitSeconds > 0 ? GetSegmentFilePath(_segmentIndex) : _outputPath;

                StartRecorder(_currentOutputPath);

                if (_splitSeconds > 0)
                {
                    _splitTimer = new Timer(_splitSeconds * 1000) { AutoReset = false };
                    _splitTimer.Elapsed += async (s, e) => await SplitSegmentAsync();
                    _splitTimer.Start();
                }

                if (stopAfterMinutes.HasValue && stopAfterMinutes.Value > 0)
                {
                    _stopAfterCts?.Cancel();
                    _stopAfterCts = new CancellationTokenSource();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(stopAfterMinutes.Value), _stopAfterCts.Token);
                            await StopAsync();
                        }
                        catch (TaskCanceledException) { }
                    });
                }

                return Task.FromResult<(bool, string?)>((true, null));
            }
            catch (Exception ex)
            {
                _recorder?.Dispose();
                _recorder = null;
                _recordingTcs = null;
                return Task.FromResult<(bool, string?)>((false, ex.Message));
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Interrompe a gravação atual e finaliza timers auxiliares.
        /// </summary>
        public async Task StopAsync()
        {
            _splitTimer?.Stop();
            _splitTimer = null;
            _stopAfterCts?.Cancel();
            _stopAfterCts = null;
            await StopCurrentSegmentAsync();
            IsPaused = false;
        }

        public void Pause()
        {
            if (_recorder == null || IsPaused)
                return;
            _recorder.Pause();
            IsPaused = true;
        }

        public void Resume()
        {
            if (_recorder == null || !IsPaused)
                return;
            _recorder.Resume();
            IsPaused = false;
        }

        private void StartRecorder(string path)
        {
            _recordingTcs = new TaskCompletionSource<bool>();
            _recorder = Recorder.CreateRecorder(_options);
            _recorder.OnRecordingComplete += (s, e) => _recordingTcs?.TrySetResult(true);
            _recorder.Record(path + ".tmp");
        }

        private async Task SplitSegmentAsync()
        {
            if (_splitTimer != null)
                _splitTimer.Stop();
            await StopCurrentSegmentAsync();
            _segmentIndex++;
            _currentOutputPath = GetSegmentFilePath(_segmentIndex);
            StartRecorder(_currentOutputPath);
            _splitTimer?.Start();
        }

        private async Task StopCurrentSegmentAsync()
        {
            if (_recorder == null)
                return;

            try
            {
                _recorder.Stop();
                if (_recordingTcs != null)
                    await _recordingTcs.Task.ConfigureAwait(false);

                var tempFile = _currentOutputPath + ".tmp";
                if (File.Exists(tempFile))
                {
                    if (!string.IsNullOrWhiteSpace(_ffmpegPath) && (File.Exists(_ffmpegPath) || _ffmpegPath == "ffmpeg"))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = _ffmpegPath,
                            Arguments = $"-y -i \"{tempFile}\" -c copy \"{_currentOutputPath}\"",
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        if (proc != null)
                            await proc.WaitForExitAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        File.Move(tempFile, _currentOutputPath, true);
                    }

                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao finalizar gravação.", ex);
            }
            finally
            {
                _recorder.Dispose();
                _recorder = null;
                _recordingTcs = null;
            }
        }

        private string GetSegmentFilePath(int index)
        {
            var dir = Path.GetDirectoryName(_outputPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(_outputPath);
            var ext = Path.GetExtension(_outputPath);
            return Path.Combine(dir, $"{name}_{index:000}{ext}");
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }
    }
}
