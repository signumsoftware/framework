using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Signum.Utilities;
using System.Threading;

namespace Signum.Engine.CodeGeneration
{
    public class ReactCodeConverter
    {
        public virtual void ToRazorInteractive()
        {
            while (true)
            {
                Console.WriteLine("Write 'r' to transform your Clipboard to React");
               
                var text = Console.ReadLine();
                if (text != "r")
                    return;

          
                Thread t = new Thread(() =>
                {
                    var aspx = Clipboard.GetText();

                    var react = ToReactView(aspx);

                    Clipboard.SetText(react);

                });
                
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();

                Console.WriteLine("Done!");
            }
        }


        public virtual string ToReactView(string razorViewText)
        {
            var result = Regex.Replace(razorViewText, @"@Html\.(?<type>(ValueLine|EntityLine|EntityList|EntityCombo|EntityStrip|EntityRepeater|EntityListCheckbox|EntityDetail))\((?<ctx>\w+)\s*,\s*(?<param>\w+)\s*=>\s*\k<param>(\.(?<token>\w+))+\s*(,\s*(?<param2>\w+)\s*=>\s*((\k<param2>\.(?<prop>\w+)\s*=\s*(?<value>[^)]+))|(?<extra>[^\)]*)))?\)",
                m =>
                {
                    var type = m.Groups["type"].Value;
                    var ctx = m.Groups["ctx"].Value;
                    var param = m.Groups["param"].Value;
                    var tokens = m.Groups["token"].Captures.Cast<Capture>().ToString(c => c.Value.FirstLower(), ".");
                    var extra = m.Groups["extra"].Value;
                    var prop = m.Groups["prop"].Value;
                    var value = m.Groups["value"].Value;
                    
                    var propAssign = prop.HasText() ? $"{prop.FirstLower()}={FixValue(value)}" : null;

                    return $"<{type} ctx={{{ctx}.subCtx({param} => {param}.{tokens})}} {propAssign} />" + (extra.HasText() ? "{/*" + extra + "*/}": null);
                }, RegexOptions.Multiline | RegexOptions.ExplicitCapture );
            

            result = Regex.Replace(result, @"class=""(?<c>[^""]+)""", m => $"className=\"{m.Groups["c"].Value}\"");

            result = Regex.Replace(result, @"@(?<type>\w+)\.(?<member>\w+)\.NiceToString\((?<params>[^)]*)\)", m =>
            {
                return $"{{{m.Groups["type"].Value},{m.Groups["member"].Value}.niceToString({m.Groups["params"].Value})}}";
            });

            return result;
        }

        string FixValue(string value)
        {
            if (value.StartsWith("new BsColumn("))
                return "{{ sm: " + value.After("new BsColumn(") + "}}";

            if (value == "true" || value == "false" || int.TryParse(value, out int _))
                return "{" + value + "}";

            return "\"" + value + "\"";
        }
    }
}
