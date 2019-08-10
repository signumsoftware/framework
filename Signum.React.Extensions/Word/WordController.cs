using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Signum.Entities;
using Signum.React.Facades;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities.Word;
using Signum.Engine.Word;
using Signum.React.Files;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Signum.React.Filters;
using Signum.Engine.Templating;
using System;
using Signum.Entities.DynamicQuery;

namespace Signum.React.Word
{
    [ValidateModelFilter]
    public class WordController : ControllerBase
    {
        [HttpPost("api/word/createReport")]
        public FileStreamResult CreateReport([Required, FromBody]CreateWordReportRequest request)
        {
            var template = request.template.RetrieveAndRemember();
            var modifiableEntity = request.entity ?? request.lite.RetrieveAndRemember();

            var file = template.CreateReportFileContent(modifiableEntity);

            return FilesController.GetFileStreamResult(file);
        }

        public class CreateWordReportRequest
        {
            public Lite<WordTemplateEntity> template { get; set; }
            public Lite<Entity> lite { get; set; }
            public ModifiableEntity entity { get; set; }
        }

        [HttpPost("api/word/constructorType")]
        public string GetConstructorType([Required, FromBody]WordModelEntity wordModel)
        {
            var type = WordModelLogic.GetEntityType(wordModel.ToType());

            return ReflectionServer.GetTypeName(type);
        }

        [HttpPost("api/word/wordTemplates")]
        public List<Lite<WordTemplateEntity>> GetWordTemplates(string queryKey, WordTemplateVisibleOn visibleOn, [Required, FromBody]GetWordTemplatesRequest request)
        {
            object type = QueryLogic.ToQueryName(queryKey);

            var entity = request.lite?.RetrieveAndForget();

            return WordTemplateLogic.GetApplicableWordTemplates(type, entity, visibleOn);
        }

        public class GetWordTemplatesRequest
        {
            public Lite<Entity> lite { get; set; }
        }
    }
}
