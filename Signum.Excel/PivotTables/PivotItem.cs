namespace Signum.Excel
{
    using System;
    using System.Xml;

    public sealed class PivotItem : IWriter
    {
        private bool _hideDetail;
        private string _name;

        public PivotItem()
        {
        }

        public PivotItem(string name)
        {
            this._name = name;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PivotItem", Namespaces.Excel);
            if (this._name != null)
            {
                writer.WriteElementString("Name", Namespaces.Excel, this._name);
            }
            if (this._hideDetail)
            {
                writer.WriteElementString("HideDetail", Namespaces.Excel, "");
            }
            writer.WriteEndElement();
        }

        public bool HideDetail
        {
            get
            {
                return this._hideDetail;
            }
            set
            {
                this._hideDetail = value;
            }
        }

        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }
    }
}

