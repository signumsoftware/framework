using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Resources;
using System.Reflection;
using System.Diagnostics;

namespace Signum.Utilities
{
    public static class StreamExtensions
    {
        const int BufferSize = 32768;

        public static byte[] ReadAllBytes(this Stream str)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                str.CopyTo(ms);
                return ms.ToArray();
            }
        }


        public static void WriteAllBytes(this Stream str, byte[] data)
        {
            str.Write(data, 0, data.Length);
        }

        public static string ReadResourceStream(this Assembly assembly, string name, Encoding encoding = null)
        {
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    throw new MissingManifestResourceException("{0} not found on {1}".FormatWith(name, assembly));

                using (StreamReader reader = encoding == null ? new StreamReader(stream) : new StreamReader(stream, encoding))
                    return reader.ReadToEnd();
            }
        }

        static int BytesToRead = sizeof(Int64);
        public static bool StreamsAreEqual(Stream first, Stream second)
        {
            int iterations = (int)Math.Ceiling((double)first.Length / BytesToRead);

            byte[] one = new byte[BytesToRead];
            byte[] two = new byte[BytesToRead];

            for (int i = 0; i < iterations; i++)
            {
                first.Read(one, 0, BytesToRead);
                second.Read(two, 0, BytesToRead);

                if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                    return false;
            }

            return true;
        }

        public static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            using (FileStream s1 = first.OpenRead())
            using (FileStream s2 = second.OpenRead())
            {
                return StreamsAreEqual(s1, s2);
            }
        }

        [DebuggerStepThrough]
        public static R Using<T, R>(this T disposable, Func<T, R> function)
            where T : IDisposable
        {
            //using (disposable)
            //    return function(disposable);

            try
            {
                return function(disposable);
            }
            catch (Exception e)
            {

                if (disposable is IDisposableException de)
                    de.OnException(e);

                throw;
            }
            finally
            {
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        [DebuggerStepThrough]
        public static void EndUsing<T>(this T disposable, Action<T> action)
            where T : IDisposable
        {
            try
            {
                action(disposable);
            }
            catch (Exception e)
            {

                if (disposable is IDisposableException de)
                    de.OnException(e);

                throw;
            }
            finally
            {
                if (disposable != null)
                    disposable.Dispose();
            }
        }
    }

    public interface IDisposableException : IDisposable
    {
        void OnException(Exception ex);
    }

    public class ProgressStream : Stream
    {
        readonly Stream InnerStream;

        public event EventHandler ProgressChanged;

        public ProgressStream(Stream innerStream)
        {
            this.InnerStream = innerStream;
        }

        public double GetProgress()
        {
            return ((double)Position) / Length;
        }

        public override bool CanRead
        {
            get { return InnerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return InnerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return InnerStream.CanWrite; }
        }

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override long Length
        {
            get { return InnerStream.Length; }
        }

        public override long Position
        {
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = InnerStream.Read(buffer, offset, count);
            ProgressChanged?.Invoke(this, EventArgs.Empty);
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            InnerStream.Write(buffer, offset, count);
            ProgressChanged?.Invoke(this, EventArgs.Empty);
        }

        public override void Close()
        {
            InnerStream.Close();
        }
    }
}
