using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Signum.Engine.Mailing.Pop3
{
    public class Pop3Client : IDisposable
    {
        string userName;
        string password;
        string server;
        bool useSSL;

        TcpClient connection;
        Stream stream;
        StreamReader reader;

        public Pop3Client(string userName, string password, string server, int port, bool useSSL)
        {
            this.userName = userName;
            this.password = password;
            this.server = server;
            this.useSSL = useSSL;
        }

        public void Connect()
        {
            connection = new TcpClient();
            connection.Connect(server, 110);

            if (useSSL)
            {
                var sslStream = new SslStream(connection.GetStream(), false);
                sslStream.AuthenticateAsClient(server);
                stream = sslStream;
            }
            else
            {
                stream = connection.GetStream();
            }

            reader = new StreamReader(stream, Encoding.UTF8);

            string response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new Pop3ClientException("Not ready for Log-in " + response);

            SendCommand("USER " + userName + "\r\n");
            response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new Pop3ClientException("Log-in not accepted " + response);

            SendCommand("PASS " + password + "\r\n");
            response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new Pop3ClientException("Password not accepted " + response);
        }

        private void SendCommand(string p)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(p);
            stream.Write(buffer, 0, buffer.Length);
        }

        public Dictionary<int, int> GetMessageSizes()
        {
            SendCommand("LIST\r\n");
            string response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new Pop3ClientException("List emails not accepted: " + response);

            Dictionary<int, int> returnValue = new Dictionary<int, int>();
            while (!(response = reader.ReadLine()).Equals("."))
            {
                string[] parts = response.Split(' ');
                if (parts.Length == 2)
                {
                    int id = System.Convert.ToInt32(parts[0]);
                    int size = System.Convert.ToInt32(parts[1]);
                    returnValue[id] = size;
                }
            }

            return returnValue;
        }

        public Dictionary<int, string> GetMessageUniqueIdentifiers()
        {
            SendCommand("UIDL\r\n");
            string response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new Pop3ClientException("List emails not accepted: " + response);

            Dictionary<int, string> returnValue = new Dictionary<int, string>();
            while (!(response = reader.ReadLine()).Equals("."))
            {
                string[] parts = response.Split(' ');
                if (parts.Length == 2)
                {
                    int id = System.Convert.ToInt32(parts[0]);
                    string uid = parts[1];
                    returnValue[id] = uid;
                }
            }

            return returnValue;
        }

        public string GetMessage(int i)
        {
            SendCommand("RETR " + i + "\r\n");
            string response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new Pop3ClientException("Retrieve messege not accepted: " + response);

            StringBuilder sb = new StringBuilder();
            while (!(response = reader.ReadLine()).Equals("."))
            {
                if (response.StartsWith(".."))
                    sb.AppendLine(response.Substring(1));
                else
                    sb.AppendLine(response);
            }
            return sb.ToString();
        }

        public void DeleteMessage(int i)
        {
            StringBuilder returnValue = new StringBuilder();
            SendCommand("DELE " + i + "\r\n");
            string response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new InvalidOperationException("Delete not accepted: " + response);
        }

        public void Disconnect()
        {
            StringBuilder returnValue = new StringBuilder();
            SendCommand("QUIT\r\n");
            string response = reader.ReadLine();
            if (!response.StartsWith("+OK"))
                throw new InvalidOperationException("Disconnect not accepted: " + response);
        }

        public void Dispose()
        {
            reader.Dispose();
            ((IDisposable)connection).Dispose();
        }
    }

    [Serializable]
    public class Pop3ClientException : Exception
    {
        public Pop3ClientException() { }
        public Pop3ClientException(string message) : base(message) { }
        public Pop3ClientException(string message, Exception inner) : base(message, inner) { }
        protected Pop3ClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
