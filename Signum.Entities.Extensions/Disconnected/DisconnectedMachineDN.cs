using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.Disconnected
{
    [Serializable]
    public class DisconnectedMachineDN : Entity
    {
        DateTime creationDate = DateTime.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value, () => CreationDate); }
        }

        string machineName;
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value, () => MachineName); }
        }

        bool isOffline;
        public bool IsOffline
        {
            get { return isOffline; }
            set { Set(ref isOffline, value, () => IsOffline); }
        }

        int seedMin;
        public int SeedMin
        {
            get { return seedMin; }
            set { Set(ref seedMin, value, () => SeedMin); }
        }

        int seedMax;
        public int SeedMax
        {
            get { return seedMax; }
            set { Set(ref seedMax, value, () => SeedMax); }
        }

        UserDN user;
        [NotNullValidator]
        public UserDN User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        static Expression<Func<DisconnectedMachineDN, string>> ToStringExpression = e => e.machineName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public interface IDisconnectedEntity : IIdentifiable
    {
        long Ticks { get; set; }
        long? LastOnlineTicks { get; set; }
        Lite<DisconnectedMachineDN> DisconnectedMachine { get; set; }
    }

    public enum DownloadStrategy
    {
        None,
        NoneUploadNew,
        All,
        AllUploadNew,
        Subset,
        SubsetUploadNew,
        SubsetUploadSubset,
    }
}
