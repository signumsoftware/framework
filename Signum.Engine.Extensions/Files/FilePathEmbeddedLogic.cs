using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Files;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Engine.Files
{
    public static class FilePathEmbeddedLogic
    { 
        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathEmbeddedLogic.Start(null!)));
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FileTypeLogic.Start(sb);

                FilePathEmbedded.CloneFunc = fp => new FilePathEmbedded(fp.FileType, fp.FileName, fp.GetByteArray());

                FilePathEmbedded.OnPreSaving += efp =>
                {
                    if(efp.BinaryFile != null) //First time
                    {
                        efp.SaveFile();
                    }
                };

                FilePathEmbedded.CalculatePrefixPair += CalculatePrefixPair;

                sb.Schema.SchemaCompleted += Schema_SchemaCompleted;
            }
        }

        private static void Schema_SchemaCompleted()
        {
            foreach (var table in Schema.Current.Tables.Values)
            {
                foreach (var field in table.FindFields(f => f.FieldType == typeof(FilePathEmbedded)))
                {
                    giAddBinding.GetInvoker(table.Type)(field.Route);
                }
            }
        }


        static GenericInvoker<Action<PropertyRoute>> giAddBinding = new GenericInvoker<Action<PropertyRoute>>(pr => AddBinding<Entity>(pr));
        static void AddBinding<T>(PropertyRoute route)
            where T : Entity
        {
            var entityEvents = Schema.Current.EntityEvents<T>();

            entityEvents.RegisterBinding<PrimaryKey>(route.Add(nameof(FilePathEmbedded.EntityId)),
                () => true,
                (t, rowId) => t.Id,
                (t, rowId, retriever) => t.Id);

            entityEvents.RegisterBinding<PrimaryKey?>(route.Add(nameof(FilePathEmbedded.MListRowId)),
                () => true,
                (t, rowId) => rowId,
                (t, rowId, retriever) => rowId);
        }

        static PrefixPair CalculatePrefixPair(this FilePathEmbedded efp)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
                return efp.FileType.GetAlgorithm().GetPrefixPair(efp);
        }

        public static byte[] GetByteArray(this FilePathEmbedded efp)
        {
            return efp.BinaryFile ?? efp.FileType.GetAlgorithm().ReadAllBytes(efp);
        }

        public static Stream OpenRead(this FilePathEmbedded efp)
        {
            return efp.FileType.GetAlgorithm().OpenRead(efp);
        }

        public static FilePathEmbedded SaveFile(this FilePathEmbedded efp)
        {
            var alg = efp.FileType.GetAlgorithm();
            alg.ValidateFile(efp);
            alg.SaveFile(efp);
            efp.BinaryFile = null!;
            return efp;
        }

        public static void DeleteFileOnCommit(this FilePathEmbedded efp)
        {
            Transaction.PostRealCommit += dic =>
            {
                efp.FileType.GetAlgorithm().DeleteFiles(new List<IFilePath> { efp });
            };
        }
    }
}
