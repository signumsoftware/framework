namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Linq.Expressions;
    using Signum.Utilities.ExpressionTrees;

    public sealed class StyleCollection  : Collection<Style>,IWriter,  IReader, IExpressionWriter
    {
        public Style Add(string id)
        {
            Style style = new Style(id);
            this.Add(style);
            return style;
        }

        public Expression CreateExpression()
        {
            return UtilExpression.ListInit(this); 
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
                if ((element2 != null) && Style.IsElement(element2))
                {
                    Style style = new Style(null);
                    ((IReader) style).ReadXml(element2);
                    this.Add(style);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Styles", Namespaces.SpreadSheet);
            for (int i = 0; i < base.Count; i++)
            {
                ((IWriter) base[i]).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Styles", Namespaces.SpreadSheet);
        }

    }
}

