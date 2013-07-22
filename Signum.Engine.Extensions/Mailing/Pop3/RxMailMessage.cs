// POP3 Email Client
// =================
//
// copyright by Peter Huber, Singapore, 2006
// this code is provided as is, bugs are probable, free for any use at own risk, no 
// responsibility accepted. All rights, title and interest in and to the accompanying content retained.  :-)
//
// based on Standard for ARPA Internet Text Messages, http://rfc.net/rfc822.html
// based on MIME Standard,  Internet Message Bodies, http://rfc.net/rfc2045.html
// based on MIME Standard, Media Types, http://rfc.net/rfc2046.html
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;


namespace Signum.Engine.Mailing.Pop3
{
    /// <summary>
    /// Stores all MIME decoded information of a received email. One email might consist of
    /// several MIME entities, which have a very similar structure to an email. A RxMailMessage
    /// can be a top most level email or a MIME entity the emails contains.
    /// 
    /// According to various RFCs, MIME entities can contain other MIME entities 
    /// recursively. However, they usually need to be mapped to alternative views and 
    /// attachments, which are non recursive.
    ///
    /// RxMailMessage inherits from System.Net.MailMessage, but provides additional receiving related information 
    /// </summary>
    public class RxMailMessage : MailMessage
    {
        /// <summary>
        /// To whom the email was delivered to
        /// </summary>
        public MailAddress DeliveredTo;
        /// <summary>
        /// To whom the email was
        /// </summary>
        public MailAddress ReturnPath;
        /// <summary>
        /// 
        /// </summary>
        public DateTime DeliveryDate;
        /// <summary>
        /// Date when the email was received
        /// </summary>
        public string MessageId;
        /// <summary>
        /// probably '1,0'
        /// </summary>
        public string MimeVersion;
        /// <summary>
        /// It may be desirable to allow one body to make reference to another. Accordingly, 
        /// bodies may be labelled using the "Content-ID" header field.    
        /// </summary>
        public string ContentId;
        /// <summary>
        /// some descriptive information for body
        /// </summary>
        public string ContentDescription;
        /// <summary>
        /// ContentDisposition contains normally redundant information also stored in the 
        /// ContentType. Since ContentType is more detailed, it is enough to analyze ContentType
        /// 
        /// something like:
        /// inline
        /// inline; filename="image001.gif
        /// attachment; filename="image001.jpg"
        /// </summary>
        public ContentDisposition ContentDisposition;
        /// <summary>
        /// something like "7bit" / "8bit" / "binary" / "quoted-printable" / "base64"
        /// </summary>
        public string TransferType;
        /// <summary>
        /// similar as TransferType, but .NET supports only "7bit" / "quoted-printable"
        /// / "base64" here, "bit8" is marked as "bit7" (i.e. no transfer encoding needed), 
        /// "binary" is illegal in SMTP
        /// </summary>
        public TransferEncoding ContentTransferEncoding;
        /// <summary>
        /// The Content-Type field is used to specify the nature of the data in the body of a
        /// MIME entity, by giving media type and subtype identifiers, and by providing 
        /// auxiliary information that may be required for certain media types. Examples:
        /// text/plain;
        /// text/plain; charset=ISO-8859-1
        /// text/plain; charset=us-ascii
        /// text/plain; charset=utf-8
        /// text/html;
        /// text/html; charset=ISO-8859-1
        /// image/gif; name=image004.gif
        /// image/jpeg; name="image005.jpg"
        /// message/delivery-status
        /// message/rfc822
        /// multipart/alternative; boundary="----=_Part_4088_29304219.1115463798628"
        /// multipart/related; 	boundary="----=_Part_2067_9241611.1139322711488"
        /// multipart/mixed; 	boundary="----=_Part_3431_12384933.1139387792352"
        /// multipart/report; report-type=delivery-status; boundary="k04G6HJ9025016.1136391237/carbon.singnet.com.sg"
        /// </summary>
        public ContentType ContentType;
        /// <summary>
        /// .NET framework combines MediaType (text) with subtype (plain) in one property, but
        /// often one or the other is needed alone. MediaMainType in this example would be 'text'.
        /// </summary>
        public string MediaMainType;
        /// <summary>
        /// .NET framework combines MediaType (text) with subtype (plain) in one property, but
        /// often one or the other is needed alone. MediaSubType in this example would be 'plain'.
        /// </summary>
        public string MediaSubType;
        /// <summary>
        /// RxMessage can be used for any MIME entity, as a normal message body, an attachement or an alternative view. ContentStream
        /// provides the actual content of that MIME entity. It's mainly used internally and later mapped to the corresponding 
        /// .NET types.
        /// </summary>
        public Stream ContentStream;
        /// <summary>
        /// A MIME entity can contain several MIME entities. A MIME entity has the same structure
        /// like an email. 
        /// </summary>
        public List<RxMailMessage> Entities;
        /// <summary>
        /// This entity might be part of a parent entity
        /// </summary>
        public RxMailMessage Parent;
        /// <summary>
        /// The top most MIME entity this MIME entity belongs to (grand grand grand .... parent)
        /// </summary>
        public RxMailMessage TopParent;
        /// <summary>
        /// The complete entity in raw content. Since this might take up quiet some space, the raw content gets only stored if the
        /// Pop3MimeClient.isGetRawEmail is set.
        /// </summary>
        public string RawContent;
        /// <summary>
        /// Headerlines not interpretable by Pop3ClientEmail
        /// <example></example>
        /// </summary>
        public List<string> UnknowHeaderlines; //


