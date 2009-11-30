namespace Signum.Excel
{
    using System;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class NamedRange : IWriter, IReader, IExpressionWriter
    {
        private bool _hidden;
        private string _name;
        private string _refersTo;

        public NamedRange()
        { 
        }
        
        public NamedRange(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            this._name = name;
        }

        public NamedRange(string name, string refersTo, bool hidden) : this(name)
        {
            this._refersTo = refersTo;
            this._hidden = hidden;
        }

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<NamedRange>(new MemberBindingList<NamedRange>()
            { 
                {null, _name, a=>a.Name},
                {null, _refersTo, a=>a.RefersTo},
                {false, _hidden, a=>a.Hidden},
            }); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._name = UtilXml.GetAttribute(element, "Name", Namespaces.SpreadSheet);
            this._refersTo = UtilXml.GetAttribute(element, "RefersTo", Namespaces.SpreadSheet);
            this._hidden = UtilXml.GetAttribute(element, "Hidden", Namespaces.SpreadSheet, false);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "NamedRange", Namespaces.SpreadSheet);
            if (this._name != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Name", Namespaces.SpreadSheet, this._name);
            }
            if (this._refersTo != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "RefersTo", Namespaces.SpreadSheet, this._refersTo);
            }
            if (this._hidden)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Hidden", Namespaces.SpreadSheet, "1");
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "NamedRange", Namespaces.SpreadSheet);
        }

        public bool Hidden
        {
            get
            {
                return this._hidden;
            }
            set
            {
                this._hidden = value;
            }
        }

        public string Name
        {
            get
            {
                return this._name;
            }
        }

        public string RefersTo
        {
            get
            {
                return this._refersTo;
            }
            set
            {
                this._refersTo = value;
            }
        }
    }
}

