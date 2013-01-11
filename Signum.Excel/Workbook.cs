namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Linq.Expressions;
    using System.Linq;
    using Signum.Utilities;
    using Signum.Utilities.ExpressionTrees;
    using System.Collections.Generic;

    public sealed class Workbook : IWriter, IReader
    {
        private DocumentProperties _documentProperties;
        private ExcelWorkbook _excelWorkbook;
        private bool _generateExcelProcessingInstruction = true;
        private NamedRangeCollection _names;
        private PivotCache _pivotCache;
        private ComponentOptions _spreadSheetComponentOptions;
        private StyleCollection _styles;
        private WorksheetCollection _worksheets;

        public CodeCompileUnit WriteCode(string fullClassName)
        {
            string className = fullClassName.Split('.').Last();
            string nameSpace = fullClassName.RemoveEnd(className.Length + 1);

            CodeCompileUnit unit = new CodeCompileUnit();

            string[] importedNamespaces = 
            { 
                "System", 
                "System.Xml",
                "System.Collections.Generic",
                "System.Linq",
                "Signum.Utilities",
                "Signum.Excel"
            };

            CodeNamespace ns = new CodeNamespace(null);
            ns.Imports.AddRange(importedNamespaces.Select(i=>new CodeNamespaceImport(i)).ToArray()); 

            unit.Namespaces.Add(ns);

            CodeNamespace ns2 = new CodeNamespace(nameSpace);
            unit.Namespaces.Add(ns2);

            CodeTypeDeclaration type = new CodeTypeDeclaration(className);
            ns2.Types.Add(type);

            if (_styles != null)
            {
                for (int i = 0; i < _styles.Count; i++)
                {
                    Style st = _styles[i];

                    var constant = new CodeMemberField(typeof(string), st.ID)
                    {
                        Attributes = MemberAttributes.Const | MemberAttributes.Private,
                        InitExpression = new CodePrimitiveExpression(st.ID),
                    };

                    if (i == 0) constant.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Styles"));
                    if (i == _styles.Count - 1) constant.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, null));

                    type.Members.Add(constant);
                }
            }

            CodeMemberMethod method = new CodeMemberMethod()
            {
                Name = "Generate",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
            type.Members.Add(method);

            Expression memberInit = UtilExpression.MemberInit<Workbook>(new MemberBindingList<Workbook>()
            {
                {_documentProperties,a=>a.Properties},
                {_names,a=>a.Names},
                {_spreadSheetComponentOptions,a=>a.SpreadSheetComponentOptions},
                {_excelWorkbook,a=>a.ExcelWorkbook},
            }); 

            method.Statements.Add(new CodeVariableDeclarationStatement(CSharpRenderer.TypeReference(typeof(Workbook), importedNamespaces), "book", UtilCodeDom.CodeSnippet(memberInit, importedNamespaces)));
            CodeVariableReferenceExpression book = new CodeVariableReferenceExpression("book");

            if (_styles != null)
            {

                CodeMemberMethod method2 = new CodeMemberMethod()
                {
                    Name = "GenerateStyles",
                    ReturnType = CSharpRenderer.TypeReference(typeof(StyleCollection), importedNamespaces),
                    Attributes = MemberAttributes.Static,
                };
           
                method2.Statements.Add(new CodeMethodReturnStatement(UtilCodeDom.CodeSnippet(_styles.CreateExpression(), importedNamespaces)));

                type.Members.Add(method2);

                CodeMethodInvokeExpression mi = new CodeMethodInvokeExpression(null, method2.Name); 
                method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(book, "Styles"), mi)); 
            }

            if (_worksheets != null)
            {
                CodePropertyReferenceExpression ws = new CodePropertyReferenceExpression(book, "Worksheets"); 
                foreach (Worksheet worksheet in _worksheets)
                {
                    string str = UtilCodeDom.CreateSafeName(worksheet.Name, "Sheet");
                    CodeMemberMethod method2 = new CodeMemberMethod()
                    {
                        Name = "GenerateWorksheet" + str,
                        ReturnType = CSharpRenderer.TypeReference(typeof(Worksheet), importedNamespaces),
                        Attributes = MemberAttributes.Static,
                    };

                    method2.Statements.Add(new CodeMethodReturnStatement(UtilCodeDom.CodeSnippet(worksheet.CreateExpression(), importedNamespaces)));
                    type.Members.Add(method2);

                    CodeMethodInvokeExpression mi = new CodeMethodInvokeExpression(null, method2.Name);
                    method.Statements.Add(new CodeMethodInvokeExpression(ws, "Add", mi));
                }
            }

             method.Statements.Add(new CodeMethodInvokeExpression(book, "Save", new CodeExpression[] { new CodeVariableReferenceExpression("filename") }));

             return unit;
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new InvalidOperationException("The specified Xml is not a valid Workbook.\nElement Name:" + element.Name);
            }
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if (element2 != null)
                {
                    if (DocumentProperties.IsElement(element2))
                    {
                        ((IReader) this.Properties).ReadXml(element2);
                        continue;
                    }
                    if (ComponentOptions.IsElement(element2))
                    {
                        ((IReader) this.SpreadSheetComponentOptions).ReadXml(element2);
                        continue;
                    }
                    if (ExcelWorkbook.IsElement(element2))
                    {
                        ((IReader) this.ExcelWorkbook).ReadXml(element2);
                        continue;
                    }
                    if (StyleCollection.IsElement(element2))
                    {
                        ((IReader) this.Styles).ReadXml(element2);
                        continue;
                    }
                    if (NamedRangeCollection.IsElement(element2))
                    {
                        ((IReader) this.Names).ReadXml(element2);
                        continue;
                    }
                    if (Worksheet.IsElement(element2))
                    {
                        Worksheet sheet = new Worksheet(null);
                        ((IReader) sheet).ReadXml(element2);
                        this.Worksheets.Add(sheet);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Workbook", Namespaces.SpreadSheet);
            writer.WriteAttributeString("xmlns", Namespaces.ExcelPrefix, null, Namespaces.Excel);
            writer.WriteAttributeString("xmlns", Namespaces.OfficePrefix, null, Namespaces.Office);
            if (this._documentProperties != null)
            {
                ((IWriter) this._documentProperties).WriteXml(writer);
            }
            if (this._spreadSheetComponentOptions != null)
            {
                ((IWriter) this._spreadSheetComponentOptions).WriteXml(writer);
            }
            if (this._excelWorkbook != null)
            {
                ((IWriter) this._excelWorkbook).WriteXml(writer);
            }
            if (this._styles != null)
            {
                ((IWriter) this._styles).WriteXml(writer);
            }
            if (this._names != null)
            {
                ((IWriter) this._names).WriteXml(writer);
            }
            if (this._worksheets != null)
            {
                ((IWriter) this._worksheets).WriteXml(writer);
            }
            if (this._pivotCache != null)
            {
                ((IWriter) this._pivotCache).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Workbook", Namespaces.SpreadSheet);
        }

        public void Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (stream.Position >= stream.Length)
            {
                stream.Position = 0L;
            }
            XmlDocument document = new XmlDocument();
            document.Load(stream);
            ((IReader)this).ReadXml(document.DocumentElement);
        }

        public void Load(string filename)
        {
            FileStream stream = null;
            try
            {
                stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                this.Load(stream);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        public void Save(Stream stream)
        {
            if (this.Worksheets.Count == 0)
            {
                this.Worksheets.Add(new Worksheet("Sheet 1"));
            }
            XmlTextWriter writer = new XmlTextWriter(stream, Encoding.UTF8) { Namespaces = true, Formatting = Formatting.Indented };
            writer.WriteProcessingInstruction("xml", "version='1.0'");
            if (this._generateExcelProcessingInstruction)
            {
                writer.WriteProcessingInstruction("mso-application", "progid='Excel.Sheet'");
            }
            ((IWriter)this).WriteXml(writer);
            writer.Flush();
        }

        public void Save(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                this.Save(stream);
            }
        }

        public byte[] SaveBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Save(ms);
                return ms.ToArray(); 
            }
        }

        public Dictionary<string, string> FindDuplicatedStyles()
        {
            List<Style> finnalStyle = new List<Style>();
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var item in _styles)
            {
                Style real = finnalStyle.SingleOrDefault(s => s.Identical(item));
                if (real != null)
                    result.Add(item.ID, real.ID);
                else
                    finnalStyle.Add(item); 
            }

            HashSet<string> used = finnalStyle.Select(a => a.Parent).ToHashSet();
            foreach (var w in Worksheets)
	        {
                used.AddRange(w.Table.Columns.Select(a => a.StyleID));
                used.AddRange(w.Table.Rows.SelectMany(r => r.Cells.Select(c => c.StyleID)));
                used.Add(w.Table.StyleID); 
	        }

            used.Remove(null);

            used.AddRange(used.Select(u => result.TryGetC(u)).ToList());

            var notUsed = finnalStyle.Select(a => a.ID).Where(id => !used.Contains(id)).ToList();

            result.SetRange(notUsed, notUsed.Select(s => (string)null));

             return result; 
        }

        public void ReplaceStyles(Dictionary<string, string> dictionary)
        {
            foreach (var w in Worksheets)
            {
                w.Table.StyleID = Replace(dictionary, w.Table.StyleID);

                foreach (var c in w.Table.Columns)
                    c.StyleID = Replace(dictionary, c.StyleID);

                foreach (var r in w.Table.Rows)
                    foreach (var c in r.Cells)
                        c.StyleID = Replace(dictionary, c.StyleID);
            }

            foreach (var style in _styles.ToList())
            {
                if (dictionary.ContainsKey(style.ID))
                    _styles.Remove(style);
                else
                    style.Parent = Replace(dictionary, style.Parent);
            }
        }

        string Replace(Dictionary<string, string> dictionary, string styleID)
        {
            if (styleID == null) return null;

            return dictionary.TryGetC(styleID) ?? styleID;
        }

        public ExcelWorkbook ExcelWorkbook
        {
            get
            {
                if (this._excelWorkbook == null)
                {
                    this._excelWorkbook = new ExcelWorkbook();
                }
                return this._excelWorkbook;
            }
            set
            {
                this._excelWorkbook = value;
            }
        }

        public bool GenerateExcelProcessingInstruction
        {
            get
            {
                return this._generateExcelProcessingInstruction;
            }
            set
            {
                this._generateExcelProcessingInstruction = value;
            }
        }

        public NamedRangeCollection Names
        {
            get
            {
                if (this._names == null)
                {
                    this._names = new NamedRangeCollection();
                }
                return this._names;
            }

            set
            {
                this._names = value; 
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public PivotCache PivotCache
        {
            get
            {
                if (this._pivotCache == null)
                {
                    this._pivotCache = new PivotCache();
                }
                return this._pivotCache;
            }

            set
            {
                this._pivotCache = value; 
            }
        }

        public DocumentProperties Properties
        {
            get
            {
                if (this._documentProperties == null)
                {
                    this._documentProperties = new DocumentProperties();
                }
                return this._documentProperties;
            }

            set
            {
                this._documentProperties = value; 
            }
        }

        public ComponentOptions SpreadSheetComponentOptions
        {
            get
            {
                if (this._spreadSheetComponentOptions == null)
                {
                    this._spreadSheetComponentOptions = new ComponentOptions();
                }
                return this._spreadSheetComponentOptions;
            }

            set
            {
                this._spreadSheetComponentOptions= value;
            }
        }

        public StyleCollection Styles
        {
            get
            {
                if (this._styles == null)
                {
                    this._styles = new StyleCollection();
                }
                return this._styles;
            }

            set
            {
                this._styles= value;
            }
        }

        public WorksheetCollection Worksheets
        {
            get
            {
                if (this._worksheets == null)
                {
                    this._worksheets = new WorksheetCollection();
                }
                return this._worksheets;
            }
            set
            {
                this._worksheets= value;
            }
        }
    }
}

