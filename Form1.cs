using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;

namespace GravadorDeTela
{
    public partial class Form1 : Form
    {
        private ScreenRecorder _recorder;
        private Thread _recordingThread;
        private string _pastaDaGravacaoAtual;
        private bool _modoWhatsApp;

        public Form1()
        {
            InitializeComponent();
            btnParar.Enabled = false;
            // Assumindo que você tem uma ProgressBar 'progressBar1' e uma Label 'lblStatus' no seu formulário
            progressBar1.Visible = false;
            lblStatus.Visible = false;
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            try
            {
                string nomeDaPasta = $"Gravacao_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                _pastaDaGravacaoAtual = Path.Combine(Application.StartupPath, nomeDaPasta);
                Directory.CreateDirectory(_pastaDaGravacaoAtual);

                btnIniciar.Enabled = false;
                btnParar.Enabled = true;
                chkModoWhatsApp.Enabled = false;
                progressBar1.Style = ProgressBarStyle.Marquee;
                progressBar1.Visible = true;
                lblStatus.Text = "Gravando...";
                lblStatus.Visible = true;
                _modoWhatsApp = chkModoWhatsApp.Checked;

                _recordingThread = new Thread(() =>
                {
                    _recorder = new ScreenRecorder(_pastaDaGravacaoAtual);
                    _recorder.Record();
                })
                { IsBackground = true };
                _recordingThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar a gravação: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnParar_Click(object sender, EventArgs e)
        {
            btnIniciar.Enabled = false;
            btnParar.Enabled = false;
            lblStatus.Text = "Finalizando gravação...";
            lblStatus.Visible = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.Visible = true;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                if (_recorder != null && _recorder.IsRecording)
                    await Task.Run(() => _recorder.Stop());

                await Task.Run(() => _recordingThread?.Join());

                GC.Collect();
                GC.WaitForPendingFinalizers();

                progressBar1.Style = ProgressBarStyle.Continuous;

                string videoPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_video.avi");
                string audioPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_audio.wav");

                if (!File.Exists(videoPath) || !File.Exists(audioPath))
                {
                    MessageBox.Show("Arquivos de gravação originais (.avi/.wav) não foram encontrados.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    FinalizarUI();
                    return;
                }

                if (_modoWhatsApp)
                {
                    await ProcessarParaWhatsApp(videoPath, audioPath);
                }
                else
                {
                    string outputPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_final.mp4");
                    await ProcessarParaArquivoUnico(videoPath, audioPath, outputPath);
                }

                // Limpeza dos arquivos temporários
                //File.Delete(videoPath);
                //File.Delete(audioPath);
                string tempFile = Path.Combine(_pastaDaGravacaoAtual, "temp_synced.mp4");
                if (File.Exists(tempFile)) File.Delete(tempFile);

                lblStatus.Text = "Processamento concluído!";
                MessageBox.Show("Gravação e processamento concluídos!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start("explorer.exe", _pastaDaGravacaoAtual);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao finalizar gravação: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FinalizarUI();
            }
        }

        private void FinalizarUI()
        {
            this.Cursor = Cursors.Default;
            btnIniciar.Enabled = true;
            btnParar.Enabled = false;
            chkModoWhatsApp.Enabled = true;
            progressBar1.Visible = false;
            lblStatus.Visible = false;
        }

        private void btnAbrirPasta_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_pastaDaGravacaoAtual) && Directory.Exists(_pastaDaGravacaoAtual))
            {
                Process.Start("explorer.exe", _pastaDaGravacaoAtual);
            }
            else
            {
                MessageBox.Show("Nenhuma gravação foi feita ainda ou a pasta não foi encontrada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task ProcessarParaArquivoUnico(string video, string audio, string saida)
        {
            string ffmpegPath = GetFfmpegPath();
            if (ffmpegPath == null) return;

            string tempFile = Path.Combine(Path.GetDirectoryName(saida), "temp_synced.mp4");

            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 1/2: Sincronizando..."));
            string args1 = $"-r {ScreenRecorder.FPS} -i \"{video}\" -i \"{audio}\" -c:v copy -c:a aac -b:a 192k -shortest -y \"{tempFile}\"";
            await ExecutarFfmpegComProgresso(ffmpegPath, args1, audio);

            if (!File.Exists(tempFile)) throw new Exception("Falha ao criar o arquivo temporário sincronizado.");

            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 2/2: Comprimindo..."));
            string args2 = $"-i \"{tempFile}\" -c:v libx264 -preset ultrafast -crf 30 -c:a copy -y \"{saida}\"";
            await ExecutarFfmpegComProgresso(ffmpegPath, args2, tempFile);
        }

        private async Task ProcessarParaWhatsApp(string video, string audio)
        {
            string ffmpegPath = GetFfmpegPath();
            if (ffmpegPath == null) return;

            // Para dividir, é melhor fazer em uma única etapa para maior precisão
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Dividindo e Comprimindo..."));
            string outputPathPattern = Path.Combine(_pastaDaGravacaoAtual, "Parte_%03d.mp4");
            string args = $"-r {ScreenRecorder.FPS} -i \"{video}\" -i \"{audio}\" -c:v libx264 -preset ultrafast -crf 30 -c:a aac -b:a 128k -f segment -segment_time 120 -reset_timestamps 1 -shortest -y \"{outputPathPattern}\"";
            await ExecutarFfmpegComProgresso(ffmpegPath, args, audio);
        }

        private string GetFfmpegPath()
        {
            string ffmpegPath = Path.Combine(Application.StartupPath, "RecursosExternos", "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                MessageBox.Show("ffmpeg.exe não encontrado na pasta RecursosExternos!", "Erro Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return ffmpegPath;
        }

        private Task<int> ExecutarFfmpegComProgresso(string ffmpegPath, string args, string inputFileForDuration)
        {
            var tcs = new TaskCompletionSource<int>();

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var ffmpegProcess = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            ffmpegProcess.Exited += (sender, e) =>
            {
                tcs.TrySetResult(ffmpegProcess.ExitCode);
                ffmpegProcess.Dispose();
            };

            TimeSpan totalDuration = TimeSpan.Zero;
            var durationRegex = new Regex(@"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            var progressRegex = new Regex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");

            ffmpegProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null) return;

                Match m = durationRegex.Match(e.Data);
                if (m.Success && totalDuration == TimeSpan.Zero)
                {
                    totalDuration = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value) * 10);
                }

                m = progressRegex.Match(e.Data);
                if (m.Success && totalDuration > TimeSpan.Zero)
                {
                    var currentTime = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value) * 10);
                    double percentage = Math.Min(100, (currentTime.TotalSeconds / totalDuration.TotalSeconds) * 100);

                    this.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.Value = (int)percentage;
                    });
                }
            };

            ffmpegProcess.Start();
            ffmpegProcess.BeginErrorReadLine();

            return tcs.Task;
        }
    }

    public class ScreenRecorder
    {
        private readonly string _pasta;
        private AviWriter _escritorVideo;
        private IAviVideoStream _videoStream;

        private WasapiLoopbackCapture _capturaAudio;
        private WaveFileWriter _escritorAudio;
        private BufferedWaveProvider _audioBuffer;

        private volatile bool _gravando;
        public bool IsRecording => _gravando;
        public const int FPS = 24;
        private int _largura;
        private int _altura;

        private Bitmap _bitmapTela;
        private Graphics _graficosTela;

        private Stopwatch _stopwatch;
        private long _audioBytesWritten;
        private bool _audioReady;

        public ScreenRecorder(string pasta)
        {
            _pasta = pasta;
            _largura = Screen.PrimaryScreen.Bounds.Width - (Screen.PrimaryScreen.Bounds.Width % 2);
            _altura = Screen.PrimaryScreen.Bounds.Height - (Screen.PrimaryScreen.Bounds.Height % 2);
        }

        public void Record()
        {
            _gravando = true;
            IniciarGravacaoUnica();

            // Aguarda primeiro pacote de áudio real
            int tentativas = 0;
            while (!_audioReady && tentativas < 20)
            {
                Thread.Sleep(100);
                tentativas++;
            }

            // Espera o buffer encher (opcional)
            while (_audioBuffer.BufferedBytes < _escritorAudio.WaveFormat.AverageBytesPerSecond / 2)
            {
                Thread.Sleep(100);
            }

            // Inicia cronômetro e descarta os 2 segundos iniciais do áudio
            _stopwatch = Stopwatch.StartNew();
            int bytesPorSegundo = _escritorAudio.WaveFormat.AverageBytesPerSecond;
            _audioBuffer.Read(new byte[bytesPorSegundo * 2], 0, bytesPorSegundo * 2);

            long frameCount = 0;
            long ticksPerFrame = Stopwatch.Frequency / FPS;
            var audioBufferTemp = new byte[bytesPorSegundo / 10]; // 100ms

            while (_gravando)
            {
                long proximoFrameTicks = frameCount * ticksPerFrame;
                var spinWait = new SpinWait();
                while (_stopwatch.ElapsedTicks < proximoFrameTicks)
                {
                    spinWait.SpinOnce();
                    if (!_gravando) break;
                }

                if (!_gravando) break;

                try
                {
                    // Captura quadro de tela
                    var frame = CapturarTela();
                    _videoStream?.WriteFrame(true, frame, 0, frame.Length);
                    frameCount++;

                    // Captura áudio disponível
                    int disponivel = _audioBuffer.BufferedBytes;
                    if (disponivel > 0)
                    {
                        if (audioBufferTemp.Length < disponivel)
                            audioBufferTemp = new byte[disponivel];

                        int lidos = _audioBuffer.Read(audioBufferTemp, 0, disponivel);
                        if (lidos > 0)
                        {
                            _escritorAudio.Write(audioBufferTemp, 0, lidos);
                            _audioBytesWritten += lidos;
                        }
                    }
                }
                catch
                {
                    if (_gravando) Stop();
                }
            }

            FinalizarGravacao();
        }

        public void Stop()
        {
            _gravando = false;
        }

        private void IniciarGravacaoUnica()
        {
            string arquivoVideo = Path.Combine(_pasta, "gravacao_video.avi");
            string arquivoAudio = Path.Combine(_pasta, "gravacao_audio.wav");

            _bitmapTela = new Bitmap(_largura, _altura, PixelFormat.Format32bppRgb);
            _graficosTela = Graphics.FromImage(_bitmapTela);

            _escritorVideo = new AviWriter(arquivoVideo)
            {
                FramesPerSecond = FPS,
                EmitIndex1 = true
            };
            _videoStream = _escritorVideo.AddVideoStream();
            _videoStream.Width = _largura;
            _videoStream.Height = _altura;
            _videoStream.Codec = "MJPG";
            _videoStream.BitsPerPixel = BitsPerPixel.Bpp24;

            _capturaAudio = new WasapiLoopbackCapture();
            _audioBuffer = new BufferedWaveProvider(_capturaAudio.WaveFormat)
            {
                DiscardOnBufferOverflow = false
            };
            _escritorAudio = new WaveFileWriter(arquivoAudio, _audioBuffer.WaveFormat);

            _capturaAudio.DataAvailable += (s, a) =>
            {
                try
                {
                    if (!_audioReady && a.BytesRecorded > 0)
                        _audioReady = true;

                    _audioBuffer?.AddSamples(a.Buffer, 0, a.BytesRecorded);
                }
                catch { }
            };

            _audioBytesWritten = 0;
            _audioReady = false;
            _capturaAudio.StartRecording();
        }

        private void FinalizarGravacao()
        {
            _stopwatch?.Stop();

            // Aguarda fim do buffer de áudio
            int tentativas = 0;
            while (_audioBuffer?.BufferedBytes > 0 && tentativas < 10)
            {
                Thread.Sleep(100);
                tentativas++;
            }

            _capturaAudio?.StopRecording();

            // Escreve o que sobrou do buffer
            try
            {
                if (_audioBuffer != null && _escritorAudio != null)
                {
                    var final = new byte[_audioBuffer.BufferedBytes];
                    int lidos = _audioBuffer.Read(final, 0, final.Length);
                    if (lidos > 0)
                    {
                        _escritorAudio.Write(final, 0, lidos);
                        _audioBytesWritten += lidos;
                    }
                }
            }
            catch { }

            _capturaAudio?.Dispose();
            _capturaAudio = null;

            _escritorAudio?.Flush();
            _escritorAudio?.Dispose();
            _escritorAudio = null;

            _escritorVideo?.Close();
            _escritorVideo = null;

            _graficosTela?.Dispose();
            _graficosTela = null;
            _bitmapTela?.Dispose();
            _bitmapTela = null;
        }

        private byte[] CapturarTela()
        {
            _graficosTela.CopyFromScreen(Point.Empty, Point.Empty, new Size(_largura, _altura));
            using (var ms = new MemoryStream())
            {
                _bitmapTela.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
    }
}