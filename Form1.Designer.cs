namespace GravadorDeTela
{
    partial class Form1
    {
        /// <summary>
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Windows Form Designer

        /// <summary>
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.btnIniciar = new System.Windows.Forms.Button();
            this.btnParar = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.chkModoWhatsApp = new System.Windows.Forms.CheckBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSegmentacao = new System.Windows.Forms.TextBox();
            this.txtStop = new System.Windows.Forms.TextBox();
            this.chkStop = new System.Windows.Forms.CheckBox();
            this.cmbAudio = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtAudioDelay = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.trkQualidade = new System.Windows.Forms.TrackBar();
            this.lblQualidadeValor = new System.Windows.Forms.Label();
            this.chkMicrofone = new System.Windows.Forms.CheckBox();
            this.cmbMicrofone = new System.Windows.Forms.ComboBox();
            this.chkOcultarAoGravar = new System.Windows.Forms.CheckBox();
            this.lblAtalhos = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trkQualidade)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI Semibold", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(286, 37);
            this.label1.TabIndex = 0;
            this.label1.Text = "Gravador de Tela Pro";
            // 
            // btnIniciar
            // 
            this.btnIniciar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIniciar.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnIniciar.ForeColor = System.Drawing.Color.White;
            this.btnIniciar.Location = new System.Drawing.Point(19, 66);
            this.btnIniciar.Name = "btnIniciar";
            this.btnIniciar.Size = new System.Drawing.Size(180, 38);
            this.btnIniciar.TabIndex = 1;
            this.btnIniciar.Text = "Iniciar gravação (F8)";
            this.btnIniciar.UseVisualStyleBackColor = true;
            this.btnIniciar.Click += new System.EventHandler(this.btnIniciar_Click);
            // 
            // btnParar
            // 
            this.btnParar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnParar.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnParar.ForeColor = System.Drawing.Color.White;
            this.btnParar.Location = new System.Drawing.Point(213, 66);
            this.btnParar.Name = "btnParar";
            this.btnParar.Size = new System.Drawing.Size(180, 38);
            this.btnParar.TabIndex = 2;
            this.btnParar.Text = "Parar gravação (F9)";
            this.btnParar.UseVisualStyleBackColor = true;
            this.btnParar.Click += new System.EventHandler(this.btnParar_Click);
            // 
            // button1
            // 
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(462, 321);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(139, 29);
            this.button1.TabIndex = 3;
            this.button1.Text = "Abrir Pasta";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnAbrirPasta_Click);
            // 
            // chkModoWhatsApp
            // 
            this.chkModoWhatsApp.AutoSize = true;
            this.chkModoWhatsApp.Checked = true;
            this.chkModoWhatsApp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkModoWhatsApp.ForeColor = System.Drawing.Color.White;
            this.chkModoWhatsApp.Location = new System.Drawing.Point(19, 250);
            this.chkModoWhatsApp.Name = "chkModoWhatsApp";
            this.chkModoWhatsApp.Size = new System.Drawing.Size(248, 17);
            this.chkModoWhatsApp.TabIndex = 4;
            this.chkModoWhatsApp.Text = "Modo WhatsApp (dividir e converter para MP4)";
            this.chkModoWhatsApp.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(19, 156);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(582, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(218)))), ((int)(((byte)(251)))));
            this.lblStatus.Location = new System.Drawing.Point(19, 182);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 20);
            this.lblStatus.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(42, 272);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(148, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Intervalo de Corte(Segundos):";
            // 
            // txtSegmentacao
            // 
            this.txtSegmentacao.Location = new System.Drawing.Point(196, 269);
            this.txtSegmentacao.Name = "txtSegmentacao";
            this.txtSegmentacao.Size = new System.Drawing.Size(72, 20);
            this.txtSegmentacao.TabIndex = 8;
            // 
            // txtStop
            // 
            this.txtStop.Location = new System.Drawing.Point(171, 324);
            this.txtStop.Name = "txtStop";
            this.txtStop.Size = new System.Drawing.Size(73, 20);
            this.txtStop.TabIndex = 9;
            // 
            // chkStop
            // 
            this.chkStop.AutoSize = true;
            this.chkStop.ForeColor = System.Drawing.Color.White;
            this.chkStop.Location = new System.Drawing.Point(19, 326);
            this.chkStop.Name = "chkStop";
            this.chkStop.Size = new System.Drawing.Size(144, 17);
            this.chkStop.TabIndex = 11;
            this.chkStop.Text = "Programar Stop(minutos):";
            this.chkStop.UseVisualStyleBackColor = true;
            // 
            // cmbAudio
            // 
            this.cmbAudio.FormattingEnabled = true;
            this.cmbAudio.Location = new System.Drawing.Point(443, 269);
            this.cmbAudio.Name = "cmbAudio";
            this.cmbAudio.Size = new System.Drawing.Size(158, 21);
            this.cmbAudio.TabIndex = 12;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(331, 251);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(140, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Escolha o audio de captura:";
            // 
            // label4
            //
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(218)))), ((int)(((byte)(251)))));
            this.label4.Location = new System.Drawing.Point(16, 116);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(372, 13);
            this.label4.TabIndex = 14;
            this.label4.Text = "Dica: marque ocultar para não exibir a tela durante a gravação em andamento.";
            //
            // txtAudioDelay
            //
            this.txtAudioDelay.Location = new System.Drawing.Point(530, 294);
            this.txtAudioDelay.Name = "txtAudioDelay";
            this.txtAudioDelay.Size = new System.Drawing.Size(72, 20);
            this.txtAudioDelay.TabIndex = 15;
            this.txtAudioDelay.Text = "0";
            //
            // label5
            //
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(331, 296);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(105, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Atraso do áudio (ms):";
            //
            // trkQualidade
            //
            this.trkQualidade.Location = new System.Drawing.Point(19, 202);
            this.trkQualidade.Maximum = 100;
            this.trkQualidade.Minimum = 1;
            this.trkQualidade.Name = "trkQualidade";
            this.trkQualidade.Size = new System.Drawing.Size(221, 45);
            this.trkQualidade.TabIndex = 17;
            this.trkQualidade.Value = 60;
            //
            // lblQualidadeValor
            //
            this.lblQualidadeValor.AutoSize = true;
            this.lblQualidadeValor.ForeColor = System.Drawing.Color.White;
            this.lblQualidadeValor.Location = new System.Drawing.Point(245, 215);
            this.lblQualidadeValor.Name = "lblQualidadeValor";
            this.lblQualidadeValor.Size = new System.Drawing.Size(116, 13);
            this.lblQualidadeValor.TabIndex = 18;
            this.lblQualidadeValor.Text = "Qualidade (1-100): 60";
            //
            // chkMicrofone
            //
            this.chkMicrofone.AutoSize = true;
            this.chkMicrofone.ForeColor = System.Drawing.Color.White;
            this.chkMicrofone.Location = new System.Drawing.Point(331, 223);
            this.chkMicrofone.Name = "chkMicrofone";
            this.chkMicrofone.Size = new System.Drawing.Size(114, 17);
            this.chkMicrofone.TabIndex = 19;
            this.chkMicrofone.Text = "Capturar microfone";
            this.chkMicrofone.UseVisualStyleBackColor = true;
            //
            // cmbMicrofone
            //
            this.cmbMicrofone.FormattingEnabled = true;
            this.cmbMicrofone.Location = new System.Drawing.Point(443, 221);
            this.cmbMicrofone.Name = "cmbMicrofone";
            this.cmbMicrofone.Size = new System.Drawing.Size(158, 21);
            this.cmbMicrofone.TabIndex = 20;
            // 
            // chkOcultarAoGravar
            // 
            this.chkOcultarAoGravar.AutoSize = true;
            this.chkOcultarAoGravar.Checked = true;
            this.chkOcultarAoGravar.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOcultarAoGravar.ForeColor = System.Drawing.Color.White;
            this.chkOcultarAoGravar.Location = new System.Drawing.Point(409, 77);
            this.chkOcultarAoGravar.Name = "chkOcultarAoGravar";
            this.chkOcultarAoGravar.Size = new System.Drawing.Size(192, 17);
            this.chkOcultarAoGravar.TabIndex = 21;
            this.chkOcultarAoGravar.Text = "Ocultar tela enquanto estiver gravando";
            this.chkOcultarAoGravar.UseVisualStyleBackColor = true;
            // 
            // lblAtalhos
            // 
            this.lblAtalhos.AutoSize = true;
            this.lblAtalhos.ForeColor = System.Drawing.Color.White;
            this.lblAtalhos.Location = new System.Drawing.Point(406, 99);
            this.lblAtalhos.Name = "lblAtalhos";
            this.lblAtalhos.Size = new System.Drawing.Size(183, 26);
            this.lblAtalhos.TabIndex = 22;
            this.lblAtalhos.Text = "Atalhos:\r\nF8 iniciar | F9 parar | Ctrl+Shift+H mostrar";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(32)))), ((int)(((byte)(47)))));
            this.ClientSize = new System.Drawing.Size(622, 365);
            this.Controls.Add(this.lblAtalhos);
            this.Controls.Add(this.chkOcultarAoGravar);
            this.Controls.Add(this.cmbMicrofone);
            this.Controls.Add(this.chkMicrofone);
            this.Controls.Add(this.lblQualidadeValor);
            this.Controls.Add(this.trkQualidade);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtAudioDelay);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbAudio);
            this.Controls.Add(this.chkStop);
            this.Controls.Add(this.txtStop);
            this.Controls.Add(this.txtSegmentacao);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.chkModoWhatsApp);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnParar);
            this.Controls.Add(this.btnIniciar);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Gravador de Tela Pro";
            ((System.ComponentModel.ISupportInitialize)(this.trkQualidade)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnIniciar;
        private System.Windows.Forms.Button btnParar;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox chkModoWhatsApp;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtSegmentacao;
        private System.Windows.Forms.TextBox txtStop;
        private System.Windows.Forms.CheckBox chkStop;
        private System.Windows.Forms.ComboBox cmbAudio;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtAudioDelay;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TrackBar trkQualidade;
        private System.Windows.Forms.Label lblQualidadeValor;
        private System.Windows.Forms.CheckBox chkMicrofone;
        private System.Windows.Forms.ComboBox cmbMicrofone;
        private System.Windows.Forms.CheckBox chkOcultarAoGravar;
        private System.Windows.Forms.Label lblAtalhos;
    }
}
