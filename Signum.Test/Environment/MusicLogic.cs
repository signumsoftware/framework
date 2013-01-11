using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Signum.Test
{
    public static class MusicLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (sb.Schema.Settings.DBMS == DBMS.SqlCompact || sb.Schema.Settings.DBMS == DBMS.SqlServer2005)
                {
                    sb.Settings.OverrideAttributes<AlbumDN>(a => a.Songs[0].Duration, new Signum.Entities.IgnoreAttribute());
                    sb.Settings.OverrideAttributes<AlbumDN>(a => a.BonusTrack.Duration, new Signum.Entities.IgnoreAttribute());
                }

                sb.Include<AlbumDN>();
                sb.Include<NoteWithDateDN>();
                sb.Include<PersonalAwardDN>();
                sb.Include<AwardNominationDN>();

                MinimumExtensions.IncludeFunction(sb.Schema.Assets);

                dqm[typeof(AlbumDN)] = (from a in Database.Query<AlbumDN>()
                                        select new
                                        {
                                            Entity = a.ToLite(),
                                            a.Id,
                                            Author = a.Author.ToLite(),
                                            Label = a.Label.ToLite(),
                                            a.Name,
                                            a.Year
                                        }).ToDynamic();

                dqm[typeof(NoteWithDateDN)] = (from a in Database.Query<NoteWithDateDN>()
                                               select new
                                               {
                                                   Entity = a.ToLite(),
                                                   a.Id,
                                                   a.Text,
                                                   Target = a.Target.ToLite(),
                                                   a.CreationTime,
                                               }).ToDynamic();

                dqm[typeof(ArtistDN)] = (from a in Database.Query<ArtistDN>()
                                         select new
                                         {
                                             Entity = a.ToLite(),
                                             a.Id,
                                             a.Name,
                                             a.IsMale,
                                             a.Sex,
                                             a.Dead,
                                             LastAward = a.LastAward.ToLite(),
                                         }).ToDynamic();

                dqm.RegisterExpression((IAuthorDN au) => Database.Query<AlbumDN>().Where(a => a.Author == au), () => "Albums", "Albums");

                dqm[typeof(BandDN)] = (from a in Database.Query<BandDN>()
                                       select new
                                       {
                                           Entity = a.ToLite(),
                                           a.Id,
                                           a.Name,
                                           LastAward = a.LastAward.ToLite(),
                                       }).ToDynamic();


                dqm[typeof(LabelDN)] = (from a in Database.Query<LabelDN>()
                                        select new
                                        {
                                            Entity = a.ToLite(),
                                            a.Id,
                                            a.Name,
                                        }).ToDynamic();


                dqm[typeof(AmericanMusicAwardDN)] = (from a in Database.Query<AmericanMusicAwardDN>()
                                                     select new
                                                     {
                                                         Entity = a.ToLite(),
                                                         a.Id,
                                                         a.Year,
                                                         a.Category,
                                                         a.Result,
                                                     }).ToDynamic();

                dqm[typeof(GrammyAwardDN)] = (from a in Database.Query<GrammyAwardDN>()
                                              select new
                                              {
                                                  Entity = a.ToLite(),
                                                  a.Id,
                                                  a.Year,
                                                  a.Category,
                                                  a.Result
                                              }).ToDynamic();

                dqm[typeof(PersonalAwardDN)] = (from a in Database.Query<PersonalAwardDN>()
                                                select new
                                                {
                                                    Entity = a.ToLite(),
                                                    a.Id,
                                                    a.Year,
                                                    a.Category,
                                                    a.Result
                                                }).ToDynamic();

                dqm[typeof(AwardNominationDN)] = (from a in Database.Query<AwardNominationDN>()
                                                  select new
                                                  {
                                                      Entity = a.ToLite(),
                                                      a.Id,
                                                      a.Award,
                                                      a.Author
                                                  }).ToDynamic();

                dqm[typeof(IAuthorDN)] = DynamicQuery.Manual((request, descriptions) =>
                {
                    var one = (from a in Database.Query<ArtistDN>()
                               select new
                               {
                                   Entity = a.ToLite<IAuthorDN>(),
                                   a.Id,
                                   Type = "Artist",
                                   a.Name,
                                   Lonely = a.Lonely(),
                                   LastAward = a.LastAward.ToLite()
                               }).ToDQueryable(descriptions)
                                .SelectMany(request.Multiplications)
                                .Where(request.Filters)
                                .OrderBy(request.Orders)
                                .Select(request.Columns)
                                .TryPaginatePartial(request.MaxElementIndex);

                    var two = (from a in Database.Query<BandDN>()
                               select new
                               {
                                   Entity = a.ToLite<IAuthorDN>(),
                                   a.Id,
                                   Type = "Band",
                                   a.Name,
                                   Lonely = a.Lonely(),
                                   LastAward = a.LastAward.ToLite()
                               }).ToDQueryable(descriptions)
                                .SelectMany(request.Multiplications)
                                .Where(request.Filters)
                                .OrderBy(request.Orders)
                                .Select(request.Columns)
                                .TryPaginatePartial(request.MaxElementIndex);

                    return one.Concat(two).OrderBy(request.Orders).TryPaginate(request.ElementsPerPage, request.CurrentPage);
                })
                                        .Column(a => a.Entity, cl => cl.Implementations = Implementations.By(typeof(ArtistDN), typeof(BandDN)))
                                        .Column(a => a.LastAward, cl => cl.Implementations = Implementations.ByAll);

                AlbumGraph.Register();

                RegisterOperations();
            }
        }

        private static void RegisterOperations()
        {
            new BasicExecute<AwardDN>(AwardOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { }
            }.Register();


            new BasicExecute<NoteWithDateDN>(NoteWithDateOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (n, _) => { }
            }.Register();

            new BasicExecute<ArtistDN>(ArtistOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { }
            }.Register();

            new BasicExecute<ArtistDN>(ArtistOperation.AssignPersonalAward)
            {
                Lite = true,
                AllowsNew = false,
                CanExecute = a => a.LastAward != null ? "Artist already has an award" : null,
                Execute = (a, para) => a.LastAward = new PersonalAwardDN() { Category = "Best Artist", Year = DateTime.Now.Year, Result = AwardResult.Won }.Execute(AwardOperation.Save)
            }.Register();

            new BasicExecute<BandDN>(BandOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (b, _) => 
                {
                    using (OperationLogic.AllowSave<ArtistDN>())
                    {
                        b.Save();
                    }
                }
            }.Register();

            new BasicExecute<LabelDN>(LabelOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (l, _) => { }
            }.Register();
        }
    }

    public class AlbumGraph : Graph<AlbumDN, AlbumState>
    {
        public static void Register()
        {
            GetState = f => (f.IsNew) ? AlbumState.New : AlbumState.Saved;

            new Execute(AlbumOperation.Save)
            {
                FromStates = new[] { AlbumState.New },
                ToState = AlbumState.Saved,
                AllowsNew = true,
                Lite = false,
                Execute = (album, _) => { album.Save(); },
            }.Register();

            new Execute(AlbumOperation.Modify)
            {
                FromStates = new[] { AlbumState.Saved },
                ToState = AlbumState.Saved,
                AllowsNew = false,
                Lite = false,
                Execute = (album, _) => { },
            }.Register();

            new ConstructFrom<BandDN>(AlbumOperation.CreateAlbumFromBand)
            {
                ToState = AlbumState.Saved,
                AllowsNew = false,
                Lite = true,
                Construct = (BandDN band, object[] args) =>
                    new AlbumDN
                    {
                        Author = band,
                        Name = args.GetArg<string>(),
                        Year = args.GetArg<int>(),
                        Label = args.GetArg<LabelDN>()
                    }.Save()
            }.Register();

            new ConstructFrom<AlbumDN>(AlbumOperation.Clone)
            {
                ToState = AlbumState.New,
                AllowsNew = false,
                Lite = true,
                Construct = (g, args) =>
                {
                    return new AlbumDN
                    {
                        Author = g.Author,
                        Label = g.Label,
                    };
                }
            }.Register();

            new ConstructFromMany<AlbumDN>(AlbumOperation.CreateGreatestHitsAlbum)
            {
                ToState = AlbumState.New,
                Construct = (albumLites, _) =>
                {
                    List<AlbumDN> albums = albumLites.Select(a => a.Retrieve()).ToList();
                    if (albums.Select(a => a.Author).Distinct().Count() > 1)
                        throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                    return new AlbumDN()
                    {
                        Author = albums.FirstEx().Author,
                        Year = DateTime.Now.Year,
                        Songs = albums.SelectMany(a => a.Songs).ToMList()
                    };
                }
            }.Register();


            new ConstructFromMany<AlbumDN>(AlbumOperation.CreateEmptyGreatestHitsAlbum)
            {
                ToState = AlbumState.New,
                Construct = (albumLites, _) =>
                {
                    List<AlbumDN> albums = albumLites.Select(a => a.Retrieve()).ToList();
                    if (albums.Select(a => a.Author).Distinct().Count() > 1)
                        throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                    return new AlbumDN()
                    {
                        Author = albums.FirstEx().Author,
                        Year = DateTime.Now.Year,
                    };
                }
            }.Register();


            new BasicDelete<AlbumDN>(AlbumOperation.Delete)
            {
                Delete = (album, _) => album.Delete()
            }.Register();
        }
    }
}
