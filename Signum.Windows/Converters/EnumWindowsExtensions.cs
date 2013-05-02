using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using Signum.Utilities;
using System.ComponentModel;
using Signum.Entities;

namespace Signum.Windows
{
    public static class EnumWindowsExtensions
    {
        public static IEnumerable<Enum> PreAndNull(this IEnumerable<Enum> collection)
        {
            return collection.PreAnd(VoidEnumMessage.Instance);
        }

        public static IEnumerable<Enum> PreAndNull(this IEnumerable<Enum> collection, bool isNullable)
        {
            if (isNullable)
                return collection.PreAnd(VoidEnumMessage.Instance);
            return collection;
        }
    }
}
