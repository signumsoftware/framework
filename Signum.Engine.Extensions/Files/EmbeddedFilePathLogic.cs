using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Files
{
    public static class EmbeddedFilePathLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FileTypeLogic.Start(sb, dqm);

                EmbeddedFilePathEntity.OnPreSaving += efp =>
                {
                    if(efp.BinaryFile != null) //First time
                    {
                        FileTypeLogic.SaveFile(efp);
                    }
                };

                EmbeddedFilePathEntity.OnPostRetrieved += efp =>
                {
                    efp.SetPrefixPair();
                };
            }
        }

        public static EmbeddedFilePathEntity SetPrefixPair(this EmbeddedFilePathEntity efp)
        {
            efp.prefixPair = FileTypeLogic.FileTypes.GetOrThrow(efp.FileType).GetPrefixPair(efp);

            return efp;
        }

        public static byte[] GetByteArray(this EmbeddedFilePathEntity efp)
        {
            return efp.BinaryFile ?? File.ReadAllBytes(efp.FullPhysicalPath);
        }

        public static EmbeddedFilePathEntity SaveFile(this EmbeddedFilePathEntity efp)
        {
            FileTypeLogic.SaveFile(efp);
            efp.BinaryFile = null;
            return efp;
        }
    }
}
