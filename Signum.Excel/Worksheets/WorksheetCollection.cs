namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Collections.Generic;

    public sealed class WorksheetCollection  : CollectionXml<Worksheet>
    {
        public Worksheet Add(string name)
        {
            Worksheet result = new Worksheet(name);
            this.Add(result);
            return result;
       }

        public int IndexOf(string name)
        {
            for (int i = 0; i < base.Count; i++)
            {
                if (string.Compare(this[i].Name, name, true, CultureInfo.InvariantCulture) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public Worksheet this[string name]
        {
            get
            {
                int index = this.IndexOf(name);
                if (index == -1)
                {
                    throw new ArgumentException("The specified worksheet " + name + " does not exists in the collection");
                }
                return (Worksheet) base[index];
            }
            set
            {
                int index = this.IndexOf(name);
                if (index == -1)
                {
                    throw new ArgumentException("The specified worksheet " + name + " does not exists in the collection");
                }
                base[index] = value;
            }
        }
    }
}

