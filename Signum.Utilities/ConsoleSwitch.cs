using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;

namespace Signum.Utilities
{
    public class ConsoleSwitch<K, V>: IEnumerable<V>
    {
        Dictionary<string, Tuple<V, string>> dictionary = new Dictionary<string, Tuple<V, string>>();
        string welcomeMessage;

        public ConsoleSwitch()
            : this("Select one of the following options:")
        {
        }

        public ConsoleSwitch(string welcomeMessage)
        {
            this.welcomeMessage = welcomeMessage;
        }

        public void Add(K key, V value)
        {
            dictionary.Add(key.ToString(), Tuple.New(value, ToString(value)));
        }

        public void Add(K key, V value, string str)
        {
            dictionary.Add(key.ToString(), Tuple.New(value, str));
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
            return Choose("Enter your selection: ");
        }

        public V Choose(string endMessage)
        {
        retry:
            try
            {
                Console.WriteLine(welcomeMessage);
                dictionary.ToConsole(kvp => " {0} - {1}".Formato(kvp.Key, kvp.Value.Second));

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
            return ChooseMultiple("Enter yout selections separated by comma: ");
        }

        public V[] ChooseMultiple(string endMessage)
        {
        retry:
            try
            {
                Console.WriteLine(welcomeMessage);
                dictionary.ToConsole(kvp => " {0} - {1}".Formato(kvp.Key, kvp.Value.Second));

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

        private V GetValue(string line)
        {
            return dictionary.Where(kvp => string.Equals(kvp.Key.ToString(), line, StringComparison.InvariantCultureIgnoreCase)).Single("No option with key {0} found".Formato(line))
                .Value.First;
        }

   
        public IEnumerator<V> GetEnumerator()
        {
            return dictionary.Values.Select(p=>p.First).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
          return GetEnumerator(); 
        }
    }
}
