namespace CarlosAg.ExcelXmlWriter.Generator
{
    using CarlosAg.ExcelXmlWriter;
    using CarlosAg.Utils.Controls;
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Resources;
    using System.Text;
    using System.Windows.Forms;
    using Signum.Excel;
    using Signum.Excel.Generator;
    using Signum.Utilities;

    public sealed class GeneratorDialog : Form
    {
        private CodeEditor _codeEditor;
        private string _filename;
        private ToolBarButton _generateButton;
        private Language _language;
        private ComboBox _languageOutputList;
        private ToolBarButton _loadButton;
        private ToolBarButton _saveButton;
        private MyToolbar _toolbar;
        private CodeEditor codeEditor1;
        private IContainer components;
        private ImageList imageList1;
        private Label label1;
        private MainMenu mainMenu1;
        private Splitter splitter1;
        private ColorDialog colorDialog1;
        private TextBox textBox1;
        private ToolBarButton zSep;

        public GeneratorDialog()
        {
            this.InitializeComponent();
            this._languageOutputList.Items.Add(new Language("C#"));
            this._languageOutputList.Items.Add(new Language("VB"));
            this._languageOutputList.Items.Add(new Language("JScript.net"));
            this._languageOutputList.Items.Add(new Language("J#"));
            this._languageOutputList.SelectedIndex = 0;
        }

        private void _codeEditor_TextChanged(object sender, EventArgs e)
        {
            this.Generate();
        }

        private void _languageOutputList_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SetLanguage((Language) this._languageOutputList.SelectedItem);
        }

