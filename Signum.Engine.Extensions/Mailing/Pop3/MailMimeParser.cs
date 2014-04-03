using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Net.Mime;
using Signum.Utilities;

namespace Signum.Engine.Mailing.Pop3
{
    public static class MailMimeParser
    {
        public static MailMessage Parse(string message)
        {
            MailMessage mail = new MailMessage();

            var parts = GetParts(message, Request.None, mail.Headers);

            foreach (var p in parts)
            {
                if (p is AlternateView)
                    mail.AlternateViews.Add((AlternateView)p);
                else if (p is Attachment)
                    mail.Attachments.Add((Attachment)p);
                else
                    throw new InvalidOperationException("{0} not expected at this stage".Formato(p.GetType().Name));
            }

            FixStandardFields(mail);

            return mail;
        }

    

        enum Request
        {
            None,
            AlternateView,
            LinkedResource,
        }

        static List<AttachmentBase> GetParts(string message, Request req, NameValueCollection headers = null)
        {
            if (headers == null)
                headers = new NameValueCollection();

            using (StringReader reader = new StringReader(message))
            {
                ReadHeaders(reader, headers);

                string contentTransferEncoding = headers["content-transfer-encoding"];

                ContentType contentType = FindContentType(headers);

                if (contentType.MediaType != null && contentType.MediaType.StartsWith("multipart/"))
                {
                    List<string> parts = SplitSubparts(reader, contentType.Boundary);


                    if (contentType.MediaType == "multipart/related")
                    {
                        var result = GetParts(parts[0], Request.AlternateView).ToList();

                        var best = result.OfType<AlternateView>().OrderByDescending(a => a.ContentType.MediaType.Contains("html")).ThenByDescending(a => a.ContentStream.Length).FirstOrDefault();

                        foreach (string item in parts.Skip(1))
                        {
                            foreach (var part in GetParts(item, best == null ? Request.None : Request.LinkedResource))
                            {
                                if (best != null && part is LinkedResource)
                                    best.LinkedResources.Add((LinkedResource)part);
                                else
                                    result.Add(best);
                            }
                        }

                        return result;
                    }
                    else
                    {
                        var result = (from p in parts
                                      from s in GetParts(p, Request.None)
                                      select s).ToList();

                        return result;
                    }
                }

                if (req == Request.AlternateView)
                    return new List<AttachmentBase> { ParseTextView(reader, contentTransferEncoding, contentType) };

                if (req == Request.LinkedResource)
                    return new List<AttachmentBase> { ParseAttachment(reader, contentTransferEncoding, contentType, headers, asLinkedResource: true) };

                if (contentType.MediaType != null && contentType.MediaType.StartsWith("text/") && !IsAttachment(headers["Content-Disposition"]))
                    return new List<AttachmentBase> { ParseTextView(reader, contentTransferEncoding, contentType) };

                var attachment = ParseAttachment(reader, contentTransferEncoding, contentType, headers, asLinkedResource: false);

                if (IsWinMailDat(contentType) && OpenWinMailAttachment != null)
                    return OpenWinMailAttachment((Attachment)attachment);

                return new List<AttachmentBase> { attachment };
            }
        }

        public static Func<Attachment, List<AttachmentBase>> OpenWinMailAttachment = null;

