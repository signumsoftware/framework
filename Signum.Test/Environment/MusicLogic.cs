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

        static Expression<Func<IAuthorEntity, IQueryable<AlbumEntity>>> AlbumsExpression =
        e => Database.Query<AlbumEntity>().Where(a => a.Author == e);
        [ExpressionField]
        public static IQueryable<AlbumEntity> Albums(this IAuthorEntity e)
        {
            return AlbumsExpression.Evaluate(e);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (!Schema.Current.Settings.TypeValues.ContainsKey(typeof(TimeSpan)))
                {
                    sb.Settings.FieldAttributes((AlbumEntity a) => a.Songs[0].Duration).Add(new Signum.Entities.IgnoreAttribute());
                    sb.Settings.FieldAttributes((AlbumEntity a) => a.BonusTrack.Duration).Add(new Signum.Entities.IgnoreAttribute());
                }

                sb.Include<AlbumEntity>()
                    .WithExpressionFrom(dqm, (IAuthorEntity au) => au.Albums())
                    .WithQuery(dqm, () => a => new
                    {
                        Entity = a,
                        a.Id,
                        a.Name,
                        a.Author,
                        a.Label,
                        a.Year
                    });
                AlbumGraph.Register();


                sb.Include<NoteWithDateEntity>()
                    .WithSave(NoteWithDateOperation.Save)
                    .WithQuery(dqm, () => a => new
                    {
                        Entity = a,
                        a.Id,
                        a.Text,
                        a.Target,
                        a.CreationTime,
                    });

                sb.Include<ConfigEntity>()
                    .WithSave(ConfigOperation.Save);

                MinimumExtensions.IncludeFunction(sb.Schema.Assets);
                sb.Include<ArtistEntity>()
                    .WithSave(ArtistOperation.Save)
                    .WithVirtualMList(a => a.Nominations, n => (Lite<ArtistEntity>)n.Author)
                    .WithQuery(dqm, () => a => new
                    {
                        Entity = a,
                        a.Id,
                        a.Name,
                        a.IsMale,
                        a.Sex,
                        a.Dead,
                        a.LastAward,
                    });

                new Graph<ArtistEntity>.Execute(ArtistOperation.AssignPersonalAward)
                {
                    Lite = true,
                    AllowsNew = false,
                    CanExecute = a => a.LastAward != null ? "Artist already has an award" : null,
                    Execute = (a, para) => a.LastAward = new PersonalAwardEntity() { Category = "Best Artist", Year = DateTime.Now.Year, Result = AwardResult.Won }.Execute(AwardOperation.Save)
                }.Register();

                sb.Include<BandEntity>()
                    .WithQuery(dqm, () => a => new
                    {
                        Entity = a,
                        a.Id,
                        a.Name,
                        a.LastAward,
                    });

                new Graph<BandEntity>.Execute(BandOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (b, _) =>
                    {
                        using (OperationLogic.AllowSave<ArtistEntity>())
                        {
                            b.Save();
                        }
                    }
                }.Register();

                sb.Include<LabelEntity>()
                    .WithSave(LabelOperation.Save)
                    .WithQuery(dqm, () => a => new
                    {
                        Entity = a,
                        a.Id,
                        a.Name,
                    });


                sb.Include<FolderEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name
                    });

                RegisterAwards(sb, dqm);

                dqm.RegisterQuery(typeof(IAuthorEntity), () => DynamicQueryCore.Manual(async (request, descriptions, token) =>
                {
                    var one = (from a in Database.Query<ArtistEntity>()
                               select new
                               {
                                   Entity = (IAuthorEntity)a,
                                   a.Id,
                                   Type = "Artist",
                                   a.Name,
                                   Lonely = a.Lonely(),
                                   LastAward = a.LastAward
                               })
                               .ToDQueryable(descriptions)
                               .AllQueryOperationsAsync(request, token);

                    var two = (from a in Database.Query<BandEntity>()
                               select new
                               {
                                   Entity = (IAuthorEntity)a,
                                   a.Id,
                                   Type = "Band",
                                   a.Name,
                                   Lonely = a.Lonely(),
                                   LastAward = a.LastAward
                               })
                               .ToDQueryable(descriptions)
                               .AllQueryOperationsAsync(request, token);

                    return (await one).Concat(await two).OrderBy(request.Orders).TryPaginate(request.Pagination);

                })
                    .Column(a => a.LastAward, cl => cl.Implementations = Implementations.ByAll)
                    .ColumnProperyRoutes(a => a.Id, PropertyRoute.Construct((ArtistEntity a) => a.Id), PropertyRoute.Construct((BandEntity a) => a.Id)),
                    entityImplementations: Implementations.By(typeof(ArtistEntity), typeof(BandEntity)));

                Validator.PropertyValidator((NoteWithDateEntity n) => n.Text)
                    .IsApplicableValidator<StringLengthValidatorAttribute>(n => Corruption.Strict);
            }
        }

        private static void RegisterAwards(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            new Graph<AwardEntity>.Execute(AwardOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { }
            }.Register();


            sb.Include<AmericanMusicAwardEntity>()
                .WithQuery(dqm, () => a => new
                {
                    Entity = a,
                    a.Id,
                    a.Year,
                    a.Category,
                    a.Result,
                });

            sb.Include<GrammyAwardEntity>()
                .WithQuery(dqm, () => a => new
                {
                    Entity = a,
                    a.Id,
                    a.Year,
                    a.Category,
                    a.Result
                });

            sb.Include<PersonalAwardEntity>()
                .WithQuery(dqm, () => a => new
                {
                    Entity = a,
                    a.Id,
                    a.Year,
                    a.Category,
                    a.Result
                });

            sb.Include<AwardNominationEntity>()
                .WithQuery(dqm, () => a => new
                {
                    Entity = a,
                    a.Id,
                    a.Award,
                    a.Author
                });
        }
    }

    public class AlbumGraph : Graph<AlbumEntity, AlbumState>
    {
        public static void Register()
        {
            GetState = f => f.State;

            new Execute(AlbumOperation.Save)
            {
                FromStates = { AlbumState.New },
                ToStates = { AlbumState.Saved },
                AllowsNew = true,
                Lite = false,
                Execute = (album, _) => { album.State = AlbumState.Saved; album.Save(); },
            }.Register();

            new Execute(AlbumOperation.Modify)
            {
                FromStates = { AlbumState.Saved },
                ToStates = { AlbumState.Saved },
                AllowsNew = false,
                Lite = false,
                Execute = (album, _) => { },
            }.Register();

            new ConstructFrom<BandEntity>(AlbumOperation.CreateAlbumFromBand)
            {
                ToStates = { AlbumState.Saved },
                AllowsNew = false,
                Lite = true,
                Construct = (BandEntity band, object[] args) =>
                    new AlbumEntity
                    {
                        Author = band,
                        Name = args.GetArg<string>(),
                        Year = args.GetArg<int>(),
                        State = AlbumState.Saved,
                        Label = args.GetArg<LabelEntity>()
                    }.Save()
            }.Register();

            new ConstructFrom<AlbumEntity>(AlbumOperation.Clone)
            {
                ToStates = { AlbumState.New },
                AllowsNew = false,
                Lite = true,
                Construct = (g, args) =>
                {
                    return new AlbumEntity
                    {
                        Author = g.Author,
                        Label = g.Label,
                        BonusTrack = new SongEmbedded
                        {
                            Name = "Clone bonus track"
                        }
                    };
                }
            }.Register();

            new ConstructFromMany<AlbumEntity>(AlbumOperation.CreateGreatestHitsAlbum)
            {
                ToStates = { AlbumState.New },
                Construct = (albumLites, _) =>
                {
                    List<AlbumEntity> albums = albumLites.Select(a => a.Retrieve()).ToList();
                    if (albums.Select(a => a.Author).Distinct().Count() > 1)
                        throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                    return new AlbumEntity()
                    {
                        Author = albums.FirstEx().Author,
                        Year = DateTime.Now.Year,
                        Songs = albums.SelectMany(a => a.Songs).ToMList()
                    };
                }
            }.Register();


            new ConstructFromMany<AlbumEntity>(AlbumOperation.CreateEmptyGreatestHitsAlbum)
            {
                ToStates = { AlbumState.New },
                Construct = (albumLites, _) =>
                {
                    List<AlbumEntity> albums = albumLites.Select(a => a.Retrieve()).ToList();
                    if (albums.Select(a => a.Author).Distinct().Count() > 1)
                        throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                    return new AlbumEntity()
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
