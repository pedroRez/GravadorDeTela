using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GravadorDeTela
{
    public partial class Form1 : Form
    {
        // ===== Configurações padrão =====
        private const int FPS = 24;
        private const int VIDEO_CRF = 23;
        private const int AUDIO_KBPS = 192;
        private const int PADRAO_SEGUNDOS_WHATSAPP = 120; // padrão se não informado
        private const int MIN_SEGUNDOS_WHATSAPP = 15;
        private const int STOP_TIMEOUT_MS = 5000;

        // ===== Estado =====
        private Process _ffmpegProc;
        private string _pastaDaGravacaoAtual;
        private CancellationTokenSource _stopAutoCts;

        // ===== Classe interna para popular o Combo =====
        private class AudioDeviceItem
        {
            public string DisplayName { get; set; }   // ex.: Mixagem estéreo (Realtek Audio)
            public string Moniker { get; set; }       // ex.: @device_cm_{...}\wave_{...}
            public override string ToString() => DisplayName;
        }

        public Form1()
        {
            InitializeComponent();

            // Estado inicial UI
            btnParar.Enabled = false;
            progressBar1.Visible = false;
            lblStatus.Visible = false;

            // Habilitação de campos dependentes
            chkModoWhatsApp.CheckedChanged += (s, e) =>
            {
                txtSegmentacao.Enabled = chkModoWhatsApp.Checked;
            };
            chkStop.CheckedChanged += (s, e) =>
            {
                txtStop.Enabled = chkStop.Checked;
            };

            // No design da sua imagem esses nomes existem:
            // chkModoWhatsApp, txtSegmentacao, chkStop, txtStop, cmbAudio
            txtSegmentacao.Enabled = chkModoWhatsApp.Checked;
            txtStop.Enabled = chkStop.Checked;

            // Carregar dispositivos de áudio dshow
            Shown += async (s, e) => await CarregarDispositivosAudio();
        }

        // ==================== UTILITÁRIOS ====================

        private void Log(string msg)
        {
            try
            {
                var baseVideos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                var raiz = Path.Combine(baseVideos, "GravadorDeTela");
                Directory.CreateDirectory(raiz);
                File.AppendAllText(Path.Combine(raiz, "gravador.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erro ao gravar log: " + ex);
            }
        }

        private void AtualizaStatus(string texto, bool marquee = true)
        {
            try
            {
                progressBar1.Style = marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
                progressBar1.Visible = true;
                lblStatus.Text = texto;
                lblStatus.Visible = true;
            }
            catch (Exception ex)
            {
                Log("Erro ao atualizar status: " + ex);
            }
        }

        private void FinalizarUI()
        {
            this.Cursor = Cursors.Default;
            btnIniciar.Enabled = true;
            btnParar.Enabled = false;
            progressBar1.Visible = false;
            lblStatus.Visible = false;
            chkModoWhatsApp.Enabled = true;
            cmbAudio.Enabled = true;
            txtSegmentacao.Enabled = chkModoWhatsApp.Checked;
            chkStop.Enabled = true;
            txtStop.Enabled = chkStop.Checked;
        }

        private string CriarDiretorioGravavel()
        {
            var baseVideos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            var raiz = Path.Combine(baseVideos, "GravadorDeTela");
            Directory.CreateDirectory(raiz);
            string nome = $"Gravacao_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            var destino = Path.Combine(raiz, nome);
            Directory.CreateDirectory(destino);
            File.WriteAllText(Path.Combine(destino, ".write_test"), "ok");
            File.Delete(Path.Combine(destino, ".write_test"));
            return destino;
        }

        private string GetFfmpegPath()
        {
            // 1) RecursosExternos\ffmpeg.exe (junto ao app)
            string p = Path.Combine(Application.StartupPath, "RecursosExternos", "ffmpeg.exe");
            if (File.Exists(p)) return p;

            // 2) Pasta atual
            p = Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe");
            if (File.Exists(p)) return p;

            // 3) PATH
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathVar.Split(Path.PathSeparator))
            {
                try
                {
                    var cand = Path.Combine(dir.Trim(), "ffmpeg.exe");
                    if (File.Exists(cand)) return cand;
                }
                catch { }
            }
            return null;
        }

        private string GetFfprobePath()
        {
            // 1) RecursosExternos\\ffprobe.exe (junto ao app)
            string p = Path.Combine(Application.StartupPath, "RecursosExternos", "ffprobe.exe");
            if (File.Exists(p)) return p;

            // 2) Pasta atual
            p = Path.Combine(Environment.CurrentDirectory, "ffprobe.exe");
            if (File.Exists(p)) return p;

            // 3) PATH
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathVar.Split(Path.PathSeparator))
            {
                try
                {
                    var cand = Path.Combine(dir.Trim(), "ffprobe.exe");
                    if (File.Exists(cand)) return cand;
                }
                catch { }
            }
            return null;
        }

        private void VerificarSincronia(string caminhoArquivo)
        {
            try
            {
                var ffprobe = GetFfprobePath();
                if (ffprobe == null || !File.Exists(caminhoArquivo)) return;

                var psi = new ProcessStartInfo
                {
                    FileName = ffprobe,
                    Arguments = $"-v error -show_entries stream=codec_type,start_time,duration -of default=noprint_wrappers=1 \"{caminhoArquivo}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string output;
                using (var p = new Process { StartInfo = psi })
                {
                    p.Start();
                    output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                }

                double vStart = 0, aStart = 0, vDur = 0, aDur = 0;
                var rx = new Regex("codec_type=(?<type>\\w+)\\s+.*?start_time=(?<start>[-0-9\\.]+)\\s+.*?duration=(?<dur>[-0-9\\.]+)", RegexOptions.Singleline);
                foreach (Match m in rx.Matches(output))
                {
                    var type = m.Groups["type"].Value;
                    var start = double.Parse(m.Groups["start"].Value, CultureInfo.InvariantCulture);
                    var dur = double.Parse(m.Groups["dur"].Value, CultureInfo.InvariantCulture);
                    if (type == "video") { vStart = start; vDur = dur; }
                    else if (type == "audio") { aStart = start; aDur = dur; }
                }

                const double LIMIAR = 0.1; // 100 ms
                double diffStart = Math.Abs(vStart - aStart);
                double diffDur = Math.Abs(vDur - aDur);

                if (diffStart > LIMIAR || diffDur > LIMIAR)
                {
                    MessageBox.Show(
                        $"Possível diferença entre áudio e vídeo.\nΔ início: {diffStart:F3}s, Δ duração: {diffDur:F3}s",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Log("Erro ao verificar sincronia: " + ex);
            }
        }

        private void AbrirPasta(string caminho)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start("explorer.exe", caminho);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", caminho);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", caminho);
                }
            }
            catch { }
        }

        // ==================== LISTAGEM DSHOW ====================

        private async Task CarregarDispositivosAudio()
        {
            cmbAudio.Items.Clear();

            var ffmpeg = GetFfmpegPath();
            if (ffmpeg == null)
            {
                MessageBox.Show("ffmpeg.exe não encontrado. Coloque em RecursosExternos\\ffmpeg.exe ou no PATH.",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string text = "";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = "-hide_banner -list_devices true -f dshow -i dummy",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true
                };

                var sb = new StringBuilder();
                using (var p = new Process { StartInfo = psi })
                {
                    // O FFmpeg escreve a listagem de devices no STDERR
                    p.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null) sb.AppendLine(e.Data);
                    };

                    p.Start();
                    p.BeginErrorReadLine();

                    var waitTask = Task.Run(() => p.WaitForExit());
                    await Task.WhenAny(waitTask, Task.Delay(7000));
                    if (!p.HasExited)
                    {
                        try { p.Kill(); } catch { }
                    }
                    text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Falha ao listar dispositivos dshow: " + ex.Message,
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ex.: [dshow @ ...] "Mixagem estéreo (Realtek Audio)" (audio)
            //      [dshow @ ...]   Alternative name "@device_cm_{...}\wave_{...}"
            var nameRx = new Regex("\"([^\"]+)\"\\s*\\((?:audio|áudio)\\)", RegexOptions.IgnoreCase);
            var altRx = new Regex(@"Alternative name\s+""([^""]+)""", RegexOptions.IgnoreCase);

            int pos = 0, found = 0;
            while (true)
            {
                var mName = nameRx.Match(text, pos);
                if (!mName.Success) break;

                string display = mName.Groups[1].Value;

                // tenta achar o Alternative name que vem em seguida
                var mAlt = altRx.Match(text, mName.Index);
                string moniker = mAlt.Success ? mAlt.Groups[1].Value : null;

                cmbAudio.Items.Add(new AudioDeviceItem { DisplayName = display, Moniker = moniker });
                found++;

                pos = mAlt.Success ? (mAlt.Index + mAlt.Length) : (mName.Index + mName.Length);
            }

            if (found == 0)
            {
                MessageBox.Show(
                    "Nenhum dispositivo de captura de ÁUDIO (saída) foi encontrado no DirectShow.\n" +
                    "Ative 'Mixagem Estéreo' no Realtek ou instale o VB-CABLE/VoiceMeeter.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Seleciona automaticamente o mais provável (stereo mix / cable / voicemeeter)
            int preferred = -1;
            for (int i = 0; i < cmbAudio.Items.Count; i++)
            {
                var n = ((AudioDeviceItem)cmbAudio.Items[i]).DisplayName.ToLowerInvariant();
                if (n.Contains("mixagem estéreo") || n.Contains("stereo mix") ||
                    n.Contains("what u hear") || n.Contains("cable output") ||
                    n.Contains("voicemeeter output"))
                {
                    preferred = i; break;
                }
            }
            cmbAudio.SelectedIndex = preferred >= 0 ? preferred : 0;
        }


        // ==================== BOTÕES ====================

        private void btnAbrirPasta_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_pastaDaGravacaoAtual) && Directory.Exists(_pastaDaGravacaoAtual))
            {
                AbrirPasta(_pastaDaGravacaoAtual);
            }
            else
            {
                MessageBox.Show("Nenhuma gravação foi feita ainda ou a pasta não foi encontrada.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            try
            {
                var ffmpeg = GetFfmpegPath();
                if (ffmpeg == null)
                {
                    MessageBox.Show("ffmpeg.exe não encontrado. Coloque em RecursosExternos\\ffmpeg.exe ou no PATH.",
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // valida dispositivo de áudio
                if (cmbAudio.SelectedItem == null)
                {
                    MessageBox.Show("Selecione um dispositivo de ÁUDIO no combo antes de iniciar.",
                        "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var dev = (AudioDeviceItem)cmbAudio.SelectedItem;

                // valida segmentação (se WhatsApp marcado)
                int segmentSeconds = PADRAO_SEGUNDOS_WHATSAPP;
                if (chkModoWhatsApp.Checked)
                {
                    if (!int.TryParse(txtSegmentacao.Text.Trim(), out segmentSeconds))
                        segmentSeconds = PADRAO_SEGUNDOS_WHATSAPP;

                    if (segmentSeconds < MIN_SEGUNDOS_WHATSAPP)
                    {
                        MessageBox.Show($"O intervalo de corte deve ser pelo menos {MIN_SEGUNDOS_WHATSAPP} segundos.",
                            "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtSegmentacao.Focus();
                        return;
                    }
                }

                // valida stop programado (se marcado)
                int stopMin = 0;
                if (chkStop.Checked)
                {
                    if (!int.TryParse(txtStop.Text.Trim(), out stopMin) || stopMin < 1)
                    {
                        MessageBox.Show("Informe minutos válidos (inteiro ≥ 1) para o Stop programado.",
                            "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtStop.Focus();
                        return;
                    }
                }

                _pastaDaGravacaoAtual = CriarDiretorioGravavel();

                // montar argumentos FFmpeg
                string videoIn = $"-rtbufsize 256M -f gdigrab -draw_mouse 1 -framerate {FPS} -use_wallclock_as_timestamps 1 -i desktop";

                // usar moniker se existir, senão o nome amigável
                string audioId = !string.IsNullOrWhiteSpace(dev.Moniker) ? dev.Moniker : dev.DisplayName;
                // Atenção: moniker contém barra invertida → precisa escapar as aspas apenas.
                string audioIn = $"-f dshow -thread_queue_size 1024 -use_wallclock_as_timestamps 1 -i audio=\"{audioId}\"";

                string map = "-map 0:v -map 1:a -shortest ";

                string argsSaida;
                if (chkModoWhatsApp.Checked)
                {
                    string pattern = Path.Combine(_pastaDaGravacaoAtual, "Parte_%03d.mp4");
                    argsSaida =
                        "-c:v libx264 -preset veryfast -crf " + VIDEO_CRF + " -pix_fmt yuv420p " +
                        "-c:a aac -b:a " + AUDIO_KBPS + "k " +
                        "-fps_mode cfr -fflags +genpts " +
                        "-f segment -segment_time " + segmentSeconds + " -segment_time_delta 0.05 " +
                        "-force_key_frames \"expr:gte(t,n_forced*" + segmentSeconds + ")\" " +
                        "-reset_timestamps 1 -segment_format mp4 -segment_format_options movflags=+faststart " +
                        $"\"{pattern}\"";
                }
                else
                {
                    string arquivoSaida = Path.Combine(_pastaDaGravacaoAtual, "gravacao_final.mp4");
                    argsSaida =
                        "-c:v libx264 -preset veryfast -crf " + VIDEO_CRF + " -pix_fmt yuv420p " +
                        "-c:a aac -b:a " + AUDIO_KBPS + "k " +
                        "-fps_mode cfr -fflags +genpts -movflags +faststart " +
                        $"\"{arquivoSaida}\"";
                }

                string args = "-hide_banner -y " + videoIn + " " + audioIn + " " + map + argsSaida;

                // Inicia FFmpeg
                IniciarFfmpeg(ffmpeg, args);

                // Stop programado (se houver)
                if (chkStop.Checked && stopMin > 0)
                {
                    _stopAutoCts?.Cancel();
                    _stopAutoCts = new CancellationTokenSource();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(stopMin), _stopAutoCts.Token);
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (btnParar.Enabled) btnParar.PerformClick();
                            }));
                        }
                        catch (TaskCanceledException) { }
                    });
                }

                // travar UI
                btnIniciar.Enabled = false;
                btnParar.Enabled = true;
                chkModoWhatsApp.Enabled = false;
                cmbAudio.Enabled = false;
                chkStop.Enabled = false;
                txtStop.Enabled = false;
                txtSegmentacao.Enabled = false;
                this.Cursor = Cursors.WaitCursor;
                AtualizaStatus("Iniciando gravação...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar: " + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                FinalizarUI();
            }
        }

        private async void btnParar_Click(object sender, EventArgs e)
        {
            try
            {
                btnParar.Enabled = false;
                AtualizaStatus("Finalizando...", marquee: true);
                _stopAutoCts?.Cancel();
                await PararFfmpeg();
                var arquivoFinal = Path.Combine(_pastaDaGravacaoAtual, "gravacao_final.mp4");
                if (File.Exists(arquivoFinal)) VerificarSincronia(arquivoFinal);
                AtualizaStatus("Processamento concluído!", marquee: false);
                AbrirPasta(_pastaDaGravacaoAtual);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao finalizar: " + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FinalizarUI();
            }
        }

        // ==================== FFmpeg ====================

        private void IniciarFfmpeg(string ffmpegPath, string args)
        {
            Log("FFmpeg START: " + args);

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            _ffmpegProc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var tail = new StringBuilder();
            var timeRx = new Regex(@"time=(\d{2}):(\d{2}):(\d{2})");

            _ffmpegProc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                tail.AppendLine(e.Data);
                if (tail.Length > 8000) tail.Remove(0, tail.Length - 8000);

                var m = timeRx.Match(e.Data);
                if (m.Success)
                {
                    try
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            lblStatus.Text = "Gravando... " + m.Value.Replace("time=", "");
                        }));
                    }
                    catch { }
                }
            };

            _ffmpegProc.Exited += (s, e) =>
            {
                Log("FFmpeg EXIT code " + _ffmpegProc.ExitCode);
                if (tail.Length > 0) Log("FFmpeg stderr (fim): " + tail.ToString());
            };

            _ffmpegProc.Start();
            _ffmpegProc.BeginErrorReadLine();
        }

        private async Task PararFfmpeg()
        {
            if (_ffmpegProc == null) return;

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                _ffmpegProc.Exited += (s, e) => tcs.TrySetResult(true);

                if (!_ffmpegProc.HasExited)
                {
                    try { _ffmpegProc.StandardInput.WriteLine("q"); } catch { }

                    if (!_ffmpegProc.HasExited)
                    {
                        await Task.WhenAny(tcs.Task, Task.Delay(STOP_TIMEOUT_MS));

                        if (!_ffmpegProc.HasExited)
                        {
                            try { _ffmpegProc.Kill(); } catch { }
                        }
                    }
                }
            }
            finally
            {
                try { _ffmpegProc.Dispose(); } catch { }
                _ffmpegProc = null;
            }
        }
    }
}
