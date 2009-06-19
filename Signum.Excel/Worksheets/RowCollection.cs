namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Linq.Expressions;
    using System.Linq;

    public sealed class RowCollection : OffsetCollection<Row>
    {
        internal int PreWrite()
        {
            if (this.Count() == 0)
                return 0;

            return this.Max(r => r.Cells.UpdateIndices());
        }
    }
}

