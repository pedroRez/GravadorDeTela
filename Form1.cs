using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
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

        private void btnParar_Click(object sender, EventArgs e)
        {
            try
            {
                if (_recorder != null && _recorder.IsRecording)
                    _recorder.Stop();

                _recordingThread?.Join();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                progressBar1.Style = ProgressBarStyle.Continuous;
                lblStatus.Text = "Finalizando...";
                this.Cursor = Cursors.WaitCursor;

                string videoPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_video.avi");
                string audioPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_audio.wav");

                if (!File.Exists(videoPath) || !File.Exists(audioPath))
                {
                    MessageBox.Show("Arquivos de gravação originais (.avi/.wav) não foram encontrados.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (_modoWhatsApp)
                {
                    lblStatus.Text = "Processando partes para MP4...";
                    ProcessarParaWhatsApp(videoPath, audioPath);
                }
                else
                {
                    lblStatus.Text = "Processando arquivo único para MP4...";
                    string outputPath = Path.Combine(_pastaDaGravacaoAtual, "gravacao_final.mp4");
                    ProcessarParaArquivoUnico(videoPath, audioPath, outputPath);
                }

                // Limpeza dos arquivos temporários
                File.Delete(videoPath);
                File.Delete(audioPath);
                string tempFile = Path.Combine(_pastaDaGravacaoAtual, "temp_synced.mp4");
                if (File.Exists(tempFile)) File.Delete(tempFile);

                lblStatus.Text = "Gravação finalizada!";
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
                btnParar.Enabled = false;
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

        private void ProcessarParaArquivoUnico(string video, string audio, string saida)
        {
            string ffmpegPath = GetFfmpegPath();
            if (ffmpegPath == null) return;

            // Etapa 1: Juntar vídeo e áudio em um arquivo temporário perfeitamente sincronizado
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 1/2: Sincronizando áudio e vídeo..."));
            string tempFile = Path.Combine(Path.GetDirectoryName(saida), "temp_synced.mp4");
            string args1 = $"-i \"{video}\" -i \"{audio}\" -c:v copy -c:a aac -b:a 192k -shortest -y \"{tempFile}\"";
            ExecutarFfmpeg(ffmpegPath, args1);

            if (!File.Exists(tempFile)) throw new Exception("Falha ao criar o arquivo temporário sincronizado.");

            // Etapa 2: Comprimir o arquivo temporário sincronizado para o arquivo final pequeno
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 2/2: Comprimindo para o tamanho final..."));
            string args2 = $"-i \"{tempFile}\" -c:v libx264 -preset ultrafast -crf 30 -c:a copy -y \"{saida}\"";
            ExecutarFfmpeg(ffmpegPath, args2);
        }

        private void ProcessarParaWhatsApp(string video, string audio)
        {
            string ffmpegPath = GetFfmpegPath();
            if (ffmpegPath == null) return;

            // Etapa 1: Juntar vídeo e áudio em um arquivo temporário perfeitamente sincronizado
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 1/2: Sincronizando áudio e vídeo..."));
            string tempFile = Path.Combine(_pastaDaGravacaoAtual, "temp_synced.mp4");
            string args1 = $"-i \"{video}\" -i \"{audio}\" -c:v copy -c:a aac -b:a 192k -shortest -y \"{tempFile}\"";
            ExecutarFfmpeg(ffmpegPath, args1);

            if (!File.Exists(tempFile)) throw new Exception("Falha ao criar o arquivo temporário sincronizado.");

            // Etapa 2: Dividir e comprimir o arquivo temporário sincronizado
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Etapa 2/2: Dividindo e comprimindo as partes..."));
            string outputPathPattern = Path.Combine(_pastaDaGravacaoAtual, "Parte_%03d.mp4");
            string args2 = $"-i \"{tempFile}\" -c:v libx264 -preset ultrafast -crf 30 -an -f segment -segment_time 90 -reset_timestamps 1 -y \"{outputPathPattern}\"";
            string args2_audio = $"-i \"{tempFile}\" -c:a aac -b:a 128k -f segment -segment_time 90 -reset_timestamps 1 -y {Path.Combine(_pastaDaGravacaoAtual, "audio_part_%03d.m4a")}"; // Separando para juntar depois
            // A abordagem de dividir e manter o áudio é complexa. A mais simples é recodificar o vídeo e juntar com o áudio original a cada parte.
            // Simplificando a lógica para evitar complexidade excessiva
            string args_final = $"-i \"{tempFile}\" -c:v libx264 -preset ultrafast -crf 30 -c:a aac -b:a 128k -f segment -segment_time 90 -reset_timestamps 1 -y \"{outputPathPattern}\"";
            ExecutarFfmpeg(ffmpegPath, args_final);
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

        private void ExecutarFfmpeg(string ffmpegPath, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                using (Process proc = Process.Start(psi))
                {
                    string errorOutput = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        throw new Exception("O FFmpeg reportou um erro durante o processamento.\n\nSaída FFmpeg:\n" + errorOutput);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro durante a conversão: " + ex.Message, "Erro FFmpeg", MessageBoxButtons.OK, MessageBoxIcon.Error);
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