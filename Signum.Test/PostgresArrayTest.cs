using Signum.Engine.Sync.Postgres;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Sdk;

namespace Signum.Test;

public class PostgresArrayTest
{
    public PostgresArrayTest()
    {
        MusicStarter.StartAndLoad();

        Connector.CurrentLogger = new DebugTextWriter();

        if (!(Connector.Current is PostgreSqlConnector con))
            throw SkipException.ForSkip("Skipping tests because not Postgres.");
    }

    [Fact]
    public void UnnestArray()
    {
        (from a in Database.View<PgIndex>()
         from b in a.indclass
         let opClass = Database.View<PgOpClass>().Where(oc => oc.oid == b).FirstOrDefault()!.opcname
         select new
         {
             opClass
         }).ToList();
    }

    [Fact]
    public void CheckIndex()
    {
        (from a in Database.View<PgIndex>()
         let opClass = Database.View<PgOpClass>().Where(oc => oc.oid == a.indclass[0]).FirstOrDefault()!.opcname
         select new
         {
             opClass
         }).ToList();
    }
}
