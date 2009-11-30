namespace Signum.Excel
{
    using System;
    using System.Xml;

    public interface IWriter
    {
        void WriteXml(XmlWriter writer);
    }
}

