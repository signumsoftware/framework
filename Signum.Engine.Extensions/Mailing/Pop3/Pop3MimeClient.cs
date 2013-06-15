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
// based on QuotedPrintable Class from ASP emporium, http://www.aspemporium.com/classes.aspx?cid=6

// based on MIME Standard, E-mail Encapsulation of HTML (MHTML), http://rfc.net/rfc2110.html
// based on MIME Standard, Multipart/Related Content-type, http://rfc.net/rfc2112.html


// ?? RFC 2557       MIME Encapsulation of Aggregate Documents http://rfc.net/rfc2557.html

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;


namespace Signum.Engine.Mailing.Pop3
{
    // POP3 Client Email
    // =================

    /// <summary>
    /// Reads POP3 / MIME based emails 
    /// </summary>
    public class Pop3MimeClient : Pop3MailClient
    {

        //character array 'constants' used for analysing POP3 / MIME
        //----------------------------------------------------------
        static char[] BracketChars = { '(', ')' };
        static char[] CommaChars = { ',' };

        static char[] WhiteSpaceChars = { ' ', '\t' };


        //Help for debugging
        /// <summary>
        /// list of all unknown header lines received, for all (!) emails 
        /// </summary>
        public static List<string> AllUnknowHeaderLines;


        /// <summary>
        /// Set this flag, if you would like to get also the email in the raw US-ASCII format
        /// as received.
        /// Good for debugging, but takes quiet some space.
        /// </summary>
        public bool IsCollectRawEmail
        {
            get { return isGetRawEmail; }
            set { isGetRawEmail = value; }
        }
        private bool isGetRawEmail = false;


        // Pop3MimeClient Constructor
        //---------------------------

        /// <summary>
        /// constructor
        /// </summary>
        public Pop3MimeClient(string popServer, int port, bool useSSL, string username, string password)
            : base(popServer, port, useSSL, username, password)
        { }


        /// <summary>
        /// Gets 1 email from POP3 server and processes it.
        /// </summary>
        /// <param name="messageNo">Email Id to be fetched from POP3 server</param>
        /// <param name="message">decoded email</param>
        /// <returns>false: no email received or email not properly formatted</returns>
        public bool GetEmail(int messageNo, out RxMailMessage message)
        {
            message = null;

            //request email, send RETRieve command to POP3 server
            if (!SendRetrCommand(messageNo))
            {
                return false;
            }

            //prepare message, set defaults as specified in RFC 2046
            //although they get normally overwritten, we have to make sure there are at least defaults
            message = new RxMailMessage();
            message.ContentTransferEncoding = TransferEncoding.SevenBit;
            message.TransferType = "7bit";
            this.messageNo = messageNo;

            //raw email tracing
            if (isGetRawEmail)
            {
                isTraceRawEmail = true;
                if (rawEmailSB == null)
                {
                    rawEmailSB = new StringBuilder(100000);
                }
                else
                {
                    rawEmailSB.Length = 0;
                }
            }

            //convert received email into RxMailMessage
            MimeEntityReturnCode messageMimeReturnCode = ProcessMimeEntity(message, "");
            if (isGetRawEmail)
            {
                //add raw email version to message
                message.RawContent = rawEmailSB.ToString();
                isTraceRawEmail = false;
            }

            if (messageMimeReturnCode == MimeEntityReturnCode.BodyComplete ||
              messageMimeReturnCode == MimeEntityReturnCode.ParentBoundaryEndFound)
            {
                TraceFrom("email with {0} body chars received", message.Body.Length);
                return true;
            }
            return false;
        }


        private int messageNo;

        private void CallGetEmailWarning(string warningText, params object[] warningParameters)
        {
            string warningString;
            try
            {
                warningString = string.Format(warningText, warningParameters);
            }
            catch (Exception)
            {
                //some strange email address can give string.Format() a problem
                warningString = warningText;
            }
            CallWarning("GetEmail", "", "Problem EmailNo {0}: " + warningString, messageNo);
        }


