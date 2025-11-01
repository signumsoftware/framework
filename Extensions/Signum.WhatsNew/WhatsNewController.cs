using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.API.Filters;
using Signum.Authorization;
using Signum.Files;

namespace Signum.WhatsNew;

public class WhatsNewController : ControllerBase
{
    [HttpGet("api/whatsnew/myNewsCount")]
    public MyNewsCountResult MyNewsCount()
    {
        return new MyNewsCountResult
        {
            NumWhatsNews = WhatsNewLogic.GetWhatNews().Where(t => t.wn.Status == WhatsNewState.Publish).Count(t => !t.isRead)
        };
    }

    public class MyNewsCountResult
    {
        public int NumWhatsNews;
    }

    [HttpGet("api/whatsnew/myNews")]
    public List<WhatsNewShort> MyNews()
    {
        return WhatsNewLogic.GetWhatNews()
            .Where(t => !t.isRead && t.wn.Status == WhatsNewState.Publish)
            .Select(t =>
            {
                var wn = t.wn;
                var cm = wn.GetCurrentMessage();
                return new WhatsNewShort
                {
                    WhatsNew = wn.ToLite(),
                    CreationDate = wn.CreationDate,
                    Title = cm.Title,
                    Description = cm.Description,
                    Status = wn.Status.ToString()
                };
            })
            .ToList();
    }

    public class WhatsNewShort
    {
        public Lite<WhatsNewEntity> WhatsNew;
        public DateTime CreationDate; 
        public string Title;
        public string Description;
        public string Status;
    }


    [HttpGet("api/whatsnew/all")]
    public List<WhatsNewFull> GetAllNews()
    {
        return WhatsNewLogic.GetWhatNews()
        .Select(t =>
        {
            var wn = t.wn;
            var cm = wn.GetCurrentMessage();
            return new WhatsNewFull
            {
                WhatsNew = wn.ToLite(),
                CreationDate = wn.CreationDate,
                Title = cm.Title,
                Description = cm.Description,
                Attachments = wn.Attachment.Count(),
                PreviewPicture = (wn.PreviewPicture != null) ? true : false,
                Status = wn.Status.ToString(),
                Read = t.isRead,
            };
        })
        .ToList();
    }


    public class WhatsNewFull
    {
        public Lite<WhatsNewEntity> WhatsNew;
        public DateTime CreationDate;
        public string Title;
        public string Description;
        public int Attachments; 
        public bool PreviewPicture;
        public string Status;
        public bool Read;
    }

    [HttpGet("api/whatsnew/previewPicture/{newsid}"), SignumAllowAnonymous]
    public FileStreamResult? GetPreviewPicture(int newsid)
    {
        using (AuthLogic.Disable())
        {
            var whatsnew = Database.Retrieve<WhatsNewEntity>(newsid);
            return (whatsnew.PreviewPicture == null) ? null : GetFileStreamResult(whatsnew.PreviewPicture.OpenRead(), whatsnew.PreviewPicture.FileName);
        }
    }

    public static FileStreamResult GetFileStreamResult(Stream stream, string fileName)
    {
        var mime = MimeMapping.GetMimeType(fileName);
        return new FileStreamResult(stream, mime);
    }

    [HttpGet("api/whatsnew/{id}")]
    public WhatsNewFull SpecificNews(int id)
    {
        var wne = WhatsNewLogic.GetWhatNew(id);
        if (wne == null)
            throw new ApplicationException(WhatsNewMessage.ThisNewIsNoLongerAvailable.NiceToString());

        if (!wne.IsRead())
        {
            using (AuthLogic.Disable())
            {
                new WhatsNewLogEntity()
                {
                    WhatsNew = wne.ToLite(),
                    User = UserEntity.Current,
                    ReadOn = Clock.Now
                }.Save();
            }
        }
        var cm = wne.GetCurrentMessage();

        return new WhatsNewFull
        {
            WhatsNew = wne.ToLite(),
            Title = cm.Title,
            Description = cm.Description,
            Attachments = wne.Attachment.Count(),
            PreviewPicture = (wne.PreviewPicture != null) ? true : false,
            Status = wne.Status.ToString(),
            Read = wne.IsRead(),
        };
    }

    [HttpPost("api/whatsnew/setNewsLog")]
    public void setNewsLogRead([FromBody, Required] Lite<WhatsNewEntity>[] lites)
    {
        Database.Query<WhatsNewEntity>()
            .Where(wn => lites.Contains(wn.ToLite()) && !wn.IsRead())
            .UnsafeInsert(wn => new WhatsNewLogEntity()
            {
                WhatsNew = wn.ToLite(),
                User = UserEntity.Current,
                ReadOn = Clock.Now
            });
    }
}
