using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Chart;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.Drawing;
using Signum.Entities.Basics;

namespace Signum.Engine.Chart
{
    public static class ChartColorLogic
    {
        public static ResetLazy<Dictionary<Type, Dictionary<PrimaryKey, Color>>> Colors = null!;

        public static readonly int Limit = 360;

        internal static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ChartColorEntity>()
                    .WithQuery(() => cc => new
                    {
                        Entity = cc,
                        cc.Related,
                        cc.Color,
                    });

                Colors = sb.GlobalLazy(() =>
                    Database.Query<ChartColorEntity>()
                        .Select(cc => new { cc.Related.EntityType, cc.Related.Id, cc.Color!.Argb })
                        .AgGroupToDictionary(a => a.EntityType!, gr => gr.ToDictionary(a => a.Id, a => Color.FromArgb(a.Argb))),
                    new InvalidateWith(typeof(ChartColorEntity)));
            }
        }

        public static Dictionary<string, string> Palettes = new Dictionary<string,string>(){
            {"Category10",  "#1f77b4 #ff7f0e #2ca02c #d62728 #9467bd #8c564b #e377c2 #7f7f7f #bcbd22 #17becf"},
            {"Category20",  "#1f77b4 #aec7e8 #ff7f0e #ffbb78 #2ca02c #98df8a #d62728 #ff9896 #9467bd #c5b0d5 #8c564b #c49c94 #e377c2 #f7b6d2 #7f7f7f #c7c7c7 #bcbd22 #dbdb8d #17becf #9edae5"},
            {"Category20b", "#393b79 #5254a3 #6b6ecf #9c9ede #637939 #8ca252 #b5cf6b #cedb9c #8c6d31 #bd9e39 #e7ba52 #e7cb94 #843c39 #ad494a #d6616b #e7969c #7b4173 #a55194 #ce6dbd #de9ed6"},
            {"Category20c", "#3182bd #6baed6 #9ecae1 #c6dbef #e6550d #fd8d3c #fdae6b #fdd0a2 #31a354 #74c476 #a1d99b #c7e9c0 #756bb1 #9e9ac8 #bcbddc #dadaeb #636363 #969696 #bdbdbd #d9d9d9"},
        };

        public static void CreateNewPalette(Type type, string palette)
        {
            AssertFewEntities(type);

            var dic = Database.RetrieveAllLite(type).Select(l => new ChartColorEntity { Related = (Lite<Entity>)l }).ToDictionary(a => a.Related);

            dic.SetRange(Database.Query<ChartColorEntity>().Where(c => c.Related.EntityType == type).ToDictionary(a => a.Related));

            var list = dic.Values.ToList();

            var cats = Palettes.GetOrThrow(palette).Split(' ');

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Color = ColorEmbedded.FromRGBHex(cats[i % cats.Length]);
            }

            list.SaveList();
        }

        private static int DivideRoundUp(int number, int divisor)
        {
            return ((number - 1) / divisor) + 1;
        }

        public static void AssertFewEntities(Type type)
        {
            int count = giCount.GetInvoker(type)();

            if (count > Limit)
                throw new ApplicationException("Too many {0} ({1}), maximum is {2}".FormatWith(type.NicePluralName(), count, Limit));
        }

        public static void SavePalette(ChartPaletteModel model)
        {
            using (Transaction tr = new Transaction())
            {
                Type type = model.Type.ToType();

                giDeleteColors.GetInvoker(type)();

                model.Colors.Where(a => a.Color != null).SaveList();
                tr.Commit();
            }
        }

        static readonly GenericInvoker<Func<int>> giCount = new GenericInvoker<Func<int>>(() => Count<Entity>());
        static int Count<T>() where T : Entity
        {
            return Database.Query<T>().Count();
        }

        static readonly GenericInvoker<Func<int>> giDeleteColors = new GenericInvoker<Func<int>>(() => DeleteColors<Entity>());
        static int DeleteColors<T>() where T : Entity
        {
            return (from t in Database.Query<T>() // To filter by type conditions
                    join cc in Database.Query<ChartColorEntity>() on t.ToLite() equals cc.Related
                    select cc).UnsafeDelete();
        }

        public static ChartPaletteModel GetPalette(Type type)
        {
            AssertFewEntities(type);

            var dic = ChartColorLogic.Colors.Value.TryGetC(type);

            return new ChartPaletteModel
            {
                Type = type.ToTypeEntity(),
                Colors = Database.RetrieveAllLite(type).Select(l => new ChartColorEntity
                {
                    Related = (Lite<Entity>)l,
                    Color = dic?.TryGetS(l.Id)?.Let(c => new ColorEmbedded { Argb = c.ToArgb() })
                }).ToMList()
            };
        }

        public static Color? ColorFor(Type type, PrimaryKey id)
        {
            return Colors.Value.TryGetC(type)?.TryGetS(id);
        }

        public static Color? ColorFor(Lite<Entity> lite)
        {
            return ColorFor(lite.EntityType, lite.Id);
        }

        public static Color? ColorFor(Entity ident)
        {
            return ColorFor(ident.GetType(), ident.Id);
        }

        public static void DeletePalette(Type type)
        {
            Database.Query<ChartColorEntity>().Where(c => c.Related.EntityType == type).UnsafeDelete();
        }
    }
}
