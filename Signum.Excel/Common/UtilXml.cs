namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.ComponentModel;
    using System.ComponentModel.Design.Serialization;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using System.Xml;

    internal static class UtilXml
    {
        public static string GetAttribute(XmlElement element, string name, string ns)
        {
            XmlAttribute attributeNode = element.GetAttributeNode(name, ns);
            if (attributeNode == null)
            {
                return null;
            }
            return attributeNode.Value;
        }

        public static bool GetAttribute(XmlElement element, string name, string ns, bool defaultValue)
        {
            string attribute = element.GetAttribute(name, ns);
            if ((attribute == null) || (attribute.Length == 0))
            {
                return defaultValue;
            }
            return (attribute == "1");
        }

        public static int GetAttribute(XmlElement element, string name, string ns, int defaultValue)
        {
            string attribute = element.GetAttribute(name, ns);
            if ((attribute == null) || (attribute.Length == 0))
            {
                return defaultValue;
            }
            int num = defaultValue;
            try
            {
                int index = attribute.IndexOf('.');
                if (index != -1)
                {
                    attribute = attribute.Substring(0, index);
                }
                num = int.Parse(attribute, CultureInfo.InvariantCulture);
            }
            catch
            {
            }
            return num;
        }

        public static float GetAttribute(XmlElement element, string name, string ns, float defaultValue)
        {
            string attribute = element.GetAttribute(name, ns);
            if ((attribute == null) || (attribute.Length == 0))
            {
                return defaultValue;
            }
            float num = defaultValue;
            try
            {
                num = float.Parse(attribute, CultureInfo.InvariantCulture);
            }
            catch
            {
            }
            return num;
        }

        public static bool IsElement(XmlElement element, string name, string ns)
        {
            return (((element != null) && (element.LocalName == name)) && (element.NamespaceURI == ns));
        }

        public static void WriteElementString(XmlWriter writer, string elementName, string prefix, bool value)
        {
            if (value)
            {
                writer.WriteElementString(elementName, prefix, "True");
            }
            else
            {
                writer.WriteElementString(elementName, prefix, "False");
            }
        }
    }
}

