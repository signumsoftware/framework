using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;

namespace Signum.Web
{
    public static class Constructor
    {
        public static ConstructorManager ConstructorManager = new ConstructorManager() {  Constructors = new Dictionary<Type,Func<object>>()};

        public static object Construct(Type type)
        {
            return ConstructorManager.Construct(type); 
        }

        public static T Construct<T>()
        {
            return (T)Construct(typeof(T));
        }
    }
    
    public class ConstructorManager
    {
        public Dictionary<Type, Func<object>> Constructors;

        public virtual object Construct(Type type)
        {
            Type exType = Reflector.ExtractLazy(type) ?? type;

            Func<object> c = Constructors.TryGetC(exType);

            object result = c != null? c(): DefaultContructor(exType); 
           
            return exType == type ? result: Activator.CreateInstance(type, result); // make lazy if possible
        }

        public static object DefaultContructor(Type type)
        {
            object result = Activator.CreateInstance(type);

            return result;
        }


        //public static object AddFilterProperties(Window window, object obj)
        //{
        //    if (obj == null)
        //        throw new ArgumentNullException("result");

        //    Type type = obj.GetType();

        //    if (window is SearchWindow)
        //    {
        //        var pairs = from pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        //                    join fo in ((SearchWindow)window).CurrentFilters().Where(fo => fo.Operation == FilterOperation.Igual) on pi.Name equals fo.Column.Name
        //                    where Server.CanConvert(fo.Value, pi.PropertyType) && fo.Value != null
        //                    select new { pi, fo };

        //        foreach (var p in pairs)
        //        {
        //            p.pi.SetValue(obj, Server.Convert(p.fo.Value, p.pi.PropertyType), null);
        //        }
        //    }

        //    return obj; 
        //}
    }

    public static class NotesProvider
    {
        public static NotesProviderManager Manager; 
    }

    public class NotesProviderManager
    {
        public Func<IdentifiableEntity, INoteDN> ConstructNote;
        /// <summary>
        /// Null means hide notes
        /// </summary>
        public Func<IdentifiableEntity, List<Lazy<INoteDN>>> RetrieveNotes;
    }
}
