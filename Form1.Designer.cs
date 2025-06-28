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
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(118, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(371, 33);
            this.label1.TabIndex = 0;
            this.label1.Text = "Gravador de Tela para Lolo";
            // 
            // btnIniciar
            // 
            this.btnIniciar.Location = new System.Drawing.Point(151, 66);
            this.btnIniciar.Name = "btnIniciar";
            this.btnIniciar.Size = new System.Drawing.Size(108, 38);
            this.btnIniciar.TabIndex = 1;
            this.btnIniciar.Text = "Iniciar";
            this.btnIniciar.UseVisualStyleBackColor = true;
            this.btnIniciar.Click += new System.EventHandler(this.btnIniciar_Click);
            // 
            // btnParar
            // 
            this.btnParar.Location = new System.Drawing.Point(328, 66);
            this.btnParar.Name = "btnParar";
            this.btnParar.Size = new System.Drawing.Size(108, 38);
            this.btnParar.TabIndex = 2;
            this.btnParar.Text = "Parar";
            this.btnParar.UseVisualStyleBackColor = true;
            this.btnParar.Click += new System.EventHandler(this.btnParar_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(481, 215);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(122, 26);
            this.button1.TabIndex = 3;
            this.button1.Text = "Abrir Pasta";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // chkModoWhatsApp
            // 
            this.chkModoWhatsApp.AutoSize = true;
            this.chkModoWhatsApp.Checked = true;
            this.chkModoWhatsApp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkModoWhatsApp.Location = new System.Drawing.Point(12, 224);
            this.chkModoWhatsApp.Name = "chkModoWhatsApp";
            this.chkModoWhatsApp.Size = new System.Drawing.Size(248, 17);
            this.chkModoWhatsApp.TabIndex = 4;
            this.chkModoWhatsApp.Text = "Modo WhatsApp (dividir e converter para MP4)";
            this.chkModoWhatsApp.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 127);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(591, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Location = new System.Drawing.Point(12, 165);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 20);
            this.lblStatus.TabIndex = 6;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(615, 253);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.chkModoWhatsApp);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnParar);
            this.Controls.Add(this.btnIniciar);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
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
    }
}

