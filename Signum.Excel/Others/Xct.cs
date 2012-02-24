namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Xct : IWriter, IReader, IExpressionWriter
    {
        private CrnCollection _operands;
        private int _sheetIndex = Namespaces.NullValue;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Xct>(new MemberBindingList<Xct>()
            {
                {Namespaces.NullValue,_sheetIndex,a=>a.SheetIndex},
                {_operands,a=>a.Operands},
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
                    if (UtilXml.IsElement(element2, "SheetIndex", Namespaces.Excel))
                    {
                        this._sheetIndex = int.Parse(element2.InnerText);
                        continue;
                    }
                    if (Crn.IsElement(element2))
                    {
                        Crn item = new Crn();
                        ((IReader) item).ReadXml(element2);
                        this.Operands.Add(item);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "Xct", Namespaces.Excel);
            if (this._operands != null)
            {
                writer.WriteElementString("Count", Namespaces.Excel, this._operands.Count.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteElementString("Count", Namespaces.Excel, "0");
            }
            if (this._sheetIndex != Namespaces.NullValue)
            {
                writer.WriteElementString("SheetIndex", Namespaces.Excel, this._sheetIndex.ToString(CultureInfo.InvariantCulture));
            }
            if (this._operands != null)
            {
                ((IWriter) this._operands).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Xct", Namespaces.Excel);
        }

        public CrnCollection Operands
        {
            get
            {
                if (this._operands == null)
                {
                    this._operands = new CrnCollection();
                }
                return this._operands;
            }
            set
            {
                this._operands = value; 
            }
        }

        public int SheetIndex
        {
            get
            {
                if (this._sheetIndex == Namespaces.NullValue)
                {
                    return 0;
                }
                return this._sheetIndex;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Invalid range, > 0");
                }
                this._sheetIndex = value;
            }
        }
    }
}

