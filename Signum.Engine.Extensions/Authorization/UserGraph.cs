using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Entities.Authorization;

namespace Signum.Engine.Authorization
{
    public class UserGraph : Graph<UserDN, UserState>
    {
        public UserGraph()
        {
            this.GetState = u => u.State;
            this.Operations = new List<IGraphOperation>
            {
                new Construct(UserOperation.Create, UserState.Createad)
                {
                    Constructor = args =>new UserDN {State = UserState.Createad}
                },                
                new Goto(UserOperation.SaveNew, UserState.Createad)
                {
                   FromStates = new []{UserState.Createad},
                   Execute = (u,_)=>{},
                   AllowsNew = true,
                   Lazy =false 
                },
                new Goto(UserOperation.Save, UserState.Createad)
                {
                   FromStates = new []{UserState.Createad},
                   Execute = (u,_)=>{},
                   AllowsNew = false,
                   Lazy =false 
                },
                  
                new Goto(UserOperation.Disable, UserState.Disabled)
                {
                   FromStates = new []{UserState.Createad},
                   Execute = (u,_)=>
                   {
                       u.AnulationDate=DateTime.Now;
                       u.State=UserState.Disabled; 
                   },
                   AllowsNew = false ,
                   Lazy =false 
                },
         
                new Goto(UserOperation.Enable, UserState.Createad)
                {
                   FromStates = new []{UserState.Disabled },
                   Execute = (u,_)=>
                   {
                       u.AnulationDate = null;
                       u.State = UserState.Createad; 
                   },
                   AllowsNew = false ,
                   Lazy =true  
                },
            };
        }
    }
}
