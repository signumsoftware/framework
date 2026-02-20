using Signum.Engine.Maps;
using Signum.DynamicQuery;
using Signum.Basics;
using System.IO;
using System.Text.Json;
using Microsoft.Data.SqlTypes;
using Pgvector;

namespace Signum.Test.Environment;

public static class MusicLogic
{
    [AutoExpressionField]
    public static IQueryable<AlbumEntity> Albums(this IAuthorEntity e) => 
        As.Expression(() => Database.Query<AlbumEntity>().Where(a => a.Author == e));

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<AlbumEntity>()
            .WithExpressionFrom((IAuthorEntity au) => au.Albums())
            .WithQuery(() => a => new
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
            .WithQuery(() => a => new
            {
                Entity = a,
                a.Id,
                a.Title,
                a.Target,
                a.CreationTime,
            });

        var embeddings = JsonSerializer.Deserialize<Dictionary<string, float[]>>(File.ReadAllText("linesWithEmbeddings.json"))!;

        Filter.GetEmbeddingForSmartSearch = (vectorToken, searchString) =>
        {
            if (embeddings.TryGetValue(searchString, out var embedding))
                return new Vector(embedding);

            throw new InvalidOperationException($"Test embedding not found for search string: '{searchString}'.");
        };

        new Graph<NoteWithDateEntity>.Execute(NoteWithDateOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (e, _) =>
            {
                if(!e.IsNew)
                    Database.Query<SimplePassageEntity>().Where(a=>a.Note.Is(e)).UnsafeDeleteChunks();

                e.Save();

                if (e.Title.HasText())
                    new SimplePassageEntity
                    {
                        Note = e.ToLite(),
                        IsTitle = true,
                        Chunk = e.Title,
                        Index = 0,
                    }.Save();

                e.Text?.SplitNoEmpty('\r', '\n', '.')
                    .Select(t => t.Trim())
                    .Where(t => t.HasText())
                    .Select((t, i) => new SimplePassageEntity
                    {
                        Note = e.ToLite(),
                        IsTitle = false,
                        Chunk = t,
                        Embedding = new Vector(embeddings.GetOrThrow(t)),
                        Index = i + 1,
                    }).SaveList();
            },
        }.Register();

        if (Connector.Current is SqlServerConnector ss && ss.SupportsFullTextSearch || Connector.Current is PostgreSqlConnector)
            sb.AddFullTextIndex<NoteWithDateEntity>(a => new { a.Title, a.Text });

        sb.Include<ConfigEntity>()
            .WithSave(ConfigOperation.Save);

        MinimumExtensions.IncludeFunction(sb.Schema.Assets);
        sb.Include<ArtistEntity>()
            .WithSave(ArtistOperation.Save)
            .WithVirtualMList(a => a.Nominations, n => (Lite<ArtistEntity>)n.Author)
            .WithQuery(() => a => new
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
            CanExecute = a => a.LastAward != null ? "Artist already has an award" : null,
            Execute = (a, para) => a.LastAward = new PersonalAwardEntity() { Category = "Best Artist", Year = DateTime.Now.Year, Result = AwardResult.Won }.Execute(AwardOperation.Save)
        }.Register();

        sb.Include<BandEntity>()
            .WithQuery(() => a => new
            {
                Entity = a,
                a.Id,
                a.Name,
                a.LastAward,
            });

        new Graph<BandEntity>.Execute(BandOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
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
            .WithQuery(() => a => new
            {
                Entity = a,
                a.Id,
                a.Name,
            });


        sb.Include<FolderEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name
            });

        if (Connector.Current.SupportsVectors)
        sb.Include<SimplePassageEntity>()
            .WithVectorIndex(a=>a.Embedding, vti =>
            {
                if (Connector.Current is SqlServerConnector)
                    vti.DelayCreation = true;
            })
            .WithQuery(() => p => new
            {
                Entity = p,
                p.Id,
                p.Note,
            });

        RegisterAwards(sb);

