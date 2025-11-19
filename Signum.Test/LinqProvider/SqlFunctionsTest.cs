using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;
using Signum.Engine.Maps;
using Signum.Utilities;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace Signum.Test.LinqProvider;

/// <summary>
/// Summary description for LinqProvider
/// </summary>
public class SqlFunctionsTest
{
    public SqlFunctionsTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }

    [Fact]
    public void StringFunctions()
    {
        var artists = Database.Query<ArtistEntity>();
        Assert.True(artists.Any(a => a.Name.IndexOf('M') == 0));
        Assert.True(artists.Any(a => a.Name.IndexOf("Mi") == 0));
        Assert.True(artists.Any(a => a.Name.Contains("Jackson")));
        Assert.True(artists.Any(a => a.Name.StartsWith("Billy")));
        Assert.True(artists.Any(a => a.Name.EndsWith("Corgan")));
        Assert.True(artists.Any(a => a.Name.Like("%Michael%")));
        Assert.True(artists.Count(a => a.Name.EndsWith("Orri Páll Dýrason")) == 1);
        Assert.True(artists.Count(a => a.Name.StartsWith("Orri Páll Dýrason")) == 1);

        Dump((ArtistEntity a) => a.Name.Length);
        Dump((ArtistEntity a) => a.Name.ToLower());
        Dump((ArtistEntity a) => a.Name.ToUpper());
        Dump((ArtistEntity a) => a.Name.TrimStart());
        Dump((ArtistEntity a) => a.Name.TrimEnd());
        Dump((ArtistEntity a) => a.Name.Substring(2).InSql());
        Dump((ArtistEntity a) => a.Name.Substring(2, 2).InSql());

        Dump((ArtistEntity a) => a.Name.Start(2).InSql());
        Dump((ArtistEntity a) => a.Name.End(2).InSql());
        Dump((ArtistEntity a) => a.Name.Reverse().InSql());
        Dump((ArtistEntity a) => a.Name.Replicate(2).InSql());
    }

    [Fact]
    public void StringFunctionsPolymorphicUnion()
    {
        Assert.True(Database.Query<AlbumEntity>().Any(a => a.Author.CombineUnion().Name.Contains("Jackson")));
    }

    [Fact]
    public void StringFunctionsPolymorphicSwitch()
    {
        Assert.True(Database.Query<AlbumEntity>().Any(a => a.Author.CombineCase().Name.Contains("Jackson")));
    }

    [Fact]
    public void CoalesceFirstOrDefault()
    {
        var list = Database.Query<BandEntity>()
           .Select(b => b.Members.FirstOrDefault(a => a.Sex == Sex.Female) ?? b.Members.FirstOrDefault(a => a.Sex == Sex.Male)!)
           .Select(a => a.ToLite()).ToList();
    }

    [Fact]
    public void StringContainsUnion()
    {
        var list = Database.Query<AlbumEntity>().Where(a => !a.Author.CombineUnion().ToString()!.Contains("Hola")).ToList();
    }

    [Fact]
    public void StringContainsSwitch()
    {
        var list = Database.Query<AlbumEntity>().Where(a => !a.Author.CombineCase().ToString()!.Contains("Hola")).ToList();
    }

    [Fact]
    public void DateParameters()
    {
        Database.Query<NoteWithDateEntity>().Where(a => a.CreationDate == DateTime.Today.ToDateOnly()).ToList();
    }

    [Fact]
    public void DateTimeFunctions()
    {
        Dump((NoteWithDateEntity n) => n.CreationTime.Year);
        Dump((NoteWithDateEntity n) => n.CreationTime.Quarter());
        Dump((NoteWithDateEntity n) => n.CreationTime.Month);
        Dump((NoteWithDateEntity n) => n.CreationTime.Day);
        Dump((NoteWithDateEntity n) => n.CreationTime.DayOfYear);
        Dump((NoteWithDateEntity n) => n.CreationTime.Hour);
        Dump((NoteWithDateEntity n) => n.CreationTime.Minute);
        Dump((NoteWithDateEntity n) => n.CreationTime.Second);
        Dump((NoteWithDateEntity n) => n.CreationTime.Millisecond);


        Dump((NoteWithDateEntity n) => n.CreationDate.Year);
        Dump((NoteWithDateEntity n) => n.CreationDate.Quarter());
        Dump((NoteWithDateEntity n) => n.CreationDate.Month);
        Dump((NoteWithDateEntity n) => n.CreationDate.Day);
        Dump((NoteWithDateEntity n) => n.CreationDate.DayOfYear);
    }

    [Fact]
    public void DateTimeFunctionsStart()
    {
        Dump((NoteWithDateEntity n) => n.CreationTime.YearStart());
        Dump((NoteWithDateEntity n) => n.CreationTime.QuarterStart());
        Dump((NoteWithDateEntity n) => n.CreationTime.MonthStart());
        Dump((NoteWithDateEntity n) => n.CreationTime.WeekStart());
        Dump((NoteWithDateEntity n) => n.CreationTime.Date);
        Dump((NoteWithDateEntity n) => n.CreationTime.TruncHours());
        Dump((NoteWithDateEntity n) => n.CreationTime.TruncMinutes());
        Dump((NoteWithDateEntity n) => n.CreationTime.TruncSeconds());

        Dump((NoteWithDateEntity n) => n.CreationDate.YearStart());
        Dump((NoteWithDateEntity n) => n.CreationDate.QuarterStart());
        Dump((NoteWithDateEntity n) => n.CreationDate.MonthStart());
        Dump((NoteWithDateEntity n) => n.CreationDate.WeekStart());
    }

    [Fact]
    public void DateTimeFunctionsConvert()
    {
        Dump((NoteWithDateEntity n) => n.CreationTime.ToDateOnly());
        Dump((NoteWithDateEntity n) => DateOnly.FromDateTime(n.CreationTime));

        Dump((NoteWithDateEntity n) => n.CreationDate.ToDateTime());
        Dump((NoteWithDateEntity n) => n.CreationDate.ToDateTime(TimeOnly.MaxValue));
    }

    [Fact]
    public void DayOfWeekWhere()
    {
        var memCount = Database.Query<NoteWithDateEntity>().ToList().Where(a => a.CreationTime.DayOfWeek == a.CreationTime.DayOfWeek).Count();
        var dbCount = Database.Query<NoteWithDateEntity>().Where(a => a.CreationTime.DayOfWeek == a.CreationTime.DayOfWeek).Count();
        Assert.Equal(memCount, dbCount);

        var memCount2 = Database.Query<NoteWithDateEntity>().ToList().Where(a => a.CreationDate.DayOfWeek == a.CreationDate.DayOfWeek).Count();
        var dbCount2 = Database.Query<NoteWithDateEntity>().Where(a => a.CreationDate.DayOfWeek == a.CreationDate.DayOfWeek).Count();
        Assert.Equal(memCount2, dbCount2);
    }

    [Fact]
    public void DayOfWeekWhereConstant()
    {
        var memCount = Database.Query<NoteWithDateEntity>().ToList().Where(a => a.CreationTime.DayOfWeek == DayOfWeek.Sunday).Count();
        var dbCount = Database.Query<NoteWithDateEntity>().Where(a => a.CreationTime.DayOfWeek == DayOfWeek.Sunday).Count();
        Assert.Equal(memCount, dbCount);

        var memCount2 = Database.Query<NoteWithDateEntity>().ToList().Where(a => a.CreationDate.DayOfWeek == DayOfWeek.Sunday).Count();
        var dbCount2 = Database.Query<NoteWithDateEntity>().Where(a => a.CreationDate.DayOfWeek == DayOfWeek.Sunday).Count();
        Assert.Equal(memCount2, dbCount2);
    }


    [Fact]
    public void DayOfWeekSelectNullable()
    {
        var list = Database.Query<ArtistEntity>()
            .Select(a => (DayOfWeek?)Database.Query<NoteWithDateEntity>().Where(n => n.Target.Is(a)).FirstOrDefault()!.CreationTime.DayOfWeek)
            .ToList();
        Assert.Contains(null, list);
    }

    [Fact]
    public void DayOfWeekSelectConstant()
    {
        var memCount = Database.Query<NoteWithDateEntity>().ToList().Select(a => a.CreationTime.DayOfWeek == DayOfWeek.Sunday).ToList();
        var dbCount = Database.Query<NoteWithDateEntity>().Select(a => a.CreationTime.DayOfWeek == DayOfWeek.Sunday).ToList();
        Assert.Equal(memCount, dbCount);

        var memCount2 = Database.Query<NoteWithDateEntity>().ToList().Select(a => a.CreationDate.DayOfWeek == DayOfWeek.Sunday).ToList();
        var dbCount2 = Database.Query<NoteWithDateEntity>().Select(a => a.CreationDate.DayOfWeek == DayOfWeek.Sunday).ToList();
        Assert.Equal(memCount2, dbCount2);
    }

    [Fact]
    public void DayOfWeekContains()
    {
        var dows = new[] { DayOfWeek.Monday, DayOfWeek.Sunday };

        var memCount = Database.Query<NoteWithDateEntity>().ToList().Where(a => dows.Contains(a.CreationTime.DayOfWeek)).Count();
        var dbCount = Database.Query<NoteWithDateEntity>().Where(a => dows.Contains(a.CreationTime.DayOfWeek)).Count();
        Assert.Equal(memCount, dbCount);

        var memCount2 = Database.Query<NoteWithDateEntity>().ToList().Where(a => dows.Contains(a.CreationDate.DayOfWeek)).Count();
        var dbCount2 = Database.Query<NoteWithDateEntity>().Where(a => dows.Contains(a.CreationDate.DayOfWeek)).Count();
        Assert.Equal(memCount2, dbCount2);
    }

    [Fact]
    public void DayOfWeekGroupByNullable()
    {
        var listy0 = Database.Query<NoteWithDateEntity>()
          .Where(a => a.ReleaseDate.HasValue)
          .GroupBy(a => (DayOfWeek?)a.ReleaseDate!.Value.DayOfWeek)
          .OrderBy(a => a.Key)
          .Select(gr => new { gr.Key, Count = gr.Count() })
          .ToList();

    }

    [Fact]
    public void DayOfWeekGroupBy()
    {

        var listA = Database.Query<NoteWithDateEntity>().GroupBy(a => a.CreationTime.DayOfWeek).Select(gr => new { gr.Key, Count = gr.Count() }).ToList();
        var listB = Database.Query<NoteWithDateEntity>().ToList().GroupBy(a => a.CreationTime.DayOfWeek).Select(gr => new { gr.Key, Count = gr.Count() });

        Assert.Equal(
            listA.OrderBy(a => a.Key).ToString(a => $"{a.Key} {a.Count}", ","),
            listB.OrderBy(a => a.Key).ToString(a => $"{a.Key} {a.Count}", ","));

        var listA2 = Database.Query<NoteWithDateEntity>().GroupBy(a => a.CreationTime.DayOfWeek).Select(gr => new { gr.Key, Count = gr.Count() }).ToList();
        var listB2 = Database.Query<NoteWithDateEntity>().ToList().GroupBy(a => a.CreationTime.DayOfWeek).Select(gr => new { gr.Key, Count = gr.Count() });

        Assert.Equal(
            listA.OrderBy(a => a.Key).ToString(a => $"{a.Key} {a.Count}", ","),
            listB.OrderBy(a => a.Key).ToString(a => $"{a.Key} {a.Count}", ","));
    }

    [Fact]
    public void DateDiffFunctions()
    {
        Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalDays.InSql());
        Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalHours.InSql());
        Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalMinutes.InSql());
        Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalSeconds.InSql());
        Dump((NoteWithDateEntity n) => (n.CreationTime.AddDays(1) - n.CreationTime).TotalMilliseconds.InSql());

        Dump((NoteWithDateEntity n) => (n.CreationDate.DayNumber - n.CreationDate.DayNumber).InSql());
    }

    [Fact]
    public void DateTimeDiffFunctionsTo()
    {
        Dump((NoteWithDateEntity n) => n.CreationTime.DaysTo(n.CreationTime).InSql());
        Dump((NoteWithDateEntity n) => n.CreationTime.MonthsTo(n.CreationTime).InSql());
        Dump((NoteWithDateEntity n) => n.CreationTime.YearsTo(n.CreationTime).InSql());
    }

    [Fact]
    public void DateOnlyDiffFunctionsTo()
    {
        Dump((NoteWithDateEntity n) => n.CreationDate.DaysTo(n.CreationDate).InSql());
        Dump((NoteWithDateEntity n) => n.CreationDate.MonthsTo(n.CreationDate).InSql());
        Dump((NoteWithDateEntity n) => n.CreationDate.YearsTo(n.CreationDate).InSql());
    }

    [Fact]
    public void DateFunctions()
    {
        Dump((NoteWithDateEntity n) => n.CreationTime.Date);

        if (Schema.Current.Settings.IsDbType(typeof(TimeSpan)))
        {
            Dump((NoteWithDateEntity n) => n.CreationTime.TimeOfDay);
        }
    }

    [Fact]
    public void DayOfWeekFunction()
    {
        var list = Database.Query<NoteWithDateEntity>().Where(n => n.CreationTime.DayOfWeek != DayOfWeek.Sunday)
            .Select(n => n.CreationTime.DayOfWeek).ToList();

        var list2 = Database.Query<NoteWithDateEntity>().Where(n => n.CreationDate.DayOfWeek != DayOfWeek.Sunday)
            .Select(n => n.CreationDate.DayOfWeek).ToList();
    }

    [Fact]
    public void TimeSpanFunction()
    {
        if (!Schema.Current.Settings.IsDbType(typeof(TimeSpan)))
            return;

        var durations = Database.MListQuery((AlbumEntity a) => a.Songs).Select(mle => mle.Element.Duration).Where(d => d != null);

        Debug.WriteLine(durations.Select(d => d!.Value.Hours.InSql()).ToString(", "));
        Debug.WriteLine(durations.Select(d => d!.Value.Minutes.InSql()).ToString(", "));
        Debug.WriteLine(durations.Select(d => d!.Value.Seconds.InSql()).ToString(", "));
        Debug.WriteLine(durations.Select(d => d!.Value.Milliseconds.InSql()).ToString(", "));


        Debug.WriteLine((from n in Database.Query<NoteWithDateEntity>()
                         from d in Database.MListQuery((AlbumEntity a) => a.Songs)
                         where d.Element.Duration != null
                         select (n.CreationTime + d.Element.Duration!.Value).InSql()).ToString(", "));

        Debug.WriteLine((from n in Database.Query<NoteWithDateEntity>()
                         from d in Database.MListQuery((AlbumEntity a) => a.Songs)
                         where d.Element.Duration != null
                         select (n.CreationTime - d.Element.Duration!.Value).InSql()).ToString(", "));
    }


    [Fact]
    public void SqlHierarchyIdFunction()
    {
        //if (!Schema.Current.Settings.UdtSqlName.ContainsKey(typeof(SqlHierarchyId)))
        //    return;


        var nodeNullable = Database.Query<LabelEntity>().Select(a => (SqlHierarchyId?)a.Node).ToList();
        Debug.WriteLine(nodeNullable.ToString("\n"));

        var nodes = Database.Query<LabelEntity>().Select(a => a.Node);

        if (Connector.Current is SqlServerConnector)
            Assert.Equal(
                nodes.ToList().Select(a => a.ToString()).ToString(", "),
                nodes.Select(a => a.ToString().InSql()).ToList().ToString(", ")
                );

        
        Debug.WriteLine(nodes.Select(n => n.GetAncestor(0).InSql()).ToString(", "));
        Debug.WriteLine(nodes.Select(n => n.GetAncestor(1).InSql()).ToString(", "));
        Debug.WriteLine(nodes.Select(n => n.GetAncestor((int)n.GetLevel()).InSql()).ToString(", "));
        Debug.WriteLine(nodes.Select(n => n.GetAncestor((int)n.GetLevel() + 1).InSql()).ToString(", "));

        Debug.WriteLine(nodes.Select(n => (int)(short)n.GetLevel().InSql()).ToString(", "));
        Debug.WriteLine(nodes.Select(n => n.ToString().InSql()).ToString(", "));
        Debug.WriteLine(nodes.Select(n => n.ToString()).ToString(", "));



        var one = SqlHierarchyId.Parse("/1/");
        var two = SqlHierarchyId.Parse("/2/");


        Debug.WriteLine(nodes.Where(n => (bool)n.IsDescendantOf(one)).ToString(", "));
        Debug.WriteLine(nodes.Where(n => (bool)n.IsDescendantOf(one)).Select(a => a.GetReparentedValue(one, two).InSql()).ToString(", "));

        var query = nodes.Where(n => (bool)(n.GetDescendant(SqlHierarchyId.Null, SqlHierarchyId.Null) > SqlHierarchyId.GetRoot()));
        if (Connector.Current is SqlServerConnector)
            Debug.WriteLine(query.ToString(", "));
        else
            Assert.Throws<InvalidOperationException>(() => query.ToString(", "));

    }

    [Fact]
    public void MathFunctions()
    {
        Dump((AlbumEntity a) => Math.Sign(a.Year));
        Dump((AlbumEntity a) => -Math.Sign(a.Year) * a.Year);
        Dump((AlbumEntity a) => Math.Abs(a.Year));
        Dump((AlbumEntity a) => Math.Sin(a.Year));
        Dump((AlbumEntity a) => Math.Asin(Math.Sin(a.Year)));
        Dump((AlbumEntity a) => Math.Cos(a.Year));
        Dump((AlbumEntity a) => Math.Acos(Math.Cos(a.Year)));
        Dump((AlbumEntity a) => Math.Tan(a.Year));
        Dump((AlbumEntity a) => Math.Atan(Math.Tan(a.Year)));
        Dump((AlbumEntity a) => Math.Atan2(1, 1).InSql());
        Dump((AlbumEntity a) => Math.Pow(a.Year, 2).InSql());
        Dump((AlbumEntity a) => Math.Sqrt(a.Year));
        Dump((AlbumEntity a) => Math.Exp(Math.Log(a.Year)));
        Dump((AlbumEntity a) => Math.Floor(a.Year + 0.5).InSql());
        Dump((AlbumEntity a) => Math.Log10(a.Year));
        Dump((AlbumEntity a) => Math.Ceiling(a.Year + 0.5).InSql());
        Dump((AlbumEntity a) => Math.Round(a.Year + 0.5).InSql());
        Dump((AlbumEntity a) => Math.Truncate(a.Year + 0.5).InSql());
    }

    internal void Dump<T, S>(Expression<Func<T, S>> bla)
        where T : Entity
    {
        Debug.WriteLine(Database.Query<T>().Select(a => bla.Evaluate(a).InSql()).ToString(","));
    }

    [Fact]
    public void ConcatenateNull()
    {
        var list = Database.Query<ArtistEntity>().Select(a => (a.Name + null).InSql()).ToList();

        Assert.DoesNotContain(list, string.IsNullOrEmpty);
    }

    [Fact]
    public void EnumToString()
    {
        var sexs = Database.Query<ArtistEntity>().Select(a => a.Sex.ToString()).ToList();
    }

    [Fact]
    public void NullableEnumToString()
    {
        var sexs = Database.Query<ArtistEntity>().Select(a => a.Status.ToString()).ToList();
    }

    [Fact]
    public void ConcatenateStringNullableNominate()
    {
        var list2 = Database.Query<ArtistEntity>().Select(a => a.Name + " is " + a.Status).ToList();
    }

    [Fact]
    public void ConcatenateStringNullableEntity()
    {
        var list1 = Database.Query<AlbumEntity>().Select(a => a.Name + " is published by " + a.Label).ToList();
    }

    [Fact]
    public void ConcatenateStringFullNominate()
    {
        var list = Database.Query<ArtistEntity>().Where(a => (a + "").Contains("Michael")).ToList();

        Assert.True(list.Count == 1);
    }

    [Fact]
    public void Etc()
    {
        Assert.True(Enumerable.SequenceEqual(
            Database.Query<AlbumEntity>().Select(a => a.Name.Etc(10)).OrderBy().ToList(),
            Database.Query<AlbumEntity>().Select(a => a.Name).ToList().Select(l => l.Etc(10)).OrderBy().ToList()));

        Assert.Equal(
            Database.Query<AlbumEntity>().Count(a => a.Name.Etc(10).EndsWith("s")),
            Database.Query<AlbumEntity>().Count(a => a.Name.EndsWith("s")));
    }

    [Fact]
    public void TableValuedFunction()
    {
        var list = Database.Query<AlbumEntity>()
            .Where(a => MinimumExtensions.MinimumTableValued((int)a.Id * 2, (int)a.Id).Select(m => m.MinValue).First() > 2).Select(a => a.Id).ToList();
    }

    [Fact]
    public void TableValuedPerformanceTest()
    {
        var songs = Database.MListQuery((AlbumEntity a) => a.Songs).Select(a => a.Element);

        var t1 = PerfCounter.Ticks;

        var fast = (from s1 in songs
                    from s2 in songs
                    from s3 in songs
                    from s4 in songs
                    select MinimumExtensions.MinimumTableValued(
                    MinimumExtensions.MinimumTableValued(s1.Seconds, s2.Seconds).Select(a => a.MinValue).First(),
                    MinimumExtensions.MinimumTableValued(s3.Seconds, s4.Seconds).Select(a => a.MinValue).First()
                    ).Select(a => a.MinValue).First()).ToList();

        var t2 = PerfCounter.Ticks;

        var fast2 = (from s1 in songs
                     from s2 in songs
                     from s3 in songs
                     from s4 in songs
                     let x = MinimumExtensions.MinimumTableValued(s1.Seconds, s2.Seconds).Select(a => a.MinValue).First()
                     let y = MinimumExtensions.MinimumTableValued(s3.Seconds, s4.Seconds).Select(a => a.MinValue).First()
                     select MinimumExtensions.MinimumTableValued(x, y).Select(a => a.MinValue).First()).ToList();

        var t3 = PerfCounter.Ticks;

        var slow = (from s1 in songs
                    from s2 in songs
                    from s3 in songs
                    from s4 in songs
                    let x = MinimumExtensions.MinimumScalar(s1.Seconds, s2.Seconds)
                    let y = MinimumExtensions.MinimumScalar(s3.Seconds, s4.Seconds)
                    select MinimumExtensions.MinimumScalar(x, y)).ToList();

        var t4 = PerfCounter.Ticks;
        if (!Schema.Current.Settings.IsPostgres)
        {
            Debug.WriteLine("MinimumTableValued: {0} ms", PerfCounter.ToMilliseconds(t1, t2));
            Debug.WriteLine("MinimumTableValued let: {0} ms", PerfCounter.ToMilliseconds(t2, t3));
            Debug.WriteLine("MinimumScalar: {0} ms", PerfCounter.ToMilliseconds(t3, t4));
        }
    }

    [Fact]
    public void SimplifyMinimumTableValued()
    {
        var result = (from b in Database.Query<BandEntity>()
                      let min = MinimumExtensions.MinimumTableValued((int)b.Id, (int)b.Id).FirstOrDefault()!.MinValue
                      select b.Name).ToList();
    }

    [Fact]
    public void NominateEnumSwitch()
    {
        var list = Database.Query<AlbumEntity>().Select(a =>
            (a.Songs.Count > 10 ? AlbumSize.Large :
            a.Songs.Count > 5 ? AlbumSize.Medium :
             AlbumSize.Small).InSql()).ToList();
    }

    public enum AlbumSize
    {
        Small,
        Medium,
        Large
    }

    [Fact]
    public void EvaluateBeforeAfter()
    {

        var note = Database.Query<NoteWithDateEntity>().Select(a => a.ToLite()).FirstEx();
        T Test<T>(string value, Expression<Func<string, T>> function)
        {
            using (var tr = new Transaction())
            {
                note.InDB().UnsafeUpdate(a => a.Title, a => value);

                return note.InDB(n => function.Evaluate(n.Title).InSql());

                //tr.Commit()
            }
        }


        Assert.Equal("A", Test("A=>B=>C", a => a.TryBefore("=>")));
        Assert.Equal("B=>C", Test("A=>B=>C", a => a.TryAfter("=>")));
        Assert.Equal("A=>B", Test("A=>B=>C", a => a.TryBeforeLast("=>")));
        Assert.Equal("C", Test("A=>B=>C", a => a.TryAfterLast("=>")));


        Assert.Equal("A", Test("A_B_C", a => a.TryBefore("_")));
        Assert.Equal("B_C", Test("A_B_C", a => a.TryAfter("_")));
        Assert.Equal("A_B", Test("A_B_C", a => a.TryBeforeLast("_")));
        Assert.Equal("C", Test("A_B_C", a => a.TryAfterLast("_")));

        Assert.Null(Test("ABC", a => a.TryBefore("_")));
        Assert.Null(Test("ABC", a => a.TryAfter("_")));
        Assert.Null(Test("ABC", a => a.TryBeforeLast("_")));
        Assert.Null(Test("ABC", a => a.TryAfterLast("_")));

        //In the database, Before behaves like TryBefore, etc..

        Assert.Equal("A", Test("A_B_C", a => a.Before("_")));
        Assert.Equal("B_C", Test("A_B_C", a => a.After("_")));
        Assert.Equal("A_B", Test("A_B_C", a => a.BeforeLast("_")));
        Assert.Equal("C", Test("A_B_C", a => a.AfterLast("_")));

        Assert.Null(Test("ABC", a => a.Before("_")));
        Assert.Null(Test("ABC", a => a.After("_")));
        Assert.Null(Test("ABC", a => a.BeforeLast("_")));
        Assert.Null(Test("ABC", a => a.AfterLast("_")));


    }
}
