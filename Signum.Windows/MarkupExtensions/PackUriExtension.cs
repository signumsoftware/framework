using System;
using System.Windows.Markup;
using Signum.Utilities;

namespace Signum.Windows
{
    [MarkupExtensionReturnType(typeof(Uri))]
    public class PackUriExtension : MarkupExtension
    {
        public Location Location { get; set; }

        [ConstructorArgument("path")]
        public string Path { get; set; }

        string assemblyName;
        public string AssemblyName
        {
            get { return assemblyName; }
            set
            {
                assemblyName = value;
                Location = Location.ReferencedAssembly;
            }
        }

        public PackUriExtension()
        {
            Location = Location.LocalAssembly;
        }
        public PackUriExtension(string path)
        {
            this.Path = path;
            Location = Location.LocalAssembly;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            switch (Location)
            {
                case Location.LocalAssembly: return PackUriHelper.Local(Path);
                case Location.ReferencedAssembly: return PackUriHelper.Reference(Path, AssemblyName);
                case Location.SiteOfOrigin: return PackUriHelper.SiteOfOrigin(Path);
                default: throw new NotSupportedException();
            }
        }
    }

    public enum Location
    {
        // For files in the local assembly
        // eg Build Action = Page | Resource | Content
        LocalAssembly,

        // For files in the referenced assembly
        // eg Build Action = Page | Resource
        ReferencedAssembly,

        // For files at the site of origin
        // eg Build Action = None
        SiteOfOrigin
    }

    public static class PackUriHelper
    {
        public static Uri Local(string path)
        {
            return new Uri("pack://application:,,,/{0}".FormatWith(path), UriKind.Absolute);
        }

        public static Uri Reference(string path, Type assemblyTypeExample)
        {
            return Reference(path, assemblyTypeExample.Assembly.GetName().Name);
        }

        public static Uri Reference(string path, string assemblyName)
        {
            return new Uri("pack://application:,,,/{0};component/{1}".FormatWith(assemblyName, path), UriKind.Absolute);
        }

        public static Uri SiteOfOrigin(string path)
        {
            return new Uri("pack://siteoforigin:,,,/{0}".FormatWith(path), UriKind.Absolute);
        }
    }
}