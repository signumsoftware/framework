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
                new Construct(UserOperation.Crear, UserState.Creado)
                {
                    Constructor = args =>new UserDN {State=UserState.Creado}
                },                
                new Goto(UserOperation.Alta, UserState.Creado)
                {
                   FromStates = new []{UserState.Creado},
                   Execute = (u,_)=>{},
                   AllowsNew = true,
                   Lazy =false 
                },
                new Goto(UserOperation.Modificar, UserState.Creado)
                {
                   FromStates = new []{UserState.Creado},
                   Execute = (u,_)=>{},
                   AllowsNew = false,
                   Lazy =false 
                },
                  
                new Goto(UserOperation.Anular, UserState.Anulado )
                {
                   FromStates = new []{UserState.Creado},
                   Execute = (u,_)=>
                   {
                       u.AnulationDate=DateTime.Now;
                       u.State=UserState.Anulado; 
                   },
                   AllowsNew = false ,
                   Lazy =false 
                },
         
                new Goto(UserOperation.Reactivar, UserState.Creado)
                {
                   FromStates = new []{UserState.Anulado },
                   Execute = (u,_)=>
                   {
                       u.AnulationDate=null ;
                       u.State=UserState.Creado ; 
                   },
                   AllowsNew = false ,
                   Lazy =true  
                },
            };
        }
    }
}
