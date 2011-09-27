namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Interior : IWriter, IReader, IExpressionWriter, IEquatable<Interior>
    {
        private string _color;
        private InteriorPattern _pattern;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Interior>(new MemberBindingList<Interior>()
            {
                {null,_color,a=>a.Color},
                {InteriorPattern.NotSet,_pattern,a=>a.Pattern}
            }); 
        }
       
        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._color = UtilXml.GetAttribute(element, "Color", Namespaces.SpreadSheet);
            string attribute = element.GetAttribute("Pattern", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length != 0))
            {
                this._pattern = (InteriorPattern) Enum.Parse(typeof(InteriorPattern), attribute, true);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Interior", Namespaces.SpreadSheet);
            if (this._color != null)
            {
                writer.WriteAttributeString("Color", Namespaces.SpreadSheet, this._color);
            }
            if (this._pattern != InteriorPattern.NotSet)
            {
                writer.WriteAttributeString("Pattern", Namespaces.SpreadSheet, this._pattern.ToString());
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Interior", Namespaces.SpreadSheet);
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

        public InteriorPattern Pattern
        {
            get
            {
                return this._pattern;
            }
            set
            {
                this._pattern = value;
            }
        }

        public bool Equals(Interior other)
        {
            if (other == null) return false;
            if (other == this) return true;

            return
                this._color == other._color &&
                this._pattern == other._pattern;
        }
    }
}

