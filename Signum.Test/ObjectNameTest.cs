using Xunit;
using Signum.Engine.Maps;
using System.Globalization;

namespace Signum.Test
{
    public class ObjectNameTest
    {
        bool isPostgree = Signum.Engine.Connector.Current.Schema.Settings.IsPostgres;

        [Fact]
        public void ParseDbo()
        {
            var simple = ObjectName.Parse("MyTable", isPostgree);
            Assert.Equal("MyTable", simple.Name);
            Assert.Equal("dbo", simple.Schema.ToString());
        }

        [Fact]
        public void ParseSchema()
        {
            var simple = ObjectName.Parse("MySchema.MyTable", isPostgree);
            Assert.Equal("MyTable", simple.Name);
            Assert.Equal("MySchema", simple.Schema.ToString());
        }

        [Fact]
        public void ParseNameEscaped()
        {
            var simple = ObjectName.Parse("MySchema.[Select]", isPostgree);
            Assert.Equal("Select", simple.Name);
            Assert.Equal("MySchema", simple.Schema.ToString());
            Assert.Equal("MySchema.[Select]", simple.ToString());
        }

        [Fact]
        public void ParseSchemaNameEscaped()
        {
            var simple = ObjectName.Parse("[Select].MyTable", isPostgree);
            Assert.Equal("MyTable", simple.Name);
            Assert.Equal("Select", simple.Schema.Name);
            Assert.Equal("[Select].MyTable", simple.ToString());
        }

        [Fact]
        public void ParseServerName()
        {
            var simple = ObjectName.Parse("[FROM].[SELECT].[WHERE].[TOP]", isPostgree);
            Assert.Equal("TOP", simple.Name);
            Assert.Equal("WHERE", simple.Schema.Name);
            Assert.Equal("SELECT", simple.Schema.Database!.Name);
            Assert.Equal("FROM", simple.Schema.Database!.Server!.Name);
        }


        [Fact]
        public void ParseServerNameSuperComplex()
        {
            var simple = ObjectName.Parse("[FROM].[SELECT].[WHERE].[TOP.DISTINCT]", isPostgree);
            Assert.Equal("TOP.DISTINCT", simple.Name);
            Assert.Equal("WHERE", simple.Schema.Name);
            Assert.Equal("SELECT", simple.Schema.Database!.Name);
            Assert.Equal("FROM", simple.Schema.Database!.Server!.Name);
        }
    }
}
