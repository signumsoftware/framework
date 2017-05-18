using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Signum.Utilities
{
    public static class XmlExtensions
    {
        /// <summary>
        /// Returns an XML document representing the given object.
        /// </summary>
        /// <param name="obj">Any object.</param>
        /// <returns>A XML string, representing the given object on success.</returns>
        public static string SerializeToXml<T>(T obj)
        {
            if (obj == null)
                return string.Empty;

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                {
                    serializer.Serialize(writer, obj);
                    return stringWriter.ToString();
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred during serialization.", e);
            }
        }

        /// <summary>
        /// Returns a deserialized object from the given string.
        /// </summary>
        /// <param name="obj">The XML string.</param>
        /// <returns>The deserialized object from the string.</returns>
        public static T DeserializeFromXml<T>(string str)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(str))
            {
                return (T)serializer.Deserialize(reader);
            }
        }
    }
}