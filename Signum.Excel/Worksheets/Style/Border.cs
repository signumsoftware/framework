namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Border : IWriter, IReader, IExpressionWriter
    {
        private string _color;
        private LineStyleOption _lineStyle;
        private Position _position;
        private int _weight = -1;


        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Border>(new MemberBindingList<Border>()
            {
                {Position.NotSet,_position, w=>w.Position},
                {-1,_weight, w=>w.Weight},
                {null,_color, w=>w.Color},
                {LineStyleOption.NotSet,_lineStyle, w=>w.LineStyle},
            }).Collapse(); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._color = UtilXml.GetAttribute(element, "Color", Namespaces.SpreadSheet);
            string attribute = element.GetAttribute("Position", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length != 0))
            {
                this._position = (Position) Enum.Parse(typeof(Position), attribute, true);
            }
            this._weight = UtilXml.GetAttribute(element, "Weight", Namespaces.SpreadSheet, -1);
            this._color = UtilXml.GetAttribute(element, "Color", Namespaces.SpreadSheet);
            attribute = element.GetAttribute("LineStyle", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length != 0))
            {
                this._lineStyle = (LineStyleOption) Enum.Parse(typeof(LineStyleOption), attribute, true);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Border", Namespaces.SpreadSheet);
            if (this._color != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Color", Namespaces.SpreadSheet, this._color);
            }
            if (this._position != Position.NotSet)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Position", Namespaces.SpreadSheet, this._position.ToString());
            }
            if (this._lineStyle != LineStyleOption.NotSet)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "LineStyle", Namespaces.SpreadSheet, this._lineStyle.ToString());
            }
            if (this._weight != -1)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Weight", Namespaces.SpreadSheet, this._weight.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Border", Namespaces.SpreadSheet);
        }

        internal int IsSpecial()
        {
            if (((this._position == Position.NotSet) || (this._weight == -1)) || (this._lineStyle == LineStyleOption.NotSet))
            {
                return 2;
            }
            if (this._color == null)
            {
                return 0;
            }
            return 1;
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

        public LineStyleOption LineStyle
        {
            get
            {
                return this._lineStyle;
            }
            set
            {
                this._lineStyle = value;
            }
        }

        public Position Position
        {
            get
            {
                return this._position;
            }
            set
            {
                this._position = value;
            }
        }

        public int Weight
        {
            get
            {
                return this._weight;
            }
            set
            {
                if (value >= 3)
                {
                    this._weight = 3;
                }
                else if (value <= 0)
                {
                    this._weight = 0;
                }
                else
                {
                    this._weight = value;
                }
            }
        }
    }
}

