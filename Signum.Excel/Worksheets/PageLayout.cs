namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class PageLayout : IWriter, IReader, IExpressionWriter
    {
        private bool _centerHorizontal;
        private bool _centerVertical;
        private Orientation _orientation;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<PageLayout>(new MemberBindingList<PageLayout>()
            {
                {Orientation.NotSet,_orientation,a=>a.Orientation},
                {false,_centerHorizontal, a=>a.CenterHorizontal },
                {false,_centerVertical, a=>a.CenterVertical },
            }).Collapse(); 
        }
     
        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            string str = UtilXml.GetAttribute(element, "Orientation", Namespaces.Excel);
            if ((str != null) && (str.Length > 0))
            {
                this._orientation =  (Orientation) Enum.Parse(typeof(Orientation), str);
            }
            this._centerHorizontal = UtilXml.GetAttribute(element, "CenterHorizontal", Namespaces.Excel, false);
            this._centerVertical = UtilXml.GetAttribute(element, "CenterVertical", Namespaces.Excel, false);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "Layout", Namespaces.Excel);
            if (this._orientation != Orientation.NotSet)
            {
                writer.WriteAttributeString("Orientation", Namespaces.Excel, this._orientation.ToString());
            }
            if (this._centerHorizontal)
            {
                writer.WriteAttributeString("CenterHorizontal", Namespaces.Excel, "1");
            }
            if (this._centerVertical)
            {
                writer.WriteAttributeString("CenterVertical", Namespaces.Excel, "1");
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Layout", Namespaces.Excel);
        }

        public bool CenterHorizontal
        {
            get
            {
                return this._centerHorizontal;
            }
            set
            {
                this._centerHorizontal = value;
            }
        }

        public bool CenterVertical
        {
            get
            {
                return this._centerVertical;
            }
            set
            {
                this._centerVertical = value;
            }
        }

        public Orientation Orientation
        {
            get
            {
                return this._orientation;
            }
            set
            {
                this._orientation = value;
            }
        }
    }
}

