using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Windows;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.Xml.Linq;
using System.Windows.Markup;

namespace Signum.Windows
{
    public partial class AutoControl : ContentControl
    {
        public AutoControl(Type type)
            : this()
        {
            Common.SetPropertyRoute(this, PropertyRoute.Root(type));
        }

        public AutoControl()
        {
            this.Initialized += new EventHandler(AutoControl_Initialized);
        }

        void AutoControl_Initialized(object sender, EventArgs e)
        {
            PropertyRoute typeContext = Common.GetPropertyRoute(this);

            if (typeContext == null || typeContext.Type == null)
            {
                Content = new Label { Foreground = Brushes.Red, Content = "No TypeContext" };
                return;
            }

            Type type = typeContext.Type;


            XElement element = GenerateEntityStackPanel(type);
            using (Common.DelayRoutes())
                this.Content = XamlReader.Load(element.CreateReader());
        }


        public XElement GenerateEntityStackPanel(Type type)
        {
            XNamespace entityNamespace = "clr-namespace:{0};assembly={1}".Formato(type.Namespace, type.Assembly.GetName().Name);
            string alias = new string(type.Namespace.Split('.').Select(a => a[0]).ToArray()).ToLower();

            XElement sp = GenerateStackPanel(type);
            sp.Add(new XAttribute("xmlns", xmlns.NamespaceName),
                   new XAttribute(XNamespace.Xmlns + "x", x.NamespaceName),
                   new XAttribute(XNamespace.Xmlns + "m", m.NamespaceName),
                   new XAttribute(XNamespace.Xmlns + alias, entityNamespace.NamespaceName),
                   new XAttribute(m + "Common.TypeContext", alias + ":" + type.Name));
            return sp;
        }

        static XElement GenerateStackPanel(Type type)
        {
            List<PropertyInfo> properties = Reflector.PublicInstancePropertiesInOrder(type)
                                           .ToList();
            return new XElement(xmlns + "StackPanel",
                properties.Select(pi => GenerateField(pi)));
        }

        static XNamespace xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        static XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        static XNamespace m = "clr-namespace:Signum.Windows;assembly=Signum.Windows";

        static XElement GenerateField(PropertyInfo pi)
        {

            Type t = pi.PropertyType;
            Type lt = t.CleanType();
            if (Reflector.IsIIdentifiable(lt))
            {
                if (Reflector.IsLowPopulation(lt))
                    return new XElement(m + "EntityCombo", new XAttribute(m + "Common.Route", pi.Name));
                else
                    return new XElement(m + "EntityLine", new XAttribute(m + "Common.Route", pi.Name));
            }

            if (t.IsEmbeddedEntity())
                return new XElement(xmlns + "GroupBox", new XAttribute("Header", pi.Name), new XAttribute(m + "Common.Route", pi.Name),
                    GenerateStackPanel(t));

            if (Reflector.IsMList(t))
            {
                Type et = t.ElementType();
                if (Reflector.IsIIdentifiable(et.CleanType()))
                    return new XElement(xmlns + "GroupBox", new XAttribute("Header", pi.Name),
                        new XElement(m + "EntityList", new XAttribute(m + "Common.Route", pi.Name), new XAttribute("MaxHeight", "150")));

                if (et.IsEmbeddedEntity())
                    return new XElement(xmlns + "GroupBox", new XAttribute("Header", pi.Name),
                        new XElement(xmlns + "Grid",
                            new XElement(xmlns + "Grid.ColumnDefinitions",
                                new XElement(xmlns + "ColumnDefinition", new XAttribute("Width", "*")),
                                new XElement(xmlns + "ColumnDefinition", new XAttribute("Width", "*"))),
                            new XElement(m + "EntityList", new XAttribute(m + "Common.Route", pi.Name), new XAttribute("ViewOnCreate", "False"), new XAttribute("Grid.Column", "0"), new XAttribute("MaxHeight", "150")),
                            new XElement(m + "DataBorder", new XAttribute(m + "Common.Route", pi.Name + "/"), new XAttribute("Grid.Column", "1"),
                                GenerateStackPanel(et))));

                return
                    new XElement(xmlns + "GroupBox", new XAttribute("Header", pi.Name),
                    new XElement(xmlns + "ListBox", new XAttribute("ItemsSource", "{Binding " + pi.Name + "}")));
            }

            return new XElement(m + "ValueLine", new XAttribute(m + "Common.Route", pi.Name));
        }
    }
}
