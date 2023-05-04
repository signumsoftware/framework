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
        Assert.Equal("return \"Hi {0}\".formatWith((e.text??\"\".length>0 ? (\":\" + fd.valToString(e.text)) : \"\"))",
            LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => $"Hi {(a.Text.HasText() ? ":" + a.Text : null)}", assert: false));

        Assert.Equal("return fd.New(\"FooModel\", {\nname: null,\n})",
            LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => new FooModel { Name = null }, assert: false));

        Assert.Equal("return \"{0}\".formatWith(fd.dateToString(e.creationDate, 'DateOnly'))",
            LambdaToJavascriptConverter.ToJavascript((NoteWithDateEntity a) => $"{a.CreationDate}", assert: false));
    }

    public class FooModel : ModelEntity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Name { get; set; }
    }

}
