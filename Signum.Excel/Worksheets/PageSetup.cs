namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class PageSetup : IWriter, IReader, IExpressionWriter
    {
        private PageFooter _footer;
        private PageHeader _header;
        private PageLayout _layout;
        private PageMargins _pageMargins;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<PageSetup>(new MemberBindingList<PageSetup>()
            {
                {_layout,a=>a.Layout},
                {_header,a=>a.Header},
                {_footer,a=>a.Footer},
                {_pageMargins,a=>a.PageMargins},
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
                    if (PageLayout.IsElement(element2))
                    {
                        ((IReader) this.Layout).ReadXml(element2);
                        continue;
                    }
                    if (PageHeader.IsElement(element2))
                    {
                        ((IReader) this.Header).ReadXml(element2);
                        continue;
                    }
                    if (PageFooter.IsElement(element2))
                    {
                        ((IReader) this.Footer).ReadXml(element2);
                        continue;
                    }
                    if (PageMargins.IsElement(element2))
                    {
                        ((IReader) this.PageMargins).ReadXml(element2);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PageSetup", Namespaces.Excel);
            if (this._layout != null)
            {
                ((IWriter) this._layout).WriteXml(writer);
            }
            if (this._header != null)
            {
                ((IWriter) this._header).WriteXml(writer);
            }
            if (this._footer != null)
            {
                ((IWriter) this._footer).WriteXml(writer);
            }
            if (this._pageMargins != null)
            {
                ((IWriter) this._pageMargins).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "PageSetup", Namespaces.Excel);
        }

        public PageFooter Footer
        {
            get
            {
                if (this._footer == null)
                {
                    this._footer = new PageFooter();
                }
                return this._footer;
            }

            set
            {
                this._footer = value; 
            }
        }

        public PageHeader Header
        {
            get
            {
                if (this._header == null)
                {
                    this._header = new PageHeader();
                }
                return this._header;
            }

            set
            {
                this._header = value; 
            }
        }

        public PageLayout Layout
        {
            get
            {
                if (this._layout == null)
                {
                    this._layout = new PageLayout();
                }
                return this._layout;
            }

            set
            {
                this._layout = value; 
            }
        }

        public PageMargins PageMargins
        {
            get
            {
                if (this._pageMargins == null)
                {
                    this._pageMargins = new PageMargins();
                }
                return this._pageMargins;
            }

            set
            {
                this._pageMargins = value; 
            }
        }
    }
}

