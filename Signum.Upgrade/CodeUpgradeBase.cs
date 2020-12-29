using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace Signum.Upgrade
{
    public abstract class CodeUpgradeBase
    {
        public string Key => $"{GetType().Name}";
        public bool IsExecuted { get; set; }
        public abstract string Description { get; }

        public abstract void Execute(UpgradeContext uctx);

   

    }
}
