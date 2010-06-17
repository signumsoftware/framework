using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Test.Properties;
using Signum.Services;
using Signum.Engine.Basics;
using Signum.Utilities;

namespace Signum.Test.Extensions
{
    [TestClass]
    public class EntityGroupsTest
    {
        static Connection connection;
        [TestInitialize]
        public void Initialize()
        {
            if (connection != null && connection == ConnectionScope.Current)
                return;

            DynamicQueryManager dqm = new DynamicQueryManager();
            SchemaBuilder sb = new SchemaBuilder();
            sb.Settings.OverrideTypeAttributes<IUserRelatedDN>(new ImplementedByAttribute());
            AuthLogic.Start(sb, dqm, "System", "Anonymous");
            sb.Include<ResourceDN>();
            EntityGroupAuthLogic.Start(sb);

            EntityGroupLogic.Register<ResourceDN>(EntityGroups.UserResources, r => r.Propietary.Is(UserDN.Current));
            EntityGroupLogic.Register<SubResourceDN>(EntityGroups.UserResources, sr => Database.Query<ResourceDN>().Where(r => r.Propietary.Is(UserDN.Current)).SelectMany(a => a.SubResources).Contains(sr));

            ConnectionScope.Default = new Connection(Settings.Default.SignumTest, sb.Schema, dqm);

            Administrator.TotalGeneration();

            sb.Schema.Initialize(InitLevel.Level0SyncEntities);

            RoleDN role = new RoleDN { Name = "Plain User" }.Save();

            UserDN lisa = new UserDN { UserName = "lisa", PasswordHash = Security.EncodePassword("lisa"), Role = role }.Save();
            UserDN bart = new UserDN { UserName = "bart", PasswordHash = Security.EncodePassword("bart"), Role = role }.Save();

            new ResourceDN
            {
                Name = "Saxo",
                Propietary = lisa,
                SubResources = new MList<SubResourceDN> 
                { 
                    new SubResourceDN{ Name = "Key"},
                    new SubResourceDN{ Name = "Mouthpiece"}
                }
            }.Save();

            new ResourceDN
            {
                Name = "Skate",
                Propietary = bart,
                SubResources = new MList<SubResourceDN>
                { 
                    new SubResourceDN{ Name = "Board"},
                    new SubResourceDN{ Name = "Wheel"}
                }
            }.Save();

            sb.Schema.Initialize();

            EntityGroupAuthLogic.SetEntityGroupAllowed(role.ToLite(), EntityGroups.UserResources, new EntityGroupAllowedDN(TypeAllowed.Create, TypeAllowed.None));

            Connection.CurrentLog = new DebugTextWriter();

            connection = (Connection)ConnectionScope.Default;
        }

        [TestMethod]
        public void EntityGroupsAuthDisable()
        {
            using (AuthLogic.Disable())
            {
                Assert.AreEqual(2, Database.Query<ResourceDN>().Count());
                Assert.AreEqual(0, Database.Query<ResourceDN>().Count(r => r.IsInGroup(EntityGroups.UserResources)));

                Assert.AreEqual(2, Database.RetrieveAll<ResourceDN>().Count);
                Assert.AreEqual(2, Database.RetrieveAllLite<ResourceDN>().Count);

                Assert.AreEqual(2, Database.Query<ResourceDN>().WhereAllowed().Count());
            }
        }

        [TestMethod]
        public void EntityGroupsQueryableAuthDisable()
        {
            using (AuthLogic.Disable())
            {
                Assert.AreEqual(4, Database.Query<SubResourceDN>().Count());
                Assert.AreEqual(0, Database.Query<SubResourceDN>().Count(r => r.IsInGroup(EntityGroups.UserResources)));

                Assert.AreEqual(4, Database.RetrieveAll<SubResourceDN>().Count);
                Assert.AreEqual(4, Database.RetrieveAllLite<SubResourceDN>().Count);

                Assert.AreEqual(4, Database.Query<SubResourceDN>().WhereAllowed().Count());
            }
        }

