import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import { Navigator, ViewPromise } from '@framework/Navigator'
import { Constructor } from '@framework/Constructor'
import { useBlocker, useLocation, useParams } from "react-router-dom"
import { Finder } from '@framework/Finder'
import { ButtonBar, ButtonBarHandle } from '@framework/Frames/ButtonBar'
import { Entity, Lite, getToString, EntityPack, JavascriptMessage, entityInfo, SelectorMessage, is, ModifiableEntity, parseLite } from '@framework/Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame, ButtonBarElement } from '@framework/TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, parseId, OperationType, newLite } from '@framework/Reflection'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { ValidationErrors, ValidationErrorsHandle } from '@framework/Frames/ValidationErrors'
import { ErrorBoundary } from '@framework/Components';
import "@framework/Frames/Frames.css"
import { AutoFocus } from '@framework/Components/AutoFocus';
import { useStateWithPromise, useForceUpdate, useMounted, useDocumentEvent, useWindowEvent, useUpdatedRef } from '@framework/Hooks'
import { Operations } from '@framework/Operations'
import WidgetEmbedded from '@framework/Frames/WidgetEmbedded'
import { useTitle } from '@framework/AppContext'
import { FunctionalAdapter } from '@framework/Modals'
import { QueryString } from '@framework/QueryString'
import { classes } from '@framework/Globals'
import FramePage, { useLooseChanges } from '../../../Signum/React/Frames/FramePage'
import { SubsClient } from './SubsClient'

interface FramePageState {
  pack: EntityPack<Entity>;
  lastEntity: string;
  getComponent: (ctx: TypeContext<Entity>) => React.ReactElement;
  refreshCount: number;
  executing?: boolean;
}

