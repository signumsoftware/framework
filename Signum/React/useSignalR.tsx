import * as React from 'react'
import * as AppContext from './AppContext'
import * as signalR from '@microsoft/signalr'
import { HubConnectionState } from '@microsoft/signalr'
import { useForceUpdate } from './Hooks';

//Originally from https://github.com/pguilbert/react-use-signalr/

declare global {
  interface Window {
    __disableSignalR: string | null;
  }
}

let messageShownFor: string[] = [];
export function useSignalRConnection(url: string, options?: signalR.IHttpConnectionOptions): signalR.HubConnection | undefined {

  if (window.__disableSignalR) {

    if (!messageShownFor.contains(url)) {
      console.warn("Skipped:" + url);
      console.warn(window.__disableSignalR);
      messageShownFor.push(url);
    }

    return undefined;
  }

  const connection = React.useMemo<signalR.HubConnection | undefined>(() => {

    var connection = new signalR.HubConnectionBuilder()
      .withUrl(AppContext.toAbsoluteUrl(url, window.__baseNameAPI), options ?? {})
      .withAutomaticReconnect()
      .build();
    return connection;
  }, [url]);

  const forceUpdate = useForceUpdate();

  React.useEffect(() => {

    if (!connection) {
      return;
    }

    let isMounted = true;
    const updateState = () => {
      if (isMounted) {
        forceUpdate();
      }
    }
    connection.onclose(updateState);
    connection.onreconnected(updateState);
    connection.onreconnecting(updateState);

    if (connection.state === signalR.HubConnectionState.Disconnected) {
      const promise = connection
        .start()
        .then(() => { forceUpdate() });

      forceUpdate();

      return () => {
        promise
          .then(() => {
            connection.stop();
          });
        isMounted = false;
      };
    }

    return () => {
      connection.stop();
    };
  }, [connection]);

  return connection;
}

export function useSignalRGroup(connection: signalR.HubConnection | undefined, options: {
  enterGroup: (connection: signalR.HubConnection) => Promise<void>,
  exitGroup: (connection: signalR.HubConnection) => Promise<void>,
  deps: any[]
}): void {

  React.useEffect(() => {

    if (connection) {
      if (connection.state == signalR.HubConnectionState.Connected) {
        options.enterGroup(connection);

        return () => {
          if (connection.state == signalR.HubConnectionState.Connected) {
            options.exitGroup(connection).catch(e => {
              if (connection.state == signalR.HubConnectionState.Connected)
                throw e;
              else { /* Silent exception */ }
            });
          }
        };
      }
    }
  }, [connection, connection?.state, ...options.deps]);
}

export function useSignalRCallback(connection: signalR.HubConnection | undefined, methodName: string, callback: (...args: any[]) => void, deps: any[]): void {

  var callback = React.useCallback(callback, deps);

  React.useEffect(() => {
    if (!connection || connection.state != HubConnectionState.Connected) {
      return;
    }

    connection.on(methodName, callback);

    return () => {
      connection.off(methodName, callback);
    }

  }, [connection, connection?.state, callback, methodName]);
}
