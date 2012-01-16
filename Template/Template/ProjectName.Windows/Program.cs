using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Windows;
using System.ServiceModel.Security;
using System.Threading;
using System.Globalization;
using Signum.Utilities;
using Signum.Windows;
using Signum.Services;
using $custommessage$.Services;

namespace $custommessage$.Windows
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Server.SetNewServerCallback(NewServer);

                Server.Connect();

                App app = new App() { ShutdownMode = ShutdownMode.OnMainWindowClose };
                app.Run(new Main());              
            }
            catch (Exception e)
            {
                HandleException("Start-up error", e);
            }
        }

        public static void HandleException(string errorTitle, Exception e)
        {
            if (e is MessageSecurityException)
            {
                MessageBox.Show("Session expired", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Hand);

            }
            else
            {
                var bla = e.FollowC(ex => ex.InnerException);

                MessageBox.Show(
                    bla.ToString(ex => "{0} : {1}".Formato(ex.GetType().Name, ex.Message), "\r\n\r\n"),
                    errorTitle + ":",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        static ChannelFactory<IServer$custommessage$> channelFactory;

        public static IBaseServer NewServer()
        {
            if (channelFactory == null)
                channelFactory = new ChannelFactory<IServer$custommessage$>("server");

            //Add Login here
            IServer$custommessage$ result = channelFactory.CreateChannel();

            return result; 
        }
    }
}
