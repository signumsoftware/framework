using Signum.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.Web
{
    public class EntityListCheckbox : EntityListBase
    {
        public IEnumerable<Lite<IEntity>> Data { get; set; }
        public int? ColumnCount { get; set; }
        public int? ColumnWidth { get; set; }

        public EntityListCheckbox(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            View = false;
            Create = false;
            Remove = false;
            Find = false;
            Move = false;
            ColumnWidth = 300;
        }

        protected override void SetReadOnly()
        {
        }

        public Action<HtmlTag, Lite<IEntity>> CustomizeLabel = null;
        public Action<HtmlTag, Lite<IEntity>> CustomizeCheckBox = null;
    }
}