export default function SubFramePage(): React.ReactElement {

  let [state, setState] = useStateWithPromise<FramePageState | undefined>(undefined);
  const stateRef = useUpdatedRef(state);
  const buttonBar = React.useRef<ButtonBarHandle>(null);
  const entityComponent = React.useRef<React.Component | null>(null);
  const validationErrors = React.useRef<React.Component>(null);
  const mounted = useMounted();
  const forceUpdate = useForceUpdate();
  const params = useParams<{ parenttype: string; parentid: string; childtype: string }>();
  const location = useLocation();

  const cti = getTypeInfo(params.childtype!);
  const pti = getTypeInfo(params.parenttype!);

  const settings = Navigator.getSettings(cti);
  const childType = cti.name;
  const parentType = pti.name;

  if (state && state.pack.entity.Type != childType)
    state = undefined;

  useTitle(getToString(state?.pack.entity) ?? "", [state?.pack.entity]);

  useLooseChanges(state && !state.executing ? ({ entity: state.pack.entity, lastEntity: state.lastEntity }) : undefined);

  function setPack(pack: EntityPack<Entity>, view: { viewName?: string, getComponent: (ctx: TypeContext<Entity>) => React.ReactElement }) {
    return setState({
      pack,
      lastEntity: JSON.stringify(pack.entity),
      getComponent: view.getComponent,
      refreshCount: state ? state.refreshCount + 1 : 0
    });
  }

  React.useEffect(() => {

    var currentEntity = stateRef.current?.pack.entity;

    loadEntity()
      .then(pack => {
        if (pack == undefined) {
          Navigator.onFramePageCreationCancelled();
        }
        else {

          loadComponent(pack).then(view => {
            if (!mounted.current)
              return undefined;

            return setPack(pack!, view);
          });
        }
      });
  }, [params.parenttype, params.childtype, params.parentid/*, location.search*/]);


  useWindowEvent("beforeunload", e => {
    if (stateRef.current && hasChanges(stateRef.current)) {
      e.preventDefault(); // If you prevent default behavior in Mozilla Firefox prompt will always be shown
      e.returnValue = '';   // Chrome requires returnValue to be set
    }
  }, []);

  useWindowEvent("keydown", handleKeyDown, []);

  function handleKeyDown(e: KeyboardEvent) {
    if (!e.openedModals && buttonBar.current)
      buttonBar.current.handleKeyDown(e);
  }

  async function loadComponent(pack: EntityPack<Entity>, forceViewName?: string | ViewPromise<ModifiableEntity>): Promise<{
    viewName?: string;
    getComponent: (ctx: TypeContext<Entity>) => React.ReactElement;
  }> {
    if (forceViewName instanceof ViewPromise) {
      var getComponent = await forceViewName.promise;
      return { viewName: undefined, getComponent: getComponent };
    } else {

      const viewName = forceViewName ?? QueryString.parse(location.search).viewName ?? undefined;
      const getComponent = await Navigator.getViewPromise(pack.entity, viewName).promise;

      return { viewName, getComponent };
    }
  }


  async function loadEntity(): Promise<undefined | EntityPack<Entity>> {
    var parentLite = newLite(pti.name, params.parentid!);
    return await SubsClient.getSubEntityPack(parentLite, cti.name);
  }

  function setComponent(c: React.Component | null) {
    if (c && entityComponent.current != c) {
      entityComponent.current = c;
      forceUpdate();
    }
  }

  if (!state) {
    return (
      <div className="normal-control">
        {renderTitle()}
      </div>
    );
  }

  const entity = state.pack.entity;

  const s = state;

  const frame: EntityFrame = {
    tabs: undefined,
    frameComponent: { forceUpdate, type: FramePage },
    entityComponent: entityComponent.current,
    pack: state.pack,
    isExecuting: () => s.executing == true,
    execute: async action => {
      if (s.executing)
        return;

      s.executing = true;
      forceUpdate();
      try {
        await action();
      } finally {
        s.executing = undefined;
        forceUpdate();
      }
    },
    onReload: (pack, reloadComponent, callback) => {

      var packEntity = (pack ?? s.pack) as EntityPack<Entity>;

      if (reloadComponent) {
        setState(undefined)
          .then(() => loadComponent(packEntity, reloadComponent == true ? undefined : reloadComponent))
          .then(gc => {
            if (mounted.current) {
              setPack(packEntity, gc).then(() => callback && callback());
            }
          });
      }
      else {
        setPack(packEntity, { getComponent: s.getComponent }).then(() => callback && callback());
      }
    },
    revalidate: () => validationErrors.current && validationErrors.current.forceUpdate(),
    setError: (ms, initialPrefix) => {
      GraphExplorer.setModelState(entity, ms, initialPrefix || "");
      forceUpdate()
    },
    refreshCount: state.refreshCount,
    allowExchangeEntity: true,
    prefix: "framePage"
  };


  const styleOptions: StyleOptions = {
    readOnly: Navigator.isReadOnly(state.pack),
    frame: frame
  };

  const ctx = new TypeContext<Entity>(undefined, styleOptions, PropertyRoute.root(cti), new ReadonlyBinding(entity, "framePage"));

  const wc: WidgetContext<Entity> = { ctx: ctx, frame: frame };

  var outdated = !state.pack.entity.isNew && (state.pack.entity.Type != childType);

  return (
    <div className="normal-control" style={{ opacity: outdated ? .5 : undefined }}>
      {renderTitle()}
      <div style={state.executing == true ? { opacity: ".7" } : undefined}>
        <div className="sf-button-widget-container">
          {entityComponent.current && <ButtonBar ref={buttonBar} frame={frame} pack={state.pack} />}
        </div>
        <ValidationErrors ref={validationErrors} entity={state.pack.entity} prefix="framePage" />
        <WidgetEmbedded widgetContext={wc} >
          <div className="sf-main-control" data-refresh-count={state.refreshCount} data-main-entity={entityInfo(ctx.value)}>
            <ErrorBoundary>
              {state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(state.getComponent(ctx), c => setComponent(c))}</AutoFocus>}
            </ErrorBoundary>
          </div>
        </WidgetEmbedded>
      </div>
    </div>
  );

  function renderTitle() {

    if (!state)
      return <h3 className="display-6 sf-entity-title">{JavascriptMessage.loading.niceToString()}</h3>;

    const entity = state.pack.entity;
    const title = Navigator.renderEntity(entity); 
    const subTitle = Navigator.getTypeSubTitle(entity, undefined);
    const widgets = renderWidgets(wc, settings?.stickyHeader);

    return (
      <h4 className={classes("border-bottom pb-3 mb-2", settings?.stickyHeader && "sf-sticky-header")} >
        {title && <>
          <span className="sf-entity-title">{title}</span>&nbsp;
        </>
        }
        {(subTitle || widgets) &&
          <div className="sf-entity-sub-title mt-2">
            {subTitle && <small className="sf-type-nice-name text-muted"> {subTitle}</small>}
            {widgets}
            <br />
          </div>
        }
      </h4>
    );
  }
}

function hasChanges(state: FramePageState) {

  if (state.executing)
    return false;

  const entity = state.pack.entity;
  const ge = GraphExplorer.propagateAll(entity);
  if (entity.modified && JSON.stringify(entity) != state.lastEntity) {
    return true
  }

  return false;
}

