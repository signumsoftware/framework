namespace Signum.Upgrade.Upgrades;

class Upgrade_20211129_CachedQueries : CodeUpgradeBase
{
    public override string Description => "Configure CachedQueries in Dashboard";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.Entities/ApplicationConfiguration.cs", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains(@"/*Exceptions*/"), @"/*Dashboard*/
[StringLengthValidator(Max = 300), FileNameValidator]
public string CachedQueryFolder { get; set; }
");          
        });

        uctx.ChangeCodeFile(@"Southwind.Logic/Starter.cs", file =>
        {
            file.ReplaceLine(a => a.Contains(@"DashboardLogic.Start(sb);"), @"DashboardLogic.Start(sb, GetFileTypeAlgorithm(p => p.CachedQueryFolder));");
            file.WarningLevel = WarningLevel.None;
            file.Replace(@"PredictorLogic.Start(sb, () => ", @"PredictorLogic.Start(sb, ");
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/SouthwindMigrations.cs", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains(@"ExceptionsFolder"), @"CachedQueryFolder = localPrefix + @""cached-query"",");
        });

        uctx.ChangeCodeFile(@"Southwind.Test.Environment/SouthwindEnvironment.cs", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains(@"ExceptionsFolder"), @"CachedQueryFolder = localPrefix + @""cached-query"",");
        });

        uctx.ChangeCodeFile(".editorconfig", file =>
        {
            file.WarningLevel = WarningLevel.None;
            file.Replace("@csharp_style_namespace_declarations", "csharp_style_namespace_declarations");
        });
    }
}
