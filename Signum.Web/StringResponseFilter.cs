using System;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Signum.Web
{
    //internal class CapturingResponseFilter : Stream
    //{
    //    private Stream _sink;
    //    private MemoryStream mem;

    //    public CapturingResponseFilter(Stream sink)
    //    {
    //        _sink = sink;
    //        mem = new MemoryStream();
    //    }

    //    #region Stream overrides
    //    public override bool CanRead
    //    {
    //        get { return true; }
    //    }

    //    public override bool CanSeek
    //    {
    //        get { return false; }
    //    }

    //    public override bool CanWrite
    //    {
    //        get { return false; }
    //    }

    //    public override long Length
    //    {
    //        get { return 0; }
    //    }

    //    public override long Position { get; set; }

    //    public override long Seek(long offset, SeekOrigin direction)
    //    {
    //        return 0;
    //    }

    //    public override void SetLength(long length)
    //    {
    //        _sink.SetLength(length);
    //    }

    //    public override void Close()
    //    {
    //        _sink.Close();
    //        mem.Close();
    //    }

    //    public override void Flush()
    //    {
    //        _sink.Flush();
    //    }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        return _sink.Read(buffer, offset, count);
    //    }

    //    // Override the Write method to filter Response to a file. 
    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        //Here we will not write to the sink b/c we want to capture
    //        //Write out the response to the file.
    //        mem.Write(buffer, 0, count);
    //    } 
    //    #endregion

    //    public string GetContents(Encoding enc)
    //    {
    //        var buffer = new byte[mem.Length];
    //        mem.Position = 0;
    //        mem.Read(buffer, 0, buffer.Length);
    //        return enc.GetString(buffer, 0, buffer.Length);
    //    }
    //}

    public static class RenderPartialExtenders
    {
        public static string RenderPartialToString(this HtmlHelper helper,
                                        string viewName, ViewDataDictionary viewData)
        {
            //Create memory writer
            var sb = new StringBuilder();
            var memWriter = new StringWriter(sb);

            //Create fake http context to render the view
            var fakeResponse = new HttpResponse(memWriter);
            var fakeContext = new HttpContext(HttpContext.Current.Request, fakeResponse);
            var fakeControllerContext = new ControllerContext(
                new HttpContextWrapper(fakeContext),
                helper.ViewContext.RouteData,
                helper.ViewContext.Controller);

            var oldContext = HttpContext.Current;
            HttpContext.Current = fakeContext;

            //Use HtmlHelper to render partial view to fake context
            var html = new HtmlHelper(new ViewContext(fakeControllerContext,
                new FakeView(), new ViewDataDictionary(), new TempDataDictionary(),memWriter),
                new ViewPage());
            html.RenderPartial(viewName, viewData);
            
            //Restore context
            HttpContext.Current = oldContext;

            //Flush memory and return output
            memWriter.Flush();
            return sb.ToString();
        }


        //public static string RenderPartialToString(this HtmlHelper helper, string partialViewName, ViewDataDictionary viewData)
        //{
        //    HttpResponseBase response = helper.ViewContext.HttpContext.Response;
        //    return Capture(
        //        () => helper.RenderPartial(partialViewName, viewData),
        //        response);
        //}

        ////TODO: based on http://www.brightmix.com/blog/how-to-renderpartial-to-string-in-asp-net-mvc/ (MvcContrib)
        //private static string Capture(Action render, HttpResponseBase response)
        //{
        //    Stream originalFilter = response.Filter;
        //    try
        //    {
        //        response.Flush();
        //        CapturingResponseFilter newFilter = new CapturingResponseFilter(originalFilter);
        //        response.Filter = newFilter;
        //        render();
        //        response.Flush();
        //        return newFilter.GetContents(response.ContentEncoding);
        //    }
        //    finally
        //    { 
        //        if(originalFilter != null)
        //            response.Filter = originalFilter;
        //    }
        //}

        public class FakeView : IView
        {
            #region IView Members
            public void Render(ViewContext viewContext, System.IO.TextWriter writer)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}
