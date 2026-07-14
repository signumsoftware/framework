import * as AppContext from "../AppContext";
import { ajaxGet, ajaxPost } from "../Services";
import { RouteObject } from "react-router";

export namespace VisualTipClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    AppContext.clearSettingsActions.push(() => API.state.cached = null);
  }

  export namespace API {

    export const state = {
      cached: null as Promise<string[] | null> | null | undefined
    };

    export function getConsumed(): Promise<string[] | null> {
      return (state.cached ??= ajaxGet({ url: "/api/visualtip/getConsumed" }));
    }

    export function consume(symbolKey: string): Promise<null> {
      state.cached = null;
      return ajaxPost({ url: "/api/visualtip/consume" }, symbolKey);
    }
  }
}
