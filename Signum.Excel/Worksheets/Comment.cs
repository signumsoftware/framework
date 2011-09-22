namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Comment : IWriter, IReader, IExpressionWriter
    {
        private string _author;
        private CommentCellData _data;
        private bool _showAlways;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Comment>(new MemberBindingList<Comment>()
            {   
                {false,_showAlways,a=>a.ShowAlways},
                {null,_author,a=>a.Author},
                {_data,a=>a.Data},
            });
        }
   
        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._showAlways = UtilXml.GetAttribute(element, "ShowAlways", Namespaces.SpreadSheet, false);
            this._author = UtilXml.GetAttribute(element, "Author", Namespaces.SpreadSheet);
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if ((element2 != null) && CellData.IsElement(element2))
                {
                    ((IReader) this.Data).ReadXml(element2);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Comment", Namespaces.SpreadSheet);
            if (this._showAlways)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "ShowAlways", Namespaces.SpreadSheet, this._showAlways.ToString(CultureInfo.InvariantCulture));
            }
            if (this._author != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Author", Namespaces.SpreadSheet, this._author);
            }
            if (this._data != null)
            {
                ((IWriter) this._data).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Comment", Namespaces.SpreadSheet);
        }

        public string Author
        {
            get
            {
                return this._author;
            }
            set
            {
                this._author = value;
            }
        }

        public CommentCellData Data
        {
            get
            {
                if (this._data == null)
                {
                    this._data = new CommentCellData();
                }
                return this._data;
            }
        }

        public bool ShowAlways
        {
            get
            {
                return this._showAlways;
            }
            set
            {
                this._showAlways = value;
            }
        }
    }
}

