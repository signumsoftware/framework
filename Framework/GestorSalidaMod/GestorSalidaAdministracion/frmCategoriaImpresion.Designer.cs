namespace Framework.GestorSalida.Administracion
{
    partial class frmCategoriaImpresoras
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
            this.cboFuncion = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lstImpresoras = new System.Windows.Forms.ListBox();
            this.cmdEliminarCategoria = new ControlesPBase.BotonP();
            this.cmdAgregarCategoria = new ControlesPBase.BotonP();
            this.cmdCancelar = new System.Windows.Forms.Button();
            this.cmd_Aceptar = new System.Windows.Forms.Button();
            this.txtNombre = new ControlesPBase.textboxXT();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Nombre";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Función de Impresión";
            // 
            // cboFuncion
            // 
            this.cboFuncion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFuncion.FormattingEnabled = true;
            this.cboFuncion.Location = new System.Drawing.Point(136, 49);
            this.cboFuncion.Name = "cboFuncion";
            this.cboFuncion.Size = new System.Drawing.Size(234, 21);
            this.cboFuncion.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 95);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(110, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Impresoras Asignadas";
            // 
            // lstImpresoras
            // 
            this.lstImpresoras.FormattingEnabled = true;
            this.lstImpresoras.Location = new System.Drawing.Point(16, 119);
            this.lstImpresoras.Name = "lstImpresoras";
            this.lstImpresoras.Size = new System.Drawing.Size(354, 212);
            this.lstImpresoras.TabIndex = 4;
            // 
            // cmdEliminarCategoria
            // 
            this.cmdEliminarCategoria.Location = new System.Drawing.Point(295, 90);
            this.cmdEliminarCategoria.Name = "cmdEliminarCategoria";
            this.cmdEliminarCategoria.Size = new System.Drawing.Size(75, 23);
            this.cmdEliminarCategoria.TabIndex = 8;
            this.cmdEliminarCategoria.Text = "Eliminar";
            this.cmdEliminarCategoria.UseVisualStyleBackColor = true;
            this.cmdEliminarCategoria.Click += new System.EventHandler(this.cmdEliminarCategoria_Click);
            // 
            // cmdAgregarCategoria
            // 
            this.cmdAgregarCategoria.Location = new System.Drawing.Point(214, 90);
            this.cmdAgregarCategoria.Name = "cmdAgregarCategoria";
            this.cmdAgregarCategoria.Size = new System.Drawing.Size(75, 23);
            this.cmdAgregarCategoria.TabIndex = 7;
            this.cmdAgregarCategoria.Text = "Agregar";
            this.cmdAgregarCategoria.UseVisualStyleBackColor = true;
            this.cmdAgregarCategoria.Click += new System.EventHandler(this.cmdAgregarCategoria_Click);
            // 
            // cmdCancelar
            // 
            this.cmdCancelar.Location = new System.Drawing.Point(283, 346);
            this.cmdCancelar.Name = "cmdCancelar";
            this.cmdCancelar.Size = new System.Drawing.Size(87, 27);
            this.cmdCancelar.TabIndex = 10;
            this.cmdCancelar.Text = "Cancelar";
            this.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.cmdCancelar.UseVisualStyleBackColor = true;
            this.cmdCancelar.Click += new System.EventHandler(this.cmdCancelar_Click);
            // 
            // cmd_Aceptar
            // 
            this.cmd_Aceptar.Location = new System.Drawing.Point(192, 346);
            this.cmd_Aceptar.Name = "cmd_Aceptar";
            this.cmd_Aceptar.Size = new System.Drawing.Size(85, 27);
            this.cmd_Aceptar.TabIndex = 9;
            this.cmd_Aceptar.Text = "Aceptar";
            this.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.cmd_Aceptar.UseVisualStyleBackColor = true;
            this.cmd_Aceptar.Click += new System.EventHandler(this.cmd_Aceptar_Click);
            // 
            // txtNombre
            // 
            this.txtNombre.Extendido = false;
            this.txtNombre.Location = new System.Drawing.Point(136, 18);
            this.txtNombre.MensajeErrorValidacion = null;
            this.txtNombre.Name = "txtNombre";
            this.txtNombre.ReadonlyXT = false;
            this.txtNombre.Size = new System.Drawing.Size(234, 20);
            this.txtNombre.TabIndex = 11;
            this.txtNombre.ToolTipText = null;
            this.txtNombre.TrimText = false;
            // 
            // frmCategoriaImpresoras
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(387, 388);
            this.Controls.Add(this.txtNombre);
            this.Controls.Add(this.cmdCancelar);
            this.Controls.Add(this.cmd_Aceptar);
            this.Controls.Add(this.cmdEliminarCategoria);
            this.Controls.Add(this.cmdAgregarCategoria);
            this.Controls.Add(this.lstImpresoras);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cboFuncion);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "frmCategoriaImpresoras";
            this.Text = "Categoría de Impresión";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboFuncion;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lstImpresoras;
        private ControlesPBase.BotonP cmdEliminarCategoria;
        private ControlesPBase.BotonP cmdAgregarCategoria;
        internal System.Windows.Forms.Button cmdCancelar;
        internal System.Windows.Forms.Button cmd_Aceptar;
        private ControlesPBase.textboxXT txtNombre;
    }
}