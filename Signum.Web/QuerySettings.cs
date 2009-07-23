using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Web
{
    public class QuerySettings
    {
        public string Title;
        public int? Top;
        public string UrlName;
    }

    public static class QueryDecorators
    {
        private static Dictionary<string, Func<object, string>> decoratorsByName;
        public static Dictionary<string, Func<object, string>> DecoratorsByName
        {
            get 
            {
                if (decoratorsByName == null)
                    decoratorsByName = new Dictionary<string, Func<object, string>>();
                return decoratorsByName;
            }
            set 
            {
                decoratorsByName = value;
            }
        }
    }
}
