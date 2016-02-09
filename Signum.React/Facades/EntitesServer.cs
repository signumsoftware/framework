using Signum.Engine.Operations;
using Signum.Entities;
using Signum.React.ApiControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Facades
{
    public static class EntityServer
    {
        public static EntityPackTS GetEntityPack(Entity entity)
        {
            var canExecutes = OperationLogic.ServiceCanExecute(entity);

            return new EntityPackTS
            {
                entity = entity,
                canExecute = canExecutes.ToDictionary(a => a.Key.Key, a => a.Value)
            };
        }
    }

    public class EntityPackTS
    {
        public Entity entity;
        public Dictionary<string, string> canExecute;
    }
}