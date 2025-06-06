using Signum.API.Json;

namespace Signum.Test;

public class JavascriptConversionTest
{
    public JavascriptConversionTest()
    {
        MusicStarter.StartAndLoad();
    }


    [Fact]
    public void TestToStringsToJavascript()
    {
        var result = LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => $"Hi {(a.Title.HasText() ? ":" + a.Title : null)}", assert: false);
        Assert.Equal("return \"Hi {0}\".formatWith((((e.text??\"\").length>0) ? (\":\" + fd.valToString(e.text)) : \"\"))", result);

        result = LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => new FooModel { Name = null }, assert: false);
        Assert.Equal("return fd.New(\"FooModel\", {\nname: null,\n})", result);

        result = LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => $"{a.CreationDate}", assert: false);
        Assert.Equal("return \"{0}\".formatWith(fd.dateToString(e.creationDate, 'DateOnly'))", result);
    }

    public class FooModel : ModelEntity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Name { get; set; }
    }

}
