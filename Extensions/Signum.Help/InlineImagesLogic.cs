using DocumentFormat.OpenXml.Drawing;
using Signum.Files;
using System.Text.RegularExpressions;

namespace Signum.Help;

public static class InlineImagesLogic
{

    [AutoExpressionField]
    public static IQueryable<HelpImageEntity> Images(this IHelpEntity e) =>
        As.Expression(() => Database.Query<HelpImageEntity>().Where(a => a.Target.Is(e)));

    public static Regex ImgRegex = new(@"<img(\s+(?<key>[\w\-]+)\s*=\s*""(?<value>[^""]+)"")+\s*/?>");

    public static bool SynchronizeInlineImages(IHelpEntity entity)
    {
        using (OperationLogic.AllowSave<HelpImageEntity>())
        {
            var toDeleteImages = entity.IsNew ? [] :
                entity.Images().Select(a => new { lite = a.ToLite(), a.Guid }).ToDictionaryEx(a => a.Guid) ;

            List<HelpImageEntity> newImages = [];

            var hasChanges = entity.ForeachHtmlField(text =>
            {
                var newText = ImgRegex.Replace(text, m =>
                {
                    Dictionary<string, string> atts = GetTagAttributes(m);
                    var newAtts = atts.ToDictionary();
                    if (atts.TryGetValue("data-help-image-guid", out var imageGuid))
                    {
                        toDeleteImages.Remove(Guid.Parse(imageGuid));
                    }

                    if (atts.TryGetValue("data-binary-file", out var base64Data))
                    {
                        var bytes = Convert.FromBase64String(base64Data);
                        var file = new FilePathEmbedded(HelpImageFileType.Image, atts.TryGetC("data-file-name") ?? "image.png", bytes);
                        var image = new HelpImageEntity
                        {
                            File = file,
                            Target = entity.ToLite(entity.IsNew),
                        };

                        newAtts.Remove("data-binary-file");

                        if (image.Target.IsNew)
                        {
                            newAtts.Add("data-hash", file.Hash!);
                            newImages.Add(image);
                        }
                        else
                        {
                            image.Save();
                            newAtts.Add("data-help-image-guid", image.Guid.ToString());
                        }
                    }

                    return $"<img {newAtts.ToString(a => @$"{a.Key}=""{a.Value}""", " ")}/>";
                });

                return newText;
            });

            if (toDeleteImages.Any())
                Database.DeleteList(toDeleteImages.Values.Select(a => a.lite).ToList());

            if (!hasChanges)
                return false;

            if (newImages.Any())
            {
                entity.Save();
                newImages.SaveList();

                var hashToHelpImageId = newImages.ToDictionary(a => a.File.Hash!, a => a.Guid);

                entity.ForeachHtmlField(text => ImgRegex.Replace(text?? "", m =>
                {
                    Dictionary<string, string> atts = GetTagAttributes(m);

                    if (atts.TryGetValue("data-hash", out var hash))
                    {
                        atts.Remove("data-hash");
                        atts.Add("data-help-image-guid", hashToHelpImageId.GetOrThrow(hash).ToString());
                    }

                    return $"<img {atts.ToString(a => $"{a.Key}=\"{a.Value}\"", " ")}/>";
                }));
            }

            return true;
        }
    }

    private static Dictionary<string, string> GetTagAttributes(Match m)
    {
        return m.Groups["key"].Captures().Select(a => a.Value)
        .Zip(m.Groups["value"].Captures().Select(a => a.Value)).ToDictionaryEx(p => p.First, p => p.Second);
    }

}
