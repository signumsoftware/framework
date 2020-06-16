using Signum.Engine.Cache;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Dynamic
{
    public static class DynamicApiLogic
    {
        public static bool IsStarted = false;
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicApiEntity>()
                    .WithSave(DynamicApiOperation.Save)
                    .WithDelete(DynamicApiOperation.Delete)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        Script = e.Eval.Script.Etc(50),
                    });

                new Graph<DynamicApiEntity>.ConstructFrom<DynamicApiEntity>(DynamicApiOperation.Clone)
                {
                    Construct = (e, _) =>
                    {
                        return new DynamicApiEntity
                        {
                            Name = e.Name + "_2",
                            Eval = new DynamicApiEval() { Script = e.Eval.Script },
                        };
                    }
                }.Register();

                DynamicCode.RegisteredDynamicTypes.Add(typeof(DynamicApiEntity));
                IsStarted = true;
            }
        }

        public static List<CodeFile> GetCodeFiles()
        {
            CacheLogic.GloballyDisabled = true;
            try
            {
                using (ExecutionMode.Global())
                {
                    var result = new List<CodeFile>();

                    var controllers = !Administrator.ExistsTable<DynamicApiEntity>() ?
                        new List<DynamicApiEntity>() :
                        Database.Query<DynamicApiEntity>()
                            .Where(a => a.Mixin<DisabledMixin>().IsDisabled == false)
                            .ToList();

                    var dtcg = new DynamicApiCodeGenerator(DynamicCode.CodeGenControllerNamespace, controllers, DynamicCode.GetNamespaces().ToHashSet());

                    var content = dtcg.GetFileCode();
                    result.Add(new CodeFile("CodeGenController.cs", content));
                    return result;
                }
            }
            finally
            {
                CacheLogic.GloballyDisabled = false;
            }
        }
    }

    public class DynamicApiCodeGenerator
    {
        public HashSet<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public List<DynamicApiEntity> DynamicApis { get; private set; }

        public DynamicApiCodeGenerator(string @namespace, List<DynamicApiEntity> dynamicApis, HashSet<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.DynamicApis = dynamicApis;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + this.Namespace);
            sb.AppendLine("{");
            sb.Append(GetControllerClassCode().Indent(4));
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string GetControllerClassCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"public class CodeGenController: ControllerBase");
            sb.AppendLine("{");

            foreach (var c in this.DynamicApis)
            {
                sb.AppendLine();
                sb.AppendLine(c.Eval.Script.Indent(4));
                sb.AppendLine();
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
