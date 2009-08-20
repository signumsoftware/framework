using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

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

        MList<SongDN> song;
        public MList<SongDN> Song
        {
            get { return song; }
            set { Set(ref song, value, "Song"); }
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
