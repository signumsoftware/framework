using Signum.DynamicQuery;
using Signum.DynamicQuery.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Test.DynamicQueries;

public class DynamicQueryTest
{
    const SubTokensOptions cto =
           SubTokensOptions.CanToArray |
           SubTokensOptions.CanOperation |
           SubTokensOptions.CanSnippet |
           SubTokensOptions.CanManual |
           SubTokensOptions.CanAggregate |
           SubTokensOptions.CanTimeSeries |
           SubTokensOptions.CanElement |
           SubTokensOptions.CanNested;

    const SubTokensOptions fto = cto | SubTokensOptions.CanAnyAll;

    public DynamicQueryTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }

    [Fact]
    public void SimpleQueryImplicitEntity()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));

        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(ArtistEntity),
            Columns =  new List<Column>
            {
                new Column(QueryUtils.Parse("Id",qd, cto), null),
                new Column(QueryUtils.Parse("Name",qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>(),
            Orders = new List<Order>(),
            Pagination = new Pagination.Firsts(10),
        });

        Assert.True(rt.Columns.Length == 2);
        Assert.True(rt.Rows.Length > 0);
        Assert.True(rt.Rows.All(a => a.Entity is Lite<Entity>));
    }

    [Fact]
    public void EntityExplicitAndImplicitQuery()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));
        var entityToken = QueryUtils.Parse("Entity", qd, cto);
        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(ArtistEntity),
            Columns = new List<Column>
            {
                new Column(entityToken, null),
            },
            Filters = new List<DynamicQuery.Filter>(),
            Orders = new List<Order>(),
            Pagination = new Pagination.Firsts(10),
        });

        Assert.True(rt.Columns.Length == 0); //Not in the columns!
        Assert.True(rt.Rows.Length > 0);
        Assert.True(rt.Rows.All(a => a.Entity is Lite<Entity>));

        Assert.True(rt.Rows.All(r => r[rt.GetResultRow(entityToken)] is Lite<Entity>));
    }

    [Fact]
    public void GroupQueryExplicitEntityOnly()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));

        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(ArtistEntity),
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Entity",qd, cto), null),
                new Column(QueryUtils.Parse("Count",qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>(),
            Orders = new List<Order>(),
            Pagination = new Pagination.Firsts(10),
            GroupResults = true,
        });

        Assert.True(rt.Columns.Length == 2);
        Assert.True(rt.Rows.Length > 0);
        Assert.True(rt.Rows.All(a => a.TryEntity == null));
    }

    [Fact]
    public void NestedQuery()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));

      
        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(ArtistEntity),
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Id",qd, cto), null),
                new Column(QueryUtils.Parse("Name",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested.Name",qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>(),
            Orders = new List<Order>(),
            Pagination = new Pagination.Firsts(10),
        });

        Assert.True(rt.Columns.Length == 3);
        Assert.True(rt.Rows.Length > 0);
    }

    [Fact]
    public void NestedQueryWithoutColumns()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = typeof(ArtistEntity),
                Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Id",qd, cto), null),
                new Column(QueryUtils.Parse("Name",qd, cto), null),
            },
                Filters = new List<DynamicQuery.Filter>
            {
                new FilterCondition(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), FilterOperation.EqualTo, 1),
            },
                Orders = new List<Order>
            {
                new Order(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), OrderType.Ascending),
            },
                Pagination = new Pagination.All(), //In Memory
            });
        });

        Assert.Contains("Unable to filter", ex.Message);
        Assert.Contains("Unable to order", ex.Message);
    }

    [Fact]
    public void NestedQueryParallelFilters()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = typeof(ArtistEntity),
                Columns = new List<Column>
                {
                    new Column(QueryUtils.Parse("Id",qd, cto), null),
                    new Column(QueryUtils.Parse("Name",qd, cto), null),
                    new Column(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), null),
                    new Column(QueryUtils.Parse("Entity.Nominations.Nested.Id",qd, cto), null),
                },
                Filters = new List<DynamicQuery.Filter>
                {
                    new FilterGroup(FilterGroupOperation.Or, null,
                    new List<Filter>
                    {
                        new FilterCondition(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), FilterOperation.EqualTo, 1),
                        new FilterCondition(QueryUtils.Parse("Entity.Nominations.Nested.Id",qd, cto), FilterOperation.EqualTo, 1),
                    })
                },
                Orders = new List<Order>
                {
                },
                Pagination = new Pagination.All(), //In Memory
            });
        });

        Assert.Contains("independent nested tokens", ex.Message);
    }

    [Fact]
    public void NestedQueryWithOrderInMemory()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));


        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(ArtistEntity),
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Id",qd, cto), null),
                new Column(QueryUtils.Parse("Name",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested.Name",qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>(),
            Orders = new List<Order> 
            {
                new Order(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), OrderType.Ascending),
            },
            Pagination = new Pagination.All(), //In Memory
        });

        Assert.True(rt.Columns.Length == 3);
        Assert.True(rt.Rows.Length > 0);
    }

    [Fact]
    public void NestedQueryWithOrderByInDB()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));

        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(ArtistEntity),
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Id",qd, cto), null),
                new Column(QueryUtils.Parse("Name",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested.Name",qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>(),
            Orders = new List<Order>
            {
                new Order(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), OrderType.Ascending),
            },
            Pagination = new Pagination.Firsts(100), //In DB
        });

        Assert.True(rt.Columns.Length == 3);
        Assert.True(rt.Rows.Length > 0);
    }

    [Fact]
    public void NestedQueryWithFilter()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(ArtistEntity));

        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(ArtistEntity),
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Id",qd, cto), null),
                new Column(QueryUtils.Parse("Name",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested",qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Friends.Nested.Name",qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>
            {
                new FilterCondition(QueryUtils.Parse("Entity.Friends.Nested.Id",qd, cto), FilterOperation.EqualTo, 1),
            },
            Orders = new List<Order>
            {
            },
            Pagination = new Pagination.Firsts(100), //In DB
        });

        Assert.True(rt.Columns.Length == 3);
        Assert.True(rt.Rows.Length > 0);
    }


}
