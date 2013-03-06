using Signum.Entities.Omnibox;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Signum.Windows.Omnibox
{
    public class SpecialOmniboxAction : ISpecialOmniboxAction
    {
        public SpecialOmniboxAction(string key, Func<bool> allowed, Action<Window> onClick)
        {
            this.Key = key;
            this.Allowed = allowed;
            this.OnClick = onClick;
        }

        public string Key { get; private set; }
        public Func<bool> Allowed { get; private set; }
        public Action<Window> OnClick { get; private set; }
    }

    public class SpecialOmniboxProvider : OmniboxProvider<SpecialOmniboxResult>
    {
        public static Dictionary<string, SpecialOmniboxAction> Actions = new Dictionary<string, SpecialOmniboxAction>();

        public static void Register(SpecialOmniboxAction action)
        {
            Actions.AddOrThrow(action.Key, action, "SpecialOmniboxAction {0} already registered"); 
        }

        public override OmniboxResultGenerator<SpecialOmniboxResult> CreateGenerator()
        {
            return new SpecialOmniboxGenerator<SpecialOmniboxAction> { Actions = Actions };
        }

        public override void RenderLines(SpecialOmniboxResult result, InlineCollection lines)
        {
            lines.Add("!");
            lines.AddMatch(result.Match);
        }

        public override void OnSelected(SpecialOmniboxResult result, Window window)
        {
            ((SpecialOmniboxAction)result.Match.Value).OnClick(window);
        }

        public override string GetName(SpecialOmniboxResult result)
        {
            return ((SpecialOmniboxAction)result.Match.Value).Key;
        }

        public override Run GetIcon()
        {
            return new Run("(Special)") { Foreground = Brushes.LimeGreen };
        }

    }

}
