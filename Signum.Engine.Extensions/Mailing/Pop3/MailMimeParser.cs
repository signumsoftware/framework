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
        public static MailMessage Parse(StringReader mimeMail)
        {
            MailMessage returnValue = ParseMessage(mimeMail);

            FixStandardFields(returnValue);

            return returnValue;
        }

        static MailMessage ParseMessage(StringReader mimeMail)
        {
            MailMessage returnValue = new MailMessage();

            ReadHeaders(mimeMail, returnValue);

            if (returnValue.Headers.Count == 0)
                return null;

            DecodeHeaders(returnValue.Headers);

            string contentTransferEncoding = returnValue.Headers["content-transfer-encoding"];

            ContentType tmpContentType = FindContentType(returnValue.Headers);

            if (tmpContentType.MediaType != null && tmpContentType.MediaType.StartsWith("multipart/"))
            {
                MailMessage tmpMessage = ParseAlternative(tmpContentType.Boundary, mimeMail);

                foreach (AlternateView view in tmpMessage.AlternateViews)
                    returnValue.AlternateViews.Add(view);

                foreach (Attachment att in tmpMessage.Attachments)
                    returnValue.Attachments.Add(att);
            }
            else if (tmpContentType.MediaType != null && tmpContentType.MediaType.StartsWith("text/"))
            {
                returnValue.AlternateViews.Add(ParseTextView(mimeMail, contentTransferEncoding, tmpContentType));
            }
            else
            {
                returnValue.Attachments.Add(ParseAttachment(mimeMail, contentTransferEncoding, tmpContentType, returnValue.Headers));
            }
           
            return returnValue;
        }

        static void ReadHeaders(StringReader mimeMail, MailMessage returnValue)
        {
            string lastHeader = null;
            while (true)
            {
                string line = mimeMail.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    break;

                //If the line starts with a whitespace it is a continuation of the previous line
                if (Regex.IsMatch(line, @"^\s"))
                {
                    returnValue.Headers[lastHeader] = GetHeaderValue(returnValue.Headers, lastHeader) + " " + line.TrimStart('\t', ' ');
                }
                else
                {
                    string headerkey = line.Before(':').ToLower();
                    string value = line.After(':').TrimStart(' ');
                    if (value.HasItems())
                        returnValue.Headers[headerkey] = value;
                    lastHeader = headerkey;
                }
            }
        }

        static void FixStandardFields(MailMessage message)
        {
            //if (message.Headers["content-type"] != null)
            //{

            //    //extract the value of the content-type
            //    string type = Regex.Match(message.Headers["content-type"], @"^([^;]*)", RegexOptions.IgnoreCase).Groups[1].Value;
            //    if (type.ToLower() == "multipart/related" || type.ToLower() == "multipart/alternative")
            //    {
            //        List<string> toBeRemoved = new List<string>();
            //        List<AlternateView> viewsToBeRemoved = new List<AlternateView>();
            //        List<AlternateView> viewsToBeAdded = new List<AlternateView>();

            //        foreach (AlternateView view in message.AlternateViews)
            //        {
            //            if (view.ContentType.MediaType == "text/html")
            //            {
            //                foreach (Attachment att in message.Attachments)
            //                {
            //                    if (!string.IsNullOrEmpty(att.ContentId))
            //                    {
            //                        LinkedResource res = new LinkedResource(att.ContentStream, att.ContentType);
            //                        res.ContentId = att.ContentId;
            //                        if (att.ContentId.StartsWith("tmpContentId123_"))
            //                        {
            //                            string tmpLocation = Regex.Match(att.ContentId, "tmpContentId123_(.*)").Groups[1].Value;
            //                            string tmpid = Guid.NewGuid().ToString();
            //                            res.ContentId = tmpid;
            //                            string oldHtml = GetStringFromStream(view.ContentStream, view.ContentType);
            //                            ContentType ct = new ContentType("text/html; charset=utf-7");
            //                            AlternateView tmpView = AlternateView.CreateAlternateViewFromString(Regex.Replace(oldHtml, "src=\"" + tmpLocation + "\"", "src=\"cid:" + tmpid + "\"", RegexOptions.IgnoreCase), ct);
            //                            tmpView.LinkedResources.Add(res);
            //                            viewsToBeAdded.Add(tmpView);
            //                            viewsToBeRemoved.Add(view);
            //                        }
            //                        else
            //                            view.LinkedResources.Add(res);

            //                        toBeRemoved.Add(att.ContentId);
            //                    }
            //                }
            //            }
            //        }
            //        foreach (AlternateView view in viewsToBeRemoved)
            //        {
            //            message.AlternateViews.Remove(view);
            //        }
            //        foreach (AlternateView view in viewsToBeAdded)
            //        {
            //            message.AlternateViews.Add(view);
            //        }
            //        foreach (string s in toBeRemoved)
            //        {
            //            foreach (Attachment att in message.Attachments)
            //            {
            //                if (att.ContentId == s)
            //                {
            //                    message.Attachments.Remove(att);
            //                    break;
            //                }
            //            }
            //        }
            //    }

            //}

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

            if (message.AlternateViews.Count == 1)
            {
                StreamReader re = new StreamReader(message.AlternateViews[0].ContentStream);
                message.Body = re.ReadToEnd();
                message.IsBodyHtml = message.AlternateViews[0].ContentType.MediaType == "text/html";
                message.AlternateViews.Clear();
            }
        }

        public static string GetStringFromStream(Stream stream, ContentType contentType)
        {
            stream.Seek(0, new SeekOrigin());
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            string returnValue = string.Empty;
            switch (contentType.CharSet.ToLower())
            {
                case "utf-8":
                    returnValue = System.Text.UTF8Encoding.UTF8.GetString(buffer);
                    break;
                case "utf-7":
                    returnValue = System.Text.UTF7Encoding.UTF7.GetString(buffer);
                    break;
            }
            return returnValue;
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
            string line = string.Empty;
            StringBuilder b = new StringBuilder();
            while ((line = r.ReadLine()) != null)
            {
                switch (encoding)
                {
                    case "quoted-printable":
                        if (line.EndsWith("="))
                            b.Append(DecodeQP(line.TrimEnd('=')));
                        else
                            b.Append(DecodeQP(line) + "\n");
                        break;
                    case "base64":
                        b.Append(DecodeBase64(line, contentType.CharSet));
                        break;
                    default:
                        b.Append(line);
                        break;
                }
            }

            AlternateView returnValue = AlternateView.CreateAlternateViewFromString(b.ToString(), null, contentType.MediaType);
            returnValue.TransferEncoding = TransferEncoding.QuotedPrintable;
            return returnValue;
        }

        static Attachment ParseAttachment(StringReader r, string encoding, ContentType contentType, NameValueCollection headers)
        {
            string line = r.ReadToEnd();
            Attachment returnValue = null;
            switch (encoding)
            {
                case "quoted-printable":
                    returnValue = new Attachment(new MemoryStream(DecodeBase64Binary(line)), contentType) { TransferEncoding = TransferEncoding.QuotedPrintable };
                    break;
                case "base64":
                    returnValue = new Attachment(new MemoryStream(DecodeBase64Binary(line)), contentType) { TransferEncoding = TransferEncoding.Base64 };
                    break;
                default:
                    returnValue = new Attachment(new MemoryStream(Encoding.ASCII.GetBytes(line)), contentType) { TransferEncoding = TransferEncoding.SevenBit };
                    break;
            }
            if (headers["content-id"] != null)
                returnValue.ContentId = headers["content-id"].ToString().Trim('<', '>');
            else if (headers["content-location"] != null)
            {
                returnValue.ContentId = "tmpContentId123_" + headers["content-location"].ToString();
            }

            return returnValue;
        }

        static MailMessage ParseAlternative(string multipartBoundary, StringReader message)
        {
            MailMessage returnValue = new MailMessage();
            string line = string.Empty;
            List<string> messageParts = new List<string>();

            //ffw until first boundary
            while (!message.ReadLine().TrimEnd().Equals("--" + multipartBoundary)) ;
            StringBuilder part = new StringBuilder();
            while ((line = message.ReadLine()) != null)
            {
                if (line.TrimEnd().Equals("--" + multipartBoundary) || line.TrimEnd().Equals("--" + multipartBoundary + "--"))
                {
                    MailMessage tmpMessage = ParseMessage(new StringReader(part.ToString()));
                    if (tmpMessage != null)
                    {
                        foreach (AlternateView view in tmpMessage.AlternateViews)
                            returnValue.AlternateViews.Add(view);
                        foreach (Attachment att in tmpMessage.Attachments)
                            returnValue.Attachments.Add(att);
                        if (line.Equals("--" + multipartBoundary))
                            part = new StringBuilder();
                        else
                            break;
                    }
                }
                else
                    part.AppendLine(line);
            }
            return returnValue;
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
            ContentType returnValue = new ContentType();
            var ct = headers["content-type"];
            if (ct == null)
                return returnValue;

            returnValue = new ContentType(Regex.Match(ct, @"^([^;]*)", RegexOptions.IgnoreCase).Groups[1].Value);
            if (Regex.IsMatch(ct, @"name=""?(.*?)""?($|;)", RegexOptions.IgnoreCase))
                returnValue.Name = Regex.Match(ct, @"name=""?(.*?)""?($|;)", RegexOptions.IgnoreCase).Groups[1].Value;

            if (Regex.IsMatch(ct, @"boundary=(.*?)(;|$)", RegexOptions.IgnoreCase))
                returnValue.Boundary = Regex.Match(ct, @"boundary=(.*?)(;|$)", RegexOptions.IgnoreCase).Groups[1].Value.Trim('\'', '"');

            if (Regex.IsMatch(ct, @"charset=(.*?)(;|$)", RegexOptions.IgnoreCase))
                returnValue.CharSet = Regex.Match(ct, @"charset=(.*?)(;|$)", RegexOptions.IgnoreCase).Groups[1].Value.Trim('\'', '"');

            return returnValue;
        }

        static void DecodeHeaders(NameValueCollection headers)
        {
            const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            foreach (string key in headers.AllKeys)
            {
                //strip qp encoding information from the header if present
                headers[key] = Regex.Replace(headers[key].ToString(), @"=\?.*?\?Q\?(.*?)\?=", m => DecodeQP(m.Groups[1].Value), options);
                headers[key] = Regex.Replace(headers[key].ToString(), @"=\?.*?\?B\?(.*?)\?=", m => Encoding.UTF7.GetString(Convert.FromBase64String(m.Groups[1].Value)), options);
            }
        }

        static string DecodeBase64(string line, string enc)
        {
            switch (enc.ToLower())
            {
                case "utf-7": return Encoding.UTF7.GetString(Convert.FromBase64String(line));
                case "utf-8": return Encoding.UTF8.GetString(Convert.FromBase64String(line));
                default: return "";
            }
        }

        static byte[] DecodeBase64Binary(string line)
        {
            return Convert.FromBase64String(line);
        }

        static string DecodeQP(string trall)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < trall.Length; i++)
            {
                if (trall[i] == '=')
                {
                    byte tmpbyte = Convert.ToByte(trall.Substring(i + 1, 2), 16);
                    i += 2;
                    b.Append((char)tmpbyte);
                }
                else if (trall[i] == '_')
                    b.Append(' ');
                else
                    b.Append(trall[i]);
            }
            return b.ToString();
        }
    }
}