        // Constructors
        // ------------
        /// <summary>
        /// default constructor
        /// </summary>
        public RxMailMessage()
        {
            //for the moment, we assume to be at the top
            //should this entity become a child, TopParent will be overwritten
            TopParent = this;
            Entities = new List<RxMailMessage>();
            UnknowHeaderlines = new List<string>();
        }


        /// <summary>
        /// Set all content type related fields
        /// </summary>
        public void SetContentTypeFields(string contentTypeString)
        {
            contentTypeString = contentTypeString.Trim();
            //set content type
            if (contentTypeString == null || contentTypeString.Length < 1)
            {
                ContentType = new ContentType("text/plain; charset=us-ascii");
            }
            else
            {
                ContentType = new ContentType(contentTypeString);
            }

            //set encoding (character set)
            if (ContentType.CharSet == null)
            {
                BodyEncoding = Encoding.ASCII;
            }
            else
            {
                try
                {
                    BodyEncoding = Encoding.GetEncoding(ContentType.CharSet);
                }
                catch
                {
                    BodyEncoding = Encoding.ASCII;
                }
            }

            //set media main and sub type
            if (ContentType.MediaType == null || ContentType.MediaType.Length < 1)
            {
                //no mediatype found
                ContentType.MediaType = "text/plain";
            }
            else
            {
                string mediaTypeString = ContentType.MediaType.Trim().ToLowerInvariant();
                int slashPosition = ContentType.MediaType.IndexOf("/");
                if (slashPosition < 1)
                {
                    //only main media type found
                    MediaMainType = mediaTypeString;
                    if (MediaMainType == "text")
                    {
                        MediaSubType = "plain";
                    }
                    else
                    {
                        MediaSubType = "";
                    }
                }
                else
                {
                    //also submedia found
                    MediaMainType = mediaTypeString.Substring(0, slashPosition);
                    if (mediaTypeString.Length > slashPosition)
                    {
                        MediaSubType = mediaTypeString.Substring(slashPosition + 1);
                    }
                    else
                    {
                        if (MediaMainType == "text")
                        {
                            MediaSubType = "plain";
                        }
                        else
                        {
                            MediaSubType = "";
                        }
                    }
                }
            }

            IsBodyHtml = MediaSubType == "html";
        }


        /// <summary>
        /// Creates an empty child MIME entity from the parent MIME entity.
        /// 
        /// An email can consist of several MIME entities. A entity has the same structure
        /// like an email, that is header and body. The child inherits few properties 
        /// from the parent as default value.
        /// </summary>
        public RxMailMessage CreateChildEntity()
        {
            RxMailMessage child = new RxMailMessage();
            child.Parent = this;
            child.TopParent = this.TopParent;
            child.ContentTransferEncoding = this.ContentTransferEncoding;
            return child;
        }

        /// <summary>
        /// Convert structure of message into a string
        /// </summary>
        /// <returns></returns>
        public string MailStructure()
        {
            StringBuilder sb = new StringBuilder(1000);
            DecodeEntity(sb, this);
            sb.AppendLine("====================================");
            return sb.ToString();
        }

        static void DecodeEntity(StringBuilder sb, RxMailMessage entity)
        {
            AppendLine(sb, "From  : {0}", entity.From);
            AppendLine(sb, "Sender: {0}", entity.Sender);
            AppendLine(sb, "To    : {0}", entity.To);
            AppendLine(sb, "CC    : {0}", entity.CC);
            AppendLine(sb, "ReplyT: {0}", entity.ReplyTo);
            AppendLine(sb, "Sub   : {0}", entity.Subject);
            AppendLine(sb, "S-Enco: {0}", entity.SubjectEncoding);

            if (entity.DeliveryDate > DateTime.MinValue)
                AppendLine(sb, "Date  : {0}", entity.DeliveryDate);
            
            if (entity.Priority != MailPriority.Normal)
                AppendLine(sb, "Priori: {0}", entity.Priority);

            if (entity.Body.Length > 0)
            {
                AppendLine(sb, "Body  : {0} byte(s)", entity.Body.Length);
                AppendLine(sb, "B-Enco: {0}", entity.BodyEncoding);
            }
            else
            {
                if (entity.BodyEncoding != Encoding.ASCII)
                    AppendLine(sb, "B-Enco: {0}", entity.BodyEncoding);
            }

            AppendLine(sb, "T-Type: {0}", entity.TransferType);
            AppendLine(sb, "C-Type: {0}", entity.ContentType);
            AppendLine(sb, "C-Desc: {0}", entity.ContentDescription);
            AppendLine(sb, "C-Disp: {0}", entity.ContentDisposition);
            AppendLine(sb, "C-Id  : {0}", entity.ContentId);
            AppendLine(sb, "M-ID  : {0}", entity.MessageId);
            AppendLine(sb, "Mime  : Version {0}", entity.MimeVersion);

            //if (entity.ContentStream != null)
            //    AppendLine(sb, "Stream: Length {0}", entity.ContentStream.Length);

            //decode all shild MIME entities
            foreach (RxMailMessage child in entity.Entities)
            {
                sb.AppendLine("------------------------------------");
                DecodeEntity(sb, child);
            }

            if (entity.ContentType != null && entity.ContentType.MediaType != null && entity.ContentType.MediaType.StartsWith("multipart"))
            {
                AppendLine(sb, "End {0}", entity.ContentType.ToString());
            }
        }

        static void AppendLine(StringBuilder sb, string format, object arg)
        {
            if (arg != null)
            {
                string argString = arg.ToString();
                if (argString.Length > 0)
                {
                    sb.AppendLine(string.Format(format, argString));
                }
            }
        }

    }

}
