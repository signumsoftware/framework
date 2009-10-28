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

            DynamicQueryManager dqm = new DynamicQueryManager(null);
            SchemaBuilder sb = new SchemaBuilder();
            sb.Settings.OverrideTypeAttributes<IEmployeeDN>(new ImplementedByAttribute());
            TypeLogic.Start(sb); 
            AuthLogic.Start(sb, dqm, "System");
            sb.Include<ResourceDN>();
            EntityGroupAuthLogic.Start(sb);

            EntityGroupLogic.Register<ResourceDN>(EntityGroups.UserResources, r => r.Propietary.Is(UserDN.Current));

            ConnectionScope.Default = new Connection(Settings.Default.SignumTest, sb.Schema);

            Administrator.TotalGeneration();

            sb.Schema.Initialize(InitLevel.Level0SyncEntities);

            RoleDN role = new RoleDN { Name = "Plain User" }.Save();

            UserDN lisa = new UserDN { UserName = "lisa", PasswordHash = Security.EncodePassword("lisa"), Role = role }.Save();
            UserDN bart = new UserDN { UserName = "bart", PasswordHash = Security.EncodePassword("bart"), Role = role }.Save();

            new ResourceDN { Name = "Saxo", Propietary = lisa }.Save();
            new ResourceDN { Name = "Skate", Propietary = bart }.Save();

            sb.Schema.Initialize();

            var list = EntityGroupAuthLogic.GetEntityGroupRules(role.ToLite());
            list[0].AllowedOut = false;
            EntityGroupAuthLogic.SetEntityGroupRules(list, role.ToLite());

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

                Assert.AreEqual(2, Database.Query<ResourceDN>().WhereGroupsAllowed().Count());
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

                using (EntityGroupAuthLogic.DisableAutoFilterQueries())
                {
                    Assert.AreEqual(2, Database.Query<ResourceDN>().Count());
                    Assert.AreEqual(2, Database.RetrieveAllLite<ResourceDN>().Count);
                    Assert.AreEqual(1, Database.Query<ResourceDN>().WhereGroupsAllowed().Count());
                }
            }
        }

        [TestMethod]
        public void EntityGrouRetrieve()
        {
            using (AuthLogic.UnsafeUser("bart"))
            {
                Assert2.Throws<UnauthorizedAccessException>(() => Database.Retrieve<ResourceDN>(1)); //Saxo
                using (EntityGroupAuthLogic.DisableAutoFilterQueries())
                {
                    Assert2.Throws<UnauthorizedAccessException>(() => Database.Query<ResourceDN>().Single(r => r.Name == "Saxo"));
                }
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
    }
}
