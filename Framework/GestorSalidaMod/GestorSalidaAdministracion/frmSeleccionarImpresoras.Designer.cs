namespace Framework.GestorSalida.Administracion
{
    partial class frmSeleccionarImpresoras
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
            this.ctrlContenedoresImpresora1 = new Framework.GestorSalida.Administracion.ctrlContenedoresImpresora();
            this.cmdCancelar = new System.Windows.Forms.Button();
            this.cmd_Aceptar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ctrlContenedoresImpresora1
            // 
            this.ctrlContenedoresImpresora1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ctrlContenedoresImpresora1.Location = new System.Drawing.Point(12, 12);
            this.ctrlContenedoresImpresora1.MensajeError = "";
            this.ctrlContenedoresImpresora1.Name = "ctrlContenedoresImpresora1";
            this.ctrlContenedoresImpresora1.Size = new System.Drawing.Size(532, 359);
            this.ctrlContenedoresImpresora1.TabIndex = 0;
            this.ctrlContenedoresImpresora1.ToolTipText = null;
            // 
            // cmdCancelar
            // 
            this.cmdCancelar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancelar.Location = new System.Drawing.Point(458, 378);
            this.cmdCancelar.Name = "cmdCancelar";
            this.cmdCancelar.Size = new System.Drawing.Size(87, 27);
            this.cmdCancelar.TabIndex = 7;
            this.cmdCancelar.Text = "Cancelar";
            this.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.cmdCancelar.UseVisualStyleBackColor = true;
            this.cmdCancelar.Click += new System.EventHandler(this.cmdCancelar_Click);
            // 
            // cmd_Aceptar
            // 
            this.cmd_Aceptar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmd_Aceptar.Location = new System.Drawing.Point(367, 378);
            this.cmd_Aceptar.Name = "cmd_Aceptar";
            this.cmd_Aceptar.Size = new System.Drawing.Size(85, 27);
            this.cmd_Aceptar.TabIndex = 6;
            this.cmd_Aceptar.Text = "Aceptar";
            this.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.cmd_Aceptar.UseVisualStyleBackColor = true;
            this.cmd_Aceptar.Click += new System.EventHandler(this.cmd_Aceptar_Click);
            // 
            // frmSeleccionarImpresoras
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(556, 413);
            this.Controls.Add(this.cmdCancelar);
            this.Controls.Add(this.cmd_Aceptar);
            this.Controls.Add(this.ctrlContenedoresImpresora1);
            this.Name = "frmSeleccionarImpresoras";
            this.Text = "Seleccionar Impresoras";
            this.ResumeLayout(false);

        }

        #endregion

        private ctrlContenedoresImpresora ctrlContenedoresImpresora1;
        internal System.Windows.Forms.Button cmdCancelar;
        internal System.Windows.Forms.Button cmd_Aceptar;
    }
}