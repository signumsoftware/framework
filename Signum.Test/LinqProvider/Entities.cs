using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Windows;
using System.Linq.Expressions;

namespace Signum.Test.LinqProvider
{

    [Serializable]
    public class NoteDN : Entity
    {
        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value, "Text"); }
        }

        [ImplementedByAll]
        IIdentifiable target;
        public IIdentifiable Target
        {
            get { return target; }
            set { Set(ref target, value, "Target"); }
        }

        DateTime creationTime;
        public DateTime CreationTime
        {
            get { return creationTime; }
            set { Set(ref creationTime, value, "CreationTime"); }
        }

        public override string ToString()
        {
            return text;
        }
    }

    [ImplementedBy(typeof(ArtistDN), typeof(BandDN))]
    public interface IAuthorDN : IIdentifiable
    {
        string Name { get; }

        AwardDN LastAward { get; }
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
            set { SetToStr(ref name, value, "Name"); }
        }

        bool dead;
        public bool Dead
        {
            get { return dead; }
            set { Set(ref dead, value, "Dead"); }
        }

        Sex sex;
        public Sex Sex
        {
            get { return sex; }
            set { Set(ref sex, value, "Sex"); }
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
            set { Set(ref lastAward, value, "LastAward"); }
        }

        public override string ToString()
        {
            return name;
        }
    }

    public enum Sex
    {
        Male,
        Female
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
            set { SetToStr(ref name, value, "Name"); }
        }

        MList<ArtistDN> members;
        public MList<ArtistDN> Members
        {
            get { return members; }
            set { Set(ref members, value, "Members"); }
        }

        AwardDN lastAward;
        public AwardDN LastAward
        {
            get { return lastAward; }
            set { Set(ref lastAward, value, "LastAward"); }
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable, ImplementedBy(typeof(GrammyAwardDN), typeof(AmericanMusicAwardDN))]
    public abstract class AwardDN : Entity
    {
        int year;
        public int Year
        {
            get { return year; }
            set { Set(ref year, value, "Year"); }
        }

        [NotNullable, SqlDbType( Size = 100)]
        string category;
        [StringLengthValidator(AllowNulls=false, Min = 3, Max = 100)]
        public string Category
        {
            get { return category; }
            set { Set(ref category, value, "Category"); }
        }

        AwardResult result;
        public AwardResult Result
        {
            get { return result; }
            set { Set(ref result, value, "Result"); }
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
            set { SetToStr(ref name, value, "Name"); }
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
            set { SetToStr(ref name, value, "Name"); }
        }

        int year;
        [NumberBetweenValidator(1900, 2100)]
        public int Year
        {
            get { return year; }
            set { Set(ref year, value, "Year"); }
        }

        IAuthorDN author;
        [NotNullValidator]
        public IAuthorDN Author
        {
            get { return author; }
            set { Set(ref author, value, "Author"); }
        }

        MList<SongDN> songs;
        public MList<SongDN> Songs
        {
            get { return songs; }
            set { Set(ref songs, value, "Song"); }
        }

        LabelDN label;
        public LabelDN Label
        {
            get { return label; }
            set { Set(ref label, value, "Label"); }
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable]
    public class SongDN : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, "Name"); }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
