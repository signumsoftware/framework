using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.API;
using Signum.API.Filters;

namespace Signum.Word;

[ValidateModelFilter]
public class WordController : ControllerBase
{
    [HttpPost("api/word/createReport")]
    public FileStreamResult CreateReport([Required, FromBody]CreateWordReportRequest request)
    {
        var template = request.Template.RetrieveAndRemember();
        var modifiableEntity = request.Entity ?? request.Lite!.RetrieveAndRemember();

        var file = template.CreateReportFileContent(modifiableEntity);

        return MimeMapping.GetFileStreamResult(file);
    }

    public class CreateWordReportRequest
    {
        public Lite<WordTemplateEntity> Template { get; set; }
        public Lite<Entity>? Lite { get; set; }
        public ModifiableEntity? Entity { get; set; }
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

        var entity = request.Lite?.Retrieve();

        return WordTemplateLogic.GetApplicableWordTemplates(type, entity, visibleOn);
    }

    public class GetWordTemplatesRequest
    {
        public Lite<Entity>? Lite { get; set; }
    }
}
