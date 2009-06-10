namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Crn : IWriter, IReader, IExpressionWriter
    {
        private int _colFirst = Namespaces.NullValue;
        private int _colLast = Namespaces.NullValue;
        private NumberCollection _numbers;
        private string _row;
        private string _text;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Crn>(new MemberBindingList<Crn>()
            {
                {null,_row,a=>a.Row},
                {Namespaces.NullValue,_colFirst,a=>a.ColFirst},
                {Namespaces.NullValue,_colLast,a=>a.ColLast},
                {_numbers, a=>a.Numbers},
                {null,_text,a=>a.Text}
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
                    if (UtilXml.IsElement(element2, "Row", Namespaces.Excel))
                    {
                        this._row = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ColFirst", Namespaces.Excel))
                    {
                        this._colFirst = int.Parse(element2.InnerText);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ColLast", Namespaces.Excel))
                    {
                        this._colLast = int.Parse(element2.InnerText);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Number", Namespaces.Excel))
                    {
                        this.Numbers.Add(element2.InnerText);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Text", Namespaces.Excel))
                    {
                        this._text = element2.InnerText;
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "Crn", Namespaces.Excel);
            if (this._row != null)
            {
                writer.WriteElementString("Row", Namespaces.Excel, this._row);
            }
            if (this._colFirst != Namespaces.NullValue)
            {
                writer.WriteElementString("ColFirst", Namespaces.Excel, this._colFirst.ToString(CultureInfo.InvariantCulture));
            }
            if (this._colLast != Namespaces.NullValue)
            {
                writer.WriteElementString("ColLast", Namespaces.Excel, this._colLast.ToString(CultureInfo.InvariantCulture));
            }
            if (this._numbers != null)
            {
                ((IWriter) this._numbers).WriteXml(writer);
            }
            if (this._text != null)
            {
                writer.WriteElementString("Text", Namespaces.Excel, this._text);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Crn", Namespaces.Excel);
        }

        public int ColFirst
        {
            get
            {
                if (this._colFirst == Namespaces.NullValue)
                {
                    return 0;
                }
                return this._colFirst;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Invalid range, > 0");
                }
                this._colFirst = value;
            }
        }

        public int ColLast
        {
            get
            {
                if (this._colLast == Namespaces.NullValue)
                {
                    return 0;
                }
                return this._colLast;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Invalid range, > 0");
                }
                this._colLast = value;
            }
        }

        public NumberCollection Numbers
        {
            get
            {
                if (this._numbers == null)
                {
                    this._numbers = new NumberCollection();
                }
                return this._numbers;
            }

            set
            {
                this._numbers = value; 
            }
        }

        public string Row
        {
            get
            {
                return this._row;
            }
            set
            {
                this._row = value;
            }
        }

        public string Text
        {
            get
            {
                return this._text;
            }
            set
            {
                this._text = value;
            }
        }
    }
}

