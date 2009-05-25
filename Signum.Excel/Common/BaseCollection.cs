using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.ObjectModel;

namespace Signum.Excel
{
    public class CollectionXml<T> : Collection<T>, IWriter where T : IWriter
    {
        public void WriteXml(XmlWriter writer)
        {
            foreach (var item in this)
            {
                item.WriteXml(writer);
            }
        }
    }
}
