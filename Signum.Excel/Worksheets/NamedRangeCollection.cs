namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Reflection;
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Linq.Expressions;

    public sealed class NamedRangeCollection  : Collection<NamedRange>,IWriter, IExpressionWriter, IReader
    {   
        protected override void InsertItem(int index, NamedRange item)
        {
            if (item== null)
            {
                throw new ArgumentNullException("namedRange");
            }
            base.InsertItem(index, item);
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
                if ((element2 != null) && NamedRange.IsElement(element2))
                {
                    NamedRange namedRange = new NamedRange();
                    ((IReader) namedRange).ReadXml(element2);
                    this.Add(namedRange);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Names", Namespaces.SpreadSheet);
            for (int i = 0; i < base.Count; i++)
            {
                ((IWriter) base[i]).WriteXml(writer);
            }
            writer.WriteEndElement();
        }   

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Names", Namespaces.SpreadSheet);
        }
    }
}

