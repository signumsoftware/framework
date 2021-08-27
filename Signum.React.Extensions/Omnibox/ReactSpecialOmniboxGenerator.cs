using System;
using System.Collections.Generic;
using Signum.Entities.Omnibox;
using Signum.Utilities;

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

        public static Signum.Utilities.ThreadVariable<SpecialOmniboxGenerator<ReactSpecialOmniboxAction>> ClientGeneratorVariable = Statics.ThreadVariable<SpecialOmniboxGenerator<ReactSpecialOmniboxAction>>("clientGeneratorVariable");

        public static IDisposable OverrideClientGenerator(SpecialOmniboxGenerator<ReactSpecialOmniboxAction> generator)
        {
            var old = ClientGeneratorVariable.Value;
            ClientGeneratorVariable.Value = generator;
            return new Disposable(() => ClientGeneratorVariable.Value = old);
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            return ClientGeneratorVariable.Value.GetHelp();
        }

        public override IEnumerable<SpecialOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            return ClientGeneratorVariable.Value.GetResults(rawQuery, tokens, tokenPattern);
        }
    }
}
