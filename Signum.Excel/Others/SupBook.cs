namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections.Specialized;
    using System.Xml;
    using System.Linq.Expressions;
    using System.Collections.ObjectModel;

    public sealed class SupBook : IWriter, IReader, IExpressionWriter
    {
        private Collection<string> _externNames;
        private string _path;
        private XctCollection _references;
        private Collection<string> _sheetNames;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<SupBook>(new MemberBindingList<SupBook>()
            {
                {null,_path,a=>a.Path},
                {_references, a=>a.References},
                {_externNames,  a=>a.ExternNames},
                {_sheetNames, a=>a.SheetNames}
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
                    if (UtilXml.IsElement(element2, "Path", Namespaces.Excel))
                    {
                        this._path = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "SheetName", Namespaces.Excel))
                    {
                        this.SheetNames.Add(element2.InnerText);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "ExternName", Namespaces.Excel))
                    {
                        this.ExternNames.Add(element2.InnerText);
                        continue;
                    }
                    if (Xct.IsElement(element2))
                    {
                        Xct item = new Xct();
                        ((IReader) item).ReadXml(element2);
                        this.References.Add(item);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "SupBook", Namespaces.Excel);
            if (this._path != null)
            {
                writer.WriteElementString("Path", Namespaces.Excel, this._path);
            }
            if (this._sheetNames != null)
            {
                foreach (string str in this._sheetNames)
                {
                    writer.WriteElementString("SheetName", Namespaces.Excel, str);
                }
            }
            if (this._externNames != null)
            {
                foreach (string str2 in this._externNames)
                {
                    writer.WriteStartElement("ExternName", Namespaces.Excel);
                    writer.WriteElementString("Name", Namespaces.Excel, str2);
                    writer.WriteEndElement();
                }
            }
            if (this._references != null)
            {
                ((IWriter) this._references).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "SupBook", Namespaces.Excel);
        }

        public Collection<string> ExternNames
        {
            get
            {
                if (this._externNames == null)
                {
                    this._externNames = new Collection<string>();
                }
                return this._externNames;
            }
            set
            {
                this._externNames= value;
            }
        }

        public string Path
        {
            get
            {
                return this._path;
            }
            set
            {
                this._path = value;
            }
        }

        public XctCollection References
        {
            get
            {
                if (this._references == null)
                {
                    this._references = new XctCollection();
                }
                return this._references;
            }
            set
            {
                this._references= value;
            }
        }

        public Collection<string> SheetNames
        {
            get
            {
                if (this._sheetNames == null)
                {
                    this._sheetNames = new Collection<string>();
                }
                return this._sheetNames;
            }
            set
            {
                this._sheetNames = value;
            }
        }
    }
}

