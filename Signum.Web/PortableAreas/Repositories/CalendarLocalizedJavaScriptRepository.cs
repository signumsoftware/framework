using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Signum.Utilities;
using System.Web.Hosting;
using System.Globalization;
using System.Text;
using System.Collections;
using System.Resources;
using System.Collections.Concurrent;
using System.Web;
using System.Web.Mvc;
using Signum.Web.Properties;
using System.Web.Script.Serialization;
using Signum.Entities;

namespace Signum.Web.PortableAreas
{
    public class CalendarLocalizedJavaScriptRepository : IFileRepository
    {
        public readonly string VirtualPathPrefix;
        
        readonly ConcurrentDictionary<CultureInfo, StaticContentResult> cachedFiles = new ConcurrentDictionary<CultureInfo, StaticContentResult>();

        public CalendarLocalizedJavaScriptRepository(string virtualPathPrefix)
        {
            if (string.IsNullOrEmpty(virtualPathPrefix))
                throw new ArgumentNullException("virtualPath");

            this.VirtualPathPrefix = virtualPathPrefix.ToLower();
        }

        public ActionResult GetFile(string file)
        {
            CultureInfo culture = GetCultureInfo(file);

            if (culture == null)
                return null;

            return this.cachedFiles.GetOrAdd(culture, ci => new StaticContentResult(CreateFile(ci), file));
        }

        byte[] CreateFile(CultureInfo ci)
        {
            using(Sync.ChangeBothCultures(ci))
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    var config = new
                    {
                        closeText = CalendarMessage.CalendarClose.NiceToString(),
                        prevText = CalendarMessage.CalendarPrevious.NiceToString(),
                        nextText = CalendarMessage.CalendarNext.NiceToString(),
                        currentText = CalendarMessage.CalendarToday.NiceToString(),
                        monthNames = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames,
                        monthNamesShort = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames,
                        dayNames = CultureInfo.CurrentCulture.DateTimeFormat.DayNames,
                        dayNamesShort = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames,
                        dayNamesMin = CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames,
                        dateFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern,
                        firstDay = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek
                    };

                    sw.WriteLine("$.datepicker.regional['{0}'] = {1};".Formato(
                        DatePickerOptions.DefaultCulture,
                        new JavaScriptSerializer().Serialize(config)));

                    sw.WriteLine("$.datepicker.setDefaults($.datepicker.regional['" + DatePickerOptions.DefaultCulture + "']);");

                    sw.WriteLine("var SF = SF || {}; SF.Locale = SF.Locale || {};");
                    sw.WriteLine("SF.Locale.defaultDatepickerOptions = {0};".Formato(new DatePickerOptions() { Format = "g" }.ToString()));
                }

                return ms.ToArray();
            }
        }
    
        public bool FileExists(string file)
        {
            return GetCultureInfo(file) != null;
        }

        CultureInfo GetCultureInfo(string virtualPath)
        {
            if (!virtualPath.StartsWith(VirtualPathPrefix, StringComparison.InvariantCultureIgnoreCase))
                return null;

            var fileName = virtualPath.Substring(VirtualPathPrefix.Length);

            if (Path.GetExtension(fileName) != ".js")
                return null;

            try
            {
                return CultureInfo.GetCultureInfo(Path.GetFileNameWithoutExtension(fileName));
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return "CalendarLocalizedJavaScript {0}".Formato(VirtualPathPrefix);
        }
    }
}