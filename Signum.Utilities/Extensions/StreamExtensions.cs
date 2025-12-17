using System.IO;
using System.Resources;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Signum.Utilities;

public static class StreamExtensions
{
    public static byte[] ReadAllBytes(this Stream str)
    {
        if (str.CanSeek && str.Position > 0)
            throw new InvalidOperationException("Already started reading");

        using (MemoryStream ms = new MemoryStream())
        {
            str.CopyTo(ms);
            return ms.ToArray();
        }
    }

    public static async Task<byte[]> ReadAllBytesAsync(this Stream str)
    {
        if (str.CanSeek && str.Position > 0)
            throw new InvalidOperationException("Already started reading");

        using (MemoryStream ms = new MemoryStream())
        {
            await str.CopyToAsync(ms);
            return ms.ToArray();
        }
    }


    public static void WriteAllBytes(this Stream str, byte[] data)
    {
        if (str.CanSeek && str.Position > 0)
            throw new InvalidOperationException("Already started writing");

        str.Write(data, 0, data.Length);
    }

    public static string ReadResourceStream(this Assembly assembly, string name, Encoding? encoding = null)
    {
        using (Stream? stream = assembly.GetManifestResourceStream(name))
        {
            if (stream == null)
                throw new MissingManifestResourceException("{0} not found on {1}".FormatWith(name, assembly));

            using (StreamReader reader = encoding == null ? new StreamReader(stream) : new StreamReader(stream, encoding))
                return reader.ReadToEnd();
        }
    }

    public static bool StreamsAreEqual(Stream stream1, Stream stream2)
    {
        if (stream1 == null || stream2 == null)
            throw new ArgumentNullException("Streams cannot be null.");

        if (!stream1.CanRead || !stream2.CanRead)
            throw new ArgumentException("Streams must be readable.");

        // Ensure streams start at the beginning
        if (stream1.CanSeek) stream1.Seek(0, SeekOrigin.Begin);
        if (stream2.CanSeek) stream2.Seek(0, SeekOrigin.Begin);

        const int bufferSize = 4096;
        byte[] buffer1 = new byte[bufferSize];
        byte[] buffer2 = new byte[bufferSize];

        int bytesRead1, bytesRead2;

        do
        {
            bytesRead1 = stream1.Read(buffer1, 0, buffer1.Length);
            bytesRead2 = stream2.Read(buffer2, 0, buffer2.Length);

            // If the number of bytes read is different, streams are not equal
            if (bytesRead1 != bytesRead2)
                return false;

            // Compare the bytes
            for (int i = 0; i < bytesRead1; i++)
            {
                if (buffer1[i] != buffer2[i])
                    return false;
            }
        } while (bytesRead1 > 0);

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
        where T : IDisposable? 
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
    public static R Using<T, R>(this T disposable, Func<R> function)
        where T : IDisposable?
    {
        //using (disposable)
        //    return function(disposable);

        try
        {
            return function();
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
        where T : IDisposable? 
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

    public event EventHandler? ProgressChanged;

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
