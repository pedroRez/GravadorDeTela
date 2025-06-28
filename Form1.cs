using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
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

        // CORREÇÃO: Método principal agora é 'async' para não travar a interface
        private async void btnParar_Click(object sender, EventArgs e)
        {
            // Desativa os botões imediatamente para evitar múltiplos cliques
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
                    _recorder.Stop();

                // CORREÇÃO: Espera a thread de gravação terminar em segundo plano, sem travar a UI
                await Task.Run(() => _recordingThread?.Join());

                GC.Collect();
                GC.WaitForPendingFinalizers();

                progressBar1.Style = ProgressBarStyle.Continuous;

                string videoPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_video.avi");
                string audioPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_audio.wav");

                if (!File.Exists(videoPath) || !File.Exists(audioPath))
                {
                    MessageBox.Show("Arquivos de gravação originais (.avi/.wav) não foram encontrados.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                File.Delete(videoPath);
                File.Delete(audioPath);
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
                this.Cursor = Cursors.Default;
                btnIniciar.Enabled = true;
                btnParar.Enabled = true;
                chkModoWhatsApp.Enabled = true;
                progressBar1.Visible = false;
                lblStatus.Visible = false;
            }
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
            string args1 = $"-i \"{video}\" -i \"{audio}\" -c:v copy -c:a aac -b:a 192k -shortest -y \"{tempFile}\"";
            await ExecutarFfmpegComProgresso(ffmpegPath, args1, video);

            if (!File.Exists(tempFile)) throw new Exception("Falha ao criar o arquivo temporário sincronizado.");

            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 2/2: Comprimindo..."));
            string args2 = $"-i \"{tempFile}\" -c:v libx264 -preset ultrafast -crf 30 -c:a copy -y \"{saida}\"";
            await ExecutarFfmpegComProgresso(ffmpegPath, args2, tempFile);
        }

        private async Task ProcessarParaWhatsApp(string video, string audio)
        {
            string ffmpegPath = GetFfmpegPath();
            if (ffmpegPath == null) return;

            string tempFile = Path.Combine(_pastaDaGravacaoAtual, "temp_synced.mp4");

            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 1/2: Sincronizando..."));
            string args1 = $"-i \"{video}\" -i \"{audio}\" -c:v copy -c:a aac -b:a 192k -shortest -y \"{tempFile}\"";
            await ExecutarFfmpegComProgresso(ffmpegPath, args1, video);

            if (!File.Exists(tempFile)) throw new Exception("Falha ao criar o arquivo temporário sincronizado.");

            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 2/2: Dividindo e Comprimindo..."));
            string outputPathPattern = Path.Combine(_pastaDaGravacaoAtual, "Parte_%03d.mp4");
            // CORREÇÃO: Garantindo que o tempo de divisão é 120 segundos
            string args2 = $"-i \"{tempFile}\" -c:v libx264 -preset ultrafast -crf 30 -c:a aac -b:a 128k -f segment -segment_time 120 -reset_timestamps 1 -y \"{outputPathPattern}\"";
            await ExecutarFfmpegComProgresso(ffmpegPath, args2, tempFile);
        }

        private string GetFfmpegPath()
        {
            string ffmpegPath = Path.Combine(Application.StartupPath, "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                MessageBox.Show("ffmpeg.exe não encontrado na pasta do programa!", "Erro Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return ffmpegPath;
        }

        private async Task ExecutarFfmpegComProgresso(string ffmpegPath, string args, string inputFileForDuration)
        {
            TimeSpan totalDuration = TimeSpan.Zero;
            var tcs = new TaskCompletionSource<int>();

            await Task.Run(() => {
                var psiDuration = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{inputFileForDuration}\" -hide_banner",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psiDuration))
                {
                    string output = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    Match m = Regex.Match(output, @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                    if (m.Success)
                    {
                        totalDuration = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value) * 10);
                    }
                }

                var psiConvert = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var ffmpegProcess = new Process
                {
                    StartInfo = psiConvert,
                    EnableRaisingEvents = true
                };

                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null) return;
                    Match m = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                    if (m.Success && totalDuration > TimeSpan.Zero)
                    {
                        var currentTime = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value) * 10);
                        double percentage = (currentTime.TotalSeconds / totalDuration.TotalSeconds) * 100;

                        this.Invoke((MethodInvoker)delegate {
                            progressBar1.Value = Math.Min(100, (int)percentage);
                        });
                    }
                };

                ffmpegProcess.Exited += (sender, e) => {
                    tcs.TrySetResult(ffmpegProcess.ExitCode);
                    ffmpegProcess.Dispose();
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                tcs.Task.Wait(); // Espera a tarefa de conversão terminar nesta thread de fundo
            });

            // Verifica o resultado após a conclusão
            if (tcs.Task.Result != 0)
            {
                throw new Exception($"FFmpeg terminou com código de erro {tcs.Task.Result}. Verifique os parâmetros e arquivos de entrada.");
            }
        }
    }

    public class ScreenRecorder
    {
        private readonly string _pasta;
        private AviWriter _escritorVideo;
        private IAviVideoStream _videoStream;

        private WasapiLoopbackCapture _capturaAudio;
        private WaveFileWriter _escritorAudio;

        private volatile bool _gravando;
        public bool IsRecording => _gravando;
        public const int FPS = 25;
        private int _largura;
        private int _altura;

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

            var stopwatch = Stopwatch.StartNew();
            long frameCount = 0;
            long ticksPerFrame = Stopwatch.Frequency / FPS;

            while (_gravando)
            {
                long proximoFrameTicks = frameCount * ticksPerFrame;
                while (stopwatch.ElapsedTicks < proximoFrameTicks)
                {
                    Thread.Sleep(1);
                    if (!_gravando) break;
                }
                if (!_gravando) break;

                try
                {
                    var frame = CapturarTela();
                    _videoStream?.WriteFrame(true, frame, 0, frame.Length);
                    frameCount++;
                }
                catch (Exception)
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

            _escritorVideo = new AviWriter(arquivoVideo) { FramesPerSecond = FPS, EmitIndex1 = true };
            _videoStream = _escritorVideo.AddVideoStream();
            _videoStream.Width = _largura;
            _videoStream.Height = _altura;
            _videoStream.Codec = "MJPG";
            _videoStream.BitsPerPixel = BitsPerPixel.Bpp24;

            _capturaAudio = new WasapiLoopbackCapture();
            _escritorAudio = new WaveFileWriter(arquivoAudio, _capturaAudio.WaveFormat);
            _capturaAudio.DataAvailable += (s, a) => _escritorAudio?.Write(a.Buffer, 0, a.BytesRecorded);

            _capturaAudio.StartRecording();
        }

        private void FinalizarGravacao()
        {
            _capturaAudio?.StopRecording();
            _capturaAudio?.Dispose();
            _capturaAudio = null;

            _escritorAudio?.Close();
            _escritorAudio = null;

            _escritorVideo?.Close();
            _escritorVideo = null;
        }

        private byte[] CapturarTela()
        {
            var bounds = new Rectangle(0, 0, _largura, _altura);
            using (var bmp = new Bitmap(bounds.Width, bounds.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }
    }
}