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
    [Serializable, EntityKind(EntityKind.Shared), Mixin(typeof(CorruptMixin)), Mixin(typeof(ColaboratorsMixin))]
    public class NoteWithDateDN : Entity
    {
        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value, () => Text); }
        }

        [ImplementedByAll]
        IIdentifiable target;
        public IIdentifiable Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        DateTime creationTime;
        public DateTime CreationTime
        {
            get { return creationTime; }
            set { Set(ref creationTime, value, () => CreationTime); }
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
            set { Set(ref colaborators, value, () => Colaborators); }
        }
    }

    public enum NoteWithDateOperation
    { 
        Save
    }

    [DescriptionOptions(DescriptionOptions.All)]
    public interface IAuthorDN : IIdentifiable
    {
        string Name { get; }

        AwardDN LastAward { get; }

        string FullName { get; }

        bool Lonely();
    }

    [Serializable, EntityKind(EntityKind.Shared)]
    public class ArtistDN : Entity, IAuthorDN
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        bool dead;
        public bool Dead
        {
            get { return dead; }
            set { Set(ref dead, value, () => Dead); }
        }

        Sex sex;
        public Sex Sex
        {
            get { return sex; }
            set { Set(ref sex, value, () => Sex); }
        }

        Status? status;
        public Status? Status
        {
            get { return status; }
            set { Set(ref status, value, () => Status); }
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
            set { Set(ref lastAward, value, () => LastAward); }
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
            set { Set(ref friends, value, () => Friends); }
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

    public enum ArtistOperation
    {
        Save,
        AssignPersonalAward
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

    [Serializable, EntityKind(EntityKind.Main)]
    public class BandDN : Entity, IAuthorDN
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        [NotNullable]
        MList<ArtistDN> members = new MList<ArtistDN>();
        public MList<ArtistDN> Members
        {
            get { return members; }
            set { Set(ref members, value, () => Members); }
        }

        [ImplementedBy(typeof(GrammyAwardDN), typeof(AmericanMusicAwardDN))]
        AwardDN lastAward;
        public AwardDN LastAward
        {
            get { return lastAward; }
            set { Set(ref lastAward, value, () => LastAward); }
        }

        [ImplementedBy(typeof(GrammyAwardDN), typeof(AmericanMusicAwardDN)), NotNullable]
        MList<AwardDN> otherAwards = new MList<AwardDN>();
        public MList<AwardDN> OtherAwards 
        {
            get { return otherAwards; }
            set { Set(ref otherAwards, value, () => OtherAwards); }
        }

        static Expression<Func<BandDN, string>> FullNameExpression =
            b => b.Name + " (" + b.Members.Count + " members )";
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

    public enum BandOperation
    { 
        Save
    }

    [Serializable, EntityKind(EntityKind.Shared)]
    public abstract class AwardDN : Entity
    {
        int year;
        public int Year
        {
            get { return year; }
            set { Set(ref year, value, () => Year); }
        }

        [NotNullable, SqlDbType( Size = 100)]
        string category;
        [StringLengthValidator(AllowNulls=false, Min = 3, Max = 100)]
        public string Category
        {
            get { return category; }
            set { Set(ref category, value, () => Category); }
        }

        AwardResult result;
        public AwardResult Result
        {
            get { return result; }
            set { Set(ref result, value, () => Result); }
        }
    }

    public enum AwardOperation
    { 
        Save
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


    [Serializable, EntityKind(EntityKind.Main)]
    public class LabelDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        CountryDN country;
        public CountryDN Country
        {
            get { return country; }
            set { Set(ref country, value, () => Country); }
        }

        Lite<LabelDN> owner;
        public Lite<LabelDN> Owner
        {
            get { return owner; }
            set { Set(ref owner, value, () => Owner); }
        }

        [UniqueIndex]
        SqlHierarchyId node;
        public SqlHierarchyId Node
        {
            get { return node; }
            set { Set(ref node, value, () => Node); }
        }

        static Expression<Func<LabelDN, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum LabelOperation
    { 
        Save
    }

    [Serializable, EntityKind(EntityKind.SystemString)]
    public class CountryDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable, EntityKind(EntityKind.Main)]
    public class AlbumDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        int year;
        [NumberBetweenValidator(1900, 2100)]
        public int Year
        {
            get { return year; }
            set { Set(ref year, value, () => Year); }
        }

        [ImplementedBy(typeof(ArtistDN), typeof(BandDN))]
        IAuthorDN author;
        [NotNullValidator]
        public IAuthorDN Author
        {
            get { return author; }
            set { Set(ref author, value, () => Author); }
        }

        [NotNullable]
        MList<SongDN> songs = new MList<SongDN>();
        public MList<SongDN> Songs
        {
            get { return songs; }
            set { Set(ref songs, value, () => Songs); }
        }

        SongDN bonusTrack;
        public SongDN BonusTrack
        {
            get { return bonusTrack; }
            set { Set(ref bonusTrack, value, () => BonusTrack); }
        }

        LabelDN label;
        public LabelDN Label
        {
            get { return label; }
            set { Set(ref label, value, () => Label); }
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

    public enum AlbumOperation
    {
        Save,
        Modify,
        CreateAlbumFromBand,
        Delete,
        Clone,
        CreateGreatestHitsAlbum,
        CreateEmptyGreatestHitsAlbum
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
            set { SetToStr(ref name, value, () => Name); }
        }

        TimeSpan? duration;
        public TimeSpan? Duration
        {
            get { return duration; }
            set
            {
                if (Set(ref duration, value, () => Duration))
                    seconds = duration == null ? null : (int?)duration.Value.TotalSeconds;
            }
        }

        int? seconds;
        public int? Seconds
        {
            get { return seconds; }
            set { Set(ref seconds, value, () => Seconds); }
        }

        static Expression<Func<SongDN, string>> ToStringExpression = a => a.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable, EntityKind(EntityKind.System)]
    public class AwardNominationDN : Entity
    {
        [ImplementedBy(typeof(ArtistDN), typeof(BandDN))]
        Lite<IAuthorDN> author;
        public Lite<IAuthorDN> Author
        {
            get { return author; }
            set { Set(ref author, value, () => Author); }
        }

        [ImplementedBy(typeof(GrammyAwardDN), typeof(PersonalAwardDN), typeof(AmericanMusicAwardDN))]
        Lite<AwardDN> award;
        public Lite<AwardDN> Award
        {
            get { return award; }
            set { Set(ref award, value, () => Award); }
        }

        int year;
        public int Year
        {
            get { return year; }
            set { Set(ref year, value, () => Year); }
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
