using Signum.Engine.Cache;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Isolation;
using Signum.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Dynamic
{
    public static class DynamicIsolationLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                DynamicLogic.GetCodeFiles += GetCodeFiles;
                DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
            }
        }

        public static void WriteDynamicStarter(StringBuilder sb, int indent)
        {
            // Nothing
        }

        public static List<CodeFile> GetCodeFiles()
        {
            var result = new List<CodeFile>();

            CacheLogic.GloballyDisabled = true;
            try
            {
                var entities = !Administrator.ExistsTable<DynamicTypeEntity>() ? new List<DynamicTypeEntity>() :
                    ExecutionMode.Global().Using(a => Database.Query<DynamicTypeEntity>().Where(a => a.BaseType == DynamicBaseType.Entity).ToList());
                var dlg = new DynamicIsolationLogicGenerator(DynamicCode.CodeGenEntitiesNamespace, entities, DynamicCode.Namespaces);
                var content = dlg.GetFileCode();
                result.Add(new CodeFile("CodeGenIsolationLogic.cs", content));
            }
            finally
            {
                CacheLogic.GloballyDisabled = false;
            }

            return result;
        }
    }

    public class DynamicIsolationLogicGenerator
    {
        public HashSet<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public List<DynamicTypeEntity> Entities { get; private set; }

        public DynamicIsolationLogicGenerator(string @namespace, List<DynamicTypeEntity> entities, HashSet<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.Entities = entities;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine($"namespace {this.Namespace}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    public static class CodeGenIsolationLogic");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public static void Start()");
            sb.AppendLine($"        {{");

            if (this.Entities != null && this.Entities.Count > 0)
                this.Entities.ForEach(m => sb.AppendLine($"IsolationLogic.Register<{m.TypeName}Entity>(IsolationStrategy.{m.TryMixin<DynamicIsolationMixin>()?.IsolationStrategy ?? IsolationStrategy.None});".Indent(12)));

            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }
    }

}
