using System;
using System.Collections.Generic;
using System.Text;

namespace VisualObject.ExploradorClases
{
    class ClassNamespaceItem
    {
        public string texto;
        public bool enabled;
        List<ClassNamespaceItem> hijos;

        public ClassNamespaceItem(string item)
        {
            enabled = true;
            hijos = new List<ClassNamespaceItem>();
        }
    }
}
