using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.UserAssets;
using Signum.Utilities;

namespace Signum.Engine.UserAssets
{
    public static class UserAssetLogLogic
    {
        static Expression<Func<IUserAssetEntity, IQueryable<UserAssetLogDN>>> UserAssetLogsExpression =
            a => Database.Query<UserAssetLogDN>().Where(log => log.Asset == a);
        public static IQueryable<UserAssetLogDN> UserAssetLogs(this IUserAssetEntity a)
        {
            return UserAssetLogsExpression.Evaluate(a);
        }

        static bool Started; 
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Started = true;
                sb.Include<UserAssetLogDN>();

                dqm.RegisterQuery(typeof(UserAssetLogDN), ()=>
                    from e in Database.Query<UserAssetLogDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Asset,
                        e.User,
                        e.CreationDate
                    });
            }
        }

        public static void Register<T>(SchemaBuilder sb, DynamicQueryManager dqm) where T : IUserAssetEntity
        {
            sb.WhenIncluded<UserAssetLogDN>(() =>
            {
                sb.Settings.AssertImplementedBy((UserAssetLogDN log) => log.Asset, typeof(T));

                dqm.RegisterExpression((T a) => a.UserAssetLogs());
            }); 
        }

        public static void LogUserAsset(this IUserAssetEntity asset)
        {
            if (Started)
                new UserAssetLogDN
                {
                    Asset = asset.ToLite(),
                    User = UserHolder.Current.ToLite(),
                }.Save();
        }
    }
   
}
