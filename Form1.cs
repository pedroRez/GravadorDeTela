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
                    lblStatus.Text = "Dividindo e convertendo para MP4...";
                    await DividirEConverter(videoPath, audioPath);
                }
                else
                {
                    lblStatus.Text = "Juntando e convertendo para MP4...";
                    string outputPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_final.mp4");
                    await JuntarParaArquivoUnico(videoPath, audioPath, outputPath);
                }

                //File.Delete(videoPath);
                //File.Delete(audioPath);

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

        private async Task JuntarParaArquivoUnico(string video, string audio, string saida)
        {
            string ffmpegPath = GetFfmpegPath();
            if (ffmpegPath == null) return;

            // LÓGICA DE UMA ETAPA: Força o FPS de entrada e junta/comprime de uma vez
            string args = $"-r {ScreenRecorder.FPS} -i \"{video}\" -i \"{audio}\" -c:v libx264 -preset ultrafast -crf 30 -c:a aac -b:a 192k -shortest -y \"{saida}\"";

            await ExecutarFfmpegComProgresso(ffmpegPath, args, audio); // Usa o áudio para pegar a duração correta
        }

        private async Task DividirEConverter(string video, string audio)
        {
            string ffmpegPath = GetFfmpegPath();
            if (ffmpegPath == null) return;

            // LÓGICA DE UMA ETAPA: Força o FPS, junta, comprime e divide de uma vez
            string outputPathPattern = Path.Combine(_pastaDaGravacaoAtual, "Parte_%03d.mp4");
            string args = $"-r {ScreenRecorder.FPS} -i \"{video}\" -i \"{audio}\" -c:v libx264 -preset ultrafast -crf 30 -c:a aac -b:a 128k -f segment -segment_time 120 -reset_timestamps 1 -y \"{outputPathPattern}\"";

            await ExecutarFfmpegComProgresso(ffmpegPath, args, audio); // Usa o áudio para pegar a duração correta
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

            ffmpegProcess.Exited += (sender, e) => {
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

                    this.Invoke((MethodInvoker)delegate {
                        progressBar1.Value = (int)percentage;
                    });
                }
            };

            ffmpegProcess.Start();
            ffmpegProcess.BeginErrorReadLine();

            return tcs.Task;
        }
    }

    // A CLASSE SCREENRECORDER NÃO FOI ALTERADA NESTA VERSÃO FINAL.
    // ELA JÁ ESTÁ ESTÁVEL E COM A SINCRONIZAÇÃO CORRETA.
    public class ScreenRecorder
    {
        private readonly string _pasta;
        private AviWriter _escritorVideo;
        private IAviVideoStream _videoStream;

        private WasapiLoopbackCapture _capturaAudio;
        private WaveFileWriter _escritorAudio;
        private BufferedWaveProvider _audioBuffer;
        private Thread _audioThread;

        private volatile bool _gravando;
        public bool IsRecording => _gravando;
        public const int FPS = 24;
        private int _largura;
        private int _altura;

        private Bitmap _bitmapTela;
        private Graphics _graficosTela;

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
                var spinWait = new SpinWait();
                while (stopwatch.ElapsedTicks < proximoFrameTicks)
                {
                    spinWait.SpinOnce();
                    if (!_gravando) break;
                }
                if (!_gravando) break;

                try
                {
                    var frame = CapturarTela();
                    _videoStream?.WriteFrame(true, frame, 0, frame.Length);
                    frameCount++;
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

            _escritorVideo = new AviWriter(arquivoVideo) { FramesPerSecond = FPS, EmitIndex1 = true };
            _videoStream = _escritorVideo.AddVideoStream();
            _videoStream.Width = _largura;
            _videoStream.Height = _altura;
            _videoStream.Codec = "MJPG";
            _videoStream.BitsPerPixel = BitsPerPixel.Bpp24;

            _capturaAudio = new WasapiLoopbackCapture();
            _audioBuffer = new BufferedWaveProvider(_capturaAudio.WaveFormat)
            {
                DiscardOnBufferOverflow = true
            };

            _escritorAudio = new WaveFileWriter(arquivoAudio, _audioBuffer.WaveFormat);
            _capturaAudio.DataAvailable += (s, a) => _audioBuffer?.AddSamples(a.Buffer, 0, a.BytesRecorded);

            _audioThread = new Thread(AudioWriteThread) { IsBackground = true };
            _audioThread.Start();

            _capturaAudio.StartRecording();
        }

        private void AudioWriteThread()
        {
            int bufferSize = _audioBuffer.WaveFormat.AverageBytesPerSecond / 10;
            var buffer = new byte[bufferSize];

            while (_gravando)
            {
                int bytesRead = _audioBuffer.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    _escritorAudio?.Write(buffer, 0, bytesRead);
                }
                Thread.Sleep(100);
            }
        }

        private void FinalizarGravacao()
        {
            _gravando = false;
            _audioThread?.Join(500);

            _capturaAudio?.StopRecording();
            _capturaAudio?.Dispose();
            _capturaAudio = null;

            _escritorAudio?.Close();
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