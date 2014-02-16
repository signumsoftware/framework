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
            string returnValue = Encoding.GetEncoding(contentType.CharSet.ToLower()).GetString(buffer);
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
            string content = r.ReadToEnd();

            string result;

            switch (encoding)
            {
                case "quoted-printable":
                    result = GetEncoding(contentType).GetString(DecodeQuotePrintable(content));

                    break;
                case "base64":
                    result = Encoding.GetEncoding(contentType.CharSet.ToLower()).GetString(Convert.FromBase64String(content));
                    break;
                default:
                    result = content;
                    break;
            }

            AlternateView returnValue = AlternateView.CreateAlternateViewFromString(result.ToString(), null, contentType.MediaType);
            returnValue.TransferEncoding = TransferEncoding.QuotedPrintable;
            return returnValue;
        }

        private static Encoding GetEncoding(System.Net.Mime.ContentType contentType)
        {
            if (contentType.CharSet == null)
                return Encoding.UTF8;

            return Encoding.GetEncoding(contentType.CharSet.ToLower());
        }

        static Attachment ParseAttachment(StringReader r, string encoding, ContentType contentType, NameValueCollection headers)
        {
            string line = r.ReadToEnd();
            Attachment returnValue = null;
            switch (encoding)
            {
                case "quoted-printable":
                    returnValue = new Attachment(new MemoryStream(DecodeQuotePrintable(line)), contentType) { TransferEncoding = TransferEncoding.QuotedPrintable };
                    break;
                case "base64":
                    returnValue = new Attachment(new MemoryStream(Convert.FromBase64String(line)), contentType) { TransferEncoding = TransferEncoding.Base64 };
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

            var m = Regex.Match(ct, @"name\s*=\s*(.*?)\s*($|;)", RegexOptions.IgnoreCase);
            if(m.Success)
                returnValue.Name = m.Groups[1].Value.Trim('\'', '"');

            m = Regex.Match(ct, @"boundary\s*=\s*(.*?)\s*($|;)", RegexOptions.IgnoreCase);
            if (m.Success)
                returnValue.Boundary = m.Groups[1].Value.Trim('\'', '"');

            m = Regex.Match(ct, @"charset\s*=\s*(.*?)\s*($|;)", RegexOptions.IgnoreCase);
            if (m.Success)
                returnValue.CharSet = m.Groups[1].Value.Trim('\'', '"');
        
            return returnValue;
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

                    byte[] bytes = m.Groups["cod"].Value == "Q" ? DecodeQuotePrintable(text.Replace('_', ' ')) : Convert.FromBase64String(text);

                    var result = Encoding.GetEncoding(m.Groups["enc"].Value.ToLower()).GetString(bytes);

                    string result2 = Attachment.CreateAttachmentFromString("", m.Value).Name;

                    if (result != result2)
                        throw new InvalidOperationException();

                    return result2;
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
