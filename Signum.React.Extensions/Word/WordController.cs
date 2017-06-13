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
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using Signum.Entities.Word;
using Signum.Engine.Word;
using System.Web;
using Signum.React.Files;
using System.IO;
using Signum.Entities.Basics;
using Signum.Engine.Maps;

namespace Signum.React.Word
{
    public class WordController : ApiController
    {
        [Route("api/word/createReport"), HttpPost]
        public HttpResponseMessage View(CreateWordReportRequest request)
        {
            var template = request.template.Retrieve();
            var entity = request.entity ?? request.lite.Retrieve();

            byte[] bytes;
            if (template.SystemWordTemplate != null)
            {
                var systemWordTemplate = (ISystemWordTemplate)SystemWordTemplateLogic.GetEntityConstructor(template.SystemWordTemplate.ToType()).Invoke(new[] { entity });
                bytes = request.template.CreateReport(entity: null, systemWordTemplate: systemWordTemplate);
            }
            else
            {
                bytes = request.template.CreateReport((Entity)entity);
            }
            
            return FilesController.GetHttpReponseMessage(new MemoryStream(bytes), template.FileName);            
        }

#pragma warning disable IDE1006 // Naming Styles
        public class CreateWordReportRequest
        {
            public Lite<WordTemplateEntity> template { get; set; }
            public Lite<Entity> lite { get; set; }
            public ModifiableEntity entity { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles

        [Route("api/word/constructorType"), HttpPost]
        public string GetConstructorType(SystemWordTemplateEntity systemWordTemplate)
        {
            var type = SystemWordTemplateLogic.GetEntityType(systemWordTemplate.ToType());

            return ReflectionServer.GetTypeName(type);
        }

        [Route("api/word/wordTemplates"), HttpGet]
        public List<Lite<WordTemplateEntity>> GetWordTemplates(string queryKey, WordTemplateVisibleOn visibleOn)
        {
            object type = QueryLogic.ToQueryName(queryKey);

            var isAllowed = Schema.Current.GetInMemoryFilter<WordTemplateEntity>(userInterface: true);
            return WordTemplateLogic.TemplatesByQueryName.Value.TryGetC(type).EmptyIfNull()
                .Where(a => isAllowed(a) && WordTemplateLogic.IsVisible(a, visibleOn))
                .Select(a => a.ToLite())
                .ToList();
        }
    }
}