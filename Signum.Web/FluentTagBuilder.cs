using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Signum.Web
{
    public class FluentTagBuilder
    {
        TagBuilder tagBuilder;
        public TagBuilder TagBuilder
        {
            get { return tagBuilder; }
        }

        public FluentTagBuilder(string tagName)
        {
            tagBuilder = new TagBuilder(tagName);
        }

        public FluentTagBuilder(string tagName, string id)
        {
            tagBuilder = new TagBuilder(tagName);

            if (!string.IsNullOrEmpty(id))
                tagBuilder.GenerateId(id);
        }

        public FluentTagBuilder(TagBuilder tagBuilder)
        {
            this.tagBuilder = tagBuilder;
        }

        public FluentTagBuilder AddCssClass(string value)
        {
            if (!string.IsNullOrEmpty(value))
                tagBuilder.AddCssClass(value);
            return this;
        }

        public FluentTagBuilder GenerateId(string name)
        {
            tagBuilder.GenerateId(name);
            return this;
        }

        public FluentTagBuilder MergeAttribute(string key, string value)
        {
            tagBuilder.MergeAttribute(key, value);
            return this;
        }

        public FluentTagBuilder MergeAttribute(string key, string value, bool replaceExisting)
        {
            tagBuilder.MergeAttribute(key, value, replaceExisting);
            return this;
        }

        public FluentTagBuilder MergeAttributes(object attributes)
        {
            tagBuilder.MergeAttributes(new RouteValueDictionary(attributes));
            return this;
        }

        public FluentTagBuilder MergeAttributes(object attributes, bool replaceExisting)
        {
            tagBuilder.MergeAttributes(new RouteValueDictionary(attributes), replaceExisting);
            return this;
        }

        public FluentTagBuilder MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes)
        {
            tagBuilder.MergeAttributes(attributes);
            return this;
        }

        public FluentTagBuilder MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting)
        {
            tagBuilder.MergeAttributes(attributes, replaceExisting);
            return this;

        }

        public FluentTagBuilder SetInnerText(string innerText)
        {
            tagBuilder.SetInnerText(innerText);
            return this;
        }

        public FluentTagBuilder InnerHtml(string html)
        {
            tagBuilder.InnerHtml = html;
            return this;
        }

        public string ToString(TagRenderMode renderMode)
        {
            return tagBuilder.ToString(renderMode);
        }
    }
}