        /// <summary>
        /// indicates the reason how a MIME entity processing has terminated
        /// </summary>
        enum MimeEntityReturnCode
        {
            Undefined = 0, //meaning like null
            BodyComplete, //end of message line found
            ParentBoundaryStartFound,
            ParentBoundaryEndFound,
            Problem //received message doesn't follow MIME specification
        }


        //buffer used by every ProcessMimeEntity() to store  MIME entity
        StringBuilder sb = new StringBuilder(100000);

        /// <summary>
        /// Process a MIME entity
        /// 
        /// A MIME entity consists of header and body.
        /// Separator lines in the body might mark children MIME entities
        /// </summary>
        MimeEntityReturnCode ProcessMimeEntity(RxMailMessage message, string parentBoundaryStart)
        {
            bool hasParentBoundary = parentBoundaryStart.Length > 0;
            string parentBoundaryEnd = parentBoundaryStart + "--";
            MimeEntityReturnCode boundaryMimeReturnCode;

            //some format fields are inherited from parent, only the default for
            //ContentType needs to be set here, otherwise the boundary parameter would be
            //inherited too !
            message.SetContentTypeFields("text/plain; charset=us-ascii");

            //get header
            //----------
            string completeHeaderField = null;     //consists of one start line and possibly several continuation lines
            string response;

            // read header lines until empty line is found (end of header)
            while (true)
            {
                if (!ReadMultiLine(out response))
                {
                    //POP3 server has not send any more lines
                    CallGetEmailWarning("incomplete MIME entity header received");
                    //empty this message
                    while (ReadMultiLine(out response)) { }
                    return MimeEntityReturnCode.Problem;
                }

                if (response.Length < 1)
                {
                    //empty line found => end of header
                    if (completeHeaderField != null)
                    {
                        ProcessHeaderField(message, completeHeaderField);
                    }
                    else
                    {
                        //there was only an empty header.
                    }
                    break;
                }

                //check if there is a parent boundary in the header (wrong format!)
                if (hasParentBoundary && ParentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    CallGetEmailWarning("MIME entity header  prematurely ended by parent boundary");
                    //empty this message
                    while (ReadMultiLine(out response)) { }
                    return boundaryMimeReturnCode;
                }
                //read header field
                //one header field can extend over one start line and multiple continuation lines
                //a continuation line starts with at least 1 blank (' ') or tab
                if (response[0] == ' ' || response[0] == '\t')
                {
                    //continuation line found.
                    if (completeHeaderField == null)
                    {
                        CallGetEmailWarning("Email header starts with continuation line");
                        //empty this message
                        while (ReadMultiLine(out response)) { }
                        return MimeEntityReturnCode.Problem;
                    }
                    else
                    {
                        // append space, if needed, and continuation line
                        if (completeHeaderField[completeHeaderField.Length - 1] != ' ')
                        {
                            //previous line did not end with a whitespace
                            //need to replace CRLF with a ' '
                            completeHeaderField += ' ' + response.TrimStart(WhiteSpaceChars);
                        }
                        else
                        {
                            //previous line did end with a whitespace
                            completeHeaderField += response.TrimStart(WhiteSpaceChars);
                        }
                    }

                }
                else
                {
                    //a new header field line found
                    if (completeHeaderField == null)
                    {
                        //very first field, just copy it and then check for continuation lines
                        completeHeaderField = response;
                    }
                    else
                    {
                        //new header line found
                        ProcessHeaderField(message, completeHeaderField);

                        //save the beginning of the next line
                        completeHeaderField = response;
                    }
                }
            }//end while read header lines


            //process body
            //------------

            sb.Length = 0;  //empty StringBuilder. For speed reasons, reuse StringBuilder defined as member of class
            string boundaryDelimiterLineStart = null;
            bool isBoundaryDefined = false;
            if (message.ContentType.Boundary != null)
            {
                isBoundaryDefined = true;
                boundaryDelimiterLineStart = "--" + message.ContentType.Boundary;
            }
            //prepare return code for the case there is no boundary in the body
            boundaryMimeReturnCode = MimeEntityReturnCode.BodyComplete;

            //read body lines
            while (ReadMultiLine(out response))
            {
                //check if there is a boundary line from this entity itself in the body
                if (isBoundaryDefined && response.TrimEnd() == boundaryDelimiterLineStart)
                {
                    //boundary line found.
                    //stop the processing here and start a delimited body processing
                    return ProcessDelimitedBody(message, boundaryDelimiterLineStart, parentBoundaryStart, parentBoundaryEnd);
                }

                //check if there is a parent boundary in the body
                if (hasParentBoundary &&
                  ParentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    //a parent boundary is found. Decode the content of the body received so far, then end this MIME entity
                    //note that boundaryMimeReturnCode is set here, but used in the return statement
                    break;
                }

                //process next line
                sb.Append(response + CRLF);
            }

            //a complete MIME body read
            //convert received US ASCII characters to .NET string (Unicode)
            string transferEncodedMessage = sb.ToString();
            bool isAttachmentSaved = false;
            switch (message.ContentTransferEncoding)
            {
                case TransferEncoding.SevenBit:
                    //nothing to do
                    SaveMessageBody(message, transferEncodedMessage);
                    break;

                case TransferEncoding.Base64:
                    //convert base 64 -> byte[]
                    byte[] bodyBytes = Convert.FromBase64String(transferEncodedMessage);
                    message.ContentStream = new MemoryStream(bodyBytes, false);

                    if (message.MediaMainType == "text")
                    {
                        //convert byte[] -> string
                        message.Body = (message.BodyEncoding ?? Encoding.UTF7).GetString(bodyBytes);

                    }
                    else if (message.MediaMainType == "image" || message.MediaMainType == "application")
                    {
                        SaveAttachment(message);
                        isAttachmentSaved = true;
                    }
                    break;

                case TransferEncoding.QuotedPrintable:
                    SaveMessageBody(message, QuotedPrintable.Decode(transferEncodedMessage));
                    break;

                default:
                    SaveMessageBody(message, transferEncodedMessage);
                    //no need to raise a warning here, the warning was done when analising the header
                    break;
            }

            if (message.ContentDisposition != null && message.ContentDisposition.DispositionType.ToLowerInvariant() == "attachment" && !isAttachmentSaved)
            {
                SaveAttachment(message);
                isAttachmentSaved = true;
            }
            return boundaryMimeReturnCode;
        }


