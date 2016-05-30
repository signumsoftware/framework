using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web
{
    public interface IViewOverrides
    {
        List<Tab> ExpandTabs(List<Tab> tabs, string containerId, HtmlHelper helper, TypeContext context);
        MvcHtmlString OnSurroundLine(PropertyRoute propertyRoute, HtmlHelper helper, TypeContext tc, MvcHtmlString result);
        bool IsVisible(PropertyRoute propertyRoute);
    }

    public class ViewOverrides<T> : IViewOverrides where T : IRootEntity
    {
        public Dictionary<string, Func<HtmlHelper, TypeContext, Tab>> BeforeTabDictionary;
        public ViewOverrides<T> BeforeTab(string id, Func<HtmlHelper, TypeContext<T>, Tab> constructor)
        {
            if (BeforeTabDictionary == null)
                BeforeTabDictionary = new Dictionary<string, Func<HtmlHelper, TypeContext, Tab>>();

            BeforeTabDictionary[id] = BeforeTabDictionary.TryGetC(id) + new Func<HtmlHelper, TypeContext, Tab>((html, tc) => constructor(html, (TypeContext<T>)tc));

            return this;
        }

        public Dictionary<string, Func<HtmlHelper, TypeContext, Tab>> AfterTabDictionary;
        public ViewOverrides<T> AfterTab(string id, Func<HtmlHelper, TypeContext, Tab> constructor)
        {
            if (AfterTabDictionary == null)
                AfterTabDictionary = new Dictionary<string, Func<HtmlHelper, TypeContext, Tab>>();

            AfterTabDictionary[id] = AfterTabDictionary.TryGetC(id) + new Func<HtmlHelper, TypeContext, Tab>((html, tc) => constructor(html, (TypeContext<T>)tc));

            return this;
        }

        HashSet<string> hiddenTabs;

        public ViewOverrides<T> HideTab(string id)
        {
            if (hiddenTabs == null)
                hiddenTabs = new HashSet<string>();
            hiddenTabs.Add(id);

            return this;
        }

        List<Tab> IViewOverrides.ExpandTabs(List<Tab> tabs, string containerId, HtmlHelper helper, TypeContext context)
        {
            if (hiddenTabs != null && hiddenTabs.Contains(containerId))
                return null;

            List<Tab> newTabs = new List<Tab>();

            var before = BeforeTabDictionary?.TryGetC(containerId);
            if (before != null)
                foreach (var b in before.GetInvocationListTyped())
                {
                    var newTab = b(helper, context);
                    if (newTab != null)
                        ExpandTab(newTab, helper, context, newTabs);
                }

            foreach (var item in tabs)
                ExpandTab(item, helper, context, newTabs);

            var after = AfterTabDictionary?.TryGetC(containerId);
            if (after != null)
                foreach (var a in after.GetInvocationListTyped())
                {
                    var newTab = a(helper, context);
                    if (newTab != null)
                        ExpandTab(newTab, helper, context, newTabs);
                }

            return newTabs;
        }

        void ExpandTab(Tab item, HtmlHelper helper, TypeContext context, List<Tab> newTabs)
        {
            var before = BeforeTabDictionary?.TryGetC(item.Id);
            if (before != null)
                foreach (var b in before.GetInvocationListTyped())
                {
                    var newTab = b(helper, context);
                    if (newTab != null)
                        ExpandTab(newTab, helper, context, newTabs);
                }

            if (hiddenTabs == null || !hiddenTabs.Contains(item.Id))
                newTabs.Add(item);

            var after = AfterTabDictionary?.TryGetC(item.Id);
            if (after != null)
                foreach (var a in after.GetInvocationListTyped())
                {
                    var newTab = a(helper, context);
                    if (newTab != null)
                        ExpandTab(newTab, helper, context, newTabs);
                }
        }

        Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>> beforeLine;
        public ViewOverrides<T> BeforeLine<S>(Expression<Func<T, S>> propertyRoute, Func<HtmlHelper, TypeContext<T>, MvcHtmlString> constructor)
        {
            return BeforeLine(PropertyRoute.Construct(propertyRoute), (helper, tc) => constructor(helper, (TypeContext<T>)tc));
        }

        public ViewOverrides<T> BeforeLine(PropertyRoute propertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)
        {
            if (beforeLine == null)
                beforeLine = new Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>>();

            beforeLine[propertyRoute] = beforeLine.TryGetC(propertyRoute) + constructor;

            return this;
        }


        Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>> afterLine;
        public ViewOverrides<T> AfterLine<S>(Expression<Func<T, S>> propertyRoute, Func<HtmlHelper, TypeContext<T>, MvcHtmlString> constructor)
        {
            return AfterLine(PropertyRoute.Construct(propertyRoute), (helper, tc) => constructor(helper, (TypeContext<T>)tc));
        }

        public ViewOverrides<T> AfterLine(PropertyRoute propertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)
        {
            if (afterLine == null)
                afterLine = new Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>>();

            afterLine[propertyRoute] = afterLine.TryGetC(propertyRoute) + constructor;
            
            return this;
        }


        MvcHtmlString IViewOverrides.OnSurroundLine(PropertyRoute propertyRoute, HtmlHelper helper, TypeContext tc, MvcHtmlString result)
        {
            var before = beforeLine?.TryGetC(propertyRoute);
            if (before != null)
                foreach (var b in before.GetInvocationListTyped())
                    result = b(helper, tc).Concat(result);

            var after = afterLine?.TryGetC(propertyRoute);
            if (after != null)
                foreach (var a in after.GetInvocationListTyped())
                    result = result.Concat(a(helper, tc));

            return result;
        }

        public ViewOverrides<T> HideLine<S>(Expression<Func<T, S>> propertyRoute)
        {
            return HideLine(PropertyRoute.Construct(propertyRoute));
        }

        HashSet<PropertyRoute> hiddenLines;
        public ViewOverrides<T> HideLine(PropertyRoute propertyRoute)
        {
            if (hiddenLines == null)
                hiddenLines = new HashSet<PropertyRoute>();

            hiddenLines.Add(propertyRoute);

            return this;
        }

        bool IViewOverrides.IsVisible(PropertyRoute propertyRoute)
        {
            return hiddenLines == null || !hiddenLines.Contains(propertyRoute);
        }
    }
}