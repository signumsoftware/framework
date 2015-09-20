using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Signum.Utilities;

namespace Signum.Engine.CodeGeneration
{
    public class ReactCodeGenerator
    {
        internal void GenerateReactFromEntities()
        {
            throw new NotImplementedException();
        }
    }


    public class ReactCodeTransformer
    {
        public virtual void ToRazorInteractive()
        {
            while (true)
            {
                Console.WriteLine("Paste your code and then write 'go'");

                StringBuilder sb = new StringBuilder();
                string line;
                while (!(line = Console.ReadLine()).Equals("go", StringComparison.InvariantCultureIgnoreCase))
                    sb.AppendLine(line);

                var react = ToReactView(sb.ToString());

                SafeConsole.WriteLineColor(ConsoleColor.Green, "React Translation:");
                Console.WriteLine();
                Console.WriteLine(react);
                Console.WriteLine();
                Console.WriteLine();
                if (!SafeConsole.Ask("Continue?"))
                    return;
            }
        }   

        public virtual string ToReactView(string razorViewText)
        {
            var result = Regex.Replace(razorViewText, @"@Html\.(?<type>(ValueLine|EntityLine|EntityList|EntityCombo|EntityStrip|EntityRepeater))"+
                @"\((?<ctx>\w+),\s*(?<param>\w+)\s*=>\s*\k<param>(\.(?<token>\w+))+(?<extra>,.+)?\)\s*$",
                m =>
                {
                    var type = m.Groups["type"].Value;
                    var ctx = m.Groups["ctx"].Value;
                    var param = m.Groups["param"].Value;
                    var tokens = m.Groups["token"].Captures.Cast<Capture>().ToString(c => c.Value.FirstLower(), ".");
                    var extra = m.Groups["extra"].Value;

                    return $"<{type} typeContext={{{ctx}.subContext({param} => {param}.{tokens})}} />" + (extra.HasText() ? "//" + extra : null);
                }, RegexOptions.Multiline );
            
            result = Regex.Replace(result, @"class=""(?<c>[^""]+)""", m => $"className=\"{m.Groups["c"].Value}\"");

            return result;
        }
    }
}
