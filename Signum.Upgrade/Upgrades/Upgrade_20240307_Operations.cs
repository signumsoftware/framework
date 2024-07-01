using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240307_Operations : CodeUpgradeBase
{ 
    public override string Description => "Operations namespace";

    public override void Execute(UpgradeContext uctx)
    {
        var simpleExports = "clearOperationSettings|operationInfos|getSettings|CreateGroup|isEntityOperation|getShortcutToString|notifySuccess|operationSettings";
        var regexHooksFinder = new Regex($@"(?<!\.)\b({simpleExports})\b", RegexOptions.Singleline);
        var simpleExportArray = simpleExports.Split("|");

        var newSimpleExport = "ConstructorOperationSettings|ConstructorOperationOptions|ConstructorOperationContext|ContextualOperationSettings|ContextualOperationOptions|ContextualOperationContext|" +
            "EntityOperationGroup|CellOperationSettings|CellOperationOptions|CellOperationContext|EntityOperationContext|AlternativeOperationSetting|EntityOperationSettings|EntityOperationOptions|KeyboardShortcut";

        var regexExport = new Regex($@"Operations\.({newSimpleExport})\b");

        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            if (file.FilePath.EndsWith("Operations.tsx") && file.FilePath.EndsWith("EntityOperations.tsx") == false && file.FilePath.EndsWith("ContextualOperations.tsx") == false)
                return;

            if (file.FilePath.EndsWith("Navigator.tsx"))
                return;

            if (file.FilePath.EndsWith("VisualTipIcon.tsx"))
                return;

            var newParts = new HashSet<string>();

            file.Replace(regexExport, m => { var part = 
                m.Value.After("Operations.");
                newParts.Add(part);
                return part; 
            });

            file.ReplaceAndCombineTypeScriptImports(path => path.EndsWith("/Operations"), parts =>
            {
                var ask = parts.Where(a => a.StartsWith("* as ")).ToList();
                if (ask.Any())
                {
                    ask.ForEach(a => parts.Remove(a));
                    parts.Add("Operations");
                }
                if (parts.RemoveWhere(simpleExportArray.Contains) > 0) 
                    parts.Add("Operations");

                parts.AddRange(newParts);

                return parts.OrderByDescending(a => a == "Operations").ToHashSet();
            });

            file.Replace(regexHooksFinder, m => "Operations." + m.Value);
        });


        simpleExports = "getConstructFromManyContextualItems|getEntityOperationsContextualItems|defaultContextualOperationClick|OperationMenuItem";
        regexHooksFinder = new Regex($@"(?<!\.)\b({simpleExports})\b", RegexOptions.Singleline);
        simpleExportArray = simpleExports.Split("|");

        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            if (file.FilePath.EndsWith("ContextualOperations.tsx"))
                return;

            file.ReplaceAndCombineTypeScriptImports(path => path.EndsWith("/ContextualOperations"), parts =>
            {
                var ask = parts.Where(a => a.StartsWith("* as ")).ToList();
                if (ask.Any())
                {
                    ask.ForEach(a => parts.Remove(a));
                    parts.Add("ContextualOperations");
                }
                if (parts.RemoveWhere(simpleExportArray.Contains) > 0)
                    parts.Add("ContextualOperations");

                return parts.OrderByDescending(a => a == "ContextualOperations").ToHashSet();
            });

            file.Replace(regexHooksFinder, m => "ContextualOperations." + m.Value);
        });


        simpleExports = "getEntityOperationButtons|withIcon|defaultOnClick|andClose|andNew|confirmInNecessary|defaultExecuteLite|defaultExecuteEntity";
        regexHooksFinder = new Regex($@"(?<!\.)\b({simpleExports})\b", RegexOptions.Singleline);
        simpleExportArray = simpleExports.Split("|");

        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            if (file.FilePath.EndsWith("EntityOperations.tsx"))
                return;

            file.ReplaceAndCombineTypeScriptImports(path => path.EndsWith("/EntityOperations"), parts =>
            {
                var ask = parts.Where(a => a.StartsWith("* as ")).ToList();
                if (ask.Any())
                {
                    ask.ForEach(a => parts.Remove(a));
                    parts.Add("EntityOperations");
                }
                if (parts.RemoveWhere(simpleExportArray.Contains) > 0)
                    parts.Add("EntityOperations");

                return parts.OrderByDescending(a => a == "EntityOperations").ToHashSet();
            });

            file.Replace(regexHooksFinder, m => "EntityOperations." + m.Value);
        });
    }
}
