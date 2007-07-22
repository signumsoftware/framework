namespace Framework.GestorSalida.Administracion
{
    partial class frmAdministrarAreaImpresion
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.cmdEditarFuncionImpresion = new ControlesPBase.BotonP();
            this.dgvFunciones = new System.Windows.Forms.DataGridView();
            this.cmdReactivarFuncionImpresion = new ControlesPBase.BotonP();
            this.cmdEliminarFuncion = new ControlesPBase.BotonP();
            this.cmdAgregarFuncionImpresion = new ControlesPBase.BotonP();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.cmdRefrescarImpresors = new ControlesPBase.BotonP();
            this.label2 = new System.Windows.Forms.Label();
            this.lstImpresorasSistema = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmdAgregarImpresora = new ControlesPBase.BotonP();
            this.ctrlContenedoresImpresora1 = new Framework.GestorSalida.Administracion.ctrlContenedoresImpresora();
            this.cmdReactivarimpresora = new ControlesPBase.BotonP();
            this.cmdEliminarImpresora = new ControlesPBase.BotonP();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.dgvCategorias = new System.Windows.Forms.DataGridView();
            this.cmdEditarCategoria = new ControlesPBase.BotonP();
            this.cmdReactivarCategoria = new ControlesPBase.BotonP();
            this.cmdEliminarCategoria = new ControlesPBase.BotonP();
            this.cmdAgregarCategoria = new ControlesPBase.BotonP();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFunciones)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCategorias)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(618, 427);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.cmdEditarFuncionImpresion);
            this.tabPage1.Controls.Add(this.dgvFunciones);
            this.tabPage1.Controls.Add(this.cmdReactivarFuncionImpresion);
            this.tabPage1.Controls.Add(this.cmdEliminarFuncion);
            this.tabPage1.Controls.Add(this.cmdAgregarFuncionImpresion);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(610, 401);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Funciones de Impresión";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // cmdEditarFuncionImpresion
            // 
            this.cmdEditarFuncionImpresion.Location = new System.Drawing.Point(358, 16);
            this.cmdEditarFuncionImpresion.Name = "cmdEditarFuncionImpresion";
            this.cmdEditarFuncionImpresion.Size = new System.Drawing.Size(75, 23);
            this.cmdEditarFuncionImpresion.TabIndex = 4;
            this.cmdEditarFuncionImpresion.Text = "Editar";
            this.cmdEditarFuncionImpresion.UseVisualStyleBackColor = true;
            this.cmdEditarFuncionImpresion.Click += new System.EventHandler(this.cmdEditarFuncionImpresion_Click);
            // 
            // dgvFunciones
            // 
            this.dgvFunciones.AllowUserToAddRows = false;
            this.dgvFunciones.AllowUserToDeleteRows = false;
            this.dgvFunciones.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFunciones.Location = new System.Drawing.Point(15, 65);
            this.dgvFunciones.Name = "dgvFunciones";
            this.dgvFunciones.ReadOnly = true;
            this.dgvFunciones.RowHeadersVisible = false;
            this.dgvFunciones.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvFunciones.Size = new System.Drawing.Size(580, 318);
            this.dgvFunciones.TabIndex = 3;
            // 
            // cmdReactivarFuncionImpresion
            // 
            this.cmdReactivarFuncionImpresion.Location = new System.Drawing.Point(439, 16);
            this.cmdReactivarFuncionImpresion.Name = "cmdReactivarFuncionImpresion";
            this.cmdReactivarFuncionImpresion.Size = new System.Drawing.Size(75, 23);
            this.cmdReactivarFuncionImpresion.TabIndex = 2;
            this.cmdReactivarFuncionImpresion.Text = "Reactivar";
            this.cmdReactivarFuncionImpresion.UseVisualStyleBackColor = true;
            this.cmdReactivarFuncionImpresion.Click += new System.EventHandler(this.cmdReactivarFuncionImpresion_Click);
            // 
            // cmdEliminarFuncion
            // 
            this.cmdEliminarFuncion.Location = new System.Drawing.Point(520, 16);
            this.cmdEliminarFuncion.Name = "cmdEliminarFuncion";
            this.cmdEliminarFuncion.Size = new System.Drawing.Size(75, 23);
            this.cmdEliminarFuncion.TabIndex = 2;
            this.cmdEliminarFuncion.Text = "Eliminar";
            this.cmdEliminarFuncion.UseVisualStyleBackColor = true;
            this.cmdEliminarFuncion.Click += new System.EventHandler(this.cmdEliminarFuncion_Click);
            // 
            // cmdAgregarFuncionImpresion
            // 
            this.cmdAgregarFuncionImpresion.Location = new System.Drawing.Point(277, 16);
            this.cmdAgregarFuncionImpresion.Name = "cmdAgregarFuncionImpresion";
            this.cmdAgregarFuncionImpresion.Size = new System.Drawing.Size(75, 23);
            this.cmdAgregarFuncionImpresion.TabIndex = 1;
            this.cmdAgregarFuncionImpresion.Text = "Agregar";
            this.cmdAgregarFuncionImpresion.UseVisualStyleBackColor = true;
            this.cmdAgregarFuncionImpresion.Click += new System.EventHandler(this.cmdAgregarFuncion_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(610, 401);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Impresoras";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Location = new System.Drawing.Point(3, 6);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.cmdRefrescarImpresors);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.lstImpresorasSistema);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2.Controls.Add(this.cmdAgregarImpresora);
            this.splitContainer1.Panel2.Controls.Add(this.ctrlContenedoresImpresora1);
            this.splitContainer1.Panel2.Controls.Add(this.cmdReactivarimpresora);
            this.splitContainer1.Panel2.Controls.Add(this.cmdEliminarImpresora);
            this.splitContainer1.Size = new System.Drawing.Size(601, 389);
            this.splitContainer1.SplitterDistance = 175;
            this.splitContainer1.TabIndex = 10;
            // 
            // cmdRefrescarImpresors
            // 
            this.cmdRefrescarImpresors.Location = new System.Drawing.Point(511, 9);
            this.cmdRefrescarImpresors.Name = "cmdRefrescarImpresors";
            this.cmdRefrescarImpresors.Size = new System.Drawing.Size(75, 23);
            this.cmdRefrescarImpresors.TabIndex = 11;
            this.cmdRefrescarImpresors.Text = "Refrescar";
            this.cmdRefrescarImpresors.UseVisualStyleBackColor = true;
            this.cmdRefrescarImpresors.Click += new System.EventHandler(this.cmdRefrescarImpresors_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(115, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Impresoras del Sistema";
            // 
            // lstImpresorasSistema
            // 
            this.lstImpresorasSistema.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lstImpresorasSistema.FormattingEnabled = true;
            this.lstImpresorasSistema.Location = new System.Drawing.Point(13, 38);
            this.lstImpresorasSistema.Name = "lstImpresorasSistema";
            this.lstImpresorasSistema.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lstImpresorasSistema.Size = new System.Drawing.Size(573, 121);
            this.lstImpresorasSistema.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Impresoras Asociadas";
            // 
            // cmdAgregarImpresora
            // 
            this.cmdAgregarImpresora.Location = new System.Drawing.Point(270, 13);
            this.cmdAgregarImpresora.Name = "cmdAgregarImpresora";
            this.cmdAgregarImpresora.Size = new System.Drawing.Size(75, 23);
            this.cmdAgregarImpresora.TabIndex = 5;
            this.cmdAgregarImpresora.Text = "Agregar";
            this.cmdAgregarImpresora.UseVisualStyleBackColor = true;
            this.cmdAgregarImpresora.Click += new System.EventHandler(this.cmdAgregarImpresora_Click);
            // 
            // ctrlContenedoresImpresora1
            // 
            this.ctrlContenedoresImpresora1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ctrlContenedoresImpresora1.Location = new System.Drawing.Point(13, 47);
            this.ctrlContenedoresImpresora1.MensajeError = "";
            this.ctrlContenedoresImpresora1.Name = "ctrlContenedoresImpresora1";
            this.ctrlContenedoresImpresora1.Size = new System.Drawing.Size(573, 158);
            this.ctrlContenedoresImpresora1.TabIndex = 0;
            this.ctrlContenedoresImpresora1.ToolTipText = null;
            // 
            // cmdReactivarimpresora
            // 
            this.cmdReactivarimpresora.Location = new System.Drawing.Point(432, 13);
            this.cmdReactivarimpresora.Name = "cmdReactivarimpresora";
            this.cmdReactivarimpresora.Size = new System.Drawing.Size(75, 23);
            this.cmdReactivarimpresora.TabIndex = 7;
            this.cmdReactivarimpresora.Text = "Reactivar";
            this.cmdReactivarimpresora.UseVisualStyleBackColor = true;
            this.cmdReactivarimpresora.Click += new System.EventHandler(this.cmdReactivarimpresora_Click);
            // 
            // cmdEliminarImpresora
            // 
            this.cmdEliminarImpresora.Location = new System.Drawing.Point(513, 13);
            this.cmdEliminarImpresora.Name = "cmdEliminarImpresora";
            this.cmdEliminarImpresora.Size = new System.Drawing.Size(75, 23);
            this.cmdEliminarImpresora.TabIndex = 6;
            this.cmdEliminarImpresora.Text = "Eliminar";
            this.cmdEliminarImpresora.UseVisualStyleBackColor = true;
            this.cmdEliminarImpresora.Click += new System.EventHandler(this.cmdEliminarImpresora_Click);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.dgvCategorias);
            this.tabPage3.Controls.Add(this.cmdEditarCategoria);
            this.tabPage3.Controls.Add(this.cmdReactivarCategoria);
            this.tabPage3.Controls.Add(this.cmdEliminarCategoria);
            this.tabPage3.Controls.Add(this.cmdAgregarCategoria);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(610, 401);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Categorías";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // dgvCategorias
            // 
            this.dgvCategorias.AllowUserToAddRows = false;
            this.dgvCategorias.AllowUserToDeleteRows = false;
            this.dgvCategorias.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCategorias.Location = new System.Drawing.Point(15, 65);
            this.dgvCategorias.Name = "dgvCategorias";
            this.dgvCategorias.ReadOnly = true;
            this.dgvCategorias.RowHeadersVisible = false;
            this.dgvCategorias.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCategorias.Size = new System.Drawing.Size(580, 318);
            this.dgvCategorias.TabIndex = 14;
            // 
            // cmdEditarCategoria
            // 
            this.cmdEditarCategoria.Location = new System.Drawing.Point(358, 16);
            this.cmdEditarCategoria.Name = "cmdEditarCategoria";
            this.cmdEditarCategoria.Size = new System.Drawing.Size(75, 23);
            this.cmdEditarCategoria.TabIndex = 13;
            this.cmdEditarCategoria.Text = "Editar";
            this.cmdEditarCategoria.UseVisualStyleBackColor = true;
            this.cmdEditarCategoria.Click += new System.EventHandler(this.cmdEditarCategoria_Click_1);
            // 
            // cmdReactivarCategoria
            // 
            this.cmdReactivarCategoria.Location = new System.Drawing.Point(439, 16);
            this.cmdReactivarCategoria.Name = "cmdReactivarCategoria";
            this.cmdReactivarCategoria.Size = new System.Drawing.Size(75, 23);
            this.cmdReactivarCategoria.TabIndex = 12;
            this.cmdReactivarCategoria.Text = "Reactivar";
            this.cmdReactivarCategoria.UseVisualStyleBackColor = true;
            this.cmdReactivarCategoria.Click += new System.EventHandler(this.cmdReactivarCategoria_Click_1);
            // 
            // cmdEliminarCategoria
            // 
            this.cmdEliminarCategoria.Location = new System.Drawing.Point(520, 16);
            this.cmdEliminarCategoria.Name = "cmdEliminarCategoria";
            this.cmdEliminarCategoria.Size = new System.Drawing.Size(75, 23);
            this.cmdEliminarCategoria.TabIndex = 11;
            this.cmdEliminarCategoria.Text = "Eliminar";
            this.cmdEliminarCategoria.UseVisualStyleBackColor = true;
            this.cmdEliminarCategoria.Click += new System.EventHandler(this.cmdEliminarCategoria_Click_1);
            // 
            // cmdAgregarCategoria
            // 
            this.cmdAgregarCategoria.Location = new System.Drawing.Point(277, 16);
            this.cmdAgregarCategoria.Name = "cmdAgregarCategoria";
            this.cmdAgregarCategoria.Size = new System.Drawing.Size(75, 23);
            this.cmdAgregarCategoria.TabIndex = 10;
            this.cmdAgregarCategoria.Text = "Agregar";
            this.cmdAgregarCategoria.UseVisualStyleBackColor = true;
            this.cmdAgregarCategoria.Click += new System.EventHandler(this.cmdAgregarCategoria_Click_1);
            // 
            // frmAdministrarAreaImpresion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(642, 462);
            this.Controls.Add(this.tabControl1);
            this.Name = "frmAdministrarAreaImpresion";
            this.Text = "Form1";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFunciones)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCategorias)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private ControlesPBase.BotonP cmdAgregarFuncionImpresion;
        private ControlesPBase.BotonP cmdEliminarFuncion;
        private System.Windows.Forms.DataGridView dgvFunciones;
        private ControlesPBase.BotonP cmdEditarFuncionImpresion;
        private ControlesPBase.BotonP cmdReactivarFuncionImpresion;
        private System.Windows.Forms.DataGridView dgvCategorias;
        private ControlesPBase.BotonP cmdEditarCategoria;
        private ControlesPBase.BotonP cmdReactivarCategoria;
        private ControlesPBase.BotonP cmdEliminarCategoria;
        private ControlesPBase.BotonP cmdAgregarCategoria;
        private ctrlContenedoresImpresora ctrlContenedoresImpresora1;
        private ControlesPBase.BotonP cmdReactivarimpresora;
        private ControlesPBase.BotonP cmdEliminarImpresora;
        private ControlesPBase.BotonP cmdAgregarImpresora;
        private System.Windows.Forms.ListBox lstImpresorasSistema;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private ControlesPBase.BotonP cmdRefrescarImpresors;
    }
}