        /// <summary>
        /// Check if the response line received is a parent boundary 
        /// </summary>
        private bool ParentBoundaryFound(string response, string parentBoundaryStart, string parentBoundaryEnd, out MimeEntityReturnCode boundaryMimeReturnCode)
        {
            boundaryMimeReturnCode = MimeEntityReturnCode.Undefined;
            if (response == null || response.Length < 2 || response[0] != '-' || response[1] != '-')
            {
                //quick test: reponse doesn't start with "--", so cannot be a separator line
                return false;
            }
            if (response == parentBoundaryStart)
            {
                boundaryMimeReturnCode = MimeEntityReturnCode.ParentBoundaryStartFound;
                return true;
            }
            else if (response == parentBoundaryEnd)
            {
                boundaryMimeReturnCode = MimeEntityReturnCode.ParentBoundaryEndFound;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Convert one MIME header field and update message accordingly
        /// </summary>
        private void ProcessHeaderField(RxMailMessage message, string headerField)
        {
            int separatorPosition = headerField.IndexOf(':');
            if (separatorPosition < 1)
            {
                // header field type not found, skip this line
                CallGetEmailWarning("character ':' missing in header format field: '{0}'", headerField);
            }
            else
            {

                //process header field type
                string headerLineType = headerField.Substring(0, separatorPosition).ToLowerInvariant();
                string headerLineContent = headerField.Substring(separatorPosition + 1).Trim(WhiteSpaceChars);
                if (headerLineType == "" || headerLineContent == "")
                {
                    //1 of the 2 parts missing, drop the line
                    return;
                }
                // add header line to headers
                message.Headers.Add(headerLineType, headerLineContent);

                //interpret if possible
                switch (headerLineType)
                {
                    case "bcc":
                        AddMailAddresses(headerLineContent, message.Bcc);
                        break;
                    case "cc":
                        AddMailAddresses(headerLineContent, message.CC);
                        break;
                    case "content-description":
                        message.ContentDescription = headerLineContent;
                        break;
                    case "content-disposition":
                        message.ContentDisposition = new ContentDisposition(headerLineContent);
                        break;
                    case "content-id":
                        message.ContentId = headerLineContent;
                        break;
                    case "content-transfer-encoding":
                        message.TransferType = headerLineContent;
                        message.ContentTransferEncoding = ConvertToTransferEncoding(headerLineContent);
                        break;
                    case "content-type":
                        message.SetContentTypeFields(headerLineContent);
                        break;
                    case "date":
                        message.DeliveryDate = ConvertToDateTime(headerLineContent);
                        break;
                    case "delivered-to":
                        message.DeliveredTo = ConvertToMailAddress(headerLineContent);
                        break;
                    case "from":
                        MailAddress address = ConvertToMailAddress(headerLineContent);
                        if (address != null)
                        {
                            message.From = address;
                        }
                        break;
                    case "message-id":
                        message.MessageId = headerLineContent;
                        break;
                    case "mime-version":
                        message.MimeVersion = headerLineContent;
                        //message.BodyEncoding = new Encoding();
                        break;
                    case "sender":
                        message.Sender = ConvertToMailAddress(headerLineContent);
                        break;
                    case "subject":
                        message.Subject = headerLineContent;
                        break;
                    case "received":
                        //throw mail routing information away
                        break;
                    case "reply-to":
                        message.ReplyTo = ConvertToMailAddress(headerLineContent);
                        break;
                    case "return-path":
                        message.ReturnPath = ConvertToMailAddress(headerLineContent);
                        break;
                    case "to":
                        AddMailAddresses(headerLineContent, message.To);
                        break;
                    default:
                        message.UnknowHeaderlines.Add(headerField);
                        if (AllUnknowHeaderLines != null)
                            AllUnknowHeaderLines.Add(headerField);
                        break;
                }
            }
        }


        /// <summary>
        /// find individual addresses in the string and add it to address collection
        /// </summary>
        /// <param name="addresses">string with possibly several email addresses</param>
        /// <param name="addressCollection">parsed addresses</param>
        private void AddMailAddresses(string addresses, MailAddressCollection addressCollection)
        {
            foreach (string adrString in addresses.Split(','))
            {
                MailAddress adr = ConvertToMailAddress(adrString);
                if (adr != null)
                {
                    addressCollection.Add(adr);
                }
            }
        }


        /// <summary>
        /// Tries to convert a string into an email address
        /// </summary>
        public MailAddress ConvertToMailAddress(string address)
        {
            address = address.Trim();
            if (address == "<>")
            {
                //empty email address, not recognised a such by .NET
                return null;
            }
            try
            {
                return new MailAddress(address);
            }
            catch
            {
                CallGetEmailWarning("address format not recognised: '" + address.Trim() + "'");
            }
            return null;
        }


        private IFormatProvider culture = new CultureInfo("en-US", true);

        /// <summary>
        /// Tries to convert string to date, following POP3 rules
        /// If there is a run time error, the smallest possible date is returned
        /// <example>Wed, 04 Jan 2006 07:58:08 -0800</example>
        /// </summary>
        public DateTime ConvertToDateTime(string date)
        {
            DateTime result;
            try
            {
                //sample; 'Wed, 04 Jan 2006 07:58:08 -0800 (PST)'
                //remove day of the week before ','
                //remove date zone in '()', -800 indicates the zone already

                //remove day of week
                string cleanDateTime = date;
                string[] dateSplit = cleanDateTime.Split(CommaChars, 2);
                if (dateSplit.Length > 1)
                {
                    cleanDateTime = dateSplit[1];
                }

                //remove time zone (PST)
                dateSplit = cleanDateTime.Split(BracketChars);
                if (dateSplit.Length > 1)
                {
                    cleanDateTime = dateSplit[0];
                }

                //convert to DateTime
                if (!DateTime.TryParse(cleanDateTime, culture,
                  DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                  out result))
                {
                    //try just to convert the date
                    int dateLength = cleanDateTime.IndexOf(':') - 3;
                    cleanDateTime = cleanDateTime.Substring(0, dateLength);

                    if (DateTime.TryParse(cleanDateTime, culture,
                      DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                      out result))
                    {
                        CallGetEmailWarning("got only date, time format not recognised: '" + date + "'");
                    }
                    else
                    {
                        CallGetEmailWarning("date format not recognised: '" + date + "'");
                        return DateTime.MinValue;
                    }
                }

            }
            catch
            {
                CallGetEmailWarning("date format not recognised: '" + date + "'");
                return DateTime.MinValue;
            }
            return result;
        }


        /// <summary>
        /// converts TransferEncoding as defined in the RFC into a .NET TransferEncoding
        /// 
        /// .NET doesn't know the type "bit8". It is translated here into "bit7", which
        /// requires the same kind of processing (none).
        /// </summary>
        /// <param name="transferEncodingString"></param>
        /// <returns></returns>
        private TransferEncoding ConvertToTransferEncoding(string transferEncodingString)
        {
            // here, "bit8" is marked as "bit7" (i.e. no transfer encoding needed)
            // "binary" is illegal in SMTP
            // something like "7bit" / "8bit" / "binary" / "quoted-printable" / "base64"
            switch (transferEncodingString.Trim().ToLowerInvariant())
            {
                case "7bit":
                case "8bit":
                    return TransferEncoding.SevenBit;
                case "quoted-printable":
                    return TransferEncoding.QuotedPrintable;
                case "base64":
                    return TransferEncoding.Base64;
                case "binary":
                    throw new Pop3Exception("SMPT does not support binary transfer encoding");
                default:
                    CallGetEmailWarning("not supported content-transfer-encoding: " + transferEncodingString);
                    return TransferEncoding.Unknown;
            }
        }


        /// <summary>
        /// Copies the content found for the MIME entity to the RxMailMessage body and creates
        /// a stream which can be used to create attachements, alternative views, ...
        /// </summary>
        void SaveMessageBody(RxMailMessage message, string contentString)
        {
            message.Body = contentString;
            using (MemoryStream bodyStream = new MemoryStream())
            using (StreamWriter bodyStreamWriter = new StreamWriter(bodyStream))
            {
                bodyStreamWriter.Write(contentString);
                bodyStreamWriter.Flush();
                message.ContentStream = bodyStream;
            }
        }


        /// <summary>
        /// each attachement is stored in its own MIME entity and read into this entity's
        /// ContentStream. SaveAttachment creates an attachment out of the ContentStream
        /// and attaches it to the parent MIME entity.
        /// </summary>
        void SaveAttachment(RxMailMessage message)
        {
            if (message.Parent != null)
            {
               
            }
            else
            {
                Attachment attachment = new Attachment(message.ContentStream, message.ContentType);

                //no idea why ContentDisposition is read only. on the other hand, it is anyway redundant
                if (message.ContentDisposition != null)
                {
                    ContentDisposition messageCD = message.ContentDisposition;
                    ContentDisposition attachmentCD = attachment.ContentDisposition;
                    if (messageCD.CreationDate > DateTime.MinValue)
                    {
                        attachmentCD.CreationDate = messageCD.CreationDate;
                    }
                    attachmentCD.DispositionType = messageCD.DispositionType;
                    attachmentCD.FileName = messageCD.FileName;
                    attachmentCD.Inline = messageCD.Inline;
                    if (messageCD.ModificationDate > DateTime.MinValue)
                    {
                        attachmentCD.ModificationDate = messageCD.ModificationDate;
                    }
                    attachmentCD.Parameters.Clear();
                    if (messageCD.ReadDate > DateTime.MinValue)
                    {
                        attachmentCD.ReadDate = messageCD.ReadDate;
                    }
                    if (messageCD.Size > 0)
                    {
                        attachmentCD.Size = messageCD.Size;
                    }
                    foreach (string key in messageCD.Parameters.Keys)
                    {
                        attachmentCD.Parameters.Add(key, messageCD.Parameters[key]);
                    }
                }

                //get ContentId
                string contentIdString = message.ContentId;
                if (contentIdString != null)
                {
                    attachment.ContentId = RemoveBrackets(contentIdString);
                }
                attachment.TransferEncoding = message.ContentTransferEncoding;
                message.Parent.Attachments.Add(attachment);
            }
        }



        /// <summary>
        /// removes leading '&lt;' and trailing '&gt;' if both exist
        /// </summary>
        /// <param name="parameterString"></param>
        /// <returns></returns>
        private string RemoveBrackets(string parameterString)
        {
            if (parameterString == null)
            {
                return null;
            }
            if (parameterString.Length < 1 ||
                  parameterString[0] != '<' ||
                  parameterString[parameterString.Length - 1] != '>')
            {
                //System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                return parameterString;
            }
            else
            {
                return parameterString.Substring(1, parameterString.Length - 2);
            }
        }


        private MimeEntityReturnCode ProcessDelimitedBody(RxMailMessage message, string boundaryStart, string parentBoundaryStart, string parentBoundaryEnd)
        {
            string response;

            if (boundaryStart.Trim() == parentBoundaryStart.Trim())
            {
                //Mime entity boundaries have to be unique
                CallGetEmailWarning("new boundary same as parent boundary: '{0}'", parentBoundaryStart);
                //empty this message
                while (ReadMultiLine(out response)) { }
                return MimeEntityReturnCode.Problem;
            }

            //
            MimeEntityReturnCode ReturnCode;
            do
            {

                //empty StringBuilder
                sb.Length = 0;
                RxMailMessage ChildPart = message.CreateChildEntity();

                //recursively call MIME part processing
                ReturnCode = ProcessMimeEntity(ChildPart, boundaryStart);

                if (ReturnCode == MimeEntityReturnCode.Problem)
                {
                    //it seems the received email doesn't follow the MIME specification. Stop here
                    return MimeEntityReturnCode.Problem;
                }

                //add the newly found child MIME part to the parent
                AddChildPartsToParent(ChildPart, message);
            } while (ReturnCode != MimeEntityReturnCode.ParentBoundaryEndFound);

            //disregard all future lines until parent boundary is found or end of complete message
            MimeEntityReturnCode boundaryMimeReturnCode;
            bool hasParentBoundary = parentBoundaryStart.Length > 0;
            while (ReadMultiLine(out response))
            {
                if (hasParentBoundary && ParentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    return boundaryMimeReturnCode;
                }
            }

            return MimeEntityReturnCode.BodyComplete;
        }


        /// <summary>
        /// Add all attachments and alternative views from child to the parent
        /// </summary>
        private void AddChildPartsToParent(RxMailMessage child, RxMailMessage parent)
        {
            //add the child itself to the parent
            parent.Entities.Add(child);

            //add the alternative views of the child to the parent
            if (child.AlternateViews != null)
            {
                foreach (AlternateView childView in child.AlternateViews)
                {
                    parent.AlternateViews.Add(childView);
                }
            }

            //add the body of the child as alternative view to parent
            //this should be the last view attached here, because the POP 3 MIME client
            //is supposed to display the last alternative view
            if (child.MediaMainType == "text" && child.ContentStream != null &&
              child.Parent.ContentType != null && child.Parent.ContentType.MediaType.ToLowerInvariant() == "multipart/alternative")
            {
                AlternateView thisAlternateView = new AlternateView(child.ContentStream)
                {
                    ContentId = RemoveBrackets(child.ContentId),
                    ContentType = child.ContentType,
                    TransferEncoding = child.ContentTransferEncoding,
                };
                parent.AlternateViews.Add(thisAlternateView);
            }

            //add the attachments of the child to the parent
            if (child.Attachments != null)
            {
                foreach (Attachment childAttachment in child.Attachments)
                {
                    parent.Attachments.Add(childAttachment);
                }
            }
        }
    }
}
