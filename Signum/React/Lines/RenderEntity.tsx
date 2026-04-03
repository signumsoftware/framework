import * as React from 'react'
import { Navigator, ViewPromise } from '../Navigator'
import { TypeContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, getTypeInfo, ReadonlyBinding, tryGetTypeInfo } from '../Reflection'
import { ModifiableEntity, Lite, Entity, isLite, isModifiableEntity } from '../Signum.Entities'
import { ErrorBoundary } from '../Components';
import { useAPI, useForceUpdate } from '../Hooks'
import { FunctionalAdapter } from '../Modals'
import { AsEntity } from './EntityBase'

export interface RenderEntityProps<V extends ModifiableEntity | Lite<Entity> | null> {
  ctx: TypeContext<V>;
  getComponent?: (ctx: TypeContext<AsEntity<V>>) => React.ReactElement;
  getViewPromise?: (e: AsEntity<V>) => undefined | string | ViewPromise<AsEntity<V>>;
  onRefresh?: () => void;
  onEntityLoaded?: () => void;
  extraProps?: any;
  currentDate?: string;
  previousDate?: string;
}

interface FuncBox<V  extends ModifiableEntity> {
  func: ((ctx: TypeContext<V>) => React.ReactElement);
  lastEntity: V;
}

export function RenderEntity<V extends ModifiableEntity | Lite<Entity> | null>(p: RenderEntityProps<V>): React.ReactElement | null {

  var e = p.ctx.value

  Navigator.useFetchAndRemember(isLite(e) && p.ctx.propertyRoute != null ? e : null, p.onEntityLoaded);
  var entity = (isLite(e) ? e.entity : e) as AsEntity<V>;
  var entityComponent = React.useRef<React.Component | null>(null);
  var forceUpdate = useForceUpdate();

  var componentBox = useAPI<FuncBox<AsEntity<V>> | "useGetComponent" | null>(() => {
    if (p.ctx.propertyRoute == null)
      return Promise.resolve(null);

    if (p.getComponent)
      return Promise.resolve("useGetComponent");

    if (entity == null)
      return Promise.resolve(null);

    var vp = p.getViewPromise && p.getViewPromise(entity);
    var viewPromise = vp == undefined || typeof vp == "string" ? Navigator.getViewPromise(entity, vp) : vp;
    return viewPromise.promise.then(p => ({ func: p, lastEntity: entity! }) as FuncBox<AsEntity<V>>);
  }, [entity, p.getComponent == null, p.getViewPromise && entity && toViewName(p.getViewPromise(entity))], { avoidReset: true });

  if (p.ctx.propertyRoute == null)
    return null;

  if (entity == undefined)
    return null;

  if (componentBox == null)
    return null;

  if (componentBox == "useGetComponent" && p.getComponent == null)
    return null;

  const lastEntity = typeof componentBox == "object" ? componentBox.lastEntity : entity;

  const ti = tryGetTypeInfo(entity.Type);

  const ctx = p.ctx;

  const pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);

  const prefix = ctx.propertyRoute!.typeReference().isLite ? ctx.prefix + ".entity" : ctx.prefix;
  const frame: EntityFrame = {
    tabs: undefined,
    frameComponent: { forceUpdate: () => { forceUpdate(); p.onRefresh?.(); }, type: RenderEntity },
    entityComponent: entityComponent.current,
    pack: { entity: lastEntity, canExecute: {} },
    revalidate: () => p.ctx.frame && p.ctx.frame.revalidate(),
    onClose: () => { throw new Error("Not implemented Exception"); },
    onReload: pack => { throw new Error("Not implemented Exception"); },
    setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
    refreshCount: (ctx.frame ? ctx.frame.refreshCount : 0),
    allowExchangeEntity: false,
    prefix: prefix,
    isExecuting: () => false,
    execute: () => { throw new Error("Not implemented Exception"); },
    currentDate: p.currentDate,
    previousDate: p.previousDate,
  };

  function setComponent(c: React.Component<any, any> | null) {
    if (c && entityComponent.current != c) {
      entityComponent.current = c;
      forceUpdate();
    }
  }


  const newCtx = new TypeContext<AsEntity<V>>(ctx, { frame }, pr, new ReadonlyBinding(lastEntity, ""), prefix);
  if (ctx.previousVersion && ctx.previousVersion.value)
    newCtx.previousVersion = { value: ctx.previousVersion.value as any };
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
function toViewName(result: undefined | string | ViewPromise<any>): string | undefined {
  return (result instanceof ViewPromise ? Anonymous : result);
}
