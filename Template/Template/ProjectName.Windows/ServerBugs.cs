using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using $custommessage$.Services;

namespace $custommessage$.Windows
{
    public static class Server$custommessage$
    {
        static ChannelFactory<IServer$custommessage$> channelFactory;

        static IServer$custommessage$ current;
        public static IServer$custommessage$ Current
        {
            get { return GetCurrent(); }
        }

        public static IServer$custommessage$ GetCurrent()
        {
            if (current == null || ((ICommunicationObject)current).State == CommunicationState.Faulted)
            {
                if (!NewServer())
                    throw new ApplicationException("Connection with the server is needed to continue");
            }

            return current;
        }

        public static bool NewServer()
        {
            try
            {
                if (channelFactory == null)
                    channelFactory = new ChannelFactory<IServer$custommessage$>("server");

                current = channelFactory.CreateChannel();

                return true;
            }
            catch
            {
                current = null;
                throw;
            }
        }
    }
}
