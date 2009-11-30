namespace Signum.Excel
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Xml;
    using System.Collections.ObjectModel;

    public sealed class PTLineItemCollection : Collection<PTLineItem>, IWriter
    {
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PTLineItems", Namespaces.Excel);
            for (int i = 0; i < base.Count; i++)
            {
                ((IWriter) base[i]).WriteXml(writer);
            }
            writer.WriteEndElement();
        }
    }
}

