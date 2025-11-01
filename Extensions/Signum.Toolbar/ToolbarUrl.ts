import { Dic } from "../../Signum/React/Globals";
import { Lite, Entity, liteKey, getToString } from "../../Signum/React/Signum.Entities";

export namespace ToolbarUrl {

  export function replaceVariables(url: string): string { 
    Dic.getKeys(urlVariables).forEach(v => {
      url = url.replaceAll(v, urlVariables[v]())
    })
    return url
  }
  export const urlVariables: { [name: string]: () => string } = {};

  export function registerUrlVariable(name: string, getValue: () => string): void {
    urlVariables[name] = getValue;
  }

  export function replaceEntity(url: string, selectedEntity: Lite<Entity>)  : string {
    url = url
      .replaceAll(":id", selectedEntity.id!.toString())
      .replace(":type", selectedEntity.EntityType)
      .replace(":key", liteKey(selectedEntity))
      .replace(":toStr", getToString(selectedEntity))
    return url
  }

  export function hasSubEntity(url: string): boolean{
    return url.contains(":type2") || url.contains(":id2") || url.contains(":key2")
  }

  export function replaceSubEntity(url: string, subEntity: Lite<Entity>): string {
    url = url
      .replaceAll(":id2", subEntity.id!.toString())
      .replace(":type2", subEntity.EntityType)
      .replace(":key2", liteKey(subEntity))
      .replace(":toStr2", getToString(subEntity))
    return url
  }

  export function isExternalLink(url: string): boolean {
    return url.startsWith("http") && !url.startsWith(window.location.origin + "/" + window.__baseName)
  }
}

