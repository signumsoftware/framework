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

namespace Signum.Test.Environment
{
    public static class MusicLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (sb.Schema.Settings.DBMS == DBMS.SqlCompact || sb.Schema.Settings.DBMS == DBMS.SqlServer2005)
                {
                    sb.Settings.OverrideAttributes((AlbumDN a) => a.Songs[0].Duration, new Signum.Entities.IgnoreAttribute());
                    sb.Settings.OverrideAttributes((AlbumDN a) => a.BonusTrack.Duration, new Signum.Entities.IgnoreAttribute());
                }

                sb.Include<AlbumDN>();
                sb.Include<NoteWithDateDN>();
                sb.Include<PersonalAwardDN>();
                sb.Include<AwardNominationDN>();
                sb.Include<ConfigDN>();

                MinimumExtensions.IncludeFunction(sb.Schema.Assets);

                dqm.RegisterQuery(typeof(AlbumDN), ()=> 
                    from a in Database.Query<AlbumDN>()
                    select new
                    {
                        Entity = a,
                        a.Id,
                        a.Name,
                        a.Author,
                        a.Label,
                        a.Year
                    });

                dqm.RegisterQuery(typeof(NoteWithDateDN), ()=> 
                    from a in Database.Query<NoteWithDateDN>()
                    select new
                    {
                        Entity = a,
                        a.Id,
                        a.Text,
                        a.Target,
                        a.CreationTime,
                    });

                dqm.RegisterQuery(typeof(ArtistDN), ()=> 
                    from a in Database.Query<ArtistDN>()
                    select new
                    {
                        Entity = a,
                        a.Id,
                        a.Name,
                        a.IsMale,
                        a.Sex,
                        a.Dead,
                        a.LastAward,
                    });

                dqm.RegisterExpression((IAuthorDN au) => Database.Query<AlbumDN>().Where(a => a.Author == au), () => typeof(AlbumDN).NiceName(), "Albums");

                dqm.RegisterQuery(typeof(BandDN), ()=> 
                    from a in Database.Query<BandDN>()
                    select new
                    {
                        Entity = a.ToLite(),
                        a.Id,
                        a.Name,
                        a.LastAward,
                    });


                dqm.RegisterQuery(typeof(LabelDN), ()=> 
                    from a in Database.Query<LabelDN>()
                    select new
                    {
                        Entity = a.ToLite(),
                        a.Id,
                        a.Name,
                    });


                dqm.RegisterQuery(typeof(AmericanMusicAwardDN), ()=> 
                    from a in Database.Query<AmericanMusicAwardDN>()
                    select new
                    {
                        Entity = a.ToLite(),
                        a.Id,
                        a.Year,
                        a.Category,
                        a.Result,
                    });

                dqm.RegisterQuery(typeof(GrammyAwardDN), ()=> 
                    from a in Database.Query<GrammyAwardDN>()
                    select new
                    {
                        Entity = a.ToLite(),
                        a.Id,
                        a.Year,
                        a.Category,
                        a.Result
                    });

                dqm.RegisterQuery(typeof(PersonalAwardDN), ()=> 
                    from a in Database.Query<PersonalAwardDN>()
                    select new
                    {
                        Entity = a.ToLite(),
                        a.Id,
                        a.Year,
                        a.Category,
                        a.Result
                    });

                dqm.RegisterQuery(typeof(AwardNominationDN), ()=> 
                    from a in Database.Query<AwardNominationDN>()
                    select new
                    {
                        Entity = a.ToLite(),
                        a.Id,
                        a.Award,
                        a.Author
                    });


                dqm.RegisterQuery(typeof(IAuthorDN), () => DynamicQuery.Manual((request, descriptions) =>
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
                                   }).ToDQueryable(descriptions).AllQueryOperations(request);

                        var two = (from a in Database.Query<BandDN>()
                                   select new
                                   {
                                       Entity = a.ToLite<IAuthorDN>(),
                                       a.Id,
                                       Type = "Band",
                                       a.Name,
                                       Lonely = a.Lonely(),
                                       LastAward = a.LastAward.ToLite()
                                   }).ToDQueryable(descriptions).AllQueryOperations(request);

                        return one.Concat(two).OrderBy(request.Orders).TryPaginate(request.Pagination);

                    }).Column(a => a.LastAward, cl => cl.Implementations = Implementations.ByAll),
                    entityImplementations: Implementations.By(typeof(ArtistDN), typeof(BandDN)));

                Validator.PropertyValidator((NoteWithDateDN n) => n.Text)
                    .IsApplicableValidator<StringLengthValidatorAttribute>(n => Corruption.Strict); 

                AlbumGraph.Register();

                RegisterOperations();
            }
        }

        private static void RegisterOperations()
        {
            new Graph<AwardDN>.Execute(AwardOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { }
            }.Register();


            new Graph<NoteWithDateDN>.Execute(NoteWithDateOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (n, _) => { }
            }.Register();

            new Graph<ArtistDN>.Execute(ArtistOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { }
            }.Register();

            new Graph<ArtistDN>.Execute(ArtistOperation.AssignPersonalAward)
            {
                Lite = true,
                AllowsNew = false,
                CanExecute = a => a.LastAward != null ? "Artist already has an award" : null,
                Execute = (a, para) => a.LastAward = new PersonalAwardDN() { Category = "Best Artist", Year = DateTime.Now.Year, Result = AwardResult.Won }.Execute(AwardOperation.Save)
            }.Register();

            new Graph<BandDN>.Execute(BandOperation.Save)
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

            new Graph<LabelDN>.Execute(LabelOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (l, _) => { }
            }.Register();

            new Graph<ConfigDN>.Execute(ConfigOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (e, _) => { },
            }.Register();
        }
    }

    public class AlbumGraph : Graph<AlbumDN, AlbumState>
    {
        public static void Register()
        {
            GetState = f => f.State;

            new Execute(AlbumOperation.Save)
            {
                FromStates = { AlbumState.New },
                ToState = AlbumState.Saved,
                AllowsNew = true,
                Lite = false,
                Execute = (album, _) => { album.State = AlbumState.Saved; album.Save(); },
            }.Register();

            new Execute(AlbumOperation.Modify)
            {
                FromStates = { AlbumState.Saved },
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
                        State = AlbumState.Saved,
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
                        BonusTrack = new SongDN
                        {
                            Name = "Clone bonus track"
                        }
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


            new Delete(AlbumOperation.Delete)
            {
                FromStates = { AlbumState.Saved },
                Delete = (album, _) => album.Delete()
            }.Register();
        }
    }
}
