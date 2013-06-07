using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Data;
using System.ComponentModel;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows
{
    [MarkupExtensionReturnType(typeof(Pagination))]
    public class AllResultsExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Pagination.AllElements();
        }
    }

    [MarkupExtensionReturnType(typeof(Pagination))]
    [DefaultProperty("TopElements")]
    public class TopExtension : MarkupExtension
    {
        public int TopElements {get;set;}

        public TopExtension(int topElements)
        {
            this.TopElements = topElements;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Pagination.Top(TopElements);
        }
    }

    [MarkupExtensionReturnType(typeof(Pagination))]
    [DefaultProperty("ElementsPerPage")]
    public class PaginateExtension : MarkupExtension
    {
        public int ElementsPerPage { get; set; }

        public PaginateExtension(int elementsPerPage)
        {
            this.ElementsPerPage = elementsPerPage;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Pagination.Paginate(ElementsPerPage, 1);
        }
    }


}
