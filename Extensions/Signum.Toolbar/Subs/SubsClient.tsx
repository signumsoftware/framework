import { RouteObject } from 'react-router'
import { ImportComponent } from '@framework/ImportComponent';
import { Lite, Entity, EntityPack } from '@framework/Signum.Entities';
import { Type } from '@framework/Reflection';

export namespace SubsClient {
 
  export function start(options: { routes: RouteObject[] }): void {

    options.routes.push({ path: "/sub/:parenttype/:parentid/:childtype", element: <ImportComponent onImport={() => getSubFramePage()} /> });
  }

  export const subs: {
    [parentType: string]: { [subType: string]: (parent: Lite<Entity>) => Promise<EntityPack<Entity> | undefined> | undefined }
  } = {};

  export function registerSub<P extends Entity, S extends Entity>(parentType: Type<P>, subType: Type<S>, getSubEntityPack: (parent: Lite<P>) => Promise<EntityPack<S> | undefined> | undefined): void {

    if (!subs[parentType.typeName])
      subs[parentType.typeName] = {};

    subs[parentType.typeName][subType.typeName] = getSubEntityPack as (lite: Lite<Entity>) => Promise<EntityPack<Entity> | undefined> | undefined;
  }

  export async function getSubEntityPack(parent: Lite<Entity>, subType: string): Promise<EntityPack<Entity> | undefined>
  {
    var dic = subs[parent.EntityType];

    if (dic == null)
      throw new Error("No subs registered for " + parent.EntityType);

    var lambda = dic[subType];

    if (lambda == null)
      throw new Error(`Type ${parent.EntityType} does not contains a sub for type ${subType}`);

    return await lambda(parent);
  }
  
  export function getSubFramePage(): Promise<typeof import("./SubFramePage")> {
    return import("./SubFramePage");
  }
}
