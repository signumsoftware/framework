namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections.Specialized;
    using System.Xml;
    using System.Linq.Expressions;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using Signum.Utilities;

    public sealed class Worksheet : IWriter, IReader, IExpressionWriter
    {
        private AutoFilter _autoFilter;
        private string _name;
        private NamedRangeCollection _names;
        private Options _options;
        private PivotTable _pivotTable;
        private bool _protected;
        private Collection<string> _sorting;
        private WorksheetTable _table;

        public Worksheet(string name)
        {
            this._name = name;
        }

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Worksheet>(() => new Worksheet(_name), new MemberBindingList<Worksheet>()
            {  
                {false,_protected,a=>a.Protected},
                {_names,a=>a.Names},
                {_table,a=>a.Table},
                {_options,a=>a.Options},
                {_autoFilter,a=>a.AutoFilter},
                {_sorting, a=>a.Sorting}
            }); 
        }
     
        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._name = UtilXml.GetAttribute(element, "Name", Namespaces.SpreadSheet);
            this._protected = UtilXml.GetAttribute(element, "Protected", Namespaces.SpreadSheet, false);
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if (element2 != null)
                {
                    if (WorksheetTable.IsElement(element2))
                    {
                        ((IReader) this.Table).ReadXml(element2);
                    }
                    else if (NamedRangeCollection.IsElement(element2))
                    {
                        ((IReader) this.Names).ReadXml(element2);
                    }
                    else if (AutoFilter.IsElement(element2))
                    {
                        ((IReader) this.AutoFilter).ReadXml(element2);
                    }
                    else if (Options.IsElement(element2))
                    {
                        ((IReader) this.Options).ReadXml(element2);
                    }
                    else if (UtilXml.IsElement(element2, "Sorting", Namespaces.Excel))
                    {
                        foreach (XmlElement element3 in element2.ChildNodes)
                        {
                            if (UtilXml.IsElement(element3, "Sort", Namespaces.Excel))
                            {
                                this.Sorting.Add(element3.InnerText);
                            }
                        }
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Worksheet", Namespaces.SpreadSheet);
            if (this._name != null)
            {
                writer.WriteAttributeString("Name", Namespaces.SpreadSheet, this._name);
            }
            if (this._protected)
            {
                writer.WriteAttributeString("Protected", Namespaces.SpreadSheet, "1");
            }
            if (this._names != null)
            {
                ((IWriter) this._names).WriteXml(writer);
            }
            if (this._table != null)
            {
                ((IWriter) this._table).WriteXml(writer);
            }
            if (this._options != null)
            {
                ((IWriter) this._options).WriteXml(writer);
            }
            if (this._autoFilter != null)
            {
                ((IWriter) this._autoFilter).WriteXml(writer);
            }
            if (this._pivotTable != null)
            {
                ((IWriter) this._pivotTable).WriteXml(writer);
            }
            if (this._sorting != null)
            {
                writer.WriteStartElement(Namespaces.ExcelPrefix, "Sorting", Namespaces.Excel);
                foreach (string str in this._sorting)
                {
                    writer.WriteElementString("Sort", Namespaces.Excel, str);
                }
                writer.WriteEndElement();
            }
            if (ConditionalFormating != null)
            {
                writer.WriteRaw(ConditionalFormating);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Worksheet", Namespaces.SpreadSheet);
        }

        public AutoFilter AutoFilter
        {
            get
            {
                if (this._autoFilter == null)
                {
                    this._autoFilter = new AutoFilter();
                }
                return this._autoFilter;
            }
            set
            {
                this._autoFilter = value;
            }
        }

        public string Name
        {
            get
            {
                return this._name;
            }
        }

        public NamedRangeCollection Names
        {
            get
            {
                if (this._names == null)
                {
                    this._names = new NamedRangeCollection();
                }
                return this._names;
            }

            set
            {
                this._names= value;
            }
        }

        public Options Options
        {
            get
            {
                if (this._options == null)
                {
                    this._options = new Options();
                }
                return this._options;
            }

            set
            {
                this._options = value; 
            }
        }

        public string ConditionalFormating { get; set; }

        public PivotTable PivotTable
        {
            get
            {
                if (this._pivotTable == null)
                {
                    this._pivotTable = new PivotTable();
                }
                return this._pivotTable;
            }

            set
            {
                this._pivotTable = value; 
            }
        }

        public bool Protected
        {
            get
            {
                return this._protected;
            }
            set
            {
                this._protected = value;
            }
        }

        public Collection<string> Sorting
        {
            get
            {
                if (this._sorting == null)
                {
                    this._sorting = new Collection<string>();
                }
                return this._sorting;
            }

            set
            {
                this._sorting = value; 
            }
        }

        public WorksheetTable Table
        {
            get
            {
                if (this._table == null)
                {
                    this._table = new WorksheetTable();
                }
                return this._table;
            }

            set
            {
                this._table= value;
            }
        }
    }
}

