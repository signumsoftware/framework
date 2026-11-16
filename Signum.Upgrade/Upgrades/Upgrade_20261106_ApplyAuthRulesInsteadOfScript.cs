namespace Signum.Upgrade.Upgrades;

class Upgrade_20261106_ApplyAuthRulesInsteadOfScript : CodeUpgradeBase
{
    public override string Description => "Apply AuthRules changes instead of executing them as a script";

    public override void Execute(UpgradeContext uctx)
    {
        //ImportRulesChanges keeps the entities instead of turning them into SQL, so the rules go in with a bulk
        //copy instead of one INSERT each. ImportRulesScript is still there to review the changes as a script.
        uctx.ChangeCodeFile(@"Southwind.Test.Environment\EnvironmentTest.cs", file =>
        {
            file.Replace(
                //Greedy, so args keeps the arguments even when one of them has its own parenthesis
                new Regex(@"AuthLogic\.ImportRulesScript\((?<args>.*)\)\!?\.PlainSqlCommand\(\)\.ExecuteLeaves\(\);"),
                "AuthLogic.ImportRulesChanges(${args}).Apply();");
        });
    }
}
