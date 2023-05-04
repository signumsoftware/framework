namespace Signum.Upgrade.Upgrades;

class Upgrade_20210205_ErrorHandling : CodeUpgradeBase
{
    public override string Description => "Prevents a forever loading spinner when starting, showing the startup error instead";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React/Views/Home/Index.cshtml", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains("pre.innerText = error.stack;"),
               @"pre.style.whiteSpace = ""pre-wrap"";");
        });

        uctx.ChangeCodeFile(@"Southwind.React/App/MainPublic.tsx", file =>
        {
            file.ReplaceLine(a => a.Contains("window.onerror = (message: Event | string, filename?: string, lineno?:"),
               @"ErrorModal.register();");
        });
    }
}
