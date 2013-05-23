using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.UserQueries
{

    [Serializable]
    public class UserAssetPreview
    {
        public Type Type { get; set; }
        public string Text { get; set; }
        public EntityAction Action { get; set; }
        public bool Override { get; set; }
        public Guid Guid { get; set; }

        public bool OverrideVisible
        {
            get { return Action == EntityAction.Different; }
        }
    }

    public enum EntityAction
    {
        Identical,
        Different,
        New,
    }

    public enum UserAssetMessage
    {
        ExportToXml,
        ImportUserAssets,
        ImportPreview,
        SelectTheEntitiesToOverride,
    }

    public enum UserAssetPermission
    {
        UserAssetsToXML,
    }
}
