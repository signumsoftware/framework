namespace Framework.GestorSalida.ClienteIU
{
    partial class frmEnviarDocumentoSalida
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmEnviarDocumentoSalida));
            this.cmdAgregarFichero = new ControlesPBase.BotonP();
            this.lstArchivosAdjuntos = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cboCanalSalida = new System.Windows.Forms.ComboBox();
            this.cmdCancelar = new ControlesPBase.BotonP();
            this.cmd_Aceptar = new ControlesPBase.BotonP();
            this.lblTamañoTotal = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cmdAgregarFichero
            // 
            this.cmdAgregarFichero.Location = new System.Drawing.Point(251, 7);
            this.cmdAgregarFichero.Name = "cmdAgregarFichero";
            this.cmdAgregarFichero.Size = new System.Drawing.Size(106, 24);
            this.cmdAgregarFichero.TabIndex = 0;
            this.cmdAgregarFichero.Text = "Agregar Archivo";
            this.cmdAgregarFichero.UseVisualStyleBackColor = true;
            this.cmdAgregarFichero.Click += new System.EventHandler(this.cmdAgregarFichero_Click);
            // 
            // lstArchivosAdjuntos
            // 
            this.lstArchivosAdjuntos.FormattingEnabled = true;
            this.lstArchivosAdjuntos.Location = new System.Drawing.Point(31, 42);
            this.lstArchivosAdjuntos.Name = "lstArchivosAdjuntos";
            this.lstArchivosAdjuntos.Size = new System.Drawing.Size(326, 108);
            this.lstArchivosAdjuntos.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Archivos Adjuntos:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 170);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Tipo de Envío";
            // 
            // cboCanalSalida
            // 
            this.cboCanalSalida.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCanalSalida.FormattingEnabled = true;
            this.cboCanalSalida.Location = new System.Drawing.Point(105, 167);
            this.cboCanalSalida.Name = "cboCanalSalida";
            this.cboCanalSalida.Size = new System.Drawing.Size(252, 21);
            this.cboCanalSalida.TabIndex = 4;
            // 
            // cmdCancelar
            // 
            this.cmdCancelar.Image = global::Framework.GestorSalida.ClienteIU.Properties.Resources.button_cancel;
            this.cmdCancelar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmdCancelar.Location = new System.Drawing.Point(172, 220);
            this.cmdCancelar.Name = "cmdCancelar";
            this.cmdCancelar.Size = new System.Drawing.Size(75, 31);
            this.cmdCancelar.TabIndex = 6;
            this.cmdCancelar.Text = "Cancelar";
            this.cmdCancelar.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cmdCancelar.UseVisualStyleBackColor = true;
            this.cmdCancelar.Click += new System.EventHandler(this.cmdCancelar_Click);
            // 
            // cmd_Aceptar
            // 
            this.cmd_Aceptar.Image = global::Framework.GestorSalida.ClienteIU.Properties.Resources._2rightarrow;
            this.cmd_Aceptar.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cmd_Aceptar.Location = new System.Drawing.Point(282, 220);
            this.cmd_Aceptar.Name = "cmd_Aceptar";
            this.cmd_Aceptar.Size = new System.Drawing.Size(75, 31);
            this.cmd_Aceptar.TabIndex = 5;
            this.cmd_Aceptar.Text = "Aceptar";
            this.cmd_Aceptar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmd_Aceptar.UseVisualStyleBackColor = true;
            this.cmd_Aceptar.Click += new System.EventHandler(this.cmd_Aceptar_Click);
            // 
            // lblTamañoTotal
            // 
            this.lblTamañoTotal.AutoSize = true;
            this.lblTamañoTotal.Location = new System.Drawing.Point(114, 13);
            this.lblTamañoTotal.Name = "lblTamañoTotal";
            this.lblTamañoTotal.Size = new System.Drawing.Size(38, 13);
            this.lblTamañoTotal.TabIndex = 7;
            this.lblTamañoTotal.Text = "(0 MB)";
            this.lblTamañoTotal.Visible = false;
            // 
            // frmEnviarDocumentoSalida
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 263);
            this.Controls.Add(this.lblTamañoTotal);
            this.Controls.Add(this.cmdCancelar);
            this.Controls.Add(this.cmd_Aceptar);
            this.Controls.Add(this.cboCanalSalida);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lstArchivosAdjuntos);
            this.Controls.Add(this.cmdAgregarFichero);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmEnviarDocumentoSalida";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Enviar Documento al Gestor de Salida";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ControlesPBase.BotonP cmdAgregarFichero;
        private System.Windows.Forms.ListBox lstArchivosAdjuntos;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboCanalSalida;
        private ControlesPBase.BotonP cmd_Aceptar;
        private ControlesPBase.BotonP cmdCancelar;
        private System.Windows.Forms.Label lblTamañoTotal;
    }
}