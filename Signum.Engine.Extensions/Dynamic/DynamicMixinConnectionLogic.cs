using Signum.Engine.Cache;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Dynamic
{
    public static class DynamicMixinConnectionLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicMixinConnectionEntity>()
                    .WithUniqueIndex(e => new { e.EntityType, e.MixinName })
                    .WithSave(DynamicMixinConnectionOperation.Save)
                    .WithDelete(DynamicMixinConnectionOperation.Delete)
                    .WithQuery(() => e => new {
                        Entity = e,
                        e.Id,
                        e.EntityType,
                        e.MixinName,
                    });

                DynamicLogic.GetCodeFiles += GetCodeFiles;
                DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
                DynamicCode.RegisteredDynamicTypes.Add(typeof(DynamicMixinConnectionEntity));
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicMixinConnectionEntity>().Where(dm => dm.EntityType.Is(type)));
            }
        }

        public static void WriteDynamicStarter(StringBuilder sb, int indent) {
            // Nothing
        }

        public static List<CodeFile> GetCodeFiles()
        {
            var result = new List<CodeFile>();
             
            CacheLogic.GloballyDisabled = true;
            try
            {
                var mixins = !Administrator.ExistsTable<DynamicMixinConnectionEntity>()?  new List<DynamicMixinConnectionEntity>() : ExecutionMode.Global().Using(a => Database.Query<DynamicMixinConnectionEntity>().ToList());
                var dlg = new DynamicMixinConnectionLogicGenerator(DynamicCode.CodeGenEntitiesNamespace, mixins, DynamicCode.Namespaces);
                var content = dlg.GetFileCode();
                result.Add(new CodeFile
                {
                    FileName = "CodeGenMixinLogic.cs",
                    FileContent = content
                });
            }
            finally
            {
                CacheLogic.GloballyDisabled = false;
            }

            return result;
        }
    }

    public class DynamicMixinConnectionLogicGenerator
    {
        public HashSet<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public List<DynamicMixinConnectionEntity> Mixins { get; private set; }

        public Dictionary<string, string> AlreadyTranslated { get; set; }

        public DynamicMixinConnectionLogicGenerator(string @namespace, List<DynamicMixinConnectionEntity> mixins, HashSet<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.Mixins = mixins;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine($"namespace {this.Namespace}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    public static class CodeGenMixinLogic");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public static void Start()");
            sb.AppendLine($"        {{");

            if (this.Mixins != null && this.Mixins.Count > 0)
                this.Mixins.ForEach(m => sb.AppendLine($"MixinDeclarations.Register<{m.EntityType}Entity, {m.MixinName}Mixin>();".Indent(12)));

            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }
    }

}
