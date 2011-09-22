namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class DocumentProperties : IWriter, IReader, IExpressionWriter
    {
        private string _author;
        private string _company;
        private DateTime _created = DateTime.MinValue;
        private string _lastAuthor;
        private DateTime _lastSaved = DateTime.MinValue;
        private string _manager;
        private string _subject;
        private string _title;
        private string _version;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<DocumentProperties>(new MemberBindingList<DocumentProperties>()
            { 
                {null,_title,a=>a.Title},
                {null,_subject,a=>a.Subject},
                {null,_author,a=>a.Author},
                {null,_lastAuthor,a=>a.LastAuthor},
                {DateTime.MinValue,_created,a=>a.Created},
                {DateTime.MinValue,_lastSaved,a=>a.LastSaved},
                {null,_manager,a=>a.Manager},
                {null,_company,a=>a.Company},
                {null,_version,a=>a.Version},
            }); 
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
                if (element2 != null)
                {
                    if (UtilXml.IsElement(element2, "Title", Namespaces.Office))
                    {
                        this._title = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Subject", Namespaces.Office))
                    {
                        this._subject = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Author", Namespaces.Office))
                    {
                        this._author = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "LastAuthor", Namespaces.Office))
                    {
                        this._lastAuthor = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Created", Namespaces.Office))
                    {
                        this._created = DateTime.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "LastSaved", Namespaces.Office))
                    {
                        this._lastSaved = DateTime.Parse(element2.InnerText, CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Manager", Namespaces.Office))
                    {
                        this._manager = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Company", Namespaces.Office))
                    {
                        this._company = element2.InnerText;
                        continue;
                    }
                    if (UtilXml.IsElement(element2, "Version", Namespaces.Office))
                    {
                        this._version = element2.InnerText;
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.OfficePrefix, "DocumentProperties", Namespaces.Office);
            if (this._title != null)
            {
                writer.WriteElementString("Title", Namespaces.Office, this._title);
            }
            if (this._subject != null)
            {
                writer.WriteElementString("Subject", Namespaces.Office, this._subject);
            }
            if (this._author != null)
            {
                writer.WriteElementString("Author", Namespaces.Office, this._author);
            }
            if (this._lastAuthor != null)
            {
                writer.WriteElementString("LastAuthor", Namespaces.Office, this._lastAuthor);
            }
            if (this._created != DateTime.MinValue)
            {
                writer.WriteElementString("Created", Namespaces.Office, this._created.ToString(Namespaces.SchemaPrefix, CultureInfo.InvariantCulture));
            }
            if (this._lastSaved != DateTime.MinValue)
            {
                writer.WriteElementString("LastSaved", Namespaces.Office, this._lastSaved.ToString(Namespaces.SchemaPrefix, CultureInfo.InvariantCulture));
            }
            if (this._manager != null)
            {
                writer.WriteElementString("Manager", Namespaces.Office, this._manager);
            }
            if (this._company != null)
            {
                writer.WriteElementString("Company", Namespaces.Office, this._company);
            }
            if (this._version != null)
            {
                writer.WriteElementString("Version", Namespaces.Office, this._version);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "DocumentProperties", Namespaces.Office);
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

        public string Company
        {
            get
            {
                return this._company;
            }
            set
            {
                this._company = value;
            }
        }

        public DateTime Created
        {
            get
            {
                return this._created;
            }
            set
            {
                this._created = value;
            }
        }

        public string LastAuthor
        {
            get
            {
                return this._lastAuthor;
            }
            set
            {
                this._lastAuthor = value;
            }
        }

        public DateTime LastSaved
        {
            get
            {
                return this._lastSaved;
            }
            set
            {
                this._lastSaved = value;
            }
        }

        public string Manager
        {
            get
            {
                return this._manager;
            }
            set
            {
                this._manager = value;
            }
        }

        public string Subject
        {
            get
            {
                return this._subject;
            }
            set
            {
                this._subject = value;
            }
        }

        public string Title
        {
            get
            {
                return this._title;
            }
            set
            {
                this._title = value;
            }
        }

        public string Version
        {
            get
            {
                return this._version;
            }
            set
            {
                this._version = value;
            }
        }
    }
}

