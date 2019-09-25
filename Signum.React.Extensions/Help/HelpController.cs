using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Signum.React.Filters;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.Engine.Help;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.Help;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Globalization;
using System;

namespace Signum.React.Help
{
    [ValidateModelFilter]
    public class HelpController : ControllerBase
    {
        [HttpGet("api/help/index")]
        public HelpIndexTS Index()
        {
            return new HelpIndexTS
            {
                Namespaces = HelpLogic.GetNamespaceHelps().Select(s => new NamespaceItemTS
                {
                    Namespace = s.Namespace,
                    Before = s.Before,
                    Title = s.Title,
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
            if (!uniqueName.HasText())
                return new AppendixHelpEntity
                {
                    Culture = CultureInfo.CurrentCulture.ToCultureInfoEntity() 
                };

            var help = HelpLogic.GetAppendixHelp(uniqueName);
            return help;
        }

        [HttpGet("api/help/type/{cleanName}")]
        public TypeHelpEntity Type(string cleanName)
        {
            var help = HelpLogic.GetTypeHelp(TypeLogic.GetType(cleanName));
            help.AssertAllowed();
            return help.GetEntity();
        }

        [HttpPost("api/help/saveType")]
        public void SaveType([Required][FromBody]TypeHelpEntity entity)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();
            using (Transaction tr = new Transaction())
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
                var hiddenProperties = entity.IsNew ? Enumerable.Empty<PropertyRouteHelpEmbedded>() : entity.ToLite().RetrieveAndForget().Properties
                    .Where(p => !currentProperties.Contains(p.Property.ToPropertyRoute()))
                    .ToList();
                entity.Properties.AddRange(hiddenProperties);
                entity.Properties.RemoveAll(a => !a.Description.HasText());

                var currentOperations = entity.Operations.Select(p => p.Operation).ToHashSet();
                var hiddenOperations = entity.IsNew ? Enumerable.Empty<OperationHelpEmbedded>() : entity.ToLite().RetrieveAndForget().Operations
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
    }

    public class HelpIndexTS
    {
        public List<NamespaceItemTS> Namespaces;
        public List<AppendiceItemTS> Appendices;
    }

    public class NamespaceItemTS
    {
        public string Namespace;
        public string? Before;
        public string Title;
        public EntityItem[] AllowedTypes;
    }

    public class AppendiceItemTS
    {
        public string UniqueName;
        public string Title;
    }
}
