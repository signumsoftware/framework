using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace Signum.Web.Help
{
    public class NamespaceModel
    {
        public NamespaceModel(string nameSpace, Type[] types)
        {
            this.Namespace = nameSpace;
            this.Types = types.Where(t => t.Namespace == nameSpace).ToList();
            this.Namespaces = (from t in types
                               where t.Namespace != nameSpace
                               group t by NextNamespacePart(t.Namespace, nameSpace) into g
                               select new NamespaceModel(g.Key, g.ToArray())).ToList();
        }

        public string NextNamespacePart(string superNamespace, string subNamespace)
        {
            int pos = superNamespace.IndexOf('.', subNamespace.Length + 1);
            if (pos == -1)
                return superNamespace;

            return superNamespace.Substring(0, pos); 
        }

        public string ShortNamespace { get { return Namespace.Split('.').Last(); } }
        public string Namespace;
        public List<Type> Types;
        public List<NamespaceModel> Namespaces;
    }
}
