namespace Framework.GestorSalida.Administracion
{
    partial class frmFuncionImpresion
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmdCancelar = new System.Windows.Forms.Button();
            this.cmd_Aceptar = new System.Windows.Forms.Button();
            this.txtNombre = new ControlesPBase.textboxXT();
            this.txtDescripcion = new ControlesPBase.textboxXT();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Nombre";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Descripción";
            // 
            // cmdCancelar
            // 
            this.cmdCancelar.Location = new System.Drawing.Point(251, 88);
            this.cmdCancelar.Name = "cmdCancelar";
            this.cmdCancelar.Size = new System.Drawing.Size(87, 27);
            this.cmdCancelar.TabIndex = 5;
            this.cmdCancelar.Text = "Cancelar";
            this.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.cmdCancelar.UseVisualStyleBackColor = true;
            this.cmdCancelar.Click += new System.EventHandler(this.cmdCancelar_Click);
            // 
            // cmd_Aceptar
            // 
            this.cmd_Aceptar.Location = new System.Drawing.Point(160, 88);
            this.cmd_Aceptar.Name = "cmd_Aceptar";
            this.cmd_Aceptar.Size = new System.Drawing.Size(85, 27);
            this.cmd_Aceptar.TabIndex = 4;
            this.cmd_Aceptar.Text = "Aceptar";
            this.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.cmd_Aceptar.UseVisualStyleBackColor = true;
            this.cmd_Aceptar.Click += new System.EventHandler(this.cmd_Aceptar_Click);
            // 
            // txtNombre
            // 
            this.txtNombre.Extendido = false;
            this.txtNombre.Location = new System.Drawing.Point(99, 10);
            this.txtNombre.MensajeErrorValidacion = null;
            this.txtNombre.Name = "txtNombre";
            this.txtNombre.ReadonlyXT = false;
            this.txtNombre.Size = new System.Drawing.Size(239, 20);
            this.txtNombre.TabIndex = 6;
            this.txtNombre.ToolTipText = null;
            this.txtNombre.TrimText = false;
            // 
            // txtDescripcion
            // 
            this.txtDescripcion.Extendido = false;
            this.txtDescripcion.Location = new System.Drawing.Point(99, 50);
            this.txtDescripcion.MensajeErrorValidacion = null;
            this.txtDescripcion.Name = "txtDescripcion";
            this.txtDescripcion.ReadonlyXT = false;
            this.txtDescripcion.Size = new System.Drawing.Size(239, 20);
            this.txtDescripcion.TabIndex = 7;
            this.txtDescripcion.ToolTipText = null;
            this.txtDescripcion.TrimText = false;
            // 
            // frmFuncionImpresion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 127);
            this.Controls.Add(this.txtDescripcion);
            this.Controls.Add(this.txtNombre);
            this.Controls.Add(this.cmdCancelar);
            this.Controls.Add(this.cmd_Aceptar);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "frmFuncionImpresion";
            this.Text = "Función de Impresión";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.Button cmdCancelar;
        internal System.Windows.Forms.Button cmd_Aceptar;
        private ControlesPBase.textboxXT txtNombre;
        private ControlesPBase.textboxXT txtDescripcion;
    }
}