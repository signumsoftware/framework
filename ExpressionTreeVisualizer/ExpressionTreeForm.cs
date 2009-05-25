using System;
using System.Text;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.CSharp;
using System.Collections;
using System.Linq.Expressions;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Diagnostics;
using System.Runtime.Serialization;
using Signum.Utilities;

namespace ExpressionVisualizer
{
    public class TreeWindow : Form
    {
        private System.Windows.Forms.TextBox errorMessageBox;
        private Label lbProperty;
        private Label lbExpression;
        private Label lbParameter;
        private Label lbLambda;
        private Label lbForeingExpression;
        private Splitter splitter1;
        public TreeView browser;

        private void InitializeComponent()
        {
            this.errorMessageBox = new System.Windows.Forms.TextBox();
            this.browser = new System.Windows.Forms.TreeView();
            this.lbProperty = new System.Windows.Forms.Label();
            this.lbExpression = new System.Windows.Forms.Label();
            this.lbParameter = new System.Windows.Forms.Label();
            this.lbLambda = new System.Windows.Forms.Label();
            this.lbForeingExpression = new System.Windows.Forms.Label();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.SuspendLayout();
            // 
            // errorMessageBox
            // 
            this.errorMessageBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.errorMessageBox.Location = new System.Drawing.Point(0, 0);
            this.errorMessageBox.Multiline = true;
            this.errorMessageBox.Name = "errorMessageBox";
            this.errorMessageBox.ReadOnly = true;
            this.errorMessageBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.errorMessageBox.Size = new System.Drawing.Size(584, 116);
            this.errorMessageBox.TabIndex = 1;
            this.errorMessageBox.TabStop = false;
            this.errorMessageBox.Text = "Bla";
            // 
            // browser
            // 
            this.browser.BackColor = System.Drawing.Color.White;
            this.browser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.browser.Location = new System.Drawing.Point(0, 116);
            this.browser.Name = "browser";
            this.browser.Size = new System.Drawing.Size(584, 648);
            this.browser.TabIndex = 2;
            this.browser.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.browser_BeforeSelect);
            // 
            // lbProperty
            // 
            this.lbProperty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbProperty.AutoSize = true;
            this.lbProperty.Location = new System.Drawing.Point(415, 746);
            this.lbProperty.Name = "lbProperty";
            this.lbProperty.Size = new System.Drawing.Size(46, 13);
            this.lbProperty.TabIndex = 3;
            this.lbProperty.Text = "Property";
            // 
            // lbExpression
            // 
            this.lbExpression.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbExpression.AutoSize = true;
            this.lbExpression.Location = new System.Drawing.Point(188, 746);
            this.lbExpression.Name = "lbExpression";
            this.lbExpression.Size = new System.Drawing.Size(58, 13);
            this.lbExpression.TabIndex = 4;
            this.lbExpression.Text = "Expression";
            // 
            // lbParameter
            // 
            this.lbParameter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbParameter.AutoSize = true;
            this.lbParameter.Location = new System.Drawing.Point(95, 746);
            this.lbParameter.Name = "lbParameter";
            this.lbParameter.Size = new System.Drawing.Size(55, 13);
            this.lbParameter.TabIndex = 5;
            this.lbParameter.Text = "Parameter";
            // 
            // lbLambda
            // 
            this.lbLambda.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbLambda.AutoSize = true;
            this.lbLambda.Location = new System.Drawing.Point(12, 746);
            this.lbLambda.Name = "lbLambda";
            this.lbLambda.Size = new System.Drawing.Size(45, 13);
            this.lbLambda.TabIndex = 6;
            this.lbLambda.Text = "Lambda";
            // 
            // lbForeingExpression
            // 
            this.lbForeingExpression.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbForeingExpression.AutoSize = true;
            this.lbForeingExpression.Location = new System.Drawing.Point(284, 746);
            this.lbForeingExpression.Name = "lbForeingExpression";
            this.lbForeingExpression.Size = new System.Drawing.Size(93, 13);
            this.lbForeingExpression.TabIndex = 7;
            this.lbForeingExpression.Text = "ForeingExpression";
            // 
            // splitter1
            // 
            this.splitter1.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 116);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(584, 3);
            this.splitter1.TabIndex = 8;
            this.splitter1.TabStop = false;
            // 
            // TreeWindow
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(584, 764);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.lbForeingExpression);
            this.Controls.Add(this.lbLambda);
            this.Controls.Add(this.lbParameter);
            this.Controls.Add(this.lbExpression);
            this.Controls.Add(this.lbProperty);
            this.Controls.Add(this.browser);
            this.Controls.Add(this.errorMessageBox);
            this.Name = "TreeWindow";
            this.Text = "Expression Tree Viewer";
            this.Load += new System.EventHandler(this.TreeWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        public TreeWindow()
        {
            InitializeComponent(); 
        }

        private void browser_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            object tag = e.Node.Tag;
            this.errorMessageBox.Text = tag == null ? "" : tag.ToString();
        }

        private void TreeWindow_Load(object sender, EventArgs e)
        {
            lbExpression.BackColor = ExpressionTreeNodeBuilder.ExpressionColor;
            lbForeingExpression.BackColor = ExpressionTreeNodeBuilder.ForeingExpressionColor;
            lbLambda.BackColor = ExpressionTreeNodeBuilder.LambdaColor;
            lbParameter.BackColor = ExpressionTreeNodeBuilder.ParameterColor;
            lbProperty.BackColor = Color.White;

            lbProperty.ForeColor = ExpressionTreeNodeBuilder.OthersForeColor;
            lbParameter.ForeColor = Color.FromArgb(MyRandom.Current.NextColor());

        }
    }
}