        [TestMethod]
        public void EntityGroupsBart()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                Assert.AreEqual(1, Database.Query<ResourceDN>().Count());
                Assert.AreEqual(1, Database.Query<ResourceDN>().Count(r => r.IsInGroup(EntityGroups.UserResources)));

                Assert.AreEqual(1, Database.RetrieveAll<ResourceDN>().Count);
                Assert.AreEqual(1, Database.RetrieveAllLite<ResourceDN>().Count);

                var resource = Database.RetrieveAll<ResourceDN>().Single();

                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert.AreEqual(2, Database.Query<ResourceDN>().Count());
                    Assert.AreEqual(2, Database.RetrieveAllLite<ResourceDN>().Count);
                    Assert.AreEqual(1, Database.Query<ResourceDN>().WhereAllowed().Count());
                }
            }
        }

        [TestMethod]
        public void EntityGroupsBartQueryable()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                Assert.AreEqual(2, Database.Query<SubResourceDN>().Count());
                Assert.AreEqual(2, Database.Query<SubResourceDN>().Count(r => r.IsInGroup(EntityGroups.UserResources)));

                Assert.AreEqual(2, Database.RetrieveAll<SubResourceDN>().Count);
                Assert.AreEqual(2, Database.RetrieveAllLite<SubResourceDN>().Count);

                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert.AreEqual(4, Database.Query<SubResourceDN>().Count());
                    Assert.AreEqual(4, Database.RetrieveAllLite<SubResourceDN>().Count);
                    Assert.AreEqual(2, Database.Query<SubResourceDN>().WhereAllowed().Count());
                }
            }
        }

        [TestMethod]
        public void EntityGroupRetrieve()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                Assert2.Throws<UnauthorizedAccessException>(() => Database.Retrieve<ResourceDN>(1)); //Saxo
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert2.Throws<UnauthorizedAccessException>(() => Database.Query<ResourceDN>().Single(r => r.Name == "Saxo"));
                }
            }
        }

        [TestMethod]
        public void EntityGroupRetrieveQueryable()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                Assert2.Throws<UnauthorizedAccessException>(() => Database.Retrieve<SubResourceDN>(1)); //Saxo Key
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert2.Throws<UnauthorizedAccessException>(() => Database.Query<SubResourceDN>().Single(r => r.Name == "Key"));
                }
            }
        }

        [TestMethod]
        public void EntityGroupUpdate()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                Assert.AreEqual(1, Database.Query<ResourceDN>().UnsafeUpdate(r => new ResourceDN { Name = r.Name + r.Name }));
                Assert.AreEqual(2, Database.Query<SubResourceDN>().UnsafeUpdate(r => new SubResourceDN { Name = r.Name + r.Name }));
            }
        }

        [TestMethod]
        public void EntityGroupDelete()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                Assert.AreEqual(1, Database.Query<ResourceDN>().UnsafeDelete());
                Assert.AreEqual(2, Database.Query<SubResourceDN>().UnsafeDelete());
            }
        }

        [TestMethod]
        public void EntityGroupJoin()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                int coutFast = Database.Query<ResourceDN>().SelectMany(r => r.SubResources).Count();
                int coutSlow = (from sr1 in Database.Query<ResourceDN>().SelectMany(r => r.SubResources)
                               join sr2 in Database.Query<SubResourceDN>() on sr1 equals sr2
                               select sr1).Count();
                Assert.AreEqual(coutFast, coutSlow);
            }
        }
    }

    public enum EntityGroups
    {
        UserResources
    }

    [Serializable]
	public class ResourceDN: Entity
    {
        UserDN propietary;
        public UserDN Propietary
        {
            get { return propietary; }
            set { Set(ref propietary, value, () => Propietary); }
        }

        [NotNullable, SqlDbType( Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls=false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value, () => Name); }
        }

        MList<SubResourceDN> subResources;
        public MList<SubResourceDN> SubResources
        {
            get { return subResources; }
            set { Set(ref subResources, value, () => SubResources); }
        }
    }

    [Serializable]
    public class SubResourceDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value, () => Name); }
        }
    }
}
