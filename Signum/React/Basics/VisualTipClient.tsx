import * as AppContext from "../AppContext";
import { ajaxGet, ajaxPost } from "../Services";
import { RouteObject } from "react-router";

export namespace VisualTipClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    AppContext.clearSettingsActions.push(() => API.cached = null);
  }
  
  export namespace API {
  
    export let cached: Promise<string[] | null> | null | undefined  = null;
  
    export function getConsumed(): Promise<string[] | null> {
      return (cached ??= ajaxGet({ url: "/api/visualtip/getConsumed" }));
    }
  
    export function consume(symbolKey: string): Promise<null> {
      cached = null;
      return ajaxPost({ url: "/api/visualtip/consume" }, symbolKey);
    }
  }
}
