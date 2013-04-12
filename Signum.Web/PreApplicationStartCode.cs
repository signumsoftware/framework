using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Web;
using System.Web.Mvc;
using System.Web.Hosting;
using Signum.Web.PortableAreas;
using System.Web.Compilation;
using Signum.Web.Controllers;
using Signum.Entities.DynamicQuery;

[assembly: PreApplicationStartMethod(typeof(Signum.Web.PreApplicationStartCode), "Start")]

namespace Signum.Web
{
    public static class PreApplicationStartCode
    {
        private static bool _startWasCalled;

        public static void Start()
        {
            if (_startWasCalled)
            {
                return;
            }
            _startWasCalled = true;

            //be sure default buildproviders are registered first
            System.Web.WebPages.PreApplicationStartCode.Start();
            System.Web.WebPages.Razor.PreApplicationStartCode.Start();

            ControllerBuilder.Current.SetControllerFactory(new SignumControllerFactory());
            HostingEnvironment.RegisterVirtualPathProvider(new CompiledVirtualPathProvider(HostingEnvironment.VirtualPathProvider));
            BuildProvider.RegisterBuildProvider(".cshtml", typeof(CompiledRazorBuildProvider));

            ModelBinders.Binders.DefaultBinder = new LiteModelBinder();
            ModelBinders.Binders.Add(typeof(DateTime), new CurrentCultureDateModelBinder());
            ModelBinders.Binders.Add(typeof(DateTime?), new CurrentCultureDateModelBinder());
            ModelBinders.Binders.Add(typeof(FindOptions), new FindOptionsModelBinder());
            ModelBinders.Binders.Add(typeof(QueryRequest), new QueryRequestModelBinder());
        }
    }

}