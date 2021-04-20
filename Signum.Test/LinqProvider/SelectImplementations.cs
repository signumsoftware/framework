using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Test.Environment;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Test.LinqProvider
{
    public class SelectImplementationsTest1
    {
        public SelectImplementationsTest1()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }


        [Fact]
        public void SelectType()
        {
            var list = Database.Query<AlbumEntity>()
                .Select(a => a.GetType()).ToList();
        }

        [Fact]
        public void SelectTypeNull()
        {
            var list = Database.Query<LabelEntity>()
                .Select(a => new { Label = a.ToLite(), a.Owner, OwnerType = a.Owner!.Entity.GetType() }).ToList();
        }

        [Fact]
        public void SelectLiteIB()
        {
            var list = Database.Query<AlbumEntity>()
                .Select(a => a.Author.ToLite()).ToList();
        }

        [Fact]
        public void SelectLiteIBDouble()
        {
            var query = Database.Query<AlbumEntity>()
                .Select(a => new
                {
                    ToStr1 = a.Author.ToLite(),
                    ToStr2 = a.Author.ToLite()
                });

            Assert.Equal(2, query.QueryText().CountRepetitions("LEFT OUTER JOIN"));
            query.ToList();
        }


        [Fact]
        public void SelectLiteIBDoubleWhereUnion()
        {
            var query = Database.Query<AlbumEntity>()
                .Where(a => a.Author.CombineUnion().ToLite().ToString()!.Length > 0)
                .Select(a => a.Author.CombineUnion().ToLite());

            Assert.Equal(3, query.QueryText().CountRepetitions("LEFT OUTER JOIN"));
            query.ToList();
        }

        [Fact]
        public void SelectLiteIBDoubleWhereSwitch()
        {
            var query = Database.Query<AlbumEntity>()
                .Where(a => a.Author.CombineCase().ToLite().ToString()!.Length > 0)
                .Select(a => a.Author.CombineCase().ToLite());

            Assert.Equal(2, query.QueryText().CountRepetitions("LEFT OUTER JOIN"));
            query.ToList();
        }

        [Fact]
        public void SelectTypeIBA()
        {
            var list = Database.Query<NoteWithDateEntity>()
                .Select(a => new { Type = a.Target.GetType(), Target = a.Target.ToLite() }).ToList();
        }

        [Fact]
        public void SelectTypeLiteIB()
        {
            var list = Database.Query<AwardNominationEntity>()
                .Select(a => a.Award.EntityType).ToList();
        }

        [Fact]
        public void SelectEntityWithLiteIb()
        {
            var list = Database.Query<AwardNominationEntity>().Where(a => a.Award.Entity is GrammyAwardEntity).ToList();
        }

        [Fact]
        public void SelectEntityWithLiteIbType()
        {
            var list = Database.Query<AwardNominationEntity>().Where(a => a.Award.Entity.GetType() == typeof(GrammyAwardEntity)).ToList();
        }

        [Fact]
        public void SelectEntityWithLiteIbTypeContains()
        {
            Type[] types = new Type[] { typeof(GrammyAwardEntity) };

            var list = Database.Query<AwardNominationEntity>().Where(a => types.Contains(a.Award.Entity.GetType())).ToList();
        }

        [Fact]
        public void SelectEntityWithLiteIbRuntimeType()
        {
            var list = Database.Query<AwardNominationEntity>().Where(a => a.Award.EntityType == typeof(GrammyAwardEntity)).ToList();
        }

        [Fact]
        public void SelectLiteUpcast()
        {
            var list = Database.Query<ArtistEntity>()
                .Select(a => a.ToLite()).ToList();
        }

        [Fact]
        public void SelectLiteCastUpcast()
        {
            var list = Database.Query<ArtistEntity>()
                .SelectMany(a => a.Friends).Select(a=>(Lite<IAuthorEntity>)a).ToList();
        }

        [Fact]
        public void SelectLiteCastNocast()
        {
            var list = Database.Query<ArtistEntity>()
                .SelectMany(a => a.Friends).Select(a =>(Lite<ArtistEntity>)a).ToList();
        }

        [Fact]
        public void SelectLiteCastDowncast()
        {
            var list = Database.Query<AlbumEntity>()
                .Select(a => (Lite<ArtistEntity>)a.Author.ToLite()).ToList();
        }


        [Fact]
        public void SelectLiteGenericUpcast()
        {
            var list = SelectAuthorsLite<ArtistEntity, IAuthorEntity>();
        }

        public List<Lite<LT>> SelectAuthorsLite<T, LT>()
            where T : Entity, LT
            where LT : class, IEntity
        {
            return Database.Query<T>().Select(a => a.ToLite<LT>()).ToList(); //an explicit convert is injected in this scenario
        }

        [Fact]
        public void SelectLiteIBRedundantUnion()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        let band = (BandEntity)a.Author
                        select new { Artist = band.ToString(), Author = a.Author.CombineUnion().ToString() }).ToList();

            Assert.Equal(Database.Query<AlbumEntity>().Count(), list.Count);
        }

        [Fact]
        public void SelectLiteIBRedundantSwitch()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        let band = (BandEntity)a.Author
                        select new { Artist = band.ToString(), Author = a.Author.CombineCase().ToString() }).ToList();

            Assert.Equal(Database.Query<AlbumEntity>().Count(), list.Count);
        }

        [Fact]
        public void SelectLiteIBWhereUnion()
        {
            var list = Database.Query<AlbumEntity>()
                .Select(a => a.Author.CombineUnion().ToLite())
                .Where(a => a.ToString()!.StartsWith("Michael")).ToList();
        }

        [Fact]
        public void SelectLiteIBWhereSwitch()
        {
            var list = Database.Query<AlbumEntity>()
                .Select(a => a.Author.CombineCase().ToLite())
                .Where(a => a.ToString()!.StartsWith("Michael")).ToList();
        }

        [Fact]
        public void SelectLiteIBA()
        {
            var list = Database.Query<NoteWithDateEntity>().Select(a => a.Target.ToLite()).ToList();
        }

        [Fact]
        public void SelectSimpleEntity()
        {
            var list3 = Database.Query<ArtistEntity>().ToList();
        }

        [Fact]
        public void SelectEntity()
        {
            var list3 = Database.Query<AlbumEntity>().ToList();
        }

        [Fact]
        public void SelectEntitySelect()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a).ToList();
        }

        [Fact]
        public void SelectEntityIB()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author).ToList();
        }

        [Fact]
        public void SelectEntityIBRedundan()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        let aut = a.Author
                        select new { aut, a.Author }).ToList();
        }

        [Fact]
        public void SelectEntityIBA()
        {
            var list = Database.Query<NoteWithDateEntity>().Select(a => a.Target).ToList();
        }


        [Fact]
        public void SelectCastIB()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select ((ArtistEntity)a.Author).Name ??
                               ((BandEntity)a.Author).Name).ToList();
        }

        [Fact]
        public void SelectCastIBPolymorphicUnion()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select a.Author.CombineUnion().Name).ToList();
        }

        [Fact]
        public void SelectCastIBPolymorphicSwitch()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select a.Author.CombineCase().Name).ToList();
        }

       [Fact]
        public void SelectCastIBPolymorphicForceNullify()
        {
            var list = (from a in Database.Query<AwardNominationEntity>()
                        select (int?)a.Award!.Entity.Year).ToList();
        }

        [Fact]
        public void SelectCastIBPolymorphicIBUnion()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select a.Author.CombineUnion().LastAward.Try(la => la.ToLite())).ToList();
        }

        [Fact]
        public void SelectCastIBPolymorphicIBSwitch()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select a.Author.CombineCase().LastAward.Try(la => la.ToLite())).ToList();
        }

        [Fact]
        public void SelectCastIBA()
        {
            var list = (from n in Database.Query<NoteWithDateEntity>()
                        select
                        ((ArtistEntity)n.Target).Name ??
                        ((AlbumEntity)n.Target).Name ??
                        ((BandEntity)n.Target).Name).ToList();

        }

        [Fact]
        public void SelectCastIBACastOperator()
        {
            var list = (from n in Database.Query<NoteWithDateEntity>()
                        select n.Target).Cast<BandEntity>().ToList();
        }


        [Fact]
        public void SelectCastIBAOfTypeOperator()
        {
            var list = (from n in Database.Query<NoteWithDateEntity>()
                        select n.Target).OfType<BandEntity>().ToList();
        }

        [Fact]
        public void SelectCastIsIB()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select (a.Author is ArtistEntity ? ((ArtistEntity)a.Author).Name :
                                                        ((BandEntity)a.Author).Name)).ToList();
        }

        [Fact]
        public void SelectCastIsIBA()
        {
            var list = (from n in Database.Query<NoteWithDateEntity>()
                        select n.Target is ArtistEntity ? ((ArtistEntity)n.Target).Name : ((BandEntity)n.Target).Name).ToList();
        }

        [Fact]
        public void SelectCastIsIBADouble()
        {
            var query = (from n in Database.Query<NoteWithDateEntity>()
                         select new
                         {
                             Name = n.Target is ArtistEntity ? ((ArtistEntity)n.Target).Name : ((BandEntity)n.Target).Name,
                             FullName = n.Target is ArtistEntity ? ((ArtistEntity)n.Target).FullName : ((BandEntity)n.Target).FullName
                         });

            Assert.Equal(1, query.QueryText().CountRepetitions("Artist"));

            query.ToList();
        }

        [Fact]
        public void SelectCastIsIBADoubleWhere()
        {
            var query = (from n in Database.Query<NoteWithDateEntity>()
                         where (n.Target is ArtistEntity ? ((ArtistEntity)n.Target).Name : ((BandEntity)n.Target).Name).Length > 0
                         select n.Target is ArtistEntity ? ((ArtistEntity)n.Target).FullName : ((BandEntity)n.Target).FullName);

            Assert.Equal(1, query.QueryText().CountRepetitions("Artist"));

            query.ToList();
        }

        [Fact]
        public void SelectIsIBLite()
        {
            var query = (from n in Database.Query<NoteWithDateEntity>()
                         where n.Target.ToLite() is Lite<AlbumEntity>
                         select n.Target.ToLite()).ToList();

        }

        [Fact]
        public void SelectIsIBALite()
        {
            var query = (from a in Database.Query<AwardNominationEntity>()
                         where a.Author is Lite<BandEntity>
                         select a.Author).ToList();

        }

        [Fact]
        public void SelectCastIBALite()
        {
            var query = (from n in Database.Query<NoteWithDateEntity>()
                         select (Lite<AlbumEntity>)n.Target.ToLite()).ToList();

        }

        [Fact]
        public void SelectCastIBLite()
        {
            var query = (from a in Database.Query<AwardNominationEntity>()
                         select (Lite<BandEntity>)a.Author).ToList();

        }

        //AwardNominationEntity
    }
}
