namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Toolbar : IWriter, IReader, IExpressionWriter
    {
        private bool _hidden;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Toolbar>(new TrioList<Toolbar>()
            {
                {false,_hidden,a=>a.Hidden}
            }); 
        }
        
        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._hidden = element.GetAttribute("Hidden", Namespaces.SpreadSheet) == "1";
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ComponentPrefix, "Toolbar", Namespaces.ComponentNamespace);
            if (this._hidden)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Hidden", Namespaces.SpreadSheet, "1");
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Toolbar", Namespaces.ComponentNamespace);
        }

        public bool Hidden
        {
            get
            {
                return this._hidden;
            }
            set
            {
                this._hidden = value;
            }
        }
    }
}

