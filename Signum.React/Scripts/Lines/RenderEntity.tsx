import * as React from 'react'
import * as Navigator from '../Navigator'
import { TypeContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, getTypeInfo, ReadonlyBinding, tryGetTypeInfo } from '../Reflection'
import { ModifiableEntity, Lite, Entity, isLite, isModifiableEntity } from '../Signum.Entities'
import { ViewPromise, useFetchAndRemember } from "../Navigator";
import { ErrorBoundary } from '../Components';
import { useAPI, useForceUpdate } from '../Hooks'
import { FunctionalAdapter } from '../Modals'

export interface RenderEntityProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
  getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
  getViewPromise?: (e: any /*T*/) => undefined | string | Navigator.ViewPromise<any>;
  onRefresh?: () => void;
  onEntityLoaded?: () => void;
  extraProps?: any;
}

interface FuncBox {
  func: ((ctx: TypeContext<any /*T*/>) => React.ReactElement<any>)
}

export function RenderEntity(p: RenderEntityProps) {

  var e = p.ctx.value

  useFetchAndRemember(isLite(e) && p.ctx.propertyRoute != null ? e : null, p.onEntityLoaded);
  var entity = isLite(e) ? e.entity : e;
  var entityComponent = React.useRef<React.Component | null>(null);
  var forceUpdate = useForceUpdate();

  var componentBox = useAPI<FuncBox | "useGetComponent" | null>(() => {
    if (p.ctx.propertyRoute == null)
      return Promise.resolve(null);

    if (p.getComponent)
      return Promise.resolve("useGetComponent");

    if (entity == null)
      return Promise.resolve(null);

    var vp = p.getViewPromise && p.getViewPromise(entity);
    var viewPromise = vp == undefined || typeof vp == "string" ? Navigator.getViewPromise(entity, vp) : vp;
    return viewPromise.promise.then(p => ({ func: p }));
  }, [entity, p.getComponent == null, p.getViewPromise && entity && toViewName(p.getViewPromise(entity))]);

  if (p.ctx.propertyRoute == null)
    return null;

  if (entity == undefined)
    return null;

  if (componentBox == null)
    return null;

  if (componentBox == "useGetComponent" && p.getComponent == null)
    return null;

  const ti = tryGetTypeInfo(entity.Type);

  const ctx = p.ctx;

  const pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);

  const prefix = ctx.propertyRoute!.typeReference().isLite ? ctx.prefix + ".entity" : ctx.prefix;
  const frame: EntityFrame = {
    tabs: undefined,
    frameComponent: { forceUpdate: () => { forceUpdate(); p.onRefresh?.(); }, type: RenderEntity },
    entityComponent: entityComponent.current,
    pack: { entity, canExecute: {} },
    revalidate: () => p.ctx.frame && p.ctx.frame.revalidate(),
    onClose: () => { throw new Error("Not implemented Exception"); },
    onReload: pack => { throw new Error("Not implemented Exception"); },
    setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
    refreshCount: (ctx.frame ? ctx.frame.refreshCount : 0),
    allowExchangeEntity: false,
    prefix: prefix,
    isExecuting: () => false,
    execute: () => { throw new Error("Not implemented Exception"); }
  };

  function setComponent(c: React.Component<any, any> | null) {
    if (c && entityComponent.current != c) {
      entityComponent.current = c;
      forceUpdate();
    }
  }


  const newCtx = new TypeContext<ModifiableEntity>(ctx, { frame }, pr, new ReadonlyBinding(entity, ""), prefix);

  var element = componentBox == "useGetComponent" ? p.getComponent!(newCtx) : componentBox.func(newCtx);

  if (p.extraProps)
    element = React.cloneElement(element, p.extraProps);

  return (
    <div data-property-path={ctx.propertyPath}>
      <ErrorBoundary>
        {FunctionalAdapter.withRef(element, c => setComponent(c))}
      </ErrorBoundary>
    </div>
  );
}

const Anonymous = "__Anonymous__";
function toViewName(result: undefined | string | Navigator.ViewPromise<ModifiableEntity>): string | undefined {
  return (result instanceof ViewPromise ? Anonymous : result);
}