        QueryLogic.Queries.Register(typeof(IAuthorEntity), () => DynamicQueryCore.Manual(async (request, description, cancellationToken) =>
        {
            var one = await (from a in Database.Query<ArtistEntity>()
                             select new
                             {
                                 Entity = (IAuthorEntity)a,
                                 a.Id,
                                 Type = "Artist",
                                 a.Name,
                                 Lonely = a.Lonely(),
                                 a.LastAward
                             })
                           .ToDQueryable(description)
                           .AllQueryOperationsAsync(request, cancellationToken, forConcat: true);

            var two = await (from a in Database.Query<BandEntity>()
                             select new
                             {
                                 Entity = (IAuthorEntity)a,
                                 a.Id,
                                 Type = "Band",
                                 a.Name,
                                 Lonely = a.Lonely(),
                                 a.LastAward
                             })
                           .ToDQueryable(description)
                           .AllQueryOperationsAsync(request, cancellationToken, forConcat: true);

            return one.Concat(two).OrderBy(request.Orders).TryPaginate(request.Pagination);

        })
            .Column(a => a.LastAward, cl => cl.Implementations = Implementations.ByAll)
            .ColumnProperyRoutes(a => a.Id, PropertyRoute.Construct((ArtistEntity a) => a.Id), PropertyRoute.Construct((BandEntity a) => a.Id)),
            entityImplementations: Implementations.By(typeof(ArtistEntity), typeof(BandEntity)));

        Validator.PropertyValidator((NoteWithDateEntity n) => n.Title)
            .IsApplicableValidator<NotNullValidatorAttribute>(n => Corruption.Strict);
    }

    //[Fact]
    //public void SimplePassage()
    //{
    //    var chunks = Database.Query<SimplePassageEntity>().Select(a => a.Chunk).ToList();

    //    File.WriteAllLines("lines.txt", chunks);
    //}

    //Run in Southwind, originally with Gemini models/text-embedding-004 with 768 dimensions
    //static void ExportEmbeddings()
    //{
    //    var lines = File.ReadAllLines(@"..\..\..\lines.txt")!;
    //    var model = ChatbotLogic.DefaultEmbeddingsModel.Value!.RetrieveFromCache();
    //    var embeddings = ChatbotLogic.GetEmbeddingsAsync(lines, model, default).ResultSafe();

    //    var dic = lines.Zip(embeddings, (l, e) => KeyValuePair.Create(l, e)).ToDictionary();

    //    File.WriteAllText(@"..\..\..\linesWithEmbeddings.json", JsonSerializer.Serialize(dic));
    //}

    private static void RegisterAwards(SchemaBuilder sb)
    {
        new Graph<AwardEntity>.Execute(AwardOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (a, _) => { }
        }.Register();


        sb.Include<AmericanMusicAwardEntity>()
            .WithQuery(() => a => new
            {
                Entity = a,
                a.Id,
                a.Year,
                a.Category,
                a.Result,
            });

        sb.Include<GrammyAwardEntity>()
            .WithQuery(() => a => new
            {
                Entity = a,
                a.Id,
                a.Year,
                a.Category,
                a.Result
            });

        sb.Include<PersonalAwardEntity>()
            .WithQuery(() => a => new
            {
                Entity = a,
                a.Id,
                a.Year,
                a.Category,
                a.Result
            });

        sb.Include<AwardNominationEntity>()
            .WithQuery(() => a => new
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
            CanBeNew = true,
            CanBeModified = true,
            Execute = (album, _) => { album.State = AlbumState.Saved; album.Save(); },
        }.Register();

        new Execute(AlbumOperation.Modify)
        {
            FromStates = { AlbumState.Saved },
            ToStates = { AlbumState.Saved },
            CanBeModified = true,
            Execute = (album, _) => { },
        }.Register();

        new ConstructFrom<BandEntity>(AlbumOperation.CreateAlbumFromBand)
        {
            ToStates = { AlbumState.Saved },
            Construct = (BandEntity band, object?[]? args) =>
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
                List<AlbumEntity> albums = albumLites.Select(a => a.RetrieveAndRemember()).ToList();
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
                List<AlbumEntity> albums = albumLites.Select(a => a.RetrieveAndRemember()).ToList();
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
