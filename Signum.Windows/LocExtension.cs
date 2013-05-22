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
        public Enum Key { get; set; }


        public LocExtension() { }
        /// <summary>
        /// Initializes new instance of the class.
        /// </summary>
        /// <param name="key">The resource key.</param>
        public LocExtension(Enum key)
        {
            Key = key;
        }

        /// <summary>
        /// Returns the object that corresponds to the specified resource key.
        /// </summary>
        /// <param name="serviceProvider">An object that can provide services for the markup extension.</param>
        /// <returns>The object that corresponds to the specified resource key.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Key == null)
                return "[null]";

            return Key.NiceToString();
        }
    }
}