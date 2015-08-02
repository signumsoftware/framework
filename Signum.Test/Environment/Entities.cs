using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;
using Microsoft.SqlServer.Types;
using Microsoft.SqlServer.Server;
using Signum.Engine;
using Signum.Engine.Maps;

namespace Signum.Test.Environment
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional), Mixin(typeof(CorruptMixin)), Mixin(typeof(ColaboratorsMixin)), PrimaryKey(typeof(Guid))]
    public class NoteWithDateEntity : Entity
    {
        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value); }
        }

        [ImplementedByAll]
        IEntity target;
        public IEntity Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        [ImplementedByAll]
        Lite<IEntity> otherTarget;
        public Lite<IEntity> OtherTarget
        {
            get { return otherTarget; }
            set { Set(ref otherTarget, value); }
        }

        DateTime creationTime;
        public DateTime CreationTime
        {
            get { return creationTime; }
            set { Set(ref creationTime, value); }
        }

        public override string ToString()
        {
            return "{0} -> {1}".FormatWith(creationTime, text);
        }
    }

    [Serializable] // Just a pattern
    public class ColaboratorsMixin : MixinEntity
    {
        ColaboratorsMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        [NotNullable]
        MList<ArtistEntity> colaborators = new MList<ArtistEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<ArtistEntity> Colaborators
        {
            get { return colaborators; }
            set { Set(ref colaborators, value); }
        }
    }

    public static class NoteWithDateOperation
    {
        public static readonly ExecuteSymbol<NoteWithDateEntity> Save = OperationSymbol.Execute<NoteWithDateEntity>();
    }

    [DescriptionOptions(DescriptionOptions.All)]
    public interface IAuthorEntity : IEntity
    {
        string Name { get; }

        AwardEntity LastAward { get; }

        string FullName { get; }

        bool Lonely();
    }

    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional)]
    public class ArtistEntity : Entity, IAuthorEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        bool dead;
        public bool Dead
        {
            get { return dead; }
            set { Set(ref dead, value); }
        }

        Sex sex;
        public Sex Sex
        {
            get { return sex; }
            set { Set(ref sex, value); }
        }

        Status? status;
        public Status? Status
        {
            get { return status; }
            set { Set(ref status, value); }
        }


        static Expression<Func<ArtistEntity, bool>> IsMaleExpression = a => a.Sex == Sex.Male;
        public bool IsMale
        {
            get { return Sex == Sex.Male; }
        }

        [ImplementedByAll]
        AwardEntity lastAward;
        public AwardEntity LastAward
        {
            get { return lastAward; }
            set { Set(ref lastAward, value); }
        }

        static Expression<Func<ArtistEntity, IEnumerable<Lite<Entity>>>> FriendsCovariantExpression =
            a => a.Friends;
        public IEnumerable<Lite<Entity>> FriendsCovariant()
        {
            return FriendsCovariantExpression.Evaluate(this);
        }

        //[NotNullable] Do not add Nullable for testing purposes
        MList<Lite<ArtistEntity>> friends = new MList<Lite<ArtistEntity>>();
        public MList<Lite<ArtistEntity>> Friends
        {
            get { return friends; }
            set { Set(ref friends, value); }
        }

        static Expression<Func<ArtistEntity, string>> FullNameExpression =
             a => a.Name + (a.Dead ? " Dead" : "") + (a.IsMale ? " Male" : " Female");
        public string FullName
        {
            get { return FullNameExpression.Evaluate(this); }
        }

        static Expression<Func<ArtistEntity, bool>> LonelyExpression =
            a => !a.Friends.Any();
        public bool Lonely()
        {
            return LonelyExpression.Evaluate(this);
        }

        static Expression<Func<ArtistEntity, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class ArtistOperation
    {
        public static readonly ExecuteSymbol<ArtistEntity> Save = OperationSymbol.Execute<ArtistEntity>();
        public static readonly ExecuteSymbol<ArtistEntity> AssignPersonalAward = OperationSymbol.Execute<ArtistEntity>();
    }

    [Flags]
    public enum Sex : short
    {
        Male,
        Female
    }

    public enum Status
    {
        Single,
        Married,
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class BandEntity : Entity, IAuthorEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        [NotNullable]
        MList<ArtistEntity> members = new MList<ArtistEntity>();
        public MList<ArtistEntity> Members
        {
            get { return members; }
            set { Set(ref members, value); }
        }

        [ImplementedBy(typeof(GrammyAwardEntity), typeof(AmericanMusicAwardEntity))]
        AwardEntity lastAward;
        public AwardEntity LastAward
        {
            get { return lastAward; }
            set { Set(ref lastAward, value); }
        }

        [ImplementedBy(typeof(GrammyAwardEntity), typeof(AmericanMusicAwardEntity)), NotNullable]
        MList<AwardEntity> otherAwards = new MList<AwardEntity>();
        public MList<AwardEntity> OtherAwards
        {
            get { return otherAwards; }
            set { Set(ref otherAwards, value); }
        }

        static Expression<Func<BandEntity, string>> FullNameExpression =
            b => b.Name + " (" + b.Members.Count + " members)";
        public string FullName
        {
            get { return FullNameExpression.Evaluate(this); }
        }

        static Expression<Func<BandEntity, bool>> LonelyExpression =
            b => !b.Members.Any();
        public bool Lonely()
        {
            return LonelyExpression.Evaluate(this);
        }

        static Expression<Func<BandEntity, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class BandOperation
    {
        public static readonly ExecuteSymbol<BandEntity> Save = OperationSymbol.Execute<BandEntity>();
    }

    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional), PrimaryKey(typeof(long))]
    public abstract class AwardEntity : Entity
    {
        int year;
        public int Year
        {
            get { return year; }
            set { Set(ref year, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string category;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Category
        {
            get { return category; }
            set { Set(ref category, value); }
        }

        AwardResult result;
        public AwardResult Result
        {
            get { return result; }
            set { Set(ref result, value); }
        }
    }

    public static class AwardOperation
    {
        public static readonly ExecuteSymbol<AwardEntity> Save = OperationSymbol.Execute<AwardEntity>();
    }

    public enum AwardResult
    {
        Won,
        Nominated
    }

    [Serializable]
    public class GrammyAwardEntity : AwardEntity
    {
    }

    [Serializable]
    public class AmericanMusicAwardEntity : AwardEntity
    {
    }

    [Serializable]
    public class PersonalAwardEntity : AwardEntity
    {
    }


    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class LabelEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        CountryEntity country;
        public CountryEntity Country
        {
            get { return country; }
            set { Set(ref country, value); }
        }

        Lite<LabelEntity> owner;
        public Lite<LabelEntity> Owner
        {
            get { return owner; }
            set { Set(ref owner, value); }
        }

        [UniqueIndex]
        SqlHierarchyId node;
        public SqlHierarchyId Node
        {
            get { return node; }
            set { Set(ref node, value); }
        }

        static Expression<Func<LabelEntity, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class LabelOperation
    {
        public static readonly ExecuteSymbol<LabelEntity> Save = OperationSymbol.Execute<LabelEntity>();
    }

    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public class CountryEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class AlbumEntity : Entity, ISecretContainer
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        int year;
        [NumberBetweenValidator(1900, 2100)]
        public int Year
        {
            get { return year; }
            set { Set(ref year, value); }
        }

        [ImplementedBy(typeof(ArtistEntity), typeof(BandEntity))]
        IAuthorEntity author;
        [NotNullValidator]
        public IAuthorEntity Author
        {
            get { return author; }
            set { Set(ref author, value); }
        }

        [NotNullable, PreserveOrder]
        MList<SongEntity> songs = new MList<SongEntity>();
        public MList<SongEntity> Songs
        {
            get { return songs; }
            set { Set(ref songs, value); }
        }

        SongEntity bonusTrack;
        public SongEntity BonusTrack
        {
            get { return bonusTrack; }
            set { Set(ref bonusTrack, value); }
        }

        LabelEntity label;
        public LabelEntity Label
        {
            get { return label; }
            set { Set(ref label, value); }
        }

        AlbumState state;
        public AlbumState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        string secret;
        string ISecretContainer.Secret
        {
            get { return secret; }
            set { Set(ref secret, value); }
        }

        static Expression<Func<AlbumEntity, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public interface ISecretContainer
    {
        string Secret { get; set; }
    }

    public enum AlbumState
    {
        [Ignore]
        New,
        Saved
    }

    public static class AlbumOperation
    {
        public static readonly ExecuteSymbol<AlbumEntity> Save = OperationSymbol.Execute<AlbumEntity>();
        public static readonly ExecuteSymbol<AlbumEntity> Modify = OperationSymbol.Execute<AlbumEntity>();
        public static readonly ConstructSymbol<AlbumEntity>.From<BandEntity> CreateAlbumFromBand = OperationSymbol.Construct<AlbumEntity>.From<BandEntity>();
        public static readonly DeleteSymbol<AlbumEntity> Delete = OperationSymbol.Delete<AlbumEntity>();
        public static readonly ConstructSymbol<AlbumEntity>.From<AlbumEntity> Clone = OperationSymbol.Construct<AlbumEntity>.From<AlbumEntity>();
        public static readonly ConstructSymbol<AlbumEntity>.FromMany<AlbumEntity> CreateGreatestHitsAlbum = OperationSymbol.Construct<AlbumEntity>.FromMany<AlbumEntity>();
        public static readonly ConstructSymbol<AlbumEntity>.FromMany<AlbumEntity> CreateEmptyGreatestHitsAlbum = OperationSymbol.Construct<AlbumEntity>.FromMany<AlbumEntity>();
    }

    [Serializable]
    public class SongEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        TimeSpan? duration;
        public TimeSpan? Duration
        {
            get { return duration; }
            set
            {
                if (Set(ref duration, value))
                    seconds = duration == null ? null : (int?)duration.Value.TotalSeconds;
            }
        }

        int? seconds;
        public int? Seconds
        {
            get { return seconds; }
            set { Set(ref seconds, value); }
        }

        int index;
        public int Index
        {
            get { return index; }
            set { Set(ref index, value); }
        }

        static Expression<Func<SongEntity, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class AwardNominationEntity : Entity
    {
        [ImplementedBy(typeof(ArtistEntity), typeof(BandEntity))]
        Lite<IAuthorEntity> author;
        public Lite<IAuthorEntity> Author
        {
            get { return author; }
            set { Set(ref author, value); }
        }

        [ImplementedBy(typeof(GrammyAwardEntity), typeof(PersonalAwardEntity), typeof(AmericanMusicAwardEntity))]
        Lite<AwardEntity> award;
        public Lite<AwardEntity> Award
        {
            get { return award; }
            set { Set(ref award, value); }
        }

        int year;
        public int Year
        {
            get { return year; }
            set { Set(ref year, value); }
        }
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class ConfigEntity : Entity
    {
        EmbeddedConfigEntity embeddedConfig;
        public EmbeddedConfigEntity EmbeddedConfig
        {
            get { return embeddedConfig; }
            set { Set(ref embeddedConfig, value); }
        }
    }

    public static class ConfigOperation
    {
        public static readonly ExecuteSymbol<ConfigEntity> Save = OperationSymbol.Execute<ConfigEntity>();
    }

    public class EmbeddedConfigEntity : EmbeddedEntity
    {
        [NotNullable]
        MList<Lite<GrammyAwardEntity>> awards = new MList<Lite<GrammyAwardEntity>>();
        [NotNullValidator, NoRepeatValidator]
        public MList<Lite<GrammyAwardEntity>> Awards
        {
            get { return awards; }
            set { Set(ref awards, value); }
        }
    }



    public static class MinimumExtensions
    {
        [SqlMethod(Name = "dbo.MinimumTableValued")]
        public static IQueryable<IntValue> MinimumTableValued(int? a, int? b)
        {
            throw new InvalidOperationException("sql only");
        }


        [SqlMethod(Name = "dbo.MinimumScalar")]
        public static int? MinimumScalar(int? a, int? b)
        {
            throw new InvalidOperationException("sql only");
        }

        internal static void IncludeFunction(SchemaAssets assets)
        {
            assets.IncludeUserDefinedFunction("MinimumTableValued", @"(@Param1 Integer, @Param2 Integer)
RETURNS Table As
RETURN (SELECT Case When @Param1 < @Param2 Then @Param1 
               Else COALESCE(@Param2, @Param1) End MinValue)");


            assets.IncludeUserDefinedFunction("MinimumScalar", @"(@Param1 Integer, @Param2 Integer)
RETURNS Integer
AS
BEGIN
   RETURN (Case When @Param1 < @Param2 Then @Param1 
           Else COALESCE(@Param2, @Param1) End);
END");
        }
    }

    public class IntValue : IView
    {
        public int? MinValue;
    }
}
