using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Collections;

namespace WizardProjectName
{
    public static class WPFEntityControls
    {
        static XNamespace m = "ERRORRR";

        public static string Render(Type type)
        {
            string result = null;
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;
                settings.IndentChars = "    ";

                settings.Indent = true;

                using (XmlWriter writer2 = XmlWriter.Create(writer, settings))
                {
                    XElement me = new XElement("UserControl", new XAttribute(XNamespace.Xmlns + "m", m), GenerateStackPanel(type));
                    me.WriteTo(writer2);

                }
                result = writer.ToString();
            }

            int firstLine = result.IndexOf("\r\n") + 2;
            int lastLine = result.LastIndexOf("\r\n");

            return result.Substring(firstLine, lastLine - firstLine);
        }


        public static XElement GenerateStackPanel(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => !Reflector.CommonProperties.Contains(p.Name)).ToArray();

            return new XElement("StackPanel",
                properties.Select(pi => GenerateField(pi))); 
        }

        public static XElement GenerateField(PropertyInfo pi)
        {
            
            Type t = pi.PropertyType;
            Type lt = Reflector.ExtractLite(t) ?? t; 
            if (Reflector.IsIIdentifiable(lt))
            {
                if(Reflector.IsLowPopulation(lt))
                    return new XElement(m + "EntityCombo", new XAttribute(m + "Common.Route", pi.Name));
                else
                    return new XElement(m + "EntityLine", new XAttribute(m + "Common.Route", pi.Name));
            }

            if (Reflector.EmbeddedEntity.IsAssignableFrom(t))
                return new XElement("GroupBox", new XAttribute("Header", pi.Name), new XAttribute(m + "Common.Route", pi.Name),
                    GenerateStackPanel(t));

            if (Reflector.IsMList(t))
            {
                Type et = Reflector.CollectionType(t);
                if (Reflector.IsIIdentifiable(Reflector.ExtractLite(et) ?? et))
                    return new XElement("GroupBox", new XAttribute("Header", pi.Name),
                        new XElement(m + "EntityList", new XAttribute(m + "Common.Route", pi.Name)));

                if (Reflector.EmbeddedEntity.IsAssignableFrom(et))
                    return new XElement("GroupBox", new XAttribute("Header", pi.Name),
                        new XElement("Grid",
                            new XElement("Grid.ColumnDefinitions",
                                new XElement("ColumnDefinition", new XAttribute("Width", "*")),
                                new XElement("ColumnDefinition", new XAttribute("Width", "*"))),
                            new XElement(m + "EntityList", new XAttribute(m + "Common.Route", pi.Name), new XAttribute("ViewOnCreate", "False"),new XAttribute("Grid.Column", "0")),
                            new XElement(m + "DataBorder", new XAttribute(m + "Common.Route", pi.Name + "/"), new XAttribute("Grid.Column", "1"),
                                GenerateStackPanel(et))));

                return
                    new XElement("GroupBox", new XAttribute("Header", pi.Name),
                    new XElement("ListBox", new XAttribute("ItemsSource", "{Binding " + pi.Name + "}")));
            }

            return new XElement(m + "ValueLine", new XAttribute(m + "Common.Route", pi.Name)); 
        }
    }
}
