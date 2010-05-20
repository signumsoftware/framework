using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Engine.Authorization
{
    public class UserGraph : Graph<UserDN, UserState>
    {
        public UserGraph()
        {
            this.GetState = u => u.State;
            this.Operations = new List<IGraphOperation>
            {
                new Construct(UserOperation.Create, UserState.Created)
                {
                    Constructor = args =>new UserDN {State = UserState.Created}
                },

                new Goto(UserOperation.SaveNew, UserState.Created)
                {
                   FromStates = new []{UserState.Created},
                   Execute = (u,_) => {},
                   AllowsNew = true,
                   Lite = false
                },

                new Goto(UserOperation.Save, UserState.Created)
                {
                   FromStates = new []{UserState.Created},
                   Execute = (u,_) => {},
                   Lite = false
                },

                new Goto(UserOperation.Disable, UserState.Disabled)
                {
                   FromStates = new []{UserState.Created},
                   Execute = (u,_) =>
                   {
                       u.AnulationDate = TimeZoneManager.Now;
                       u.State = UserState.Disabled;
                   },
                   AllowsNew = false,
                   Lite = true
                },

                new Goto(UserOperation.Enable, UserState.Created)
                {
                   FromStates = new []{UserState.Disabled },
                   Execute = (u,_) =>
                   {
                       u.AnulationDate = null;
                       u.State = UserState.Created;
                   },
                   AllowsNew = false,
                   Lite = true
                },
            };
        }
    }
}