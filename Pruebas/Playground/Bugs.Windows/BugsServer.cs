using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Bugs.Contract;

namespace Bugs.Windows
{
    public static class ServerBugs
    {
        static ChannelFactory<IServerBugs> channelFactory;

        static IServerBugs current;
        public static IServerBugs Current
        {
            get { return GetCurrent(); }
        }

        public static IServerBugs GetCurrent()
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
                    channelFactory = new ChannelFactory<IServerBugs>("server");

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
