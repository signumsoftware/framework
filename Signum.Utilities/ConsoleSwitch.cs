using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using Signum.Utilities.Properties;

namespace Signum.Utilities
{
    public class ConsoleSwitch<K, V>: IEnumerable<V>
    {
        Dictionary<string, Tuple<V, string>> dictionary = new Dictionary<string, Tuple<V, string>>(StringComparer.InvariantCultureIgnoreCase);
        string welcomeMessage;

        public ConsoleSwitch()
            : this(Resources.SelectOneOfTheFollowingOptions)
        {
        }

        public ConsoleSwitch(string welcomeMessage)
        {
            this.welcomeMessage = welcomeMessage;
        }

        public void Add(K key, V value)
        {
            dictionary.AddOrThrow(key.ToString(), Tuple.Create(value, ToString(value)), "Key {0} already in ConsoleSwitch");
        }

        public void Add(K key, V value, string str)
        {
            dictionary.Add(key.ToString(), Tuple.Create(value, str));
        }

        private string ToString(object value)
        {
            if (value is Delegate)
                return ((Delegate)value).Method.Name.SpacePascal(true);
            if (value is Enum)
                return ((Enum)value).NiceToString();
            if (value == null)
                return "[No Name]"; 
            return value.ToString();
        }

        public V Choose()
        {
            return Choose(Resources.EnterYourSelection);
        }

        public V Choose(string endMessage)
        {
        retry:
            try
            {
                Console.WriteLine(welcomeMessage);
                dictionary.ToConsole(kvp => " {0} - {1}".Formato(kvp.Key, kvp.Value.Item2));

                Console.Write(endMessage);
                string line = Console.ReadLine();
                
                Console.WriteLine();

                return GetValue(line);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                goto retry;
            }
        }

        public V[] ChooseMultiple()
        {
            return ChooseMultiple(Resources.EnterYoutSelectionsSeparatedByComma);
        }

        public V[] ChooseMultiple(string endMessage)
        {
        retry:
            try
            {
                Console.WriteLine(welcomeMessage);
                dictionary.ToConsole(kvp => " {0} - {1}".Formato(kvp.Key, kvp.Value.Item2));

                Console.Write(endMessage);
                string line = Console.ReadLine();

                if (string.IsNullOrEmpty(line))
                    return null;

                Console.WriteLine(); 

                return line.Split(',').Select(str => GetValue(str)).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                goto retry;
            }
        }

        public V GetValue(string line)
        {
            return dictionary.GetOrThrow(line, Resources.NoOptionWithKey0Found).Item1;
        }
   
        public IEnumerator<V> GetEnumerator()
        {
            return dictionary.Values.Select(p=>p.Item1).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
          return GetEnumerator(); 
        }
    }
}
