import { RouteObject } from 'react-router'
import { ImportComponent } from '@framework/ImportComponent';
import { Lite, Entity, EntityPack, getToString } from '@framework/Signum.Entities';
import { Type, getTypeInfo, typeAllowedInDomain } from '@framework/Reflection';
import { Finder } from '@framework/Finder';
import { Navigator } from '@framework/Navigator';
import { Constructor } from '@framework/Constructor';
import MessageModal from '@framework/Modals/MessageModal';
import { SubPageMessage } from '../Signum.Toolbar';

export namespace SubsClient {
 
  export function start(options: { routes: RouteObject[] }): void {

    options.routes.push({ path: "/sub/:parenttype/:parentid/:childtype", element: <ImportComponent onImport={() => getSubFramePage()} /> });
  }

  export const subs: {
    [parentType: string]: { [subType: string]: (parent: Lite<Entity>) => Promise<EntityPack<Entity> | undefined> | undefined }
  } = {};

  export function registerSubAuto<P extends Entity, S extends Entity>(parentType: Type<P>, subType: Type<S>, lambdaToProperty: (v: S) => P | Lite<P> | null | undefined): void {
    const member = subType.memberInfo(lambdaToProperty);
    registerSub(parentType, subType, parent => {
      return Finder.fetchLites({
        queryName: subType,
        filterOptions: [{ token: subType.token(lambdaToProperty), value: parent }]
      }).then(async list => {
        var entity = list.singleOrNull();
        if (entity)
          return Navigator.API.fetchEntityPack(entity);

        const typeName = getTypeInfo(subType).niceName;
        const parentName = getToString(parent);

        if (!typeAllowedInDomain(subType, parent, true)) {

          if (typeAllowedInDomain(subType, parent)) {
            await MessageModal.showError(SubPageMessage.No0FoundIn1.niceToString(typeName, parentName));
            return undefined;
          }

          await MessageModal.showError(SubPageMessage.NotAllowedToCreate0In1.niceToString(typeName, parentName));
          return undefined;
        }

        debugger;
        return Constructor.constructPack(subType, { [member.name.firstLower()]: parent } as Partial<S>);
      });
    });
  }

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
