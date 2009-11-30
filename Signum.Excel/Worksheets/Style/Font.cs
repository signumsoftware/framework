namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Font : IWriter, IReader, IExpressionWriter, IEquatable<Font>
    {
        private bool _bold;
        private string _color;
        private string _fontName;
        private bool _italic;
        private float _size;
        private bool _strikethrough;
        private UnderlineStyle _underline;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Font>(new MemberBindingList<Font>()
            { 
                {false,_bold,a=>a.Bold},
                {false,_italic,a=>a.Italic},
                {UnderlineStyle.None,_underline,a=>a.Underline},
                {false,_strikethrough,a=>a.Strikethrough},
                {null,_fontName,a=>a.FontName},
                {0.0f,_size,a=>a.Size},
                {null,_color,a=>a.Color}
            }).Collapse(); 
        }
     
        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._bold = element.GetAttribute("Bold", Namespaces.SpreadSheet) == "1";
            this._italic = element.GetAttribute("Italic", Namespaces.SpreadSheet) == "1";
            string attribute = element.GetAttribute("Underline", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length != 0))
            {
                this._underline = (UnderlineStyle) Enum.Parse(typeof(UnderlineStyle), attribute, true);
            }
            this._strikethrough = element.GetAttribute("StrikeThrough", Namespaces.SpreadSheet) == "1";
            this._fontName = UtilXml.GetAttribute(element, "FontName", Namespaces.SpreadSheet);
            this._size = UtilXml.GetAttribute(element, "Size", Namespaces.SpreadSheet, 0.0f);
            this._color = UtilXml.GetAttribute(element, "Color", Namespaces.SpreadSheet);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Font", Namespaces.SpreadSheet);
            if (this._bold)
            {
                writer.WriteAttributeString("Bold", Namespaces.SpreadSheet, "1");
            }
            if (this._italic)
            {
                writer.WriteAttributeString("Italic", Namespaces.SpreadSheet, "1");
            }
            if (this._underline != UnderlineStyle.None)
            {
                writer.WriteAttributeString("Underline", Namespaces.SpreadSheet, this._underline.ToString());
            }
            if (this._strikethrough)
            {
                writer.WriteAttributeString("StrikeThrough", Namespaces.SpreadSheet, "1");
            }
            if (this._fontName != null)
            {
                writer.WriteAttributeString("FontName", Namespaces.SpreadSheet, this._fontName.ToString(CultureInfo.InvariantCulture));
            }
            if (this._size != 0.0f)
            {
                writer.WriteAttributeString("Size", Namespaces.SpreadSheet, this._size.ToString(CultureInfo.InvariantCulture));
            }
            if (this._color != null)
            {
                writer.WriteAttributeString("Color", Namespaces.SpreadSheet, this._color);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Font", Namespaces.SpreadSheet);
        }

        public bool Bold
        {
            get
            {
                return this._bold;
            }
            set
            {
                this._bold = value;
            }
        }

        public string Color
        {
            get
            {
                return this._color;
            }
            set
            {
                this._color = value;
            }
        }

        public string FontName
        {
            get
            {
                return this._fontName;
            }
            set
            {
                this._fontName = value;
            }
        }

        public bool Italic
        {
            get
            {
                return this._italic;
            }
            set
            {
                this._italic = value;
            }
        }

        public float Size
        {
            get
            {
                return this._size;
            }
            set
            {
                this._size = value;
            }
        }

        public bool Strikethrough
        {
            get
            {
                return this._strikethrough;
            }
            set
            {
                this._strikethrough = value;
            }
        }

        public UnderlineStyle Underline
        {
            get
            {
                return this._underline;
            }
            set
            {
                this._underline = value;
            }
        }

        public bool Equals(Font other)
        {
            if (other == null) return false;
            if (other == this) return true;

            return
                this._bold == other._bold &&
                this._color == other._color &&
                this._fontName == other._fontName &&
                this._italic == other._italic &&
                this._size == other._size &&
                this._strikethrough == other._strikethrough &&
                this._underline == other._underline;
        }
    }
}

