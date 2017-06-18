using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using Signum.Entities.Profiler;
using System.Drawing;
using Signum.Entities.Reflection;
using System.Xml.Linq;
using System.IO;
using Signum.React.Files;
using System.Threading.Tasks;
using Signum.Utilities.DataStructures;

namespace Signum.React.Profiler
{
    public class ProfilerHeavyController : ApiController
    {
        [Route("api/profilerHeavy/clear"), HttpPost]
        public void Clear()
        {
            ProfilerPermission.ViewHeavyProfiler.AssertAuthorized();

            HeavyProfiler.Clean();
        }

        [Route("api/profilerHeavy/setEnabled/{isEnabled}"), HttpPost]
        public void SetEnabled(bool isEnabled)
        {
            ProfilerPermission.ViewHeavyProfiler.AssertAuthorized();

            HeavyProfiler.Enabled = isEnabled;
        }

        [Route("api/profilerHeavy/isEnabled"), HttpGet]
        public bool IsEnabled()
        {
            ProfilerPermission.ViewHeavyProfiler.AssertAuthorized();

            return HeavyProfiler.Enabled;
        }

        [Route("api/profilerHeavy/entries"), HttpGet]
        public List<HeavyProfofilerEntryTS> Entries()
        {
            ProfilerPermission.ViewHeavyProfiler.AssertAuthorized();

            lock (HeavyProfiler.Entries)
                return HeavyProfiler.Entries.Select(e => new HeavyProfofilerEntryTS(e, false)).ToList();
        }

        [Route("api/profilerHeavy/details/{fullIndex}"), HttpGet]
        public List<HeavyProfofilerEntryTS> Details(string fullIndex)
        {
            ProfilerPermission.ViewHeavyProfiler.AssertAuthorized();

            var entry = HeavyProfiler.Find(fullIndex);

            var result = new List<HeavyProfofilerEntryTS>();

            HeavyProfofilerEntryTS.Fill(result, entry, 0);
   
            return result;
        }

        [Route("api/profilerHeavy/stackTrace/{fullIndex}"), HttpGet]
        public List<StackTraceTS> StackTrace(string fullIndex)
        {
            ProfilerPermission.ViewHeavyProfiler.AssertAuthorized();

            var e = HeavyProfiler.Find(fullIndex);

            if (e == null || e.StackTrace == null)
                return null;


            return (from i in 0.To(e.StackTrace.FrameCount)
                    let sf = e.StackTrace.GetFrame(i)
                    let mi = sf.GetMethod()
                    let t = mi.DeclaringType
                    select new StackTraceTS
                    {
                        Namespace = t?.Namespace,
                        Type = t?.TypeName(),
                        Color = t == null ? null : ColorExtensions.ToHtmlColor(t.Assembly.FullName.GetHashCode()),
                        Method = mi.Name,
                        FileName = sf.GetFileName(),
                        LineNumber = sf.GetFileLineNumber(),
                    }).ToList();
        }

        [Route("api/profilerHeavy/download"), HttpGet]
        public HttpResponseMessage Download(string indices = null)
        {
            XDocument doc = indices == null ?
             HeavyProfiler.ExportXml() :
             HeavyProfiler.Find(indices).ExportXmlDocument();

            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms);

                string fileName = "Profile-{0}.xml".FormatWith(DateTime.Now.ToString("o").Replace(":", "."));

                return FilesController.GetHttpReponseMessage(new MemoryStream(ms.ToArray()), fileName);
            }
        }

        [Route("api/profilerHeavy/upload"), HttpPost]
        public void Upload(FileUpload file)
        {
            using (MemoryStream sr = new MemoryStream(file.content))
            {
                var doc = XDocument.Load(sr);
                HeavyProfiler.ImportXml(doc, rebaseTime: true);
            }
        }

        public class FileUpload
        {
            public string fileName;
            public byte[] content;
        }



        private static string GetColor(string role)
        {
            if (role == null)
                return Color.Gray.ToHtml();

            if (RoleColors.TryGetValue(role, out Color color))
                return color.ToHtml();

            return ColorExtensions.ToHtmlColor(StringHashEncoder.GetHashCode32(role));
        }

        public static Dictionary<string, Color> RoleColors = new Dictionary<string, Color>
        {
            { "SQL", Color.Gold },
            { "DB", Color.MediumSlateBlue },
            { "LINQ", Color.Violet },
            { "MvcRequest", Color.LimeGreen },
            { "MvcResult", Color.SeaGreen }
        };


        public class HeavyProfofilerEntryTS
        {
            public long BeforeStart;
            public long Start;
            public long End;
            public string Elapsed;
            public string Role;
            public string Color;
            public int Depth;
            public int AsyncDepth;
            public string AdditionalData;
            public string FullIndex; 

            public HeavyProfofilerEntryTS(HeavyProfilerEntry e, bool fullAditionalData)
            {
                BeforeStart = e.BeforeStart;
                Start = e.Start;
                End = e.End;
                Elapsed = e.ElapsedToString();
                Role = e.Role;
                Color = GetColor(e.Role);
                Depth = e.Depth;
                FullIndex = e.FullIndex();
                AdditionalData = fullAditionalData ? e.AdditionalData : e.AdditionalDataPreview();
            }

            internal static int Fill(List<HeavyProfofilerEntryTS> result, HeavyProfilerEntry entry, int asyncDepth)
            {
                result.Add(new HeavyProfofilerEntryTS(entry, true) { AsyncDepth = asyncDepth });

                if (entry.Entries == null)
                    return asyncDepth;
                
            
                Dictionary<HeavyProfilerEntry, int> newDepths = new Dictionary<HeavyProfilerEntry, int>();
                foreach (var e in entry.Entries)
                {
                    var maxAsyncDepth = newDepths.Where(kvp => kvp.Key.Overlaps(e)).Max(a => (int?)a.Value);

                    var newAsyncDepth = Fill(result, e, maxAsyncDepth.HasValue ? maxAsyncDepth.Value + 1 : asyncDepth + 1);
                    newDepths.Add(e, newAsyncDepth);
                }

                return newDepths.Values.Max();
            }
        }

        public class StackTraceTS
        {
            public string Color;
            public string Namespace;
            public string Type;
            public string Method;
            public string FileName;
            public int LineNumber;

        }
    }
}