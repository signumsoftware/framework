namespace Signum.Excel
{
    using System;
    using System.Globalization;
    using System.Xml;

    public sealed class PTLineItem : IWriter
    {
        private string _item;
        private ItemType _itemType;

        public PTLineItem()
        {
        }

        public PTLineItem(string item)
        {
            this._item = item;
        }

        public PTLineItem(string item, ItemType itemType)
        {
            this._item = item;
            this._itemType = itemType;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PTLineItem", Namespaces.Excel);
            writer.WriteElementString("Item", Namespaces.Excel, this._item);
            if (this._itemType != ItemType.NotSet)
            {
                writer.WriteElementString("ItemType", Namespaces.Excel, this._itemType.ToString());
            }
            writer.WriteEndElement();
        }

        public string Item
        {
            get
            {
                return this._item;
            }
            set
            {
                this._item = value;
            }
        }

        public ItemType ItemType
        {
            get
            {
                return this._itemType;
            }
            set
            {
                this._itemType = value;
            }
        }
    }
}

