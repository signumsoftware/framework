using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.ViewLog
{
    [Serializable]
    public class ViewLogConfigurationEntity : EmbeddedEntity
    {

        bool active;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value); }
        }

        bool queryUrlLog;
        public bool QueryUrlLog
        {
            get { return queryUrlLog; }
            set { queryUrlLog = value; }
        }

        bool queryTextLog;
        public bool QueryTextLog
        {
            get { return queryTextLog; }
            set { queryTextLog = value; }
        }

        [NotNullable, PreserveOrder]
        MList<TypeEntity> typeList = new MList<TypeEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<TypeEntity> TypeList
        {
            get { return typeList; }
            set {
                if (Set(ref typeList, value))
                    types = null;
                }
        }

        [Ignore]
        HashSet<Type> types = null;
        public HashSet<Type> Types
        {
            get {

                if (types == null)
                    types = TypeList.Select(t => t.ToType()).ToHashSet();

                return types;
            }
           
        }


    }
}
