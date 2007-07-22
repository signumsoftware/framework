namespace Framework.GestorSalida.ClienteIU
{
    partial class frmConfiguracionEnvioImpresion
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmConfiguracionEnvioImpresion));
            this.label1 = new System.Windows.Forms.Label();
            this.nudCopias = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.cboFuncionImpresora = new System.Windows.Forms.ComboBox();
            this.chkPersistente = new ControlesPBase.CheckBoxP();
            this.label3 = new System.Windows.Forms.Label();
            this.tcbPrioridad = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.chkMostrarTicket = new ControlesPBase.CheckBoxP();
            this.cmdCancelar = new ControlesPBase.BotonP();
            this.cmdAtras = new ControlesPBase.BotonP();
            this.cmd_Aceptar = new ControlesPBase.BotonP();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudCopias)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tcbPrioridad)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Número de Copias";
            // 
            // nudCopias
            // 
            this.nudCopias.Location = new System.Drawing.Point(125, 11);
            this.nudCopias.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudCopias.Name = "nudCopias";
            this.nudCopias.Size = new System.Drawing.Size(45, 20);
            this.nudCopias.TabIndex = 1;
            this.nudCopias.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Tipo de Impresora";
            // 
            // cboFuncionImpresora
            // 
            this.cboFuncionImpresora.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFuncionImpresora.FormattingEnabled = true;
            this.cboFuncionImpresora.Location = new System.Drawing.Point(125, 60);
            this.cboFuncionImpresora.Name = "cboFuncionImpresora";
            this.cboFuncionImpresora.Size = new System.Drawing.Size(253, 21);
            this.cboFuncionImpresora.TabIndex = 3;
            // 
            // chkPersistente
            // 
            this.chkPersistente.AutoSize = true;
            this.chkPersistente.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkPersistente.ColorBaseIluminacion = System.Drawing.Color.Orange;
            this.chkPersistente.Location = new System.Drawing.Point(266, 122);
            this.chkPersistente.Name = "chkPersistente";
            this.chkPersistente.Size = new System.Drawing.Size(15, 14);
            this.chkPersistente.TabIndex = 4;
            this.chkPersistente.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 173);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Prioridad de la salida";
            // 
            // tcbPrioridad
            // 
            this.tcbPrioridad.Location = new System.Drawing.Point(139, 168);
            this.tcbPrioridad.Name = "tcbPrioridad";
            this.tcbPrioridad.Size = new System.Drawing.Size(239, 45);
            this.tcbPrioridad.TabIndex = 6;
            this.tcbPrioridad.Value = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(136, 200);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(28, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Baja";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(353, 200);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(25, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Alta";
            // 
            // chkMostrarTicket
            // 
            this.chkMostrarTicket.AutoSize = true;
            this.chkMostrarTicket.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkMostrarTicket.ColorBaseIluminacion = System.Drawing.Color.Orange;
            this.chkMostrarTicket.Location = new System.Drawing.Point(224, 240);
            this.chkMostrarTicket.Name = "chkMostrarTicket";
            this.chkMostrarTicket.Size = new System.Drawing.Size(15, 14);
            this.chkMostrarTicket.TabIndex = 4;
            this.chkMostrarTicket.UseVisualStyleBackColor = true;
            // 
            // cmdCancelar
            // 
            this.cmdCancelar.Image = global::Framework.GestorSalida.ClienteIU.Properties.Resources.button_cancel;
            this.cmdCancelar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmdCancelar.Location = new System.Drawing.Point(106, 290);
            this.cmdCancelar.Name = "cmdCancelar";
            this.cmdCancelar.Size = new System.Drawing.Size(75, 31);
            this.cmdCancelar.TabIndex = 10;
            this.cmdCancelar.Text = "Cancelar";
            this.cmdCancelar.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cmdCancelar.UseVisualStyleBackColor = true;
            this.cmdCancelar.Click += new System.EventHandler(this.cmdCancelar_Click);
            // 
            // cmdAtras
            // 
            this.cmdAtras.Image = global::Framework.GestorSalida.ClienteIU.Properties.Resources._2leftarrow;
            this.cmdAtras.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmdAtras.Location = new System.Drawing.Point(222, 290);
            this.cmdAtras.Name = "cmdAtras";
            this.cmdAtras.Size = new System.Drawing.Size(75, 31);
            this.cmdAtras.TabIndex = 9;
            this.cmdAtras.Text = "Atrás";
            this.cmdAtras.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cmdAtras.UseVisualStyleBackColor = true;
            this.cmdAtras.Click += new System.EventHandler(this.cmdAtras_Click);
            // 
            // cmd_Aceptar
            // 
            this.cmd_Aceptar.Image = global::Framework.GestorSalida.ClienteIU.Properties.Resources.apply;
            this.cmd_Aceptar.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cmd_Aceptar.Location = new System.Drawing.Point(303, 290);
            this.cmd_Aceptar.Name = "cmd_Aceptar";
            this.cmd_Aceptar.Size = new System.Drawing.Size(75, 31);
            this.cmd_Aceptar.TabIndex = 9;
            this.cmd_Aceptar.Text = "Aceptar";
            this.cmd_Aceptar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmd_Aceptar.UseVisualStyleBackColor = true;
            this.cmd_Aceptar.Click += new System.EventHandler(this.cmd_Aceptar_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(16, 240);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(202, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Mostrar Ticket de referencia tras el envío";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(16, 122);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(244, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Guardar Copia en el Servidor de Gestión de Salida";
            // 
            // frmConfiguracionEnvioImpresion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(412, 333);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cmdCancelar);
            this.Controls.Add(this.cmdAtras);
            this.Controls.Add(this.cmd_Aceptar);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tcbPrioridad);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.chkMostrarTicket);
            this.Controls.Add(this.chkPersistente);
            this.Controls.Add(this.cboFuncionImpresora);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nudCopias);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmConfiguracionEnvioImpresion";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configuración de Envío a Impresora";
            ((System.ComponentModel.ISupportInitialize)(this.nudCopias)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tcbPrioridad)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudCopias;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboFuncionImpresora;
        private ControlesPBase.CheckBoxP chkPersistente;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TrackBar tcbPrioridad;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private ControlesPBase.BotonP cmd_Aceptar;
        private ControlesPBase.BotonP cmdCancelar;
        private ControlesPBase.CheckBoxP chkMostrarTicket;
        private ControlesPBase.BotonP cmdAtras;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
    }
}