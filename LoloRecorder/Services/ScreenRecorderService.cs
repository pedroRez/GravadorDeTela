using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
                }
            };
        }

        /// <summary>
        /// Inicia a gravação de tela.
        /// </summary>
        public Task StartAsync()
        {
            if (_recorder != null)
                throw new InvalidOperationException("Gravação já iniciada.");

            _recordingTcs = new TaskCompletionSource<bool>();
            _recorder = Recorder.CreateRecorder(_options);
            _recorder.OnRecordingComplete += (s, e) => _recordingTcs.TrySetResult(true);

            // Gravar em arquivo temporário para permitir pós-processamento com ffmpeg
            _recorder.Record(_outputPath + ".tmp");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Interrompe a gravação e, se possível, utiliza ffmpeg para gerar o arquivo final.
        /// </summary>
        public async Task StopAsync()
        {
            if (_recorder == null)
                return;

            try
            {
                _recorder.Stop();
                if (_recordingTcs != null)
                    await _recordingTcs.Task.ConfigureAwait(false);

                var tempFile = _outputPath + ".tmp";
                if (File.Exists(tempFile))
                {
                    // Se o ffmpeg estiver disponível, usa-o para muxar/copiar
                    if (!string.IsNullOrWhiteSpace(_ffmpegPath) && (File.Exists(_ffmpegPath) || _ffmpegPath == "ffmpeg"))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = _ffmpegPath,
                            Arguments = $"-y -i \"{tempFile}\" -c copy \"{_outputPath}\"",
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
                        File.Move(tempFile, _outputPath, true);
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

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }
    }
}
