using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;
using System.Resources;
using System.Reflection;
using System.Windows;
using System.ComponentModel;
using Signum.Utilities;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Signum.Windows
{
    /// <summary>
    /// Represents a localization makrup extension.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    //[ContentProperty("Key")]
    public class LocExtension : MarkupExtension
    {
        /// <summary>
        /// Gets or sets the resource key.
        /// </summary>
        [ConstructorArgument("key")]
        public string Key { get; set; }

        public Type AssemblyType { get; set; }


        public LocExtension() { }
        /// <summary>
        /// Initializes new instance of the class.
        /// </summary>
        /// <param name="key">The resource key.</param>
        public LocExtension(string key)
        {
            Key = key;
        }

        static Regex regex = new Regex(@"/(?<an>[^/]*)\;component/");

        /// <summary>
        /// Returns the object that corresponds to the specified resource key.
        /// </summary>
        /// <param name="serviceProvider">An object that can provide services for the markup extension.</param>
        /// <returns>The object that corresponds to the specified resource key.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return "[null]";
            Assembly assembly = GetAssembly(serviceProvider);
            if (assembly == null)
                return Key;

            ResourceManager manager = new ResourceManager(assembly.GetName().Name + ".Properties.Resources", assembly);
            if (manager == null)
                return Key;

            return manager.GetObject(Key);
        }

        private Assembly GetAssembly(IServiceProvider serviceProvider)
        {
            if (AssemblyType != null)
                return AssemblyType.Assembly;

            IUriContext uriContext = serviceProvider.GetService(typeof(IUriContext)) as IUriContext;
            if (uriContext != null)
            {
                Match m = regex.Match(uriContext.BaseUri.ToString());
                if (m != null && m.Success)
                {
                    string an = m.Groups["an"].Value;
                    return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == an);
                }
            }

            return Assembly.GetExecutingAssembly();
        }
    }
}