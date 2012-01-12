using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Deployment
{
    [Serializable]
    public class DeploymentLogDN : Entity
    {
        DateTime creationDate;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value, () => CreationDate); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string version;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Version
        {
            get { return version; }
            set { Set(ref version, value, () => Version); }
        }

        [NotNullable, SqlDbType(Size = 500)]
        string description;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 500)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value, () => Description); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string machineName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value, () => MachineName); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string databaseName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DatabaseName
        {
            get { return databaseName; }
            set { Set(ref databaseName, value, () => DatabaseName); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string dataSourceName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DataSourceName
        {
            get { return dataSourceName; }
            set { Set(ref dataSourceName, value, () => DataSourceName); }
        }
    }  

}
