namespace Signum.Excel
{

    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Linq;
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Linq.Expressions;
    using Signum.Utilities;

    public sealed class BorderCollection  : Collection<Border>,IWriter, IReader, IExpressionWriter, IEquatable<BorderCollection>
    {
        public Border Add(Position position, LineStyleOption lineStyle)
        {
            return this.Add(position, lineStyle, 0, null);
        }

        public Border Add(Position position, LineStyleOption lineStyle, int weight)
        {
            return this.Add(position, lineStyle, weight, null);
        }

        public Border Add(Position position, LineStyleOption lineStyle, int weight, string color)
        {
            Border border = new Border();
            border.Position = position;
            border.LineStyle = lineStyle;
            border.Weight = weight;
            border.Color = color;
            this.Add(border);
            return border;
        }

        public Expression CreateExpression()
        {
            return UtilExpression.ListInit(this);
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if ((element2 != null) && Border.IsElement(element2))
                {
                    Border border = new Border();
                    ((IReader) border).ReadXml(element2);
                    this.Add(border);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Borders", Namespaces.SpreadSheet);
            for (int i = 0; i < base.Count; i++)
            {
                ((IWriter) base[i]).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

  
        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Borders", Namespaces.SpreadSheet);
        }

        public bool Equals(BorderCollection other)
        {
            if (other == null) return false;
            if (other == this) return true;
   
            return other.ToDictionary(b=>b.Position).OuterJoinDictionaryCC(this.ToDictionary(b=>b.Position), (p,b1,b2)=> 
                b1 != null && b2 != null &&
                b1.Color == b2.Color && b1.LineStyle == b2.LineStyle && b1.Weight == b2.Weight).Values.All(b=>b);
        }
    }
}

