using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Resources;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Windows
{
    /// <summary>
    /// Represents a class that manages the localication features.
    /// </summary>
    public static class LocalizationManager
    {
        /// <summary>
        /// The <see cref="ResourceManager"/> by which resources as accessed.
        /// </summary>
        static  Dictionary<Assembly, ResourceManager> dictionary = new Dictionary<Assembly,ResourceManager>();


        /// <summary>
        /// Gets or sets the resource manager to use to access the resources.
        /// </summary>
        public static ResourceManager GetResourceManager(Assembly assembly)
        {
            ResourceManager rm = dictionary.GetOrCreate(assembly, () => LocalizationManager.FindResourceManager(assembly));

            return rm; 
        }

        public static void SetResourceManager(Assembly assembly, ResourceManager resourceManager)
        {
            dictionary[assembly] = resourceManager;
            UpdateLocalizations();
        }
      
        /// <summary>
        /// Gets or sets the current UI culture.
        /// </summary>
        /// <remarks>
        /// This property changes the UI culture of the current thread to the specified value
        /// and updates all localized property to reflect values of the new culture.
        /// </remarks>
        public static CultureInfo UICulture
        {
            get
            {
                return Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                Thread.CurrentThread.CurrentUICulture = value;

                UpdateLocalizations();
            }
        }

        /// <summary>
        /// Holds the list of localization instances.
        /// </summary>
        /// <remarks>
        /// <see cref="WeakReference"/> cannot be used as a localization instance
        /// will be garbage collected on the next garbage collection
        /// as the localizaed object does not hold reference to it.
        /// </remarks>
        static List<LocExtension> _localizations = new List<LocExtension>();

        /// <summary>
        /// Holds the number of localizations added since the last purge of localizations.
        /// </summary>
        static int _localizationPurgeCount;

        /// <summary>
        /// Adds the specified localization instance to the list of manages localization instances.
        /// </summary>
        /// <param name="localization">The localization instance.</param>
        internal static void AddLocalization(LocExtension localization)
        {
            if (localization == null)
            {
                throw new ArgumentNullException("localization");
            }

            if (_localizationPurgeCount > 50)
            {
                // It's really faster to fill a new list instead of removing elements
                // from the existing list when there are a lot of elements to remove.

                var localizatons = new List<LocExtension>(_localizations.Count);

                foreach (var item in _localizations)
                {
                    if (item.IsAlive)
                    {
                        localizatons.Add(item);
                    }
                }

                _localizations = localizatons;

                _localizationPurgeCount = 0;
            }

            _localizations.Add(localization);

            _localizationPurgeCount++;
        }

        /// <summary>
        /// Returns resource manager to access the resources the application's main assembly.
        /// </summary>
        /// <returns></returns>
        static ResourceManager FindResourceManager(Assembly assembly)
        {
            //var assembly = Assembly.GetEntryAssembly();

            //if (assembly == null)
            //{
            //    // Design time

            //    // Try to find the main assembly

            //    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            //    foreach (var item in assemblies)
            //    {
            //        // Check if the assembly is executable

            //        if (item.EntryPoint != null)
            //        {
            //            // Check if the assembly contains WPF application (e.g. MyApplication.App class
            //            // that derives from System.Windows.Application)

            //            var applicationType = item.GetType(item.GetName().Name + ".App", false);

            //            if (applicationType != null && typeof(System.Windows.Application).IsAssignableFrom(applicationType))
            //            {
            //                assembly = item;

            //                break;
            //            }
            //        }
            //    }
            //}

            if (assembly != null)
            {
                try
                {
                    // The resoures cannot be found in the manifest of the assembly

                    return new ResourceManager(assembly.GetName().Name + ".Properties.Resources", assembly);
                }
                catch (MissingManifestResourceException) { }
            }

            return null;
        }

        /// <summary>
        /// Updates the localizations.
        /// </summary>
        static void UpdateLocalizations()
        {
            foreach (var item in _localizations)
            {
                item.UpdateTargetValue();
            }
        }
    }
}
