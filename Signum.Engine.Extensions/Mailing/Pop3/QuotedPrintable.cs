// POP3 Quoted Printable
// =====================
//
// copyright by Peter Huber, Singapore, 2006
// this code is provided as is, bugs are probable, free for any use, no responsiblity accepted :-)
//
// based on QuotedPrintable Class from ASP emporium, http://www.aspemporium.com/classes.aspx?cid=6


using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security;

namespace Signum.Engine.Mailing.Pop3
{
    /// <summary>
    /// <para>
    /// Robust and fast implementation of Quoted Printable
    /// Multipart Internet Mail Encoding (MIME) which encodes every 
    /// character, not just "special characters" for transmission over SMTP.
    /// </para>
    /// <para>
    /// More information on the quoted-printable encoding can be found
    /// here: http://www.freesoft.org/CIE/RFC/1521/6.htm
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// detailed in: RFC 1521
    /// </para>
    /// <para>
    /// more info: http://www.freesoft.org/CIE/RFC/1521/6.htm
    /// </para>
    /// <para>
    /// The QuotedPrintable class encodes and decodes strings and files
    /// that either were encoded or need encoded in the Quoted-Printable
    /// MIME encoding for Internet mail. The encoding methods of the class
    /// use pointers wherever possible to guarantee the fastest possible 
    /// encoding times for any size file or string. The decoding methods 
    /// use only the .NET framework classes.
    /// </para>
    /// <para>
    /// The Quoted-Printable implementation
    /// is robust which means it encodes every character to ensure that the
    /// information is decoded properly regardless of machine or underlying
    /// operating system or protocol implementation. The decode can recognize
    /// robust encodings as well as minimal encodings that only encode special
    /// characters and any implementation in between. Internally, the
    /// class uses a regular expression replace pattern to decode a quoted-
    /// printable string or file.
    /// </para>
    /// </remarks>
    /// <example>
    /// This example shows how to quoted-printable encode an html file and then
    /// decode it.
    /// <code>
    /// string encoded = QuotedPrintable.EncodeFile(
    /// 	@"C:\WEBS\wwwroot\index.html"
    /// 	);
    /// 
    /// string decoded = QuotedPrintable.Decode(encoded);
    /// 
    /// Console.WriteLine(decoded);
    /// </code>
    /// </example>
    static class QuotedPrintable
    {
        /// <summary>
        /// Gets the maximum number of characters per quoted-printable
        /// line as defined in the RFC minus 1 to allow for the =
        /// character (soft line break).
        /// </summary>
        /// <remarks>
        /// (Soft Line Breaks): The Quoted-Printable encoding REQUIRES 
        /// that encoded lines be no more than 76 characters long. If 
        /// longer lines are to be encoded with the Quoted-Printable 
        /// encoding, 'soft' line breaks must be used. An equal sign 
        /// as the last character on a encoded line indicates such a 
        /// non-significant ('soft') line break in the encoded text.
        /// </remarks>
        public const int RFC_1521_MAX_CHARS_PER_LINE = 75;



        static string HexDecoderEvaluator(Match m)
        {
            string hex = m.Groups[2].Value;
            int iHex = Convert.ToInt32(hex, 16);
            char c = (char)iHex;
            return c.ToString();
        }

        static string HexDecoder(string line)
        {
            if (line == null)
                throw new ArgumentNullException();

            //parse looking for =XX where XX is hexadecimal
            Regex re = new Regex(
              "(\\=([0-9A-F][0-9A-F]))",
              RegexOptions.IgnoreCase
            );
            return re.Replace(line, new MatchEvaluator(HexDecoderEvaluator));
        }

        /// <summary>
        /// decodes an entire file's contents into plain text that 
        /// was encoded with quoted-printable.
        /// </summary>
        /// <param name="filepath">
        /// The path to the quoted-printable encoded file to decode.
        /// </param>
        /// <returns>The decoded string.</returns>
        /// <exception cref="ObjectDisposedException">
        /// A problem occurred while attempting to decode the 
        /// encoded string.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// There is insufficient memory to allocate a buffer for the
        /// returned string. 
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// A string is passed in as a null reference.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurs, such as the stream being closed.
        /// </exception>  
        /// <exception cref="FileNotFoundException">
        /// The file was not found.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The caller does not have the required permission to open
        /// the file specified in filepath.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// filepath is read-only or a directory.
        /// </exception>
        /// <remarks>
        /// Decodes a quoted-printable encoded file into a string
        /// of unencoded text of any size.
        /// </remarks>
        public static string DecodeFile(string filepath)
        {
            if (filepath == null)
                throw new ArgumentNullException();

            string decodedHtml = "", line;
            FileInfo f = new FileInfo(filepath);

            if (!f.Exists)
                throw new FileNotFoundException();

            StreamReader sr = f.OpenText();
            try
            {
                while ((line = sr.ReadLine()) != null)
                    decodedHtml += Decode(line);

                return decodedHtml;
            }
            finally
            {
                sr.Close();
                sr = null;
                f = null;
            }
        }


        /// <summary>
        /// Decodes a Quoted-Printable string of any size into 
        /// it's original text.
        /// </summary>
        /// <param name="encoded">
        /// The encoded string to decode.
        /// </param>
        /// <returns>The decoded string.</returns>
        /// <exception cref="ArgumentNullException">
        /// A string is passed in as a null reference.
        /// </exception>
        /// <remarks>
        /// Decodes a quoted-printable encoded string into a string
        /// of unencoded text of any size.
        /// </remarks>
        public static string Decode(string encoded)
        {
            if (encoded == null)
                throw new ArgumentNullException();

            string line;
            StringWriter sw = new StringWriter();
            StringReader sr = new StringReader(encoded);
            try
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.EndsWith("="))
                        sw.Write(HexDecoder(line.Substring(0, line.Length - 1)));
                    else
                        sw.WriteLine(HexDecoder(line));

                    sw.Flush();
                }
                return sw.ToString();
            }
            finally
            {
                sw.Close();
                sr.Close();
                sw = null;
                sr = null;
            }
        }
    }
}
