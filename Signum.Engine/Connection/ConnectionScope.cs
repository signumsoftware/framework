using System;
using System.Collections.Generic;
using System.Text;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.IO;

namespace Signum.Engine
{
    public class ConnectionScope: IDisposable
    {
        static readonly IVariable<BaseConnection> currentConnection = Statics.ThreadVariable<BaseConnection>("connection");

        BaseConnection oldConnection;

        public ConnectionScope(BaseConnection connection)
        {
            oldConnection = currentConnection.Value;

            currentConnection.Value = connection;
        }

        public void Dispose()
        {
            currentConnection.Value = oldConnection;
        }

        public static BaseConnection Current
        {
            get { return currentConnection.Value ?? Default; }
        }

        static BaseConnection @default;
        public static BaseConnection Default
        {
            get { return @default; }
            set { @default = value; }
        }
    }
}
