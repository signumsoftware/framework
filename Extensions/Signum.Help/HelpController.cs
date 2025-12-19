using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Basics;
using Signum.Files;
using Signum.UserAssets;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;

namespace Signum.Help;

[ValidateModelFilter]
public class HelpController : ControllerBase
{
    [HttpGet("api/help/index")]
    public HelpIndexTS Index()
    {
        HelpPermissions.ViewHelp.AssertAuthorized();
        return new HelpIndexTS
        {
            Culture = HelpLogic.GetCulture().ToCultureInfoEntity(),

            Namespaces = HelpLogic.GetNamespaceHelps().Select(s => new NamespaceItemTS
            {
                Namespace = s.Namespace,
                Module = s.Module,
                Title = s.Title,
                HasEntity = s.DBEntity != null,
                AllowedTypes = s.AllowedTypes
            }).ToList(),

            Appendices = HelpLogic.GetAppendixHelps().Select(s => new AppendiceItemTS
            {
                UniqueName = s.UniqueName,
                Title = s.Title,
            }).ToList(),
        };
    }

    [HttpGet("api/help/namespace/{namespace}")]
    public NamespaceHelp Namespace(string @namespace)
    {
        HelpPermissions.ViewHelp.AssertAuthorized();
        var help = HelpLogic.GetNamespaceHelp(@namespace.Replace("_", "."));
        help.AssertAllowed();
        return help;
    }

    [HttpPost("api/help/saveNamespace")]
    public void SaveNamespace([Required][FromBody]NamespaceHelpEntity entity)
    {
        HelpPermissions.ViewHelp.AssertAuthorized();

        if (!entity.Title.HasText() && !entity.Description.HasText())
        {
            if (!entity.IsNew)
                entity.ToLite().DeleteLite(NamespaceHelpOperation.Delete);
        }
        else
            entity.Execute(NamespaceHelpOperation.Save);
    }

    [HttpGet("api/help/appendix/{uniqueName?}")]
    public AppendixHelpEntity Appendix(string? uniqueName)
    {
        HelpPermissions.ViewHelp.AssertAuthorized();

        if (string.IsNullOrWhiteSpace(uniqueName))
            return new AppendixHelpEntity
            {
                Culture = HelpLogic.GetCulture().ToCultureInfoEntity() 
            };

        var help = HelpLogic.GetAppendixHelp(uniqueName);
        return help;
    }

    [HttpPost("api/help/saveAppendix")]
    public void SaveNamespace([Required][FromBody] AppendixHelpEntity entity)
    {
        HelpPermissions.ViewHelp.AssertAuthorized();

        entity.Execute(AppendixHelpOperation.Save);
    }

    [HttpGet("api/help/type/{cleanName}")]
    public TypeHelpEntity Type(string cleanName)
    {
        //HelpPermissions.ViewHelp.AssertAuthorized();
        var type = TypeLogic.GetType(cleanName);
        var help = HelpLogic.GetTypeHelp(type);
        help.AssertAllowed();
        return help.GetEntity();
    }

    [HttpPost("api/help/saveType")]
    public void SaveType([Required][FromBody]TypeHelpEntity entity)
    {
        HelpPermissions.ViewHelp.AssertAuthorized();
        
        using (var tr = new Transaction())
        {
            foreach (var query in entity.Queries)
            {
                query.Columns.RemoveAll(a => !a.Description.HasText());

                if (query.Columns.IsEmpty() && !query.Description.HasText())
                {
                    if (!query.IsNew)
                        query.ToLite().DeleteLite(QueryHelpOperation.Delete);
                }
                else
                    query.Execute(QueryHelpOperation.Save);
            }

            var currentProperties = entity.Properties.Select(p => p.Property.ToPropertyRoute()).ToHashSet();
            var hiddenProperties = entity.IsNew ? Enumerable.Empty<PropertyRouteHelpEmbedded>() : entity.ToLite().Retrieve().Properties
                .Where(p => !currentProperties.Contains(p.Property.ToPropertyRoute()))
                .ToList();
            entity.Properties.AddRange(hiddenProperties);
            entity.Properties.RemoveAll(a => !a.Description.HasText());

            var currentOperations = entity.Operations.Select(p => p.Operation).ToHashSet();
            var hiddenOperations = entity.IsNew ? Enumerable.Empty<OperationHelpEmbedded>() : entity.ToLite().Retrieve().Operations
                .Where(p => !currentOperations.Contains(p.Operation))
                .ToList();
            entity.Operations.AddRange(hiddenOperations);
            entity.Operations.RemoveAll(a => !a.Description.HasText());

            

            if (entity.Properties.IsEmpty() && entity.Operations.IsEmpty() && !entity.Description.HasText())
            {
                if (!entity.IsNew)
                    entity.ToLite().DeleteLite(TypeHelpOperation.Delete);
            }
            else
                entity.Execute(TypeHelpOperation.Save);
            tr.Commit();
        }

    }

    [HttpPost("api/help/export")]
    public FileStreamResult Export([Required, FromBody] Lite<IHelpEntity>[] lites)
    {
        HelpPermissions.ExportHelp.AssertAuthorized();
        
        var bytes = HelpExportImport.ExportToZipBytes([.. lites.RetrieveList()], "Help");

        var typeName = lites.Select(a => a.EntityType).Distinct().SingleEx().ToTypeEntity().CleanName;
        var Ids = lites.ToString(a => a.Id.ToString().Truncate(5), "_");
        var fileName = $"{typeName}{Ids}.zip";

        return MimeMapping.GetFileStreamResult(new MemoryStream(bytes), fileName);
    }

    [HttpPost("api/help/importPreview")]
    public HelpImportPreviewModel ImportPreview([Required, FromBody] FileUpload file)
    {
        return HelpExportImport.ImportPreviewFromZip(file.content);
    }

    [HttpPost("api/help/applyImport")]
    public HelpImportReportModel ApplyImport([Required, FromBody] FileUploadWithModel<HelpImportPreviewModel> fileModel)
    {
        return HelpExportImport.ImportFromZip(fileModel.file.content, fileModel.model);
    }

}

public class HelpIndexTS
{
    public CultureInfoEntity Culture;
    public List<NamespaceItemTS> Namespaces;
    public List<AppendiceItemTS> Appendices;

}

public class NamespaceItemTS
{
    public string Namespace;
    public string? Module;
    public string Title;
    public bool HasEntity;
    public EntityItem[] AllowedTypes;
}

public class AppendiceItemTS
{
    public string UniqueName;
    public string Title;
}
