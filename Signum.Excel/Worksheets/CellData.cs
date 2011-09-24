namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public class CellData : IWriter, IReader, IExpressionWriter
    {
        protected string _text;
        protected DataType _type = DataType.String;

        #region Constructores
        public CellData()
        { }

        public CellData(string text)
        {
            this._text = text;
            this._type = DataType.String;
        }
      
        public CellData(bool value)
        {
            this._text = value.ToString();
            this._type = DataType.Boolean;
        }

        public CellData(decimal value)
        {
            this._text = value.ToStringExcel();
            this._type = DataType.Number;
        }

        public CellData(DateTime dateTime)
        {
            this._text = dateTime.ToStringExcel();
            this._type = DataType.DateTime;
        }

        public CellData(DataType type, string text)
        {
            this._type = type;
            this._text = text;
        } 
        #endregion

        public virtual Expression CreateExpression()
        {
            return UtilExpression.MemberInit<CellData>(new MemberBindingList<CellData>()
            {
                {null,_text,a=>a.Text},
                {DataType.NotSet, _type, a=>a.Type}
            }).Collapse(); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            string attribute = element.GetAttribute("Type", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length > 0))
            {
                this._type = (DataType) Enum.Parse(typeof(DataType), attribute);
            }
            if (!element.IsEmpty)
            {
                this._text = element.InnerText;
            }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Data", Namespaces.SpreadSheet);
            if ((this._type != DataType.NotSet))
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Type", Namespaces.SpreadSheet, this._type.ToString());
            writer.WriteString(this._text);
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Data", Namespaces.SpreadSheet);
        }
  
        public string Text
        {
            get
            {
                return this._text;
            }
            set
            {
                this._text = value;
            }
        }

        public DataType Type
        {
            get
            {
                return this._type;
            }
            set
            {
                this._type = value;
            }
        }
    }

    public sealed class CommentCellData : CellData
    {
        public override Expression CreateExpression()
        {
            return UtilExpression.MemberInit<CellData>(new MemberBindingList<CellData>()
            {
                {null,_text,a=>a.Text}
            });
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Data", Namespaces.SpreadSheet);
            writer.WriteString(this._text);
            writer.WriteEndElement();
        }
    }
}

