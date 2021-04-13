import { Dic } from './Globals';
import { Entity, ModifiableEntity, SelectorMessage, EntityPack } from './Signum.Entities';
import { Type, getTypeInfo, OperationType, New, OperationInfo, PropertyRoute, tryGetTypeInfo } from './Reflection';
import SelectorModal from './SelectorModal';
import * as Operations from './Operations';
import * as Navigator from './Navigator';
import {API} from "./Navigator";

export const customConstructors: { [typeName: string]: (props?: any, pr?: PropertyRoute) => ModifiableEntity | Promise<ModifiableEntity | undefined> } = {}

export function construct<T extends ModifiableEntity>(type: Type<T>, props?: Partial<T>, pr?: PropertyRoute): Promise<T | undefined>;
export function construct(type: string, props?: any, pr?: PropertyRoute): Promise<ModifiableEntity | undefined>;
export function construct(type: string | Type<any>, props?: any, pr?: PropertyRoute): Promise<ModifiableEntity | undefined> {
  return constructPack(type as string, props, pr)
    .then(pack => pack?.entity);
}

export function constructPack<T extends ModifiableEntity>(type: Type<T>, props?: Partial<T>, pr?: PropertyRoute): Promise<EntityPack<T> | undefined>;
export function constructPack(type: string, props?: any, pr?: PropertyRoute): Promise<EntityPack<ModifiableEntity> | undefined>;
export function constructPack(type: string | Type<any>, props?: any, pr?: PropertyRoute): Promise<EntityPack<ModifiableEntity> | undefined> {
  
  const typeName = (type as Type<any>).typeName ?? type as string;

  const ti = tryGetTypeInfo(typeName);
  if (ti)
    pr = PropertyRoute.root(ti);
  
  const c = customConstructors[typeName];
  if (c)
    return asPromise(c(props, pr)).then<EntityPack<ModifiableEntity> | undefined>(e => {
      if (e == undefined)
        return undefined;

      assertCorrect(e);
      return Navigator.toEntityPack(e);
    });


  if (ti) {

    if (ti.hasConstructorOperation) {

      const ctrs = Dic.getValues(ti.operations!).filter(a => a.operationType == "Constructor");

      if (!ctrs.length)
        throw new Error("No constructor is allowed!");

      return SelectorModal.chooseElement(ctrs, { buttonDisplay: c => c.niceName, buttonName: c => c.key, message: SelectorMessage.PleaseSelectAConstructor.niceToString() })
        .then((oi: OperationInfo | undefined) => {

          if (!oi)
            return undefined;

          const settings = Operations.getSettings(oi.key) as Operations.ConstructorOperationSettings<Entity>;

          var ctx = new Operations.ConstructorOperationContext(oi, settings, ti);

          if (settings?.onConstruct)
            return settings.onConstruct(ctx, props);

          return ctx.defaultConstruct().then(p => {
            p && props && Dic.assign(p.entity, props);
            return p;
          });
        }).then((p: EntityPack<Entity> | undefined) => {
          if (p == undefined)
            return undefined;

          assertCorrect(p.entity);
          return p;
        });
    }
  }

  const result = New(typeName, props, pr);

  assertCorrect(result);

  return Navigator.toEntityPack(result);
}

function asPromise<T>(valueOrPromise: T | Promise<T>) {
  if (valueOrPromise && (valueOrPromise as Promise<T>).then)
    return valueOrPromise as Promise<T>;

  return Promise.resolve(valueOrPromise as T);
}

function assertCorrect(m: ModifiableEntity) {
  if (m && !m.isNew && !(m as Entity).id)
    throw new Error("Member 'isNew' expected after constructor");

  if (m.modified == undefined)
    throw new Error("Member 'modified' expected after constructor");
}

export function registerConstructor<T extends ModifiableEntity>(type: Type<T>, constructor: (props?: Partial<T>, pr?: PropertyRoute) => T | Promise<T | undefined>, options?: { override?: boolean }) {
  if (customConstructors[type.typeName] && !(options?.override))
    throw new Error(`Constructor for ${type.typeName} already registered`);

  customConstructors[type.typeName] = constructor;
}

export function clearCustomConstructors() {
  Dic.clear(customConstructors);
}
