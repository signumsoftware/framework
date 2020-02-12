using Xunit;
using Signum.Engine.Maps;
using System.Globalization;
using Signum.Test.Environment;
using Signum.Engine;
using Signum.Utilities;

namespace Signum.Test
{
    public class ObjectNameTest
    {
        bool isPostgres;
        public ObjectNameTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
            this.isPostgres = Signum.Engine.Connector.Current.Schema.Settings.IsPostgres;
        }

        [Fact]
        public void ParseDbo()
        {
            var simple = ObjectName.Parse("MyTable", isPostgres);
            Assert.Equal("MyTable", simple.Name);
            Assert.Equal(isPostgres? "\"public\"" : "dbo", simple.Schema.ToString());
        }

        [Fact]
        public void ParseSchema()
        {
            if (isPostgres)
            {
                var simple = ObjectName.Parse("my_schema.my_table", isPostgres);
                Assert.Equal("my_table", simple.Name);
                Assert.Equal("my_schema", simple.Schema.ToString());
            }
            else
            {
                var simple = ObjectName.Parse("MySchema.MyTable", isPostgres);
                Assert.Equal("MyTable", simple.Name);
                Assert.Equal("MySchema", simple.Schema.ToString());
            }
        }

        [Fact]
        public void ParseNameEscaped()
        {
            if (isPostgres)
            {
                var simple = ObjectName.Parse("\"MySchema\".\"Select\"", isPostgres);
                Assert.Equal("Select", simple.Name);
                Assert.Equal("\"MySchema\"", simple.Schema.ToString());
                Assert.Equal("\"MySchema\".\"Select\"", simple.ToString());
            }
            else
            {
                var simple = ObjectName.Parse("MySchema.[Select]", isPostgres);
                Assert.Equal("Select", simple.Name);
                Assert.Equal("MySchema", simple.Schema.ToString());
                Assert.Equal("MySchema.[Select]", simple.ToString());
            }
        }

        [Fact]
        public void ParseSchemaNameEscaped()
        {
            if (isPostgres)
            {
                var simple = ObjectName.Parse("\"Select\".mytable", isPostgres);
                Assert.Equal("mytable", simple.Name);
                Assert.Equal("Select", simple.Schema.Name);
                Assert.Equal("\"Select\".mytable", simple.ToString());
            }
            else
            {
                var simple = ObjectName.Parse("[Select].MyTable", isPostgres);
                Assert.Equal("MyTable", simple.Name);
                Assert.Equal("Select", simple.Schema.Name);
                Assert.Equal("[Select].MyTable", simple.ToString());
            }
        }

        [Fact]
        public void ParseServerName()
        {
            var simple = ObjectName.Parse(isPostgres ?
                "\"FROM\".\"SELECT\".\"WHERE\".\"TOP\"" :
                "[FROM].[SELECT].[WHERE].[TOP]",
                isPostgres);
            Assert.Equal("TOP", simple.Name);
            Assert.Equal("WHERE", simple.Schema.Name);
            Assert.Equal("SELECT", simple.Schema.Database!.Name);
            Assert.Equal("FROM", simple.Schema.Database!.Server!.Name);
        }


        [Fact]
        public void ParseServerNameSuperComplex()
        {
            var simple = ObjectName.Parse(isPostgres ?
                "\"FROM\".\"SELECT\".\"WHERE\".\"TOP.DISTINCT\"" :
                "[FROM].[SELECT].[WHERE].[TOP.DISTINCT]",
                isPostgres);
            Assert.Equal("TOP.DISTINCT", simple.Name);
            Assert.Equal("WHERE", simple.Schema.Name);
            Assert.Equal("SELECT", simple.Schema.Database!.Name);
            Assert.Equal("FROM", simple.Schema.Database!.Server!.Name);
        }
    }
}
