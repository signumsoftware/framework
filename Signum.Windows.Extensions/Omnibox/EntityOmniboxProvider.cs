using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Omnibox;
using System.Windows.Documents;
using System.Windows.Media;
using Signum.Utilities;
using System.Windows;
using Signum.Entities.Authorization;

namespace Signum.Windows.Omnibox
{
    public class EntityOmniboxProvider : OmniboxProvider<EntityOmniboxResult>
    {
        public override OmniboxResultGenerator<EntityOmniboxResult> CreateGenerator()
        {
            return new EntityOmniboxResultGenenerator();
        }

        public override void RenderLines(EntityOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.TypeMatch);

            lines.Add(" ");

            if (result.Id == null && result.ToStr == null)
            {
                lines.Add("...");
            }
            else
            {
                if (result.Id != null)
                {
                    lines.Add(result.Id.ToString());
                    lines.Add(": ");
                    if (result.Lite == null)
                    {
                        lines.Add(new Run(OmniboxMessage.NotFound.NiceToString()) { Foreground = Brushes.Gray });
                    }
                    else
                    {
                        string str = result.Lite.TryToString();
                        if (str.HasText())
                            lines.Add(str);
                    }
                }
                else
                {
                    if (result.Lite == null)
                    {
                        lines.Add("\"");
                        lines.Add(result.ToStr);
                        lines.Add("\": ");
                        lines.Add(new Run(OmniboxMessage.NotFound.NiceToString()) { Foreground = Brushes.Gray });
                    }
                    else
                    {
                        lines.Add(result.Lite.Id.ToString());
                        lines.Add(": ");
                        lines.AddMatch(result.ToStrMatch);
                    }
                }
            }

        }

        public override Run GetIcon()
        {
            return new Run("({0})".FormatWith(AuthMessage.View.NiceToString())) { Foreground = Brushes.DodgerBlue };
        }

        public override void OnSelected(EntityOmniboxResult result, Window window)
        {
            if (result.Lite != null)
                Navigator.NavigateUntyped(result.Lite);
        }

        public override string GetName(EntityOmniboxResult result)
        {
            return "E:" + result.Lite.Try(l => l.Key()); 
        }
    }
}
