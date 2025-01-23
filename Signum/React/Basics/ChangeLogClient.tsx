import { DateTime } from "luxon";
import { ajaxGet, ajaxPost } from "../Services";
import { RouteObject } from "react-router";

export namespace ChangeLogClient {
  
  export function start(options: { routes: RouteObject[], applicationName: string, mainChangeLog: () => Promise<{ default: ChangeLogDic }> }): void {
    Options.mainChangeLog = options.mainChangeLog;
    Options.applicationName = options.applicationName;
    registerChangeLogModule("Signum", () => import("../../Changelog"))
  }
  
  export interface ChangeItem {
    module: string;
    deployDate: string;
    implDate: string;
    changeLog: ChangeLogLine[];
  }
  
  export async function getChangeLogs(): Promise<ChangeItem[]> {
    var [mainLog, modules] = await Promise.all([
      Options.mainChangeLog().then(a => a.default),
      Promise.all(Object.entries(Options.changeLogs).map(async ([modName, v]) => ({ modName, dic: (await v()).default }))
      )]);
  
    var modLogs = modules.flatMap((m, i) => {
      return Object.entries(m.dic).map<ChangeItem>(([date, changeLog]) => {
        return { module: m.modName, implDate: date, deployDate: null! as string, changeLog: Array.isArray(changeLog) ? [...changeLog] : [changeLog] };
      });
    });
  
    var mainLogs = Object.entries(mainLog).map<ChangeItem>(([date, changeLog]) => {
      return { module: Options.applicationName, deployDate: date, implDate: date, changeLog: Array.isArray(changeLog) ? [...changeLog] : [changeLog]  };
    });
  
    var result: ChangeItem[] = [];
  
    const regex = /Update (?<mod>\w+)( to (?<date>\d{4}.\d{2}.\d{2}))?/;
  
    mainLogs.orderBy(log => log.deployDate).forEach((log, i) => {
  
      result.push(log);
  
      log.changeLog.extract(cl => {
  
        var a = regex.exec(cl);
  
        if (a) {
          var mod = a.groups!.mod;
          var date = a.groups!.date ?? log.deployDate;
  
          var newModueles = modLogs.extract(l => (l.module == mod || l.module.startsWith(mod + ".")) && l.implDate < date);
          newModueles.forEach(a => a.deployDate = date);
  
          result.push(...newModueles);
  
          return true; 
        }
  
        return false;
      });
  
      if (i == (mainLogs.length - 1)) {
        modLogs.forEach(a => a.deployDate = log.deployDate);
        result.push(...modLogs);
        modLogs.clear();
      }
    });
  
    return result;
  }
  
  export namespace Options {
    export let applicationName: string;
    export let mainChangeLog: () => Promise<{ default: ChangeLogDic }>;
    export let changeLogs: { [key: string]: () => Promise<{ default: ChangeLogDic }> } = {};
  }
  
  export namespace API {
    export function getLastDate(): Promise<string | null> {
      return ajaxGet({ url: "/api/changelog/getLastDate" });
    }
  
    export function updateLastDate(): Promise<string | null> {
      return ajaxPost({ url: "/api/changelog/updateLastDate" }, null);
    }
  }

  export function registerChangeLogModule(name: string, loader: () => Promise<{ default: ChangeLogDic }>): void {
    Options.changeLogs[name] = loader;
  }
}

export interface ChangeLogDic {
  [date: string]: ChangeLogLine | ChangeLogLine[];
}

export type ChangeLogLine = ("Update Signum" | "Update Signum to yyyy.MM.dd" | string & {});

