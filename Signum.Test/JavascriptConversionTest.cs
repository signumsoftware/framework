using Signum.Engine.DynamicQuery;
using Signum.Engine.Json;
using Signum.Entities.DynamicQuery;

namespace Signum.Test;

public class JavascriptConversionTest
{
    [Fact]
    public void TestToStringsToJavascript()
    {
        Assert.Equal("return \"Hi {0}\".formatWith((e.text??\"\".length>0 ? (\":\" + fd.valToString(e.text)) : \"\"))",
            LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => $"Hi {(a.Text.HasText() ? ":" + a.Text : null)}", assert: false));

        Assert.Equal("return \"{0}\".formatWith(fd.dateToString(e.creationDate, 'DateOnly'))",
            LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => $"{a.CreationDate}", assert: false));
    }

}
