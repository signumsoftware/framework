import * as React from 'react'
import * as Navigator from '../Navigator'
import { TypeContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, getTypeInfo, ReadonlyBinding } from '../Reflection'
import { ModifiableEntity, Lite, Entity, isLite, isModifiableEntity } from '../Signum.Entities'
import { ViewPromise } from "../Navigator";
import { ErrorBoundary } from '../Components';
import { FunctionalAdapter } from '../Frames/FrameModal';
import { useFetchAndRemember, useAPI, useForceUpdate } from '../Hooks'

export interface RenderEntityProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
  getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
  getViewPromise?: (e: any /*T*/) => undefined | string | Navigator.ViewPromise<any>;
  onEntityLoaded?: () => void;
  extraProps?: any;
}

export function RenderEntity(p: RenderEntityProps) {

  var e = p.ctx.value

  var entityFromLite = useFetchAndRemember(isLite(e) ? e : null, p.onEntityLoaded);
  var entity = isLite(e) ? e.entity : e;
  var entityComponent = React.useRef<React.Component | null>(null);
  var forceUpdate = useForceUpdate();

  var getComponent = useAPI(null, signal => {
    if (p.getComponent)
      return Promise.resolve(p.getComponent);

    if (entity == null)
      return Promise.resolve(null);

    var vp = p.getViewPromise && p.getViewPromise(entity);
    var viewPromise = vp == undefined || typeof vp == "string" ? Navigator.getViewPromise(entity, vp) : vp;
    return viewPromise.promise;

  }, [entity, p.getComponent == null, p.getViewPromise && entity && toViewName(p.getViewPromise(entity))]);


  if (entity == undefined)
    return null;

  if (getComponent == null)
    return null;

  const ti = getTypeInfo(entity.Type);

  const ctx = p.ctx;

  const pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);

  const frame: EntityFrame = {
    frameComponent: { forceUpdate },
    entityComponent: entityComponent.current,
    pack: undefined,
    revalidate: () => p.ctx.frame && p.ctx.frame.revalidate(),
    onClose: () => { throw new Error("Not implemented Exception"); },
    onReload: pack => { throw new Error("Not implemented Exception"); },
    setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
    refreshCount: (ctx.frame ? ctx.frame.refreshCount : 0),
    allowChangeEntity: false,
  };

  function setComponent(c: React.Component<any, any> | null) {
    if (c && entityComponent.current != c) {
      entityComponent.current = c;
      forceUpdate();
    }
  }

  var prefix = ctx.propertyRoute.typeReference().isLite ? ctx.prefix + ".entity" : ctx.prefix;

  const newCtx = new TypeContext<ModifiableEntity>(ctx, { frame }, pr, new ReadonlyBinding(entity, ""), prefix);

  var element = getComponent(newCtx);

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
