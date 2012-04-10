using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        static readonly Lazy<Dictionary<Type, Dictionary<int, Color>>> Colors = GlobalLazy.Create(() =>
              Database.Query<ChartColorDN>()
              .Select(cc => new { cc.Related.RuntimeType, cc.Related.Id, cc.Color.Argb })
              .AgGroupToDictionary(a => a.RuntimeType, gr => gr.ToDictionary(a => a.Id, a => Color.FromArgb(a.Argb))))
        .InvalidateWith(typeof(ChartColorDN));

        static readonly int Limit = 360; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ChartColorDN>();

                dqm[typeof(ChartColorDN)] = (from cc in Database.Query<ChartColorDN>()
                                             select new
                                             {
                                                 Entity = cc,
                                                 cc.Related,
                                                 cc.Color,
                                             }).ToDynamic();
            }
        }

        public static void SetFullPalette(Type type)
        {
            int count = giCount.GetInvoker(type)();

            if (count > Limit)
                throw new ApplicationException("Too many {0} ({1}), maximum is {2}".Formato(type.NicePluralName(), count, Limit));

            var dic = Database.RetrieveAllLite(type).Select(l => new ChartColorDN { Related = l.ToLite<IdentifiableEntity>() }).ToDictionary(a => a.Related);

            dic.SetRange(Database.Query<ChartColorDN>().Where(c => c.Related.RuntimeType == type).ToDictionary(a=>a.Related));

            double[] bright = dic.Count < 18 ? new double[]{.60}:
                            dic.Count < 72 ? new double[]{.80, .40}:
                            new double[]{.90, .50, .30};

            var hues = (dic.Count / bright.Length);

            var hueStep = 360 / hues;

            var values = dic.Values.ToList();

            for (int i = 0; i < bright.Length; i++)
            {
                for (int h = 0; h < hues; h++)
                {
                    values[i * hues + h].Color = new ColorDN { Argb = ColorExtensions.FromHsv(h * hueStep, .8, bright[i]).ToArgb() };
                }
            }

            values.SaveList();
        }


        static readonly GenericInvoker<Func<int>> giCount = new GenericInvoker<Func<int>>(() => Count<IdentifiableEntity>());
        static int Count<T>() where T : IdentifiableEntity
        {
            return Database.Query<T>().Count(); 
        }
    }
}
