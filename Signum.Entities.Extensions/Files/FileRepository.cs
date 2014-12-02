using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Linq.Expressions;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FileTypeSymbol : Symbol
    {
        private FileTypeSymbol() { } 

        [MethodImpl(MethodImplOptions.NoInlining)]
        public FileTypeSymbol([CallerMemberName]string memberName = null) : 
            base(new StackFrame(1, false), memberName)
        {
        }
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class FileRepositoryEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        [NotNullable, SqlDbType(Size = 500)]
        string physicalPrefix;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 500)]
        public string PhysicalPrefix
        {
            get { return physicalPrefix; }
            set { Set(ref physicalPrefix, value); }
        }


        static Expression<Func<FileRepositoryEntity, string>> FullPhysicalPrefixExpression = fr => ConvertToAbsolute(fr.PhysicalPrefix);
        public string FullPhysicalPrefix
        {
            get { return ConvertToAbsolute(PhysicalPrefix); }
        }

        static string ConvertToAbsolute(string phsicalPrefix)
        {
            if (!Path.IsPathRooted(phsicalPrefix) && OverridenPhisicalCurrentDirectory != null)
                return Path.Combine(OverridenPhisicalCurrentDirectory, phsicalPrefix);

            return phsicalPrefix;
        }

        public static string OverridenPhisicalCurrentDirectory; 

        [SqlDbType(Size = 500)]
        string webPrefix;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 500)]
        public string WebPrefix
        {
            get { return webPrefix; }
            set { Set(ref webPrefix, value); }
        }

        bool active = true;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value); }
        }

        [NotNullable]
        MList<FileTypeSymbol> fileTypes = new MList<FileTypeSymbol>();
        public MList<FileTypeSymbol> FileTypes
        {
            get { return fileTypes; }
            set { Set(ref fileTypes, value); }
        }

        static readonly Expression<Func<FileRepositoryEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class FileRepositoryOperation
    {
        public static readonly ExecuteSymbol<FileRepositoryEntity> Save = OperationSymbol.Execute<FileRepositoryEntity>();
    }
}
