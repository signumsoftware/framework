using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class JsFindOptions : JsRenderer
    {
        public JsValue<string> Prefix { get; set; }
        public FindOptions FindOptions { get; set; }
        public JsValue<int?> ElementsPerPage { get; set; }
        /// <summary>
        /// To be called to open a Find window
        /// </summary>
        public JsValue<string> NavigatorControllerUrl { get; set; }
        /// <summary>
        /// To be called when clicking the search button
        /// </summary>
        public JsValue<string> SearchControllerUrl { get; set; }
        /// <summary>
        /// To be called when closing the popup (if exists) with the Ok button
        /// </summary>
        public JsFunction OnOk { get; set; }
        public JsFunction OnCancelled { get; set; }
        public JsValue<bool?> Async { get; set; }

        public JsFindOptions()
        {
            Renderer = () =>
            {
                JsOptionsBuilder options = new JsOptionsBuilder(true)
                {
                    {"prefix", Prefix.TryCC(t=>t.ToJS())},
                    {"elems", ElementsPerPage.TryCC(t=>t.ToJS()) },
                    {"navigatorControllerUrl", NavigatorControllerUrl.TryCC(t=>t.ToJS()) ?? RouteHelper.New().SignumAction("PartialFind").SingleQuote() },
                    {"searchControllerUrl",SearchControllerUrl.TryCC(t=>t.ToJS()) ?? RouteHelper.New().SignumAction("Search").SingleQuote() },
                    {"onOk",OnOk.TryCC(t=>t.ToJS())},
                    {"onCancelled", OnCancelled.TryCC(t=>t.ToJS())},
                    {"async", Async.TryCC(t=>t.ToJS())},
                };

                if (FindOptions != null)
                    FindOptions.Fill(options);

                return options.ToJS();
            };
        }
    }

    public class JsFindNavigator : JsInstruction
    {
        public JsFindNavigator(JsFindOptions options)
        {
            Renderer = () => "new SF.FindNavigator({0})".Formato(options.ToJS());
        }

        /// <summary>
        /// To be used when we only need to locate and retrieve values of an already open finder
        /// </summary>
        /// <param name="prefix"></param>
        public JsFindNavigator(JsValue<string> prefix)
            : this(new JsFindOptions { Prefix = prefix })
        {   
        }

        public JsInstruction openFinder()
        {
            return new JsInstruction(() => "{0}.openFinder()".Formato(this.ToJS()));
        }

        public JsInstruction search()
        {
            return new JsInstruction(() => "{0}.search()".Formato(this.ToJS()));
        }

        public JsInstruction selectedItems(JsFindOptions options)
        {
            return new JsInstruction(() => "{0}.selectedItems()".Formato(this.ToJS()));
        }

        public JsInstruction hasSelectedItems(JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.hasSelectedItems({1})".Formato(this.ToJS(), onSuccess.ToJS()));
        }

        public JsInstruction splitSelectedIds()
        {
            return new JsInstruction(() => "{0}.splitSelectedIds()".Formato(this.ToJS()));
        }

        public JsInstruction requestData()
        {
            return new JsInstruction(() => "{0}.requestDataForSearch()".Formato(this.ToJS()));
        }
    }
}
