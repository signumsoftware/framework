namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class AutoFilter : IReader, IWriter, IExpressionWriter
    {
        private string _range;
        

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<AutoFilter>(new TrioList<AutoFilter>()
            {
                {null,_range, w=>w.Range},
            }); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._range = UtilXml.GetAttribute(element, "Range", Namespaces.Excel);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "AutoFilter", Namespaces.Excel);
            if (this._range != null)
            {
                writer.WriteAttributeString("Range", Namespaces.Excel, this._range);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "AutoFilter", Namespaces.Excel);
        }

        public string Range
        {
            get
            {
                return this._range;
            }
            set
            {
                this._range = value;
            }
        }
    }
}

