namespace VisualObject
{
    partial class VisualObjectFrm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VisualObjectFrm));
            this.button1 = new System.Windows.Forms.Button();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.tbViento = new System.Windows.Forms.TrackBar();
            this.tbRepulsion = new System.Windows.Forms.TrackBar();
            this.tbMuelles = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbGravedad = new System.Windows.Forms.TrackBar();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.tbDistMuelles = new System.Windows.Forms.TrackBar();
            this.label5 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.cbLimitDeep = new System.Windows.Forms.CheckBox();
            this.nUDLimitDeep = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.cbLimitObjts = new System.Windows.Forms.CheckBox();
            this.nUDLimitObjts = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.visualObjectCtrl1 = new VisualObject.VisualObjectCtrl();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.label9 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tbViento)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbRepulsion)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMuelles)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbGravedad)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbDistMuelles)).BeginInit();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nUDLimitDeep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUDLimitObjts)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.ImageIndex = 0;
            this.button1.ImageList = this.imageList1;
            this.button1.Location = new System.Drawing.Point(12, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(58, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Play";
            this.button1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.Images.SetKeyName(0, "media_play_green.png");
            this.imageList1.Images.SetKeyName(1, "media_pause.png");
            this.imageList1.Images.SetKeyName(2, "media_stop_red.png");
            // 
            // tbViento
            // 
            this.tbViento.BackColor = System.Drawing.SystemColors.Control;
            this.tbViento.Location = new System.Drawing.Point(3, 3);
            this.tbViento.Name = "tbViento";
            this.tbViento.Size = new System.Drawing.Size(104, 45);
            this.tbViento.TabIndex = 3;
            this.tbViento.Value = 6;
            this.tbViento.Scroll += new System.EventHandler(this.tbViento_Scroll);
            // 
            // tbRepulsion
            // 
            this.tbRepulsion.BackColor = System.Drawing.SystemColors.Control;
            this.tbRepulsion.Location = new System.Drawing.Point(3, 51);
            this.tbRepulsion.Maximum = 30;
            this.tbRepulsion.Name = "tbRepulsion";
            this.tbRepulsion.Size = new System.Drawing.Size(104, 45);
            this.tbRepulsion.TabIndex = 4;
            this.tbRepulsion.Value = 15;
            this.tbRepulsion.Scroll += new System.EventHandler(this.tbRepulsion_Scroll);
            // 
            // tbMuelles
            // 
            this.tbMuelles.BackColor = System.Drawing.SystemColors.Control;
            this.tbMuelles.Location = new System.Drawing.Point(3, 102);
            this.tbMuelles.Name = "tbMuelles";
            this.tbMuelles.Size = new System.Drawing.Size(104, 45);
            this.tbMuelles.TabIndex = 5;
            this.tbMuelles.Value = 5;
            this.tbMuelles.Scroll += new System.EventHandler(this.tbMuelles_Scroll);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Wind";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Repulsion";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 134);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(76, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Spring Strength";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 254);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Friction";
            // 
            // tbGravedad
            // 
            this.tbGravedad.BackColor = System.Drawing.SystemColors.Control;
            this.tbGravedad.Location = new System.Drawing.Point(3, 222);
            this.tbGravedad.Maximum = 100;
            this.tbGravedad.Name = "tbGravedad";
            this.tbGravedad.Size = new System.Drawing.Size(104, 45);
            this.tbGravedad.TabIndex = 9;
            this.tbGravedad.Value = 70;
            this.tbGravedad.Scroll += new System.EventHandler(this.tbGravedad_Scroll);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(12, 33);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(159, 400);
            this.tabControl1.TabIndex = 11;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label6);
            this.tabPage3.Controls.Add(this.tbDistMuelles);
            this.tabPage3.Controls.Add(this.label2);
            this.tabPage3.Controls.Add(this.label4);
            this.tabPage3.Controls.Add(this.label1);
            this.tabPage3.Controls.Add(this.tbGravedad);
            this.tabPage3.Controls.Add(this.tbViento);
            this.tabPage3.Controls.Add(this.label3);
            this.tabPage3.Controls.Add(this.tbRepulsion);
            this.tabPage3.Controls.Add(this.tbMuelles);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(151, 374);
            this.tabPage3.TabIndex = 0;
            this.tabPage3.Text = "Phisics";
            this.tabPage3.UseVisualStyleBackColor = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 197);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Spring Distance";
            // 
            // tbDistMuelles
            // 
            this.tbDistMuelles.BackColor = System.Drawing.SystemColors.Control;
            this.tbDistMuelles.Location = new System.Drawing.Point(3, 165);
            this.tbDistMuelles.Maximum = 40;
            this.tbDistMuelles.Minimum = 5;
            this.tbDistMuelles.Name = "tbDistMuelles";
            this.tbDistMuelles.Size = new System.Drawing.Size(104, 45);
            this.tbDistMuelles.TabIndex = 11;
            this.tbDistMuelles.Value = 18;
            this.tbDistMuelles.Scroll += new System.EventHandler(this.tbDistMuelles_Scroll);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label5.Location = new System.Drawing.Point(15, 436);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(128, 39);
            this.label5.TabIndex = 12;
            this.label5.Text = "Olmo del Corral\r\nolmobrutall@hotmail.com\r\nArkadel 21, Madrid (Spain)";
            // 
            // button2
            // 
            this.button2.ImageIndex = 2;
            this.button2.ImageList = this.imageList1;
            this.button2.Location = new System.Drawing.Point(77, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(67, 23);
            this.button2.TabIndex = 13;
            this.button2.Text = "Stop";
            this.button2.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.treeView1);
            this.tabPage1.Controls.Add(this.checkBox1);
            this.tabPage1.Controls.Add(this.label8);
            this.tabPage1.Controls.Add(this.nUDLimitObjts);
            this.tabPage1.Controls.Add(this.cbLimitObjts);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.nUDLimitDeep);
            this.tabPage1.Controls.Add(this.cbLimitDeep);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(151, 374);
            this.tabPage1.TabIndex = 1;
            this.tabPage1.Text = "Build";
            // 
            // cbLimitDeep
            // 
            this.cbLimitDeep.AutoSize = true;
            this.cbLimitDeep.Location = new System.Drawing.Point(6, 6);
            this.cbLimitDeep.Name = "cbLimitDeep";
            this.cbLimitDeep.Size = new System.Drawing.Size(136, 17);
            this.cbLimitDeep.TabIndex = 1;
            this.cbLimitDeep.Text = "Limit exploration deep to";
            // 
            // nUDLimitDeep
            // 
            this.nUDLimitDeep.Location = new System.Drawing.Point(25, 29);
            this.nUDLimitDeep.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nUDLimitDeep.Name = "nUDLimitDeep";
            this.nUDLimitDeep.Size = new System.Drawing.Size(46, 20);
            this.nUDLimitDeep.TabIndex = 2;
            this.nUDLimitDeep.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(77, 29);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(28, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "steps";
            // 
            // cbLimitObjts
            // 
            this.cbLimitObjts.AutoSize = true;
            this.cbLimitObjts.Location = new System.Drawing.Point(6, 55);
            this.cbLimitObjts.Name = "cbLimitObjts";
            this.cbLimitObjts.Size = new System.Drawing.Size(115, 17);
            this.cbLimitObjts.TabIndex = 4;
            this.cbLimitObjts.Text = "Limit total objects to";
            // 
            // nUDLimitObjts
            // 
            this.nUDLimitObjts.Location = new System.Drawing.Point(27, 78);
            this.nUDLimitObjts.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nUDLimitObjts.Name = "nUDLimitObjts";
            this.nUDLimitObjts.Size = new System.Drawing.Size(44, 20);
            this.nUDLimitObjts.TabIndex = 5;
            this.nUDLimitObjts.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(77, 78);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 13);
            this.label8.TabIndex = 6;
            this.label8.Text = "objects";
            // 
            // visualObjectCtrl1
            // 
            this.visualObjectCtrl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.visualObjectCtrl1.BackColor = System.Drawing.Color.White;
            this.visualObjectCtrl1.Location = new System.Drawing.Point(177, 3);
            this.visualObjectCtrl1.Name = "visualObjectCtrl1";
            this.visualObjectCtrl1.Size = new System.Drawing.Size(481, 479);
            this.visualObjectCtrl1.TabIndex = 1;
            this.visualObjectCtrl1.VisualObjectEngine = null;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(6, 104);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(104, 17);
            this.checkBox1.TabIndex = 7;
            this.checkBox1.Text = "Pack ICollections";
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.CheckBoxes = true;
            this.treeView1.Location = new System.Drawing.Point(6, 140);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(139, 228);
            this.treeView1.TabIndex = 8;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 124);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(118, 13);
            this.label9.TabIndex = 9;
            this.label9.Text = "Enable/Disable by Class";
            // 
            // VisualObjectFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(670, 494);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.visualObjectCtrl1);
            this.Controls.Add(this.button1);
            this.Name = "VisualObjectFrm";
            this.Text = "VisualObject";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VisualObjectFrm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.tbViento)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbRepulsion)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMuelles)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbGravedad)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbDistMuelles)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nUDLimitDeep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUDLimitObjts)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TrackBar tbViento;
        private System.Windows.Forms.TrackBar tbRepulsion;
        private System.Windows.Forms.TrackBar tbMuelles;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar tbGravedad;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TrackBar tbDistMuelles;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown nUDLimitObjts;
        private System.Windows.Forms.CheckBox cbLimitObjts;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown nUDLimitDeep;
        private System.Windows.Forms.CheckBox cbLimitDeep;
        private VisualObjectCtrl visualObjectCtrl1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Label label9;
    }
}

