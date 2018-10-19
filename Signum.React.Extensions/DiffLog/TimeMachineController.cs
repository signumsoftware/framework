using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DiffLog;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DiffLog;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Signum.React.ApiControllers;

namespace Signum.React.DiffLog
{
    public class TimeMachineController : ApiController
    {
        [HttpGet("api/retrieveVersion/{typeName}/{id}")]
        public Entity RetrieveVersion(string typeName, string id, DateTime asOf)
        {
            var type = TypeLogic.GetType(typeName);
            var pk = PrimaryKey.Parse(id, type);


            using (SystemTime.Override(asOf.AddMilliseconds(1)))
                return Database.Retrieve(type, pk);
        }

        [HttpGet("api/diffVersions/{typeName}/{id}")]
        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> DiffVersiones(string typeName, string id, DateTime from, DateTime to)
        {
            var type = TypeLogic.GetType(typeName);
            var pk = PrimaryKey.Parse(id, type);


            var f = SystemTime.Override(from.AddMilliseconds(1)).Using(_ => Database.Retrieve(type, pk));
            var t = SystemTime.Override(to.AddMilliseconds(1)).Using(_ => Database.Retrieve(type, pk));

            var fDump = GetDump(f);
            var tDump = GetDump(t);
            StringDistance sd = new StringDistance();

            return sd.DiffText(fDump, tDump);
        }

        private string GetDump(Entity entity)
        {
            using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
                return entity.Dump();
        }

    }
}