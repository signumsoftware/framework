using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Files;
using Signum.Entities;
using Signum.Engine.Basics;
using Signum.Utilities;
using System.IO;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.DynamicQuery;

namespace Signum.Engine.Files
{

    public static class FilePathLogic
    {
        static Dictionary<Enum, FileTypeAlgorithm> fileTypes = new Dictionary<Enum, FileTypeAlgorithm>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined<FilePathDN>())
            {
                sb.Include<FilePathDN>();

                EnumLogic<FileTypeDN>.Start(sb, () => fileTypes.Keys.ToHashSet());


                sb.Schema.Saving += new EntityEventHandler(Schema_Saving);
                sb.Schema.Retrieved += new EntityEventHandler(Schema_Retrieved);
                sb.Schema.Deleting += new TypeIdEventHandler(Schema_Deleting);

                dqm[typeof(FileRepositoryDN)] = (from r in Database.Query<FileRepositoryDN>()
                                                 select new
                                                 {
                                                     Entity = r.ToLazy(),
                                                     r.Id,
                                                     r.Name,
                                                     r.Active,
                                                     r.PhysicalPrefix,
                                                     r.WebPrefix
                                                 }).ToDynamic();

                dqm[typeof(FilePathDN)] = (from p in Database.Query<FilePathDN>()
                                                 select new
                                                 {
                                                     Entity = p.ToLazy(),
                                                     p.Id,
                                                     p.FileName,
                                                     FileType = p.FileType.ToLazy(),
                                                     p.FullPhysicalPath,
                                                     p.FullWebPath,
                                                     Repository = p.Repository.ToLazy()
                                                 }).ToDynamic();                
            }
        }

        static void Schema_Deleting(Schema sender, Type type, int id)
        {
            if (type == typeof(FilePathDN))
            {
                string fullPath = (from f in Database.Query<FilePathDN>()
                                   where f.Id == id
                                   select f.FullPhysicalPath).Single();

                Transaction.RealCommit += () => File.Delete(fullPath);
            }
            throw new NotImplementedException();
        }

        const long ERROR_DISK_FULL = 112L; // see winerror.h

        static void Schema_Saving(Schema sender, IdentifiableEntity ident)
        {
            FilePathDN fp = ident as FilePathDN;
            if (fp != null)
            {
                if (ident.IsNew)
                {
                    //asignar el typedn a partir del enum
                    fp.FileType = EnumLogic<FileTypeDN>.ToEntity(fp.FileTypeEnum);

                    FileTypeAlgorithm alg = fileTypes[fp.FileTypeEnum];
                    string sufix = alg.CalculateSufix(fp);
                    if (!sufix.HasText())
                        throw new ApplicationException(Resources.SufixNotSet);

                    do
                    {
                        fp.Repository = alg.GetRepository(fp);
                        if (fp.Repository == null)
                            throw new ApplicationException(Resources.RepositoryNotSet);
                        int i = 2;
                        fp.Sufix = sufix;
                        while (File.Exists(fp.FullPhysicalPath) && alg.RenameOnCollision)
                        {
                            fp.Sufix = alg.RenameAlgorithm(sufix, i);
                            i++;
                        }
                    }
                    while (!SaveFile(fp));
                }
            }
        }

        private static bool SaveFile(FilePathDN fp)
        {
            try
            {
                File.WriteAllBytes(fp.FullPhysicalPath, fp.BinaryFile);
            }
            catch (IOException ex)
            {
                int hresult = (int)ex.GetType().GetField("_HResult",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ex); // The error code is stored in just the lower 16 bits
                if ((hresult & 0xFFFF) == ERROR_DISK_FULL)
                {
                    fp.Repository.Active = false;
                    Database.Save(fp.Repository);
                    return false;
                }
                else
                    throw;
            }
            return true;
        }

        static void Schema_Retrieved(Schema sender, IdentifiableEntity ident)
        {
            if (ident is FilePathDN)
            {
                FilePathDN file = (FilePathDN)ident;
                file.BinaryFile = System.IO.File.ReadAllBytes(file.FullPhysicalPath);
            }
        }


        public static void Register(Enum fileTypeKey, FileTypeAlgorithm algorithm)
        {
            fileTypes.Add(fileTypeKey, algorithm);
        }
    }

    public sealed class FileTypeAlgorithm
    {
        public Func<FilePathDN, FileRepositoryDN> GetRepository { get; set; }
        public Func<FilePathDN, string> CalculateSufix { get; set; }
        private bool renameOnCollision = true;
        public bool RenameOnCollision
        {
            get
            { return renameOnCollision; }
            set
            { renameOnCollision = value; }
        }
        public Func<string, int, string> RenameAlgorithm { get; set; }

        public FileTypeAlgorithm()
        {
            RenameAlgorithm = DefaultRenameAlgorithm;
            GetRepository = DefaultGetRepository;
            CalculateSufix = DefaultCalculateSufix;
        }

        public static readonly Func<string, int, string> DefaultRenameAlgorithm = (sufix, num) =>
           Path.Combine(Path.GetDirectoryName(sufix),
              "{0}({1}){2}".Formato(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));

        public static readonly Func<FilePathDN, FileRepositoryDN> DefaultGetRepository = (FilePathDN fp) =>
            Database.Query<FileRepositoryDN>().Where(r => r.Active && r.FileTypes.Contains(fp.FileType)).FirstOrDefault();

        public static readonly Func<FilePathDN, string> DefaultCalculateSufix = (FilePathDN fp) => fp.FileName;
    }
}
