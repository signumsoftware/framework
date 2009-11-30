namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class PrintOptions : IWriter, IReader, IExpressionWriter
    {
        private bool _blackAndWhite;
        private PrintCommentsLayout _commentsLayout;
        private bool _draftQuality;
        private int _fitHeight = 1;
        private int _fitWidth = 1;
        private bool _gridLines;
        private int _horizontalResolution = 600;
        private bool _leftToRight;
        private int _paperSizeIndex = Namespaces.NullValue;
        private PrintErrorsOption _printErrors;
        private bool _rowColHeadings;
        private int _scale = 100;
        private bool _validPrinterInfo;
        private int _verticalResolution = 600;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<PrintOptions>(new MemberBindingList<PrintOptions>()
            {
                {Namespaces.NullValue,_paperSizeIndex,a=>a.PaperSizeIndex},
                {600,_horizontalResolution,a=>a.HorizontalResolution},
                {600,_verticalResolution,a=>a.VerticalResolution},
                {false,_blackAndWhite,a=>a.BlackAndWhite},
                {false,_draftQuality,a=>a.DraftQuality},
                {false,_gridLines,a=>a.GridLines},
                {100,_scale,a=>a.Scale},
                {1,_fitWidth,a=>a.FitWidth},
                {1,_fitHeight,a=>a.FitHeight},
                {false,_leftToRight,a=>a.LeftToRight},
                {false,_rowColHeadings,a=>a.RowColHeadings},
                {PrintErrorsOption.Displayed,_printErrors,a=>a.PrintErrors},
                {PrintCommentsLayout.None,_commentsLayout,a=>a.CommentsLayout},
                {false,_validPrinterInfo,a=>a.ValidPrinterInfo},
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
                    if (UtilXml.IsElement(element2, "Gridlines", Namespaces.Excel))
                    {
                        this._gridLines = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "BlackAndWhite", Namespaces.Excel))
                    {
                        this._blackAndWhite = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "DraftQuality", Namespaces.Excel))
                    {
                        this._draftQuality = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ValidPrinterInfo", Namespaces.Excel))
                    {
                        this._validPrinterInfo = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "PaperSizeIndex", Namespaces.Excel))
                    {
                        this._paperSizeIndex = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "HorizontalResolution", Namespaces.Excel))
                    {
                        this._horizontalResolution = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "VerticalResolution", Namespaces.Excel))
                    {
                        this._verticalResolution = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "RowColHeadings", Namespaces.Excel))
                    {
                        this._rowColHeadings = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "LeftToRight", Namespaces.Excel))
                    {
                        this._leftToRight = true;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Scale", Namespaces.Excel))
                    {
                        this._scale = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "FitWidth", Namespaces.Excel))
                    {
                        this._fitWidth = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "FitHeight", Namespaces.Excel))
                    {
                        this._fitHeight = int.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "PrintErrors", Namespaces.Excel))
                    {
                        this._printErrors = (PrintErrorsOption) Enum.Parse(typeof(PrintErrorsOption), element2.InnerText);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "CommentsLayout", Namespaces.Excel))
                    {
                        this._commentsLayout = (PrintCommentsLayout) Enum.Parse(typeof(PrintCommentsLayout), element2.InnerText);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "Print", Namespaces.Excel);
            if (this._paperSizeIndex != Namespaces.NullValue)
            {
                writer.WriteElementString("PaperSizeIndex", Namespaces.Excel, this._paperSizeIndex.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteElementString("HorizontalResolution", Namespaces.Excel, this._horizontalResolution.ToString(CultureInfo.InvariantCulture));
            writer.WriteElementString("VerticalResolution", Namespaces.Excel, this._verticalResolution.ToString(CultureInfo.InvariantCulture));
            if (this._blackAndWhite)
            {
                writer.WriteElementString("BlackAndWhite", Namespaces.Excel, "");
            }
            if (this._draftQuality)
            {
                writer.WriteElementString("DraftQuality", Namespaces.Excel, "");
            }
            if (this._gridLines)
            {
                writer.WriteElementString("Gridlines", Namespaces.Excel, "");
            }
            if (this._scale != 100)
            {
                writer.WriteElementString("Scale", Namespaces.Excel, this._scale.ToString(CultureInfo.InvariantCulture));
            }
            if (this._fitWidth != 1)
            {
                writer.WriteElementString("FitWidth", Namespaces.Excel, this._fitWidth.ToString(CultureInfo.InvariantCulture));
            }
            if (this._fitHeight != 1)
            {
                writer.WriteElementString("FitHeight", Namespaces.Excel, this._fitHeight.ToString(CultureInfo.InvariantCulture));
            }
            if (this._leftToRight)
            {
                writer.WriteElementString("LeftToRight", Namespaces.Excel, "");
            }
            if (this._rowColHeadings)
            {
                writer.WriteElementString("RowColHeadings", Namespaces.Excel, "");
            }
            if (this._printErrors != PrintErrorsOption.Displayed)
            {
                writer.WriteElementString("PrintErrors", Namespaces.Excel, this._printErrors.ToString());
            }
            if (this._commentsLayout != PrintCommentsLayout.None)
            {
                writer.WriteElementString("CommentsLayout", Namespaces.Excel, this._commentsLayout.ToString());
            }
            if (this._validPrinterInfo)
            {
                writer.WriteElementString("ValidPrinterInfo", Namespaces.Excel, "");
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Print", Namespaces.Excel);
        }

        public bool BlackAndWhite
        {
            get
            {
                return this._blackAndWhite;
            }
            set
            {
                this._blackAndWhite = value;
            }
        }

        public PrintCommentsLayout CommentsLayout
        {
            get
            {
                return this._commentsLayout;
            }
            set
            {
                this._commentsLayout = value;
            }
        }

        public bool DraftQuality
        {
            get
            {
                return this._draftQuality;
            }
            set
            {
                this._draftQuality = value;
            }
        }

        public int FitHeight
        {
            get
            {
                return this._fitHeight;
            }
            set
            {
                this._fitHeight = value;
            }
        }

        public int FitWidth
        {
            get
            {
                return this._fitWidth;
            }
            set
            {
                this._fitWidth = value;
            }
        }

        public bool GridLines
        {
            get
            {
                return this._gridLines;
            }
            set
            {
                this._gridLines = value;
            }
        }

        public int HorizontalResolution
        {
            get
            {
                return this._horizontalResolution;
            }
            set
            {
                this._horizontalResolution = value;
            }
        }

        public bool LeftToRight
        {
            get
            {
                return this._leftToRight;
            }
            set
            {
                this._leftToRight = value;
            }
        }

        public int PaperSizeIndex
        {
            get
            {
                if (this._paperSizeIndex == Namespaces.NullValue)
                {
                    return 0;
                }
                return this._paperSizeIndex;
            }
            set
            {
                this._paperSizeIndex = value;
            }
        }

        public PrintErrorsOption PrintErrors
        {
            get
            {
                return this._printErrors;
            }
            set
            {
                this._printErrors = value;
            }
        }

        public bool RowColHeadings
        {
            get
            {
                return this._rowColHeadings;
            }
            set
            {
                this._rowColHeadings = value;
            }
        }

        public int Scale
        {
            get
            {
                return this._scale;
            }
            set
            {
                this._scale = value;
            }
        }

        public bool ValidPrinterInfo
        {
            get
            {
                return this._validPrinterInfo;
            }
            set
            {
                this._validPrinterInfo = value;
            }
        }

        public int VerticalResolution
        {
            get
            {
                return this._verticalResolution;
            }
            set
            {
                this._verticalResolution = value;
            }
        }
    }
}

