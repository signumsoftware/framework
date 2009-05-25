namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Xml;
    using System.Linq.Expressions;
    using Signum.Utilities.ExpressionTrees;

    public sealed class Style : IWriter, IReader, IExpressionWriter
    {
        private Alignment _alignment;
        private BorderCollection _borders;
        private Font _font;
        private string _id;
        private Interior _interior;
        private string _name;
        private string _numberFormat = "General";
        private string _parent;

        public Style(string id)
        {
            this._id = id;
        }

        public Expression CreateExpression()
        {
            string id = _id; // las variables locales si se tratan como valores
            return UtilExpression.MemberInit<Style>(()=>new Style(Tree.Literal<string>(id)), //escribe bla, no "bla"
                new TrioList<Style>()
            {
                {null,_name,a=>a.Name},
                {null,_parent,a=>a.Parent},
                {_font,a=>a.Font},
                {_interior,a=>a.Interior},
                {_alignment,a=>a.Alignment},
                {_borders,a=>a.Borders},
                {"General",_numberFormat,a=>a.NumberFormat}
            }); 
        }


        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._id = UtilXml.GetAttribute(element, "ID", Namespaces.SpreadSheet);
            this._name = UtilXml.GetAttribute(element, "Name", Namespaces.SpreadSheet);
            this._parent = UtilXml.GetAttribute(element, "Parent", Namespaces.SpreadSheet);
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if (element2 != null)
                {
                    if (Font.IsElement(element2))
                    {
                        ((IReader) this.Font).ReadXml(element2);
                        continue;
                    }
                    if (Interior.IsElement(element2))
                    {
                        ((IReader) this.Interior).ReadXml(element2);
                        continue;
                    }
                    if (Alignment.IsElement(element2))
                    {
                        ((IReader) this.Alignment).ReadXml(element2);
                        continue;
                    }
                    if (BorderCollection.IsElement(element2))
                    {
                        ((IReader) this.Borders).ReadXml(element2);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "NumberFormat", Namespaces.SpreadSheet))
                    {
                        string attribute = element2.GetAttribute("Format", Namespaces.SpreadSheet);
                        if ((attribute == null) || (attribute.Length <= 0))
                        {
                            continue;
                        }
                        this._numberFormat = attribute;
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Style", Namespaces.SpreadSheet);
            if (this._id != null)
            {
                writer.WriteAttributeString("ID", Namespaces.SpreadSheet, this._id);
            }
            if (this._name != null)
            {
                writer.WriteAttributeString("Name", Namespaces.SpreadSheet, this._name);
            }
            if (this._parent != null)
            {
                writer.WriteAttributeString("Parent", Namespaces.SpreadSheet, this._parent);
            }
            if (this._alignment != null)
            {
                ((IWriter) this._alignment).WriteXml(writer);
            }
            if (this._borders != null)
            {
                ((IWriter) this._borders).WriteXml(writer);
            }
            if (this._font != null)
            {
                ((IWriter) this._font).WriteXml(writer);
            }
            if (this._interior != null)
            {
                ((IWriter) this._interior).WriteXml(writer);
            }
            if (this._numberFormat != "General")
            {
                writer.WriteStartElement("NumberFormat", Namespaces.SpreadSheet);
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Format", Namespaces.SpreadSheet, this._numberFormat);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Style", Namespaces.SpreadSheet);
        }

        public Alignment Alignment
        {
            get
            {
                if (this._alignment == null)
                {
                    this._alignment = new Alignment();
                }
                return this._alignment;
            }

            set
            {
                this._alignment = value; 
            }
        }

        public BorderCollection Borders
        {
            get
            {
                if (this._borders == null)
                {
                    this._borders = new BorderCollection();
                }
                return this._borders;
            }
            set
            {
                this._borders = value; 
            }
        }

        public Font Font
        {
            get
            {
                if (this._font == null)
                {
                    this._font = new Font();
                }
                return this._font;
            }

            set
            {
                this._font = value; 
            }
        }

        public string ID
        {
            get
            {
                return this._id;
            }
        }

        public Interior Interior
        {
            get
            {
                if (this._interior == null)
                {
                    this._interior = new Interior();
                }
                return this._interior;
            }

            set
            {
                this._interior = value; 
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

        public string NumberFormat
        {
            get
            {
                return this._numberFormat;
            }
            set
            {
                this._numberFormat = value;
            }
        }

        public string Parent
        {
            get
            {
                return this._parent;
            }
            set
            {
                this._parent = value;
            }
        }

    }
}

