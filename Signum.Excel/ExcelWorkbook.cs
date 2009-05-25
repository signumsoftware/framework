namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class ExcelWorkbook : IWriter, IReader, IExpressionWriter
    {
        private int _activeSheet = Namespaces.NullValue;
        private bool _hideWorkbookTabs;
        private ExcelLinksCollection _links;
        private bool _protectStructure;
        private bool _protectWindows;
        private int _windowHeight = Namespaces.NullValue;
        private int _windowTopX = Namespaces.NullValue;
        private int _windowTopY = Namespaces.NullValue;
        private int _windowWidth = Namespaces.NullValue;

        public ExcelWorkbook()
        {
            CrnCollection.GlobalCounter = 0;
        }

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<ExcelWorkbook>(new TrioList<ExcelWorkbook>()
            {
                {_links, a=>a.Links},
                {false,_hideWorkbookTabs,a=>a.HideWorkbookTabs},
                {Namespaces.NullValue,_windowHeight,a=>a.WindowHeight},
                {Namespaces.NullValue,_windowWidth,a=>a.WindowWidth},
                {Namespaces.NullValue,_windowTopX,a=>a.WindowTopX},
                {Namespaces.NullValue,_windowTopY,a=>a.WindowTopY},
                {Namespaces.NullValue,_activeSheet,a=>a.ActiveSheetIndex},
                {false, _protectWindows, a=>a.ProtectWindows},
                {false, _protectWindows, a=>a.ProtectStructure},
            }); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if (element2 != null)
                {
                    if (UtilXml.IsElement(element2, "HideWorkbookTabs", Namespaces.Excel))
                    {
                        this._hideWorkbookTabs = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "WindowHeight", Namespaces.Excel))
                    {
                        this._windowHeight = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "WindowTopX", Namespaces.Excel))
                    {
                        this._windowTopX = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "WindowTopY", Namespaces.Excel))
                    {
                        this._windowTopY = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "WindowWidth", Namespaces.Excel))
                    {
                        this._windowWidth = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ActiveSheet", Namespaces.Excel))
                    {
                        this._activeSheet = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (SupBook.IsElement(element2))
                    {
                        SupBook link = new SupBook();
                        ((IReader) link).ReadXml(element2);
                        this.Links.Add(link);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "ExcelWorkbook", Namespaces.Excel);
            if (this._links != null)
            {
                ((IWriter) this._links).WriteXml(writer);
            }
            if (this._hideWorkbookTabs)
            {
                writer.WriteElementString("HideWorkbookTabs", Namespaces.Excel, "");
            }
            if (this._windowHeight != Namespaces.NullValue)
            {
                writer.WriteElementString("WindowHeight", Namespaces.Excel, this._windowHeight.ToString(CultureInfo.InvariantCulture));
            }
            if (this._windowTopX != Namespaces.NullValue)
            {
                writer.WriteElementString("WindowTopX", Namespaces.Excel, this._windowTopX.ToString(CultureInfo.InvariantCulture));
            }
            if (this._windowTopY != Namespaces.NullValue)
            {
                writer.WriteElementString("WindowTopY", Namespaces.Excel, this._windowTopY.ToString(CultureInfo.InvariantCulture));
            }
            if (this._windowWidth != Namespaces.NullValue)
            {
                writer.WriteElementString("WindowWidth", Namespaces.Excel, this._windowWidth.ToString(CultureInfo.InvariantCulture));
            }
            if (this._activeSheet != Namespaces.NullValue)
            {
                writer.WriteElementString("ActiveSheet", Namespaces.Excel, this._activeSheet.ToString(CultureInfo.InvariantCulture));
            }
            UtilXml.WriteElementString(writer, "ProtectStructure", Namespaces.Excel, this._protectStructure);
            UtilXml.WriteElementString(writer, "ProtectWindows", Namespaces.Excel, this._protectWindows);
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "ExcelWorkbook", Namespaces.Excel);
        }

        public int ActiveSheetIndex
        {
            get
            {
                return this._activeSheet;
            }
            set
            {
                this._activeSheet = value;
            }
        }

        public bool HideWorkbookTabs
        {
            get
            {
                return this._hideWorkbookTabs;
            }
            set
            {
                this._hideWorkbookTabs = value;
            }
        }

        public ExcelLinksCollection Links
        {
            get
            {
                if (this._links == null)
                {
                    this._links = new ExcelLinksCollection();
                }
                return this._links;
            }
            set
            {
                this._links = value;
            }
        }

        public bool ProtectStructure
        {
            get
            {
                return this._protectStructure;
            }
            set
            {
                this._protectStructure = value;
            }
        }

        public bool ProtectWindows
        {
            get
            {
                return this._protectWindows;
            }
            set
            {
                this._protectWindows = value;
            }
        }

        public int WindowHeight
        {
            get
            {
                return this._windowHeight;
            }
            set
            {
                this._windowHeight = value;
            }
        }

        public int WindowTopX
        {
            get
            {
                return this._windowTopX;
            }
            set
            {
                this._windowTopX = value;
            }
        }

        public int WindowTopY
        {
            get
            {
                return this._windowTopY;
            }
            set
            {
                this._windowTopY = value;
            }
        }

        public int WindowWidth
        {
            get
            {
                return this._windowWidth;
            }
            set
            {
                this._windowWidth = value;
            }
        }
    }
}

