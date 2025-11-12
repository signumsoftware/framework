using DocumentFormat.OpenXml.Drawing;
using Signum.Files;
using System.Text.RegularExpressions;

namespace Signum.Help;

public static class InlineImagesLogic
{

    [AutoExpressionField]
    public static IQueryable<HelpImageEntity> Images(this IHelpImageTarget e) =>
        As.Expression(() => Database.Query<HelpImageEntity>().Where(a => a.Target.Is(e)));

    public static Regex ImgRegex = new Regex(@"<img(\s+(?<key>[\w\-]+)\s*=\s*""(?<value>[^""]+)"")+\s*/?>");

    public static bool SynchronizeInlineImages(IHelpImageTarget entity)
    {
        using (OperationLogic.AllowSave<HelpImageEntity>())
        {
            var toDeleteImages = entity.IsNew ? new Dictionary<PrimaryKey, Lite<HelpImageEntity>>() :
                entity.Images().Select(a => a.ToLite()).ToDictionaryEx(a => a.Id) ;

            List<HelpImageEntity> newImages = new List<HelpImageEntity>();

            var hasChanges = entity.ForeachHtmlField(text =>
            {
                var newText = ImgRegex.Replace(text, m =>
                {
                    Dictionary<string, string> atts = GetTagAttributes(m);
                    var newAtts = atts.ToDictionary();
                    if (atts.TryGetValue("data-help-image-id", out var imageId))
                    {
                        toDeleteImages.Remove(PrimaryKey.Parse(imageId, typeof(HelpImageEntity)));
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
                            newAtts.Add("data-help-image-id", image.Id.ToString());
                        }
                    }

                    return $"<img {newAtts.ToString(a => @$"{a.Key}=""{a.Value}""", " ")}/>";
                });

                return newText;
            });

            if (toDeleteImages.Any())
                Database.DeleteList(toDeleteImages.Values.ToList());

            if (!hasChanges)
                return false;

            if (newImages.Any())
            {
                entity.Save();
                newImages.SaveList();

                var hashToHelpImageId = newImages.ToDictionary(a => a.File.Hash!, a => a.Id);

                entity.ForeachHtmlField(text => ImgRegex.Replace(text?? "", m =>
                {
                    Dictionary<string, string> atts = GetTagAttributes(m);

                    if (atts.TryGetValue("data-hash", out var hash))
                    {
                        atts.Remove("data-hash");
                        atts.Add("data-help-image-id", hashToHelpImageId.GetOrThrow(hash).ToString());
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
