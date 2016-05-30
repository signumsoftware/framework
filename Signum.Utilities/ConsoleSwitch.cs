using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.ComponentModel;

namespace Signum.Utilities
{
    public class ConsoleSwitch<K, V> : IEnumerable<KeyValuePair<string, WithDescription<V>>> where V : class
    {
        Dictionary<string, WithDescription<V>> dictionary = new Dictionary<string, WithDescription<V>>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<int, string> separators = new Dictionary<int, string>();
        string welcomeMessage;

        public ConsoleSwitch()
            : this(ConsoleMessage.SelectOneOfTheFollowingOptions.NiceToString())
        {
        }

        public ConsoleSwitch(string welcomeMessage)
        {
            this.welcomeMessage = welcomeMessage;
        }

        //Separator
        public void Add(string value)
        {
            separators.AddOrThrow(dictionary.Keys.Count, value, "Already a separator on {0}");
        }

        public void Add(K key, V value)
        {
            dictionary.AddOrThrow(key.ToString(), new WithDescription<V>(value), "Key {0} already in ConsoleSwitch");
        }

        public void Add(K key, V value, string description)
        {
            dictionary.AddOrThrow(key.ToString(), new WithDescription<V>(value, description), "Key {0} already in ConsoleSwitch");
        }

        public V Choose()
        {
            return Choose(ConsoleMessage.EnterYourSelection.NiceToString());
        }

        public V Choose(string endMessage)
        {
            var tuple = ChooseTuple(endMessage);

            if (tuple == null)
                return null;

            return tuple.Value;
        }

        public WithDescription<V> ChooseTuple()
        {
            return ChooseTuple(ConsoleMessage.EnterYourSelection.NiceToString());
        }

        public WithDescription<V> ChooseTuple(string endMessage)
        {
        retry:
            try
            {
                var step = Console.WindowHeight - 10;

                Console.WriteLine(welcomeMessage);
                for (int i = 0; i < dictionary.Count; i += step)
                {
                    PrintOptions(i, step);

                    if (i + step < dictionary.Count)
                    {
                        SafeConsole.WriteColor(ConsoleColor.White, " +");
                        Console.WriteLine(" - " + ConsoleMessage.More.NiceToString());
                    }

                    Console.WriteLine(endMessage);
                    string line = Console.ReadLine();

                    if (string.IsNullOrEmpty(line))
                        return null;

                    if (line.Trim() == "+")
                        continue;

                    Console.WriteLine();

                    return GetValue(line);
                }

                return null;             
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                goto retry;
            }
        }

        void PrintOptions(int skip, int take)
        {
            var keys = dictionary.Keys.ToList();
            var max = Math.Min(keys.Count, skip + take);
            for (int i = skip; i < max; i++)
            {
                var key = keys[i];

                string value = separators.TryGetC(i);
                if (value.HasText())
                {
                    Console.WriteLine();
                    Console.WriteLine(value);
                }

                SafeConsole.WriteColor(ConsoleColor.White, " " + keys[i]);
                Console.WriteLine(" - " + dictionary[key].Description);
            }
        }

        public V[] ChooseMultiple(string[] args = null)
        {
            return ChooseMultiple(ConsoleMessage.EnterYoutSelectionsSeparatedByComma.NiceToString(), args);
        }


        public V[] ChooseMultiple(string endMessage, string[] args = null)
        {
            var array = ChooseMultipleWithDescription(endMessage, args);

            if (array == null)
                return null;

            return array.Select(a => a.Value).ToArray();

        }

        public WithDescription<V>[] ChooseMultipleWithDescription(string[] args = null)
        {
            return ChooseMultipleWithDescription(ConsoleMessage.EnterYoutSelectionsSeparatedByComma.NiceToString(), args);
        }

        public WithDescription<V>[] ChooseMultipleWithDescription(string endMessage, string[] args = null)
        {
            if (args != null)
                return args.ToString(" ").Split(',').SelectMany(GetValuesRange).ToArray();

        retry:
            try
            {
                Console.WriteLine(welcomeMessage);
                PrintOptions(0, this.dictionary.Count);

                Console.WriteLine(endMessage);
                string line = Console.ReadLine();

                if (string.IsNullOrEmpty(line))
                {
                    Console.Clear();
                    return null;
                }

                Console.WriteLine();

                return line.Split(',').SelectMany(GetValuesRange).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                goto retry;
            }
        }

        IEnumerable<WithDescription<V>> GetValuesRange(string line)
        {
            if (line.Contains('-'))
            {
                int? from = line.Before('-')?.Let(s => s.HasText() ? GetIndex(s) : (int?)null);
                int? to = line.After('-')?.Let(s => s.HasText() ? GetIndex(s) : (int?)null);

                if (from == null && to == null)
                    return Enumerable.Empty<WithDescription<V>>();

                if (from == null && to.HasValue)
                    return dictionary.Keys.Take(to.Value + 1).Select(s => dictionary.GetOrThrow(s));

                if (from.HasValue && to == null)
                    return dictionary.Keys.Skip(from.Value).Select(s => dictionary.GetOrThrow(s));

                return dictionary.Keys.Skip(from.Value).Take((to.Value + 1) - from.Value).Select(s => dictionary.GetOrThrow(s));
            }
            else
            {
                return new[] { GetValue(line) };
            }
        }

        int GetIndex(string value)
        {
            int index = dictionary.Keys.IndexOf(value);
            if (index == -1)
                throw new KeyNotFoundException(ConsoleMessage.NoOptionWithKey0Found.NiceToString().FormatWith(value));

            return index;
        }

        WithDescription<V> GetValue(string value)
        {
            return dictionary.GetOrThrow(value, ConsoleMessage.NoOptionWithKey0Found.NiceToString());
        }

        public IEnumerator<KeyValuePair<string, WithDescription<V>>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public enum ConsoleMessage
    {
        [Description("Enter your selection (nothing to exit): ")]
        EnterYourSelection,
        [Description("Enter your selections separated by comma or hyphen (nothing to exit): ")]
        EnterYoutSelectionsSeparatedByComma,
        [Description("No option with key {0} found")]
        NoOptionWithKey0Found,
        [Description("Select one of the following options:")]
        SelectOneOfTheFollowingOptions,
        [Description("more...")]
        More
    }

    public class WithDescription<T>
    {
        public T Value { get; private set; }

        public string Description { get; private set; }

        public WithDescription(T value)
            : this(value, DefaultDescription(value))
        {

        }

        public WithDescription(T value, string description)
        {
            this.Value = value;
            this.Description = description;
        }

        static string DefaultDescription(object value)
        {
            if (value is Delegate)
                return ((Delegate)value).Method.Name.SpacePascal(true);
            if (value is Enum)
                return ((Enum)value).NiceToString();
            if (value == null)
                return "[No Name]";
            return value.ToString();
        }
    }

    public static class ConsoleSwitchExtensions
    {
        public static T ChooseConsole<T>(this List<T> collection, Func<T, string> getString = null) where T : class
        {
            var cs = new ConsoleSwitch<int, T>();
            cs.Load(collection, getString);
            return cs.Choose();
        }

        public static ConsoleSwitch<int, T> Load<T>(this ConsoleSwitch<int, T> cs, List<T> collection, Func<T, string> getString = null) where T : class
        {
            for (int i = 0; i < collection.Count; i++)
            {
                var item = collection[i];
                if (getString != null)
                    cs.Add(i, item, getString(item));
                else
                    cs.Add(i, item);
            }

            return cs;
        }

    }
}
