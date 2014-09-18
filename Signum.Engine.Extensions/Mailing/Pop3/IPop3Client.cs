using Signum.Entities;
using Signum.Entities.Mailing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Extensions.Mailing.Pop3
{
    public interface IPop3Client : IDisposable
    {
        List<MessageInfo> GetMessageInfos();

        EmailMessageDN GetMessage(MessageInfo messageInfo, Lite<Pop3ReceptionDN> reception);

        void DeleteMessage(MessageInfo messageInfo);
        void Disconnect();
    }

    public struct MessageInfo
    {
        public MessageInfo(string uid, int number, int size)
        {
            Uid = uid;
            Number = number;
            Size = size;
        }

        public readonly string Uid;
        public readonly int Number;
        public readonly int Size;
    }
}
