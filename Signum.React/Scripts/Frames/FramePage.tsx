import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import * as AppContext from '../AppContext'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import { Prompt } from "react-router-dom"
import * as Finder from '../Finder'
import { ButtonBar, ButtonBarHandle } from './ButtonBar'
import { Entity, Lite, getToString, EntityPack, JavascriptMessage, entityInfo } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame, ButtonBarElement } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, parseId, OperationType } from '../Reflection'
import { renderWidgets,  WidgetContext } from './Widgets'
import { ValidationErrors, ValidationErrorsHandle } from './ValidationErrors'
import { ErrorBoundary } from '../Components';
import "./Frames.css"
import { AutoFocus } from '../Components/AutoFocus';
import { useStateWithPromise, useForceUpdate, useMounted, useDocumentEvent, useWindowEvent, useUpdatedRef } from '../Hooks'
import * as Operations from '../Operations'
import WidgetEmbedded from './WidgetEmbedded'
import { useTitle } from '../AppContext'
import { FunctionalAdapter } from '../Modals'
import { QueryString } from '../QueryString'

interface FramePageProps extends RouteComponentProps<{ type: string; id?: string }> {

}

interface FramePageState {
  pack: EntityPack<Entity>;
  lastEntity?: string;
  getComponent: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
  refreshCount: number;
  createNew?: () => Promise<EntityPack<Entity> | undefined>;
  avoidPrompt?: boolean;
}

