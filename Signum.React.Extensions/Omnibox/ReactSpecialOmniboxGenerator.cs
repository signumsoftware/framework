using System;
using System.Collections.Generic;
using Signum.Entities.Omnibox;

namespace Signum.React.Omnibox
{

    public class ReactSpecialOmniboxAction : ISpecialOmniboxAction
    {
        public string Key { get; set; }

        //filtered client-side to avoid duplication, at the end the action itself is server-side checked
        public Func<bool> Allowed { get; } = () => true;
    }

    public class ReactSpecialOmniboxGenerator : OmniboxResultGenerator<SpecialOmniboxResult>
    {
        //Depends on client-side information
        public static SpecialOmniboxGenerator<ReactSpecialOmniboxAction> ClientGenerator;

        public override List<HelpOmniboxResult> GetHelp()
        {
            return ClientGenerator.GetHelp();
        }

        public override IEnumerable<SpecialOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            return  ClientGenerator.GetResults(rawQuery, tokens, tokenPattern);
        }
    }
}