namespace Signum.Excel.Schemas
{
    using System;

    public abstract class SchemaType: IWriter
    {
        public abstract void WriteXml(System.Xml.XmlWriter writer);
    }
}

