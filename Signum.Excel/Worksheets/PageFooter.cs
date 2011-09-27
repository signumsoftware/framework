namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class PageFooter : IWriter, IReader, IExpressionWriter
    {
        private string _data;
        private float _margin = 0.5f;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<PageFooter>(new MemberBindingList<PageFooter>()
            {  
                {null,_data,a=>a.Data},
                {0.5f,_margin,a=>a.Margin}
            }).Collapse(); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._margin = UtilXml.GetAttribute(element, "Margin", Namespaces.Excel, (float) 0.5f);
            this._data = UtilXml.GetAttribute(element, "Data", Namespaces.Excel);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "Footer", Namespaces.Excel);
            if (this._data != null)
            {
                writer.WriteAttributeString("Data", Namespaces.Excel, this._data);
            }
            if (this._margin != 0.5)
            {
                writer.WriteAttributeString("Margin", Namespaces.Excel, this._margin.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Footer", Namespaces.Excel);
        }

        public string Data
        {
            get
            {
                return this._data;
            }
            set
            {
                this._data = value;
            }
        }

        public float Margin
        {
            get
            {
                return this._margin;
            }
            set
            {
                this._margin = value;
            }
        }
    }
}

