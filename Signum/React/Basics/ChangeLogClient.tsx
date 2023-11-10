import { ajaxGet, ajaxPost } from "../Services";
import { RouteObject } from "react-router";

export function start(options: { routes: RouteObject[], applicationName: string, mainChangeLog: () => Promise<{ default: ChangeLogDic }> }) {

  changeLogs["Framework"] = () => import("./Changelog");
  changeLogs[options.applicationName] = options.mainChangeLog;

}

export let changeLogs: { [key: string]: () => Promise<{ default: ChangeLogDic }> } = {};
export module API {
  export function getLastDate(): Promise<string | null> {
    return ajaxGet({ url: "/api/changelog/getLastDate" });
  }
}

export interface ChangeLogDic {
  [date: string]: ChangeLogLine | ChangeLogLine[];
}

export type ChangeLogLine = ("Update Signum" | "Update Signum to yyyy.MM.dd" | string & {});

export function registerChangeLogModule(name: string, loader: () => Promise<{ default: ChangeLogDic}>) {
  changeLogs[name] = loader;
}
