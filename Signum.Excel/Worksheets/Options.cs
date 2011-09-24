namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Options : IWriter, IReader, IExpressionWriter
    {
        private int _activePane = Namespaces.NullValue;
        private bool _fitToPage;
        private bool _freezePanes;
        private string _gridLineColor;
        private int _leftColumnRightPane = Namespaces.NullValue;
        private PageSetup _pageSetup;
        private PrintOptions _print;
        private bool _protectObjects;
        private bool _protectScenarios;
        private bool _selected;
        private int _splitHorizontal = Namespaces.NullValue;
        private int _splitVertical = Namespaces.NullValue;
        private int _topRowBottomPane = Namespaces.NullValue;
        private int _topRowVisible = Namespaces.NullValue;
        private string _viewableRange;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Options>(new MemberBindingList<Options>()
            {
                {false,_selected,a=>a.Selected},
                {Namespaces.NullValue,_topRowVisible,a=>a.TopRowVisible},
                {false,_freezePanes,a=>a.FreezePanes},
                {false,_fitToPage,a=>a.FitToPage},
                {Namespaces.NullValue,_splitHorizontal,a=>a.SplitHorizontal},
                {Namespaces.NullValue,_topRowBottomPane,a=>a.TopRowBottomPane},
                {Namespaces.NullValue,_splitVertical,a=>a.SplitVertical},
                {Namespaces.NullValue,_leftColumnRightPane,a=>a.LeftColumnRightPane},
                {Namespaces.NullValue,_activePane,a=>a.ActivePane},
                {null,_viewableRange,a=>a.ViewableRange},
                {null,_gridLineColor,a=>a.GridLineColor},
                {false,_protectObjects, a=>a.ProtectObjects },
                {false,_protectScenarios, a=>a.ProtectScenarios },
                {_pageSetup,a=>a.PageSetup},
                {_print,a=>a.Print}
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
                    if (UtilXml.IsElement(element2, "Selected", Namespaces.Excel))
                    {
                        this._selected = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "TopRowVisible", Namespaces.Excel))
                    {
                        this._topRowVisible = UtilXml.GetAttribute(element2, "TopRowVisible", Namespaces.Excel, Namespaces.NullValue);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "FreezePanes", Namespaces.Excel))
                    {
                        this._freezePanes = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "SplitHorizontal", Namespaces.Excel))
                    {
                        this._splitHorizontal = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "TopRowBottomPane", Namespaces.Excel))
                    {
                        this._topRowBottomPane = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "SplitVertical", Namespaces.Excel))
                    {
                        this._splitVertical = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "LeftColumnRightPane", Namespaces.Excel))
                    {
                        this._leftColumnRightPane = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ActivePane", Namespaces.Excel))
                    {
                        this._activePane = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ViewableRange", Namespaces.Excel))
                    {
                        this._viewableRange = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "GridlineColor", Namespaces.Excel))
                    {
                        this._gridLineColor = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ProtectObjects", Namespaces.Excel))
                    {
                        this._protectObjects = bool.Parse(element2.InnerText);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ProtectScenarios", Namespaces.Excel))
                    {
                        this._protectScenarios = bool.Parse(element2.InnerText);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "FitToPage", Namespaces.Excel))
                    {
                        this._fitToPage = true;
                        continue;
                    }
                    if (PageSetup.IsElement(element2))
                    {
                        ((IReader) this.PageSetup).ReadXml(element2);
                        continue;
                    }
                    if (PrintOptions.IsElement(element2))
                    {
                        ((IReader) this.Print).ReadXml(element2);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "WorksheetOptions", Namespaces.Excel);
            if (this._selected)
            {
                writer.WriteElementString("Selected", Namespaces.Excel, "");
            }
            if (this._fitToPage)
            {
                writer.WriteElementString("FitToPage", Namespaces.Excel, "");
            }
            if (this._topRowVisible != Namespaces.NullValue)
            {
                writer.WriteElementString("TopRowVisible", Namespaces.Excel, this._topRowVisible.ToString(CultureInfo.InvariantCulture));
            }
            if (this._freezePanes)
            {
                writer.WriteElementString("FreezePanes", Namespaces.Excel, "");
            }
            if (this._splitHorizontal != Namespaces.NullValue)
            {
                writer.WriteElementString("SplitHorizontal", Namespaces.Excel, this._splitHorizontal.ToString(CultureInfo.InvariantCulture));
            }
            if (this._topRowBottomPane != Namespaces.NullValue)
            {
                writer.WriteElementString("TopRowBottomPane", Namespaces.Excel, this._topRowBottomPane.ToString(CultureInfo.InvariantCulture));
            }
            if (this._splitVertical != Namespaces.NullValue)
            {
                writer.WriteElementString("SplitVertical", Namespaces.Excel, this._splitVertical.ToString(CultureInfo.InvariantCulture));
            }
            if (this._leftColumnRightPane != Namespaces.NullValue)
            {
                writer.WriteElementString("LeftColumnRightPane", Namespaces.Excel, this._leftColumnRightPane.ToString(CultureInfo.InvariantCulture));
            }
            if (this._activePane != Namespaces.NullValue)
            {
                writer.WriteElementString("ActivePane", Namespaces.Excel, this._activePane.ToString(CultureInfo.InvariantCulture));
            }
            if (this._viewableRange != null)
            {
                writer.WriteElementString("ViewableRange", Namespaces.Excel, this._viewableRange);
            }
            if (this._gridLineColor != null)
            {
                writer.WriteElementString("GridlineColor", Namespaces.Excel, this._gridLineColor);
            }
            writer.WriteElementString("ProtectObjects", Namespaces.Excel, this._protectObjects.ToString(CultureInfo.InvariantCulture));
            writer.WriteElementString("ProtectScenarios", Namespaces.Excel, this._protectScenarios.ToString(CultureInfo.InvariantCulture));
            if (this._pageSetup != null)
            {
                ((IWriter) this._pageSetup).WriteXml(writer);
            }
            if (this._print != null)
            {
                ((IWriter) this._print).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "WorksheetOptions", Namespaces.Excel);
        }

        public int ActivePane
        {
            get
            {
                return this._activePane;
            }
            set
            {
                this._activePane = value;
            }
        }

        public bool FitToPage
        {
            get
            {
                return this._fitToPage;
            }
            set
            {
                this._fitToPage = value;
            }
        }

        public bool FreezePanes
        {
            get
            {
                return this._freezePanes;
            }
            set
            {
                this._freezePanes = value;
            }
        }

        public string GridLineColor
        {
            get
            {
                return this._gridLineColor;
            }
            set
            {
                this._gridLineColor = value;
            }
        }

        public int LeftColumnRightPane
        {
            get
            {
                return this._leftColumnRightPane;
            }
            set
            {
                this._leftColumnRightPane = value;
            }
        }

        public PageSetup PageSetup
        {
            get
            {
                if (this._pageSetup == null)
                {
                    this._pageSetup = new PageSetup();
                }
                return this._pageSetup;
            }
            set
            {
                this._pageSetup = value; 
            }
        }

        public PrintOptions Print
        {
            get
            {
                if (this._print == null)
                {
                    this._print = new PrintOptions();
                }
                return this._print;
            }

            set
            {
                this._print = value; 
            }
        }

        public bool ProtectObjects
        {
            get
            {
                return this._protectObjects;
            }
            set
            {
                this._protectObjects = value;
            }
        }

        public bool ProtectScenarios
        {
            get
            {
                return this._protectScenarios;
            }
            set
            {
                this._protectScenarios = value;
            }
        }

        public bool Selected
        {
            get
            {
                return this._selected;
            }
            set
            {
                this._selected = value;
            }
        }

        public int SplitHorizontal
        {
            get
            {
                return this._splitHorizontal;
            }
            set
            {
                this._splitHorizontal = value;
            }
        }

        public int SplitVertical
        {
            get
            {
                return this._splitVertical;
            }
            set
            {
                this._splitVertical = value;
            }
        }

        public int TopRowBottomPane
        {
            get
            {
                return this._topRowBottomPane;
            }
            set
            {
                this._topRowBottomPane = value;
            }
        }

        public int TopRowVisible
        {
            get
            {
                return this._topRowVisible;
            }
            set
            {
                this._topRowVisible = value;
            }
        }

        public string ViewableRange
        {
            get
            {
                return this._viewableRange;
            }
            set
            {
                this._viewableRange = value;
            }
        }
    }
}

