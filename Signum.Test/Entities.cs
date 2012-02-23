using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Test
{
    [Serializable]
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
            return text;
        }
    }


    public interface IAuthorDN : IIdentifiable
    {
        string Name { get; }

        AwardDN LastAward { get; }

        string FullName { get; }

        bool Lonely();
    }

    [Serializable]
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

        public override string ToString()
        {
            return name;
        }
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

    [Serializable]
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

        [ImplementedBy(typeof(GrammyAwardDN), typeof(AmericanMusicAwardDN))]
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

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable]
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


    [Serializable]
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

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable]
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

    [Serializable]
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

        public override string ToString()
        {
            return name;
        }
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
            set { Set(ref duration, value, () => Duration); }
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable]
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
}
