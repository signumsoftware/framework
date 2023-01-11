using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades;
class Upgrade_20221219_JsonNodes : CodeUpgradeBase
{
    public override string Description => "change Newtonsoft.Json.Linq to System.Text.Json.Nodes";


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.React/views/home/index.cshtml", file =>
        {
            file.Replace("Newtonsoft.Json.Linq", "System.Text.Json.Nodes");
            file.InsertAfterFirstLine(a => a.Contains("@{"),
                "   string GetWebpackPath(string jsonContent, string moduleName)\n"
              + "   {\n"
              + "       var jsonObj = (JsonObject)JsonNode.Parse(jsonContent)!;\n"
              + "       var mainObj = (JsonObject)jsonObj[moduleName]!;\n"
              + "       ((JsonValue)mainObj[\"js\"]!).TryGetValue<string>(out var result);\n"
              + "       return result!;\n"
              + "   }\n"
              );
            file.Replace("var main = (string)JObject.Parse(json).Property(\"main\")!.Value[\"js\"]!;", "string main = GetWebpackPath(json, \"main\");");
            file.Replace("var vendor = (string)JObject.Parse(jsonDll).Property(\"vendor\")!.Value[\"js\"]!", "string vendor = GetWebpackPath(jsonDll, \"vendor\");");
        });
    }
}