export default function FramePage(p: FramePageProps) {

  const [state, setState] = useStateWithPromise<FramePageState | undefined>(undefined);
  const stateRef = useUpdatedRef(state);
  const buttonBar = React.useRef<ButtonBarHandle>(null);
  const entityComponent = React.useRef<React.Component | null>(null);
  const validationErrors = React.useRef<React.Component>(null);
  const mounted = useMounted();
  const forceUpdate = useForceUpdate();

  const ti = getTypeInfo(p.match.params.type);
  const type = ti.name;
  const id = p.match.params.id;

  useTitle(state?.pack.entity.toStr ?? "", [state?.pack.entity]);

  React.useEffect(() => {
    loadEntity()
      .then(a => loadComponent(a.pack!).then(getComponent => mounted.current ? setState({
        pack: a.pack!,
        lastEntity: JSON.stringify(a.pack!.entity),
        createNew: a.createNew,
        getComponent: getComponent,
        refreshCount: state ? state.refreshCount + 1 : 0
      }) : undefined))
      .done();
  }, [type, id, p.location.search]);


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

  function loadComponent(pack: EntityPack<Entity>): Promise<(ctx: TypeContext<Entity>) => React.ReactElement<any>> {
    const viewName = QueryString.parse(p.location.search).viewName ?? undefined;
    return Navigator.getViewPromise(pack.entity, viewName).promise;
  }


  function loadEntity(): Promise<{ pack?: EntityPack<Entity>, createNew?: () => Promise<EntityPack<Entity> | undefined> }> {

    const queryString = QueryString.parse(p.location.search);

    if (queryString.waitData) {
      if (window.opener!.dataForChildWindow == undefined) {
        throw new Error("No dataForChildWindow in parent found!")
      }

      var pack = window.opener!.dataForChildWindow as EntityPack<Entity>;
      window.opener!.dataForChildWindow = undefined;
      var txt = JSON.stringify(pack);
      return Promise.resolve({
        pack,
        createNew: () => Promise.resolve(JSON.parse(txt))
      });
    }

    if (id) {

      const lite: Lite<Entity> = {
        EntityType: ti.name,
        id: parseId(ti, id!),
      };

      return Navigator.API.fetchEntityPack(lite)
        .then(pack => {
          return Promise.resolve({
            pack,
            createNew: undefined
          });
        });

    } else {
      const cn = queryString["constructor"];
      if (cn != null && typeof cn == "string") {
        const oi = Operations.operationInfos(ti).single(a => a.operationType == "Constructor" && a.key.toLowerCase().endsWith(cn.toLowerCase()));
        return Operations.API.construct(ti.name, oi.key).then(pack => ({
          pack: pack!,
          createNew: () => Operations.API.construct(ti.name, oi.key)
        }));
      }

      return Constructor.constructPack(ti.name).then(pack => ({
        pack: pack! as EntityPack<Entity>,
        createNew: () => Constructor.constructPack(ti.name) as Promise<EntityPack<Entity>>
      }));
    }
  }

  function onClose() {
    if (Finder.isFindable(p.match.params.type, true))
      AppContext.history.push(Finder.findOptionsPath({ queryName: p.match.params.type }));
    else
      AppContext.history.push("~/");
  }

  function setComponent(c: React.Component | null) {
    if (c && entityComponent.current != c) {
      entityComponent.current = c;
      forceUpdate();
    }
  }

  if (!state || state.pack.entity.Type != type || state.pack.entity.id != id) {
    return (
      <div className="normal-control">
        {renderTitle()}
      </div>
    );
  }

  const entity = state.pack.entity;


  const frame: EntityFrame = {
    tabs: undefined,
    frameComponent: { forceUpdate, type: FramePage as any },
    entityComponent: entityComponent.current,
    pack: state.pack,
    avoidPrompt: () => state.avoidPrompt = true,
    onReload: (pack, reloadComponent, callback) => {

      var packEntity = (pack ?? state.pack) as EntityPack<Entity>;

      if (packEntity.entity.id != null && entity.id == null)
        AppContext.history.push(Navigator.navigateRoute(packEntity.entity));
      else {
        if (reloadComponent) {
          setState(undefined)
            .then(() => loadComponent(packEntity))
            .then(gc => {
              if (mounted.current)
                setState({
                  pack: packEntity,
                  getComponent: gc,
                  refreshCount: state.refreshCount + 1,
                  
                }).then(callback).done();
            })
            .done();
        }
        else {
          setState({
            pack: packEntity,
            getComponent: state.getComponent,
            refreshCount: state.refreshCount + 1,
          }).then(callback).done();
        }
      }
    },
    onClose: () => onClose(),
    revalidate: () => validationErrors.current && validationErrors.current.forceUpdate(),
    setError: (ms, initialPrefix) => {
      GraphExplorer.setModelState(entity, ms, initialPrefix || "");
      forceUpdate()
    },
    refreshCount: state.refreshCount,
    createNew: state.createNew,
    allowExchangeEntity: true,
    prefix: "framePage"
  };


  const styleOptions: StyleOptions = {
    readOnly: Navigator.isReadOnly(state.pack),
    frame: frame
  };

  const ctx = new TypeContext<Entity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(entity, "framePage"));

  const wc: WidgetContext<Entity> = { ctx: ctx, frame: frame };


  return (
    <div className="normal-control">
      <Prompt when={true} message={() => hasChanges(state) ? JavascriptMessage.loseCurrentChanges.niceToString() : true} />
      {renderTitle()}
      <div className="sf-button-widget-container">
        {renderWidgets(wc)}
        {entityComponent.current && <ButtonBar ref={buttonBar} frame={frame} pack={state.pack} />}
      </div>
      <ValidationErrors ref={validationErrors} entity={state.pack.entity} prefix="framePage" />
      <WidgetEmbedded widgetContext={wc} >
        <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-main-entity={entityInfo(ctx.value)}>
          <ErrorBoundary>
            {state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(state.getComponent(ctx), c => setComponent(c))}</AutoFocus>}
          </ErrorBoundary>
        </div>
      </WidgetEmbedded>
    </div>
  );

  function renderTitle() {

    if (!state)
      return <h3 className="display-6 sf-entity-title">{JavascriptMessage.loading.niceToString()}</h3>;

    const entity = state.pack.entity;

    return (
      <h4>
        <span className="display-6 sf-entity-title">{getToString(entity)}</span>
        <br />
        <small className="sf-type-nice-name text-muted">{Navigator.getTypeTitle(entity, undefined)}</small>
      </h4>
    );
  }
}

function hasChanges(state: FramePageState) {

  if (state.avoidPrompt)
    return false;

  const entity = state.pack.entity;
  const ge = GraphExplorer.propagateAll(entity);
  if (entity.modified && JSON.stringify(entity) != state.lastEntity) {
    return true
  }

  return false;
}