        private void _toolbar_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
        {
            if (e.Button == this._loadButton)
            {
                this.LoadFile();
            }
            else if (e.Button == this._saveButton)
            {
                this.SaveFile();
            }
            else if (e.Button == this._generateButton)
            {
                this.GenerateFile();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Generate()
        {
            this.codeEditor1.Text = this.Generate(this._codeEditor.Text);
        }

        private string Generate(string data)
        {
            if (this._filename == null)
                return "";

            Workbook workbook = this.LoadWorkbook(this._filename);
            if (workbook == null)
                return "";

            var dup = workbook.FindDuplicatedStyles();
            if (dup.Count > 0)
                if (MessageBox.Show(this, "Some identical styles have been found:\r\n{0}\r\n\r\nSimplify?".Formato(dup.ToString(kvp => "{0} -> {1}".Formato(kvp.Key, kvp.Value), "\r\n")), "Simplify?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    workbook.ReplaceStyles(dup);

            CodeCompileUnit unit = workbook.WriteCode(textBox1.Text);

            this.codeEditor1.Colorizer = this._language.GetColorizer();
            return this.GenerateCode(unit, this._language);
        }

        private string GenerateCode(CodeCompileUnit unit, Language language)
        {
            try
            {
                CodeDomProvider generator = language.GetCodeProvider();
                StringBuilder sb = new StringBuilder();
                StringWriter w = new StringWriter(sb);
                CodeGeneratorOptions o = new CodeGeneratorOptions() { BracingStyle = "C" };
                generator.GenerateCodeFromCompileUnit(unit, w, o);
                w.Close();
                return sb.ToString();
            }
            catch (Exception exception)
            {
                MessageBox.Show("There was an error while parsing the code:\n" + exception.ToString(), "Error:", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return exception.ToString();
            }
        }

        private void GenerateFile()
        {
            string fileNameForLoading = this.GetFileNameForLoading();
            if (fileNameForLoading != null)
            {
                string fileName = this.GetFileName();
                if (fileName != null)
                {
                    Workbook workbook = this.LoadWorkbook(fileNameForLoading);
                    if (workbook != null)
                    {
                        this._filename = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileNameForLoading));
                        workbook.Save(this._filename);
                        string data = null;
                        using (StreamReader reader = new StreamReader(this._filename))
                        {
                            data = reader.ReadToEnd();
                        }
                        data = this.Generate(data);
                        StreamWriter writer = new StreamWriter(fileName);
                        writer.Write(data);
                        writer.Close();
                    }
                }
            }
        }

        private string GetFileName()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Select the File Name";
                dialog.OverwritePrompt = true;
                dialog.CheckPathExists = true;
                dialog.Filter = "C# Files|*.cs|VB.NET Files|*.vb|JScript Files|*.js|J# Files|*.vjs|All files|*.*";
                if (this._language.Name == "C#")
                {
                    dialog.FilterIndex = 1;
                }
                else if (this._language.Name == "VB")
                {
                    dialog.FilterIndex = 2;
                }
                else if (this._language.Name == "JScript.net")
                {
                    dialog.FilterIndex = 3;
                }
                else if (this._language.Name == "J#")
                {
                    dialog.FilterIndex = 4;
                }
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }

        private string GetFileNameForLoading()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel Xml Worksheet|*.xml";
                if (this._filename != null)
                {
                    dialog.FileName = this._filename;
                }
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            CarlosAg.Utils.Colorizers.AspColorizer aspColorizer1 = new CarlosAg.Utils.Colorizers.AspColorizer();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneratorDialog));
            CarlosAg.Utils.Colorizers.AspColorizer aspColorizer2 = new CarlosAg.Utils.Colorizers.AspColorizer();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this._codeEditor = new CarlosAg.Utils.Controls.CodeEditor();
            this._toolbar = new CarlosAg.Utils.Controls.MyToolbar();
            this.zSep = new System.Windows.Forms.ToolBarButton();
            this._loadButton = new System.Windows.Forms.ToolBarButton();
            this._saveButton = new System.Windows.Forms.ToolBarButton();
            this._generateButton = new System.Windows.Forms.ToolBarButton();
            this._languageOutputList = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.codeEditor1 = new CarlosAg.Utils.Controls.CodeEditor();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this._toolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // _codeEditor
            // 
            this._codeEditor.Colorizer = aspColorizer1;
            this._codeEditor.DetectUrls = false;
            this._codeEditor.Dock = System.Windows.Forms.DockStyle.Top;
            this._codeEditor.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._codeEditor.HideSelection = false;
            this._codeEditor.IsDirty = true;
            this._codeEditor.Location = new System.Drawing.Point(2, 30);
            this._codeEditor.Name = "_codeEditor";
            this._codeEditor.ReadOnly = true;
            this._codeEditor.Size = new System.Drawing.Size(844, 196);
            this._codeEditor.TabIndex = 2;
            this._codeEditor.Text = "";
            this._codeEditor.WordWrap = false;
            this._codeEditor.TextChanged += new System.EventHandler(this._codeEditor_TextChanged);
            // 
            // _toolbar
            // 
            this._toolbar.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.zSep,
            this._loadButton,
            this._saveButton,
            this._generateButton});
            this._toolbar.ButtonSize = new System.Drawing.Size(110, 28);
            this._toolbar.Controls.Add(this._languageOutputList);
            this._toolbar.Controls.Add(this.label1);
            this._toolbar.DropDownArrows = true;
            this._toolbar.ImageList = this.imageList1;
            this._toolbar.Location = new System.Drawing.Point(2, 2);
            this._toolbar.Name = "_toolbar";
            this._toolbar.ShowToolTips = true;
            this._toolbar.Size = new System.Drawing.Size(844, 28);
            this._toolbar.TabIndex = 3;
            this._toolbar.TextAlign = System.Windows.Forms.ToolBarTextAlign.Right;
            this._toolbar.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this._toolbar_ButtonClick);
            // 
            // zSep
            // 
            this.zSep.ImageIndex = 0;
            this.zSep.Name = "zSep";
            this.zSep.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
            // 
            // _loadButton
            // 
            this._loadButton.ImageIndex = 1;
            this._loadButton.Name = "_loadButton";
            this._loadButton.Text = "Load Workbook";
            // 
            // _saveButton
            // 
            this._saveButton.ImageIndex = 0;
            this._saveButton.Name = "_saveButton";
            this._saveButton.Text = "Save";
            // 
            // _generateButton
            // 
            this._generateButton.ImageIndex = 0;
            this._generateButton.Name = "_generateButton";
            this._generateButton.Text = "Generate(Fast)";
            // 
            // _languageOutputList
            // 
            this._languageOutputList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._languageOutputList.Location = new System.Drawing.Point(434, 3);
            this._languageOutputList.Name = "_languageOutputList";
            this._languageOutputList.Size = new System.Drawing.Size(184, 21);
            this._languageOutputList.TabIndex = 3;
            this._languageOutputList.SelectedIndexChanged += new System.EventHandler(this._languageOutputList_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(328, 2);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 21);
            this.label1.TabIndex = 4;
            this.label1.Text = "Language:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Purple;
            this.imageList1.Images.SetKeyName(0, "guardar.png");
            this.imageList1.Images.SetKeyName(1, "abrirFichero.png");
            // 
            // codeEditor1
            // 
            this.codeEditor1.Colorizer = aspColorizer2;
            this.codeEditor1.DetectUrls = false;
            this.codeEditor1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.codeEditor1.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.codeEditor1.HideSelection = false;
            this.codeEditor1.IsDirty = true;
            this.codeEditor1.Location = new System.Drawing.Point(2, 236);
            this.codeEditor1.Name = "codeEditor1";
            this.codeEditor1.Size = new System.Drawing.Size(844, 344);
            this.codeEditor1.TabIndex = 4;
            this.codeEditor1.Text = "";
            this.codeEditor1.WordWrap = false;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(2, 226);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(844, 10);
            this.splitter1.TabIndex = 5;
            this.splitter1.TabStop = false;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(626, 7);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(217, 21);
            this.textBox1.TabIndex = 6;
            this.textBox1.Text = "InformesExcel.Informe1";
            // 
            // GeneratorDialog
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(848, 582);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.codeEditor1);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this._codeEditor);
            this.Controls.Add(this._toolbar);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Menu = this.mainMenu1;
            this.Name = "GeneratorDialog";
            this.Padding = new System.Windows.Forms.Padding(2);
            this.Text = "Excel XML Workbook Code Generator";
            this._toolbar.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void LoadFile()
        {
            string fileNameForLoading = this.GetFileNameForLoading();
            if (fileNameForLoading != null)
            {
                Workbook workbook = this.LoadWorkbook(fileNameForLoading);
                if (workbook != null)
                {
                    this._filename = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileNameForLoading));
                    workbook.Save(this._filename);
                    this._codeEditor.LoadFile(this._filename, RichTextBoxStreamType.PlainText);
                }
            }
        }

        private Workbook LoadWorkbook(string filename)
        {
            Workbook workbook = new Workbook();
            try
            {
                workbook.Load(filename);
            }
            catch (Exception exception)
            {
                MessageBox.Show(string.Format("An error occurred while trying to read the file: {0}.\n\nPlease make sure it is a valid Xml Spreadsheet (in Excel use 'File->Save As...' and choose Xml Spreadsheet). \n\nError:\n{1}", filename, exception.ToString()), "Error:", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }
            return workbook;
        }

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();
            Application.Run(new GeneratorDialog());
        }

        private void SaveFile()
        {
            string fileName = this.GetFileName();
            if (fileName != null)
            {
                this.codeEditor1.SaveFile(fileName, RichTextBoxStreamType.PlainText);
            }
        }

        private void SetLanguage(Language language)
        {
            this._language = language;
            this.Generate();
        }
    }
}

