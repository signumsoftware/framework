using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Entities.Omnibox;

namespace Signum.React.Omnibox
{
    public class ReactSpecialOmniboxAction : ISpecialOmniboxAction
    {
        public Func<bool> Allowed { get; set; }

        public string Key { get; set; }
    }

    public class ReactSpecialOmniboxGenerator : SpecialOmniboxGenerator<ReactSpecialOmniboxAction>
    {
        private ReactSpecialOmniboxGenerator()
        {
            
        }

        public static ReactSpecialOmniboxGenerator Singletone { get; } = new ReactSpecialOmniboxGenerator();

        public static void Register(ReactSpecialOmniboxAction action)
        {
            Singletone.Actions.AddOrThrow(action.Key, action, "ReactSpecialOmniboxAction {0} already registered");
        }
    }
}