namespace Framework.GestorSalida.ClienteIU
{
    partial class frmEnvio
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.loadingCircle1 = new MRG.Controls.UI.LoadingCircle();
            this.label1 = new System.Windows.Forms.Label();
            this.txtTicket = new ControlesPBase.textboxXT();
            this.SuspendLayout();
            // 
            // loadingCircle1
            // 
            this.loadingCircle1.Active = true;
            this.loadingCircle1.Color = System.Drawing.Color.Blue;
            this.loadingCircle1.InnerCircleRadius = 8;
            this.loadingCircle1.Location = new System.Drawing.Point(1, 38);
            this.loadingCircle1.Name = "loadingCircle1";
            this.loadingCircle1.NumberSpoke = 24;
            this.loadingCircle1.OuterCircleRadius = 9;
            this.loadingCircle1.RotationSpeed = 100;
            this.loadingCircle1.Size = new System.Drawing.Size(75, 23);
            this.loadingCircle1.SpokeThickness = 4;
            this.loadingCircle1.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
            this.loadingCircle1.TabIndex = 0;
            this.loadingCircle1.Text = "loadingCircle1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(82, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(211, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Enviando Documento al Gestor de Salida...";
            // 
            // txtTicket
            // 
            this.txtTicket.Extendido = false;
            this.txtTicket.Location = new System.Drawing.Point(12, 76);
            this.txtTicket.MensajeErrorValidacion = null;
            this.txtTicket.Name = "txtTicket";
            this.txtTicket.ReadOnly = true;
            this.txtTicket.ReadonlyXT = true;
            this.txtTicket.Size = new System.Drawing.Size(304, 20);
            this.txtTicket.TabIndex = 2;
            this.txtTicket.ToolTipText = null;
            this.txtTicket.TrimText = false;
            this.txtTicket.Visible = false;
            // 
            // frmEnvio
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(328, 108);
            this.ControlBox = false;
            this.Controls.Add(this.txtTicket);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.loadingCircle1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmEnvio";
            this.Text = "Envío de Documento de Salida";
            this.Shown += new System.EventHandler(this.frmEnvio_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MRG.Controls.UI.LoadingCircle loadingCircle1;
        private System.Windows.Forms.Label label1;
        private ControlesPBase.textboxXT txtTicket;
    }
}