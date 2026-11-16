using Signum.DynamicQuery;

namespace Signum.Test.LinqProvider;

public class SetOperatorsTest
{
    public SetOperatorsTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = SqlDumpTextWriter.Enabled ? new SqlDumpTextWriter() : new DebugTextWriter();
    }

    #region Scalars
    [Fact]
    public void ConcatScalar()
    {
        var names = Database.Query<ArtistEntity>().Select(a => a.Name)
            .Concat(Database.Query<BandEntity>().Select(b => b.Name))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count() + Database.Query<BandEntity>().Count(), names.Count);
    }

    [Fact]
    public void UnionScalar()
    {
        var years = Database.Query<AlbumEntity>().Select(a => a.Year)
            .Union(Database.Query<AlbumEntity>().Select(a => a.Year))
            .ToList();

        var expected = Database.Query<AlbumEntity>().Select(a => a.Year).ToList().Distinct().Order().ToList();
        Assert.Equal(expected, years.Order().ToList());
    }

    [Fact]
    public void IntersectExceptScalar()
    {
        var allYears = Database.Query<AlbumEntity>().Select(a => a.Year).ToList();
        var midYear = allYears.Order().ElementAt(allYears.Count / 2);

        var intersect = Database.Query<AlbumEntity>().Where(a => a.Year <= midYear).Select(a => a.Year)
            .Intersect(Database.Query<AlbumEntity>().Where(a => a.Year >= midYear).Select(a => a.Year))
            .ToList();

        Assert.Equal(new[] { midYear }, intersect);

        var except = Database.Query<AlbumEntity>().Select(a => a.Year)
            .Except(Database.Query<AlbumEntity>().Where(a => a.Year >= midYear).Select(a => a.Year))
            .ToList();

        Assert.Equal(allYears.Where(y => y < midYear).Distinct().Order(), except.Order());
    }

    [Fact]
    public void ConcatEnum()
    {
        var sexes = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.Sex)
            .Concat(Database.Query<ArtistEntity>().Where(a => !a.Dead).Select(a => a.Sex))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Select(a => a.Sex).ToList().Order(), sexes.Order());
    }

    [Fact]
    public void ConcatNullableEnum()
    {
        var status = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.Status)
            .Concat(Database.Query<ArtistEntity>().Where(a => !a.Dead).Select(a => (Status?)null))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), status.Count);
    }
    #endregion

    #region Entities
    [Fact]
    public void ConcatEntities()
    {
        var artists = Database.Query<ArtistEntity>().Where(a => a.Dead)
            .Concat(Database.Query<ArtistEntity>().Where(a => !a.Dead))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), artists.Count);
        Assert.All(artists, a => Assert.NotNull(a.Name)); //Retrieved after the set operator
    }

    [Fact]
    public void UnionEntitiesComparesById()
    {
        var artists = Database.Query<ArtistEntity>()
            .Union(Database.Query<ArtistEntity>())
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), artists.Count);
    }

    [Fact]
    public void IntersectExceptEntities()
    {
        var dead = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.Id).ToList();

        var intersect = Database.Query<ArtistEntity>()
            .Intersect(Database.Query<ArtistEntity>().Where(a => a.Dead))
            .ToList();

        Assert.Equal(dead.Order(), intersect.Select(a => a.Id).Order());

        var except = Database.Query<ArtistEntity>()
            .Except(Database.Query<ArtistEntity>().Where(a => a.Dead))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count() - dead.Count, except.Count);
        Assert.DoesNotContain(except, a => a.Dead);
    }

    [Fact]
    public void ConcatEntitiesImplementedBy()
    {
        var authors = Database.Query<ArtistEntity>().Select(a => (IAuthorEntity)a)
            .Concat(Database.Query<BandEntity>().Select(b => (IAuthorEntity)b))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), authors.OfType<ArtistEntity>().Count());
        Assert.Equal(Database.Query<BandEntity>().Count(), authors.OfType<BandEntity>().Count());
        Assert.All(authors, a => Assert.NotNull(a.Name));
    }

    [Fact]
    public void UnionEntitiesImplementedByOverlap()
    {
        var authors = Database.Query<AlbumEntity>().Select(a => a.Author)
            .Union(Database.Query<ArtistEntity>().Select(a => (IAuthorEntity)a))
            .ToList();

        var expected = Database.Query<AlbumEntity>().Select(a => a.Author.ToLite()).ToList()
            .Concat(Database.Query<ArtistEntity>().Select(a => a.ToLite<IAuthorEntity>()).ToList())
            .Distinct().Count();

        Assert.Equal(expected, authors.Count);
    }

    [Fact]
    public void ConcatEntitiesImplementedByAll()
    {
        var awards = Database.Query<ArtistEntity>().Select(a => a.LastAward)
            .Concat(Database.Query<BandEntity>().Select(b => b.LastAward))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count() + Database.Query<BandEntity>().Count(), awards.Count);
        Assert.Equal(
            Database.Query<ArtistEntity>().Count(a => a.LastAward != null) + Database.Query<BandEntity>().Count(b => b.LastAward != null),
            awards.NotNull().Count());
    }

    [Fact]
    public void ConcatEntityWithNull()
    {
        var list = Database.Query<ArtistEntity>().Select(a => new { Artist = (ArtistEntity?)a, Band = (BandEntity?)null })
            .Concat(Database.Query<BandEntity>().Select(b => new { Artist = (ArtistEntity?)null, Band = (BandEntity?)b }))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), list.Count(a => a.Artist != null && a.Band == null));
        Assert.Equal(Database.Query<BandEntity>().Count(), list.Count(a => a.Band != null && a.Artist == null));
    }
    #endregion

    #region Lites
    [Fact]
    public void ConcatLites()
    {
        var lites = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.ToLite())
            .Concat(Database.Query<ArtistEntity>().Where(a => !a.Dead).Select(a => a.ToLite()))
            .ToList();

        var expected = Database.Query<ArtistEntity>().Select(a => a.ToLite()).ToList();

        Assert.Equal(expected.Count, lites.Count);
        Assert.All(lites, l => Assert.Equal(expected.Single(e => e.Is(l)).ToString(), l.ToString())); //model recomposed
    }

    [Fact]
    public void UnionLitesImplementedBy()
    {
        var lites = Database.Query<AwardNominationEntity>().Select(a => a.Author)
            .Union(Database.Query<BandEntity>().Select(b => b.ToLite<IAuthorEntity>()))
            .ToList();

        var expected = Database.Query<AwardNominationEntity>().Select(a => a.Author).ToList()
            .Concat(Database.Query<BandEntity>().Select(b => b.ToLite<IAuthorEntity>()).ToList())
            .Distinct().ToList();

        Assert.Equal(expected.Count, lites.Count);
        Assert.All(lites, l => Assert.NotNull(l.ToString()));
    }

    [Fact]
    public void ConcatLitesCustomModelTypes()
    {
        var awards = Database.Query<AwardNominationEntity>().Where(a => a.Year < 2000).Select(a => a.Award)
            .Concat(Database.Query<AwardNominationEntity>().Where(a => !(a.Year < 2000)).Select(a => a.Award))
            .ToList();

        var expected = Database.Query<AwardNominationEntity>().Select(a => a.Award).ToList();

        Assert.Equal(expected.Count, awards.Count);
        Assert.Equal(
            expected.NotNull().Select(a => a.ModelType).GroupCount(),
            awards.NotNull().Select(a => a.ModelType).GroupCount());
    }

    [Fact]
    public void ConcatLitesCustomModelMismatch()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Database.Query<ArtistEntity>().Select(a => a.ToLite())
            .Concat(Database.Query<ArtistEntity>().Select(a => a.ToLite(a.Name + "!")))
            .ToList());
    }

    [Fact]
    public void ConcatLitesCustomModelExpression()
    {
        var lites = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.ToLite(a.Name + "!"))
            .Concat(Database.Query<ArtistEntity>().Where(a => !a.Dead).Select(a => a.ToLite(a.Name + "?")))
            .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), lites.Count);
        Assert.All(lites, l => Assert.True(l.ToString()!.EndsWith("!") || l.ToString()!.EndsWith("?")));
    }
    #endregion

    #region Anonymous / Embedded / Type
    [Fact]
    public void ConcatAnonymous()
    {
        var list = (from a in Database.Query<ArtistEntity>()
                    select new { Entity = (IAuthorEntity)a, a.Id, Type = "Artist", a.Name, Lonely = a.Lonely(), a.LastAward })
                   .Concat(
                    from b in Database.Query<BandEntity>()
                    select new { Entity = (IAuthorEntity)b, b.Id, Type = "Band", b.Name, Lonely = b.Lonely(), b.LastAward })
                   .ToList();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), list.Count(a => a.Type == "Artist" && a.Entity is ArtistEntity));
        Assert.Equal(Database.Query<BandEntity>().Count(), list.Count(a => a.Type == "Band" && a.Entity is BandEntity));
        Assert.All(list, a => Assert.Equal(a.Name, a.Entity.Name));
    }

    [Fact]
    public void UnionAnonymousKeepsAllColumns()
    {
        //Only Sex is used afterwards, but the UNION has to compare Name too
        var count = Database.Query<ArtistEntity>().Select(a => new { a.Name, a.Sex })
            .Union(Database.Query<ArtistEntity>().Select(a => new { a.Name, a.Sex }))
            .Select(a => a.Sex)
            .Count();

        Assert.Equal(Database.Query<ArtistEntity>().Count(), count);
    }

    [Fact]
    public void ConcatEmbedded()
    {
        var songs = Database.Query<AlbumEntity>().Select(a => a.BonusTrack)
            .Concat(Database.Query<AlbumEntity>().Select(a => (SongEmbedded?)null))
            .ToList();

        Assert.Equal(Database.Query<AlbumEntity>().Count() * 2, songs.Count);
        Assert.Equal(Database.Query<AlbumEntity>().Count(a => a.BonusTrack != null), songs.NotNull().Count());
    }

    [Fact]
    public void UnionEmbeddedValueSemantics()
    {
        var songs = Database.Query<AlbumEntity>().Where(a => a.BonusTrack != null).Select(a => a.BonusTrack!)
            .Union(Database.Query<AlbumEntity>().Where(a => a.BonusTrack != null).Select(a => a.BonusTrack!))
            .ToList();

        var expected = Database.Query<AlbumEntity>().Where(a => a.BonusTrack != null)
            .Select(a => new { a.BonusTrack!.Name, a.BonusTrack.Duration, a.BonusTrack.Seconds, a.BonusTrack.Index })
            .ToList().Distinct().Count();

        Assert.Equal(expected, songs.Count);
    }

    [Fact]
    public void ConcatType()
    {
        var types = Database.Query<AlbumEntity>().Select(a => a.Author.GetType())
            .Concat(Database.Query<ArtistEntity>().Select(a => a.GetType()))
            .Distinct()
            .ToList();

        Assert.Equal(new[] { typeof(ArtistEntity), typeof(BandEntity) }.Order(new TypeNameComparer()), types.Order(new TypeNameComparer()));
    }

    class TypeNameComparer : IComparer<Type>
    {
        public int Compare(Type? x, Type? y) => string.Compare(x?.Name, y?.Name, StringComparison.Ordinal);
    }
    #endregion

    #region Composition
    [Fact]
    public void ConcatOrderByTake()
    {
        var names = Database.Query<ArtistEntity>().Select(a => a.Name)
            .Concat(Database.Query<BandEntity>().Select(b => b.Name))
            .OrderBy(n => n)
            .Take(3)
            .ToList();

        Assert.Equal(3, names.Count);
        Assert.Equal(names.Order(StringComparer.CurrentCultureIgnoreCase), names); //ordered in SQL
    }

    [Fact]
    public void ConcatPartsWithOrderByTake()
    {
        var names = Database.Query<ArtistEntity>().OrderBy(a => a.Name).Select(a => a.Name).Take(2)
            .Concat(Database.Query<BandEntity>().OrderByDescending(b => b.Name).Select(b => b.Name).Take(1))
            .ToList();

        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void ChainedSetOperators()
    {
        var count = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.ToLite())
            .Concat(Database.Query<ArtistEntity>().Where(a => !a.Dead).Select(a => a.ToLite()))
            .Union(Database.Query<ArtistEntity>().Select(a => a.ToLite()))
            .Except(Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.ToLite()))
            .Count();

        Assert.Equal(Database.Query<ArtistEntity>().Count(a => !a.Dead), count);
    }

    [Fact]
    public void SetOperatorInSubquery()
    {
        var albums = Database.Query<AlbumEntity>()
            .Where(al => Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => (IAuthorEntity)a)
                .Concat(Database.Query<BandEntity>().Select(b => (IAuthorEntity)b))
                .Contains(al.Author))
            .Select(a => a.ToLite())
            .ToList();

        var expected = Database.Query<AlbumEntity>()
            .Where(al => al.Author is BandEntity || ((ArtistEntity)al.Author).Dead)
            .Count();

        Assert.Equal(expected, albums.Count);
    }

    [Fact]
    public void ConcatWhereAfter()
    {
        var list = Database.Query<ArtistEntity>().Select(a => (IAuthorEntity)a)
            .Concat(Database.Query<BandEntity>().Select(b => (IAuthorEntity)b))
            .Where(a => a.Name.StartsWith("S"))
            .Select(a => new { a.Name, Lite = a.ToLite() })
            .ToList();

        var expected = Database.Query<ArtistEntity>().Count(a => a.Name.StartsWith("S")) + Database.Query<BandEntity>().Count(a => a.Name.StartsWith("S"));

        Assert.Equal(expected, list.Count);
    }
    #endregion

    #region Not supported
    [Fact]
    public void InMemoryNotSupported()
    {
        var names = new List<string> { "A", "B" };

        Assert.Throws<NotSupportedException>(() => Database.Query<ArtistEntity>().Select(a => a.Name).Concat(names).ToList());
    }

    [Fact]
    public void ComparerNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => Database.Query<ArtistEntity>().Select(a => a.Name)
            .Union(Database.Query<BandEntity>().Select(b => b.Name), StringComparer.OrdinalIgnoreCase).ToList());
    }

    [Fact]
    public void ByOperatorsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => Database.Query<ArtistEntity>()
            .UnionBy(Database.Query<ArtistEntity>(), a => a.Name).ToList());

        Assert.Throws<NotSupportedException>(() => Database.Query<ArtistEntity>()
            .DistinctBy(a => a.Name).ToList());
    }

    [Fact]
    public void CollectionsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => Database.Query<BandEntity>().Select(b => new { b.Name, b.Members })
            .Concat(Database.Query<BandEntity>().Select(b => new { b.Name, b.Members })).ToList());
    }
    #endregion

    #region DynamicQuery
    [Fact]
    public void AuthorsConcatQuery()
    {
        const SubTokensOptions cto = SubTokensOptions.CanElement | SubTokensOptions.CanAggregate;

        ResultTable Execute(object queryName)
        {
            var qd = QueryLogic.Queries.QueryDescription(queryName);

            return QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = queryName,
                Columns = new List<Column>
                {
                    new Column(QueryUtils.Parse("Name", qd, cto), null),
                    new Column(QueryUtils.Parse("Type", qd, cto), null),
                    new Column(QueryUtils.Parse("LastAward", qd, cto), null),
                },
                Filters = new List<DynamicQuery.Filter>(),
                Orders = new List<Order> { new Order(QueryUtils.Parse("Name", qd, cto), OrderType.Ascending) },
                Pagination = new Pagination.All(),
            });
        }

        var manual = Execute(AuthorQuery.AuthorsManual);
        var concat = Execute(AuthorQuery.AuthorsConcat);

        Assert.Equal(manual.Rows.Length, concat.Rows.Length);
        Assert.Equal(manual.Rows.Select(r => r.Entity).ToHashSet(), concat.Rows.Select(r => r.Entity).ToHashSet());
    }
    #endregion
}
