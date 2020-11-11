using Signum.Utilities;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace Signum.Upgrade
{
    public abstract class CodeUpgradeBase
    {
        public bool IsExecuted { get; set; }
        public abstract string Description { get; }
        public abstract string SouthwindCommitHash { get; }

        protected abstract void ExecuteInternal(UpgradeContext uctx);

        public void Execute(UpgradeContext uctx)
        {
            ExecuteInternal(uctx);
        }

        public string Key => $"{GetType().Name}";
    }
}
