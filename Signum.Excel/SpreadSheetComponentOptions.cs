namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class ComponentOptions : IWriter, IReader, IExpressionWriter
    {
        private bool _doNotEnableResize;
        private string _maxHeight;
        private string _maxWidth;
        private int _nextSheetNumber = Namespaces.NullValue;
        private bool _preventPropBrowser;
        private bool _spreadsheetAutoFit;
        private Toolbar _toolbar;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<ComponentOptions>(new MemberBindingList<ComponentOptions>()
            {
                {_toolbar, a=>a.Toolbar},
                {Namespaces.NullValue,_nextSheetNumber,a=>a.NextSheetNumber},
                {false,_spreadsheetAutoFit,a=>a.SpreadsheetAutoFit},
                {false,_doNotEnableResize,a=>a.DoNotEnableResize},
                {false,_preventPropBrowser,a=>a.PreventPropBrowser},
                {null,_maxHeight,a=>a.MaxHeight},
                {null,_maxWidth,a=>a.MaxWidth},
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
                    if (Toolbar.IsElement(element2))
                    {
                        ((IReader) this.Toolbar).ReadXml(element2);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "NextSheetNumber", Namespaces.ComponentNamespace))
                    {
                        this._nextSheetNumber = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "SpreadsheetAutoFit", Namespaces.ComponentNamespace))
                    {
                        this._spreadsheetAutoFit = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "DoNotEnableResize", Namespaces.ComponentNamespace))
                    {
                        this._doNotEnableResize = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "PreventPropBrowser", Namespaces.ComponentNamespace))
                    {
                        this._preventPropBrowser = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "MaxHeight", Namespaces.ComponentNamespace))
                    {
                        this._maxHeight = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "MaxWidth", Namespaces.ComponentNamespace))
                    {
                        this._maxWidth = element2.InnerText;
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ComponentPrefix, "ComponentOptions", Namespaces.ComponentNamespace);
            if (this._toolbar != null)
            {
                ((IWriter) this._toolbar).WriteXml(writer);
            }
            if (this._nextSheetNumber != Namespaces.NullValue)
            {
                writer.WriteElementString("NextSheetNumber", Namespaces.ComponentNamespace, this._nextSheetNumber.ToString(CultureInfo.InvariantCulture));
            }
            if (this._spreadsheetAutoFit)
            {
                writer.WriteElementString("SpreadsheetAutoFit", Namespaces.ComponentNamespace, "");
            }
            if (this._doNotEnableResize)
            {
                writer.WriteElementString("DoNotEnableResize", Namespaces.ComponentNamespace, "");
            }
            if (this._preventPropBrowser)
            {
                writer.WriteElementString("PreventPropBrowser", Namespaces.ComponentNamespace, "");
            }
            if (this._maxHeight != null)
            {
                writer.WriteElementString("MaxHeight", Namespaces.ComponentNamespace, this._maxHeight);
            }
            if (this._maxWidth != null)
            {
                writer.WriteElementString("MaxWidth", Namespaces.ComponentNamespace, this._maxWidth);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "ComponentOptions", Namespaces.ComponentNamespace);
        }

        public bool DoNotEnableResize
        {
            get
            {
                return this._doNotEnableResize;
            }
            set
            {
                this._doNotEnableResize = value;
            }
        }

        public string MaxHeight
        {
            get
            {
                return this._maxHeight;
            }
            set
            {
                this._maxHeight = value;
            }
        }

        public string MaxWidth
        {
            get
            {
                return this._maxWidth;
            }
            set
            {
                this._maxWidth = value;
            }
        }

        public int NextSheetNumber
        {
            get
            {
                return this._nextSheetNumber;
            }
            set
            {
                this._nextSheetNumber = value;
            }
        }

        public bool PreventPropBrowser
        {
            get
            {
                return this._preventPropBrowser;
            }
            set
            {
                this._preventPropBrowser = value;
            }
        }

        public bool SpreadsheetAutoFit
        {
            get
            {
                return this._spreadsheetAutoFit;
            }
            set
            {
                this._spreadsheetAutoFit = value;
            }
        }

        public Toolbar Toolbar
        {
            get
            {
                if (this._toolbar == null)
                {
                    this._toolbar = new Toolbar();
                }
                return this._toolbar;
            }
            set
            {
                this._toolbar= value;
            }
        }
    }
}

