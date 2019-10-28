using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.DiffLog
{
    public class TimeMachineController : ControllerBase
    {
        [HttpGet("api/retrieveVersion/{typeName}/{id}")]
        public Entity RetrieveVersion(string typeName, string id, DateTime asOf)
        {
            var type = TypeLogic.GetType(typeName);
            var pk = PrimaryKey.Parse(id, type);


            using (SystemTime.Override(asOf.AddMilliseconds(1).ToUniversalTime()))
                return Database.Retrieve(type, pk);
        }

        [HttpGet("api/diffVersions/{typeName}/{id}")]
        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> DiffVersiones(string typeName, string id, DateTime from, DateTime to)
        {
            var type = TypeLogic.GetType(typeName);
            var pk = PrimaryKey.Parse(id, type);


            var f = SystemTime.Override(from.AddMilliseconds(1).ToUniversalTime()).Using(_ => Database.Retrieve(type, pk));
            var t = SystemTime.Override(to.AddMilliseconds(1).ToUniversalTime()).Using(_ => Database.Retrieve(type, pk));

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
