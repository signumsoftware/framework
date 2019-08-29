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
                    Guid = s.Guid,
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

        [HttpGet("api/help/appendix/{uniqueName}")]
        public AppendixHelpEntity Appendix(string? uniqueName)
        {
            if (uniqueName == null)
                return new AppendixHelpEntity
                {
                    Culture = CultureInfo.CurrentCulture.ToCultureInfoEntity() 
                };

            var help = HelpLogic.GetAppendixHelp(uniqueName);
            return help;
        }

        [HttpGet("api/help/entity/{cleanName}")]
        public TypeHelpEntity Entity(string cleanName)
        {
            var help = HelpLogic.GetTypeHelp(TypeLogic.GetType(cleanName));
            help.AssertAllowed();
            return help.GetEntity();
        }

        [HttpPost("api/help/saveEntity")]
        public void SaveEntity([Required][FromBody]TypeHelpEntity entity)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var oldProperties = entity.IsNew ? new List<PropertyRouteHelpEmbedded>() : entity.ToLite().RetrieveAndForget().Properties.ToList();

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

            entity.Properties.AddRange(oldProperties.Where(p => !currentProperties.Contains(p.Property.ToPropertyRoute()))); //Hidden properties due to permissions
            entity.Properties.RemoveAll(a => !a.Description.HasText());

            if (entity.Properties.IsEmpty() && !entity.Description.HasText())
            {
                if (!entity.IsNew)
                    entity.ToLite().DeleteLite(TypeHelpOperation.Delete);
            }
            else
                entity.Execute(TypeHelpOperation.Save);
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
        public Guid Guid;
        public string Title;
    }
}