        static bool IsWinMailDat(ContentType contentType)
        {
            return contentType.MediaType != null &&
                (string.Equals(contentType.MediaType, "application/ms-tnef") || string.Equals(contentType.MediaType, "application/dat")) &&
                string.Equals(contentType.Name, "winmail.dat", StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool IsAttachment(string contentDisposition)
        {
            return contentDisposition.HasText() && contentDisposition.StartsWith("attachment", StringComparison.InvariantCultureIgnoreCase);
        }

        static NameValueCollection ReadHeaders(StringReader reader, NameValueCollection headers)
        {
            string lastHeader = null;
            while (true)
            {
                string line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    break;

                //If the line starts with a whitespace it is a continuation of the previous line
                if (Regex.IsMatch(line, @"^\s"))
                {
                    headers[lastHeader] += line.TrimStart('\t', ' ');
                }
                else
                {
                    string headerkey = line.Before(':').ToLower();
                    string value = line.After(':').TrimStart(' ');
                    if (value.HasItems())
                        headers[headerkey] = value;
                    lastHeader = headerkey;
                }
            }

            DecodeHeaders(headers);

            return headers;
        }

        static void FixStandardFields(MailMessage message)
        {
            if (string.IsNullOrEmpty(message.Subject))
                message.Subject = GetHeaderValue(message.Headers, "subject");

            try
            {
                message.From = new MailAddress(message.Headers["from"].DefaultText("missing@missing.com"));
            }
            catch
            {
                message.From = new MailAddress("error@error.com");
            }

            FillAddressesCollection(message.CC, message.Headers["cc"]);
            FillAddressesCollection(message.To, message.Headers["to"]);
            FillAddressesCollection(message.Bcc, message.Headers["bcc"]);

            foreach (AlternateView view in message.AlternateViews)
            {
                view.ContentStream.Seek(0, SeekOrigin.Begin);
            }

            var onlyView = message.AlternateViews.Only();

            if (onlyView != null && onlyView.LinkedResources.IsEmpty())
            {
                message.Body = new StreamReader(onlyView.ContentStream, GetEncoding(onlyView.ContentType)).Using(r => r.ReadToEnd());
                message.IsBodyHtml = onlyView.ContentType.MediaType == "text/html";
                message.AlternateViews.Clear();
            }
        }

        static void FillAddressesCollection(ICollection<MailAddress> addresses, string addressHeader)
        {
            if (string.IsNullOrEmpty(addressHeader))
                return;

            foreach (var email in addressHeader.Split(','))
            {
                MailAddress address;

                try
                {
                    address = new MailAddress(email);
                }
                catch
                {
                    address = new MailAddress("error@error.com");
                }

                addresses.Add(address);
            }
        }

        static AlternateView ParseTextView(StringReader r, string encoding, System.Net.Mime.ContentType contentType)
        {
            string content = r.ReadToEnd();

            string result;

            var enc = GetEncoding(contentType);

            switch (encoding == null ? null : encoding.ToLower())
            {
                case "quoted-printable":
                    result = enc.GetString(DecodeQuotePrintable(content));

                    break;
                case "base64":
                    result = enc.GetString(Convert.FromBase64String(content));
                    break;
                default:
                    result = content;
                    break;
            }

            AlternateView returnValue = AlternateView.CreateAlternateViewFromString(result.ToString(), GetEncoding(contentType), contentType.MediaType);
            returnValue.TransferEncoding = TransferEncoding.QuotedPrintable;
            return returnValue;
        }

        public static Encoding GetEncoding(System.Net.Mime.ContentType contentType)
        {
            if (contentType.CharSet == null)
                return Encoding.UTF8;

            try
            {
                return Encoding.GetEncoding(contentType.CharSet.ToLower());
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        static AttachmentBase ParseAttachment(StringReader r, string encoding, ContentType contentType, NameValueCollection headers, bool asLinkedResource)
        {
            TransferEncoding transferEncoding =  
                encoding == "quoted-printable" ? TransferEncoding.QuotedPrintable: 
                encoding == "base64" ? TransferEncoding.Base64: 
                TransferEncoding.SevenBit; 
          
            string str = r.ReadToEnd();
            
            byte[] data = 
                transferEncoding == TransferEncoding.QuotedPrintable? DecodeQuotePrintable(str) : 
                transferEncoding == TransferEncoding.Base64 ? Convert.FromBase64String(str) : 
                Encoding.ASCII.GetBytes(str); 
            
            var returnValue = asLinkedResource?
                (AttachmentBase)new LinkedResource(new MemoryStream(data), contentType) :
                (AttachmentBase)new Attachment(new MemoryStream(data), contentType);
                
            if (headers["content-id"] != null)
                returnValue.ContentId = headers["content-id"].ToString().Trim('<', '>');
            else if (headers["content-location"] != null)
            {
                returnValue.ContentId = "tmpContentId123_" + headers["content-location"].ToString();
            }

            return returnValue;
        }

        static List<string> SplitSubparts(StringReader reader, string multipartBoundary)
        {
            List<string> result = new List<string>();

            string line;
            //ffw until first boundary
            while (!reader.ReadLine().TrimEnd().Equals("--" + multipartBoundary)) ;
            StringBuilder part = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.TrimEnd().Equals("--" + multipartBoundary) || line.TrimEnd().Equals("--" + multipartBoundary + "--"))
                {
                    result.Add(part.ToString());

                    if (line.Equals("--" + multipartBoundary))
                        part = new StringBuilder();
                    else
                        break;
                }
                else
                    part.AppendLine(line);
            }

            return result;
        }

        static string GetHeaderValue(NameValueCollection collection, string key)
        {
            foreach (string k in collection.Keys)
            {
                if (k.Equals(key, StringComparison.InvariantCultureIgnoreCase))
                    return collection[k];
            }
            return string.Empty;
        }

        static ContentType FindContentType(NameValueCollection headers)
        {
            ContentType result = new ContentType();
            var ct = headers["content-type"];
            if (ct == null)
                return result;

            result = new ContentType(Regex.Match(ct, @"^([^;]*)", RegexOptions.IgnoreCase).Groups[1].Value);

            var m = Regex.Match(ct, @"name\s*=\s*(.*?)\s*($|;)", RegexOptions.IgnoreCase);
            if(m.Success)
                result.Name = m.Groups[1].Value.Trim('\'', '"');

            m = Regex.Match(ct, @"boundary\s*=\s*(.*?)\s*($|;)", RegexOptions.IgnoreCase);
            if (m.Success)
                result.Boundary = m.Groups[1].Value.Trim('\'', '"');

            m = Regex.Match(ct, @"charset\s*=\s*(.*?)\s*($|;)", RegexOptions.IgnoreCase);
            if (m.Success)
                result.CharSet = m.Groups[1].Value.Trim('\'', '"');

            if (result.CharSet != null)
                result.CharSet = result.CharSet.ToLowerInvariant();

            if (result.MediaType != null)
                result.MediaType = result.MediaType.ToLowerInvariant();

            return result;
        }

        static void DecodeHeaders(NameValueCollection headers)
        {
            const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            foreach (string key in headers.AllKeys)
            {
                //strip qp encoding information from the header if present
                headers[key] = Regex.Replace(headers[key].ToString(), @"=\?(?<enc>[^?]*)?\?(?<cod>Q|B)\?(?<text>.*?)\?=", m =>
                {
                    string text = m.Groups["text"].Value;

                    byte[] bytes = m.Groups["cod"].Value.ToUpper() == "Q" ? DecodeQuotePrintable(text.Replace('_', ' ')) : Convert.FromBase64String(text);

                    var result = Encoding.GetEncoding(m.Groups["enc"].Value.ToLower()).GetString(bytes);

                    //string result2 = Attachment.CreateAttachmentFromString("", m.Value).Name;

                    //if (result != result2)
                    //    throw new InvalidOperationException();

                    return result;
                }
                , options);
            }
        }

        private static byte[] DecodeQuotePrintable(string input)
        {
            var i = 0;
            var output = new List<byte>();
            while (i < input.Length)
            {
                if (input[i] == '=' && input[i + 1] == '\r' && input[i + 2] == '\n')
                {
                    //Skip
                    i += 3;
                }
                else if (input[i] == '=')
                {
                    string sHex = input;
                    sHex = sHex.Substring(i + 1, 2);
                    int hex = Convert.ToInt32(sHex, 16);
                    byte b = Convert.ToByte(hex);
                    output.Add(b);
                    i += 3;
                }
                else
                {
                    output.Add((byte)input[i]);
                    i++;
                }
            }

            return output.ToArray();
        }
    }
}
