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
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional), Mixin(typeof(CorruptMixin)), Mixin(typeof(ColaboratorsMixin))]
    public class NoteWithDateDN : Entity
    {
        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value); }
        }

        [ImplementedByAll]
        IIdentifiable target;
        public IIdentifiable Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        [ImplementedByAll]
        Lite<IIdentifiable> otherTarget;
        public Lite<IIdentifiable> OtherTarget
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
            return "{0} -> {1}".Formato(creationTime, text);
        }
    }

    [Serializable] // Just a pattern
    public class ColaboratorsMixin : MixinEntity
    {
        ColaboratorsMixin(IdentifiableEntity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        [NotNullable]
        MList<ArtistDN> colaborators = new MList<ArtistDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<ArtistDN> Colaborators
        {
            get { return colaborators; }
            set { Set(ref colaborators, value); }
        }
    }

    public static class NoteWithDateOperation
    {
        public static readonly ExecuteSymbol<NoteWithDateDN> Save = OperationSymbol.Execute<NoteWithDateDN>();
    }

    [DescriptionOptions(DescriptionOptions.All)]
    public interface IAuthorDN : IIdentifiable
    {
        string Name { get; }

        AwardDN LastAward { get; }

        string FullName { get; }

        bool Lonely();
    }

    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional)]
    public class ArtistDN : Entity, IAuthorDN
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


        static Expression<Func<ArtistDN, bool>> IsMaleExpression = a => a.Sex == Sex.Male;
        public bool IsMale
        {
            get { return Sex == Sex.Male; }
        }

        [ImplementedByAll]
        AwardDN lastAward;
        public AwardDN LastAward
        {
            get { return lastAward; }
            set { Set(ref lastAward, value); }
        }

        static Expression<Func<ArtistDN, IEnumerable<Lite<IdentifiableEntity>>>> FriendsCovariantExpression =
            a => a.Friends; 
        public IEnumerable<Lite<IdentifiableEntity>> FriendsCovariant()
        {
            return FriendsCovariantExpression.Evaluate(this);
        }

        //[NotNullable] Do not add Nullable for testing purposes
        MList<Lite<ArtistDN>> friends = new MList<Lite<ArtistDN>>();
        public MList<Lite<ArtistDN>> Friends
        {
            get { return friends; }
            set { Set(ref friends, value); }
        }

        static Expression<Func<ArtistDN, string>> FullNameExpression =
             a => a.Name + (a.Dead ? " Dead" : "") + (a.IsMale ? " Male" : " Female");
        public string FullName
        {
            get{ return FullNameExpression.Evaluate(this); }
        }

        static Expression<Func<ArtistDN, bool>> LonelyExpression =
            a => !a.Friends.Any();
        public bool Lonely()
        {
            return LonelyExpression.Evaluate(this);
        }

        static Expression<Func<ArtistDN, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class ArtistOperation
    {
        public static readonly ExecuteSymbol<ArtistDN> Save = OperationSymbol.Execute<ArtistDN>();
        public static readonly ExecuteSymbol<ArtistDN> AssignPersonalAward = OperationSymbol.Execute<ArtistDN>();
    }

    [Flags]
    public enum Sex
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
    public class BandDN : Entity, IAuthorDN
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
        MList<ArtistDN> members = new MList<ArtistDN>();
        public MList<ArtistDN> Members
        {
            get { return members; }
            set { Set(ref members, value); }
        }

        [ImplementedBy(typeof(GrammyAwardDN), typeof(AmericanMusicAwardDN))]
        AwardDN lastAward;
        public AwardDN LastAward
        {
            get { return lastAward; }
            set { Set(ref lastAward, value); }
        }

        [ImplementedBy(typeof(GrammyAwardDN), typeof(AmericanMusicAwardDN)), NotNullable]
        MList<AwardDN> otherAwards = new MList<AwardDN>();
        public MList<AwardDN> OtherAwards 
        {
            get { return otherAwards; }
            set { Set(ref otherAwards, value); }
        }

        static Expression<Func<BandDN, string>> FullNameExpression =
            b => b.Name + " (" + b.Members.Count + " members)";
        public string FullName
        {
            get { return FullNameExpression.Evaluate(this); }
        }

        static Expression<Func<BandDN, bool>> LonelyExpression =
            b => !b.Members.Any();
        public bool Lonely()
        {
            return LonelyExpression.Evaluate(this);
        }

        static Expression<Func<BandDN, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class BandOperation
    {
        public static readonly ExecuteSymbol<BandDN> Save = OperationSymbol.Execute<BandDN>();
    }

    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional)]
    public abstract class AwardDN : Entity
    {
        int year;
        public int Year
        {
            get { return year; }
            set { Set(ref year, value); }
        }

        [NotNullable, SqlDbType( Size = 100)]
        string category;
        [StringLengthValidator(AllowNulls=false, Min = 3, Max = 100)]
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
        public static readonly ExecuteSymbol<AwardDN> Save = OperationSymbol.Execute<AwardDN>();
    }

    public enum AwardResult 
    {
        Won,
        Nominated
    }

    [Serializable]
    public class GrammyAwardDN : AwardDN
    {
    }

    [Serializable]
    public class AmericanMusicAwardDN : AwardDN
    {
    }

    [Serializable]
    public class PersonalAwardDN : AwardDN
    {
    }


    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class LabelDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        CountryDN country;
        public CountryDN Country
        {
            get { return country; }
            set { Set(ref country, value); }
        }

        Lite<LabelDN> owner;
        public Lite<LabelDN> Owner
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

        static Expression<Func<LabelDN, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class LabelOperation
    {
        public static readonly ExecuteSymbol<LabelDN> Save = OperationSymbol.Execute<LabelDN>();
    }

    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public class CountryDN : Entity
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
    public class AlbumDN : Entity
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

        [ImplementedBy(typeof(ArtistDN), typeof(BandDN))]
        IAuthorDN author;
        [NotNullValidator]
        public IAuthorDN Author
        {
            get { return author; }
            set { Set(ref author, value); }
        }

        [NotNullable]
        MList<SongDN> songs = new MList<SongDN>();
        public MList<SongDN> Songs
        {
            get { return songs; }
            set { Set(ref songs, value); }
        }

        SongDN bonusTrack;
        public SongDN BonusTrack
        {
            get { return bonusTrack; }
            set { Set(ref bonusTrack, value); }
        }

        LabelDN label;
        public LabelDN Label
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

        static Expression<Func<AlbumDN, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum AlbumState
    {
        [Ignore]
        New,
        Saved
    }

    public static class AlbumOperation
    {
        public static readonly ExecuteSymbol<AlbumDN> Save = OperationSymbol.Execute<AlbumDN>();
        public static readonly ExecuteSymbol<AlbumDN> Modify = OperationSymbol.Execute<AlbumDN>();
        public static readonly ConstructSymbol<AlbumDN>.From<BandDN> CreateAlbumFromBand = OperationSymbol.Construct<AlbumDN>.From<BandDN>();
        public static readonly DeleteSymbol<AlbumDN> Delete = OperationSymbol.Delete<AlbumDN>();
        public static readonly ConstructSymbol<AlbumDN>.From<AlbumDN> Clone = OperationSymbol.Construct<AlbumDN>.From<AlbumDN>();
        public static readonly ConstructSymbol<AlbumDN>.FromMany<AlbumDN> CreateGreatestHitsAlbum = OperationSymbol.Construct<AlbumDN>.FromMany<AlbumDN>();
        public static readonly ConstructSymbol<AlbumDN>.FromMany<AlbumDN> CreateEmptyGreatestHitsAlbum = OperationSymbol.Construct<AlbumDN>.FromMany<AlbumDN>();
    }

    [Serializable]
    public class SongDN : EmbeddedEntity
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

        static Expression<Func<SongDN, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class AwardNominationDN : Entity
    {
        [ImplementedBy(typeof(ArtistDN), typeof(BandDN))]
        Lite<IAuthorDN> author;
        public Lite<IAuthorDN> Author
        {
            get { return author; }
            set { Set(ref author, value); }
        }

        [ImplementedBy(typeof(GrammyAwardDN), typeof(PersonalAwardDN), typeof(AmericanMusicAwardDN))]
        Lite<AwardDN> award;
        public Lite<AwardDN> Award
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
    public class ConfigDN : Entity
    {
        EmbeddedConfigDN embeddedConfig;
        public EmbeddedConfigDN EmbeddedConfig
        {
            get { return embeddedConfig; }
            set { Set(ref embeddedConfig, value); }
        }
    }

    public static class ConfigOperation
    {
        public static readonly ExecuteSymbol<ConfigDN> Save = OperationSymbol.Execute<ConfigDN>();
    }

    public class EmbeddedConfigDN : EmbeddedEntity
    {
        [NotNullable]
        MList<Lite<GrammyAwardDN>> awards = new MList<Lite<GrammyAwardDN>>();
        [NotNullValidator, NoRepeatValidator]
        public MList<Lite<GrammyAwardDN>> Awards
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
