namespace Signum.Excel
{
    using System;
    using System.Xml;

    public interface IReader
    {
        void ReadXml(XmlElement element);
    }
}

