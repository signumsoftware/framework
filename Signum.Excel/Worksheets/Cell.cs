namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;
    using System.Collections.ObjectModel;
    using Signum.Utilities.ExpressionTrees;

    public sealed class Cell : Indexed, IWriter, IReader, IExpressionWriter
    {
        private Comment _comment;
        private CellData _data;
        private string _formula;
        private string _href;
        private int _mergeAcross = -1;
        private int _mergeDown = -1;
        private Collection<string> _namedCell;
        private string _styleID;

        #region Constructores
        public Cell()
        {

        }

        public Cell(string styleID)
        {
            this._styleID = styleID;
        }

        public Cell(string styleID, string text)
            : this(styleID)
        {
            this.Data = new CellData(text);
        }

        public Cell(string styleID, bool value)
            : this(styleID)
        {
            this.Data = new CellData(value);
        }

        public Cell(string styleID, bool? value)
            : this(styleID)
        {
            if (value.HasValue)
                this.Data = new CellData(value.Value);
        }

        public Cell(string styleID, decimal number)
            : this(styleID)
        {
            this.Data = new CellData(number);
        }

        public Cell(string styleID, decimal? number)
            : this(styleID)
        {
            if (number.HasValue)
                this.Data = new CellData(number.Value);
        }

        public Cell(string styleID, DateTime value)
            : this(styleID)
        {
            this.Data = new CellData(value);
        }

        public Cell(string styleID, DateTime? value)
            : this(styleID)
        {
            if (value.HasValue)
                this.Data = new CellData(value.Value);
        }

        public Cell(string styleID, DataType type, string text)
            : this(styleID)
        {
            this.Data = new CellData(type, text);
        } 
        #endregion

        public Expression CreateExpression()
        {       
            Expression<Func<Cell>> constructor = Constructor(Data.Text, Data.Type, _styleID);

            return UtilExpression.MemberInit<Cell>(constructor,
                new MemberBindingList<Cell>()
            {   
                {0,_offset,a=>a.Offset},
                {-1,_mergeAcross,a=>a.MergeAcross},
                {-1,_mergeDown,a=>a.MergeDown},
                {null,_formula,a=>a.Formula},
                {null,_href,a=>a.HRef},
                {_comment,a=>a.Comment},
                {_namedCell, a=>a.NamedCell},
            }).Collapse(); 
        }

        private static Expression<Func<Cell>> Constructor(string text, DataType type, string styleID)
        {
            switch (type)
            {
                case DataType.Boolean:
                    bool b = bool.Parse(text);
                    return () => new Cell(CSharpRenderer.Literal<string>(styleID), b);
                case DataType.DateTime:
                    DateTime dt = DateTime.Parse(text);
                    return () => new Cell(CSharpRenderer.Literal<string>(styleID), dt);
                case DataType.Number:
                    decimal d = decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                    return () => new Cell(CSharpRenderer.Literal<string>(styleID), d);
                case DataType.String:
                    if (string.IsNullOrEmpty(text))
                        return () => new Cell(CSharpRenderer.Literal<string>(styleID));
                    else
                        return () => new Cell(CSharpRenderer.Literal<string>(styleID), text);
                case DataType.Error:
                case DataType.NotSet:
                    return () => new Cell(CSharpRenderer.Literal<string>(styleID), type, text);
            }
            return null;
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._index = UtilXml.GetAttribute(element, "Index", Namespaces.SpreadSheet, 0);
            this._mergeAcross = UtilXml.GetAttribute(element, "MergeAcross", Namespaces.SpreadSheet, -1);
            this._mergeDown = UtilXml.GetAttribute(element, "MergeDown", Namespaces.SpreadSheet, -1);
            this._styleID = UtilXml.GetAttribute(element, "StyleID", Namespaces.SpreadSheet);
            this._formula = UtilXml.GetAttribute(element, "Formula", Namespaces.SpreadSheet);
            this._href = UtilXml.GetAttribute(element, "HRef", Namespaces.SpreadSheet);
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if (element2 != null)
                {
                    if (CellData.IsElement(element2))
                    {
                        ((IReader) this.Data).ReadXml(element2);
                        continue;
                    }
                    if (Comment.IsElement(element2))
                    {
                        ((IReader) this.Comment).ReadXml(element2);
                        continue;
                    }
                    if (element2.LocalName == "NamedCell")
                    {
                        string name = UtilXml.GetAttribute(element2, "Name", Namespaces.SpreadSheet);
                        if ((name == null) || (name.Length <= 0))
                        {
                            continue;
                        }
                        this.NamedCell.Add(name);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Cell", Namespaces.SpreadSheet);
            if (this._index != 0)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Index", Namespaces.SpreadSheet, this._index.ToString(CultureInfo.InvariantCulture));
            }
            if (this._mergeAcross > 0)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "MergeAcross", Namespaces.SpreadSheet, this._mergeAcross.ToString(CultureInfo.InvariantCulture));
            }
            if (this._mergeDown >= 0)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "MergeDown", Namespaces.SpreadSheet, this._mergeDown.ToString(CultureInfo.InvariantCulture));
            }
            if (this._styleID != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "StyleID", Namespaces.SpreadSheet, this._styleID);
            }
            if (this._formula != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Formula", Namespaces.SpreadSheet, this._formula);
            }
            if (this._href != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "HRef", Namespaces.SpreadSheet, this._href);
            }
            if (this._comment != null)
            {
                ((IWriter) this._comment).WriteXml(writer);
            }
            if (this._data != null)
            {
                ((IWriter) this._data).WriteXml(writer);
            }
            if (this._namedCell != null)
            {
                foreach (string str in this._namedCell)
                {
                    writer.WriteStartElement(Namespaces.SchemaPrefix, "NamedCell", Namespaces.SpreadSheet);
                    writer.WriteAttributeString(Namespaces.SchemaPrefix, "Name", Namespaces.SpreadSheet, str);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Cell", Namespaces.SpreadSheet);
        }

        public Comment Comment
        {
            get
            {
                if (this._comment == null)
                {
                    this._comment = new Comment();
                }
                return this._comment;
            }

            set
            {
                this._comment = null;
            }
        }

        public CellData Data
        {
            get
            {
                if (this._data == null)
                {
                    this._data = new CellData();
                }
                return this._data;
            }

            set
            {
                this._data = value; 
            }
        }

        public string Formula
        {
            get
            {
                return this._formula;
            }
            set
            {
                this._formula = value;
            }
        }

        public string HRef
        {
            get
            {
                return this._href;
            }
            set
            {
                this._href = value;
            }
        }

      

      

        public int MergeAcross
        {
            get
            {
                if (this._mergeAcross == -1)
                {
                    return 0;
                }
                return this._mergeAcross;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this._mergeAcross = value;
            }
        }

        public int MergeDown
        {
            get
            {
                if (this._mergeDown == -1)
                {
                    return 0;
                }
                return this._mergeDown;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this._mergeDown = value;
            }
        }

        public Collection<string> NamedCell
        {
            get
            {
                if (this._namedCell == null)
                {
                    this._namedCell = new Collection<string>();
                }
                return this._namedCell;
            }

            set
            {
                this._namedCell = value; 
            }
        }

        public string StyleID
        {
            get
            {
                return this._styleID;
            }
            set
            {
                this._styleID = value;
            }
        }
    }
}

