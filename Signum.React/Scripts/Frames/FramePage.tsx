import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import ButtonBar from './ButtonBar'
import { Entity, Lite, getToString, EntityPack, JavascriptMessage, entityInfo } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, parseId } from '../Reflection'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from './Widgets'
import ValidationErrors from './ValidationErrors'
import * as QueryString from 'query-string'
import { ErrorBoundary } from '../Components';
import "./Frames.css"
import { AutoFocus } from '../Components/AutoFocus';
import { FunctionalAdapter } from './FrameModal';

interface FramePageProps extends RouteComponentProps<{ type: string; id?: string }> {

}

interface FramePageState {
  pack?: EntityPack<Entity>;
  getComponent?: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
  refreshCount: number;
}

export default class FramePage extends React.Component<FramePageProps, FramePageState> {
  constructor(props: FramePageProps) {
    super(props);
    this.state = this.newState(props);
  }

  componentWillMount() {
    this.load(this.props);
  }

  componentDidMount() {
    window.addEventListener("keydown", this.hanldleKeyDown);
  }

  getTypeInfo(): TypeInfo {
    return getTypeInfo(this.props.match.params.type);
  }

  newState(props: FramePageProps): FramePageState {
    return {
      getComponent: undefined,
      pack: undefined,
      refreshCount: 0
    };
  }

  componentWillReceiveProps(newProps: FramePageProps) {
    const newParams = newProps.match.params;
    const oldParams = this.props.match.params;
    if(newParams.type != oldParams.type || newParams.id != oldParams.id) {
      this.setState(this.newState(newProps), () => {
        this.load(newProps);
      }); 
    }
  }

  componentWillUnmount() {
    Navigator.setTitle();
    window.removeEventListener("keydown", this.hanldleKeyDown);
  }

  hanldleKeyDown = (e: KeyboardEvent) => {
    if (!e.openedModals && this.buttonBar)
      this.buttonBar.hanldleKeyDown(e);
  }

  load(props: FramePageProps) {

    this.loadEntity(props)
      .then(pack => { Navigator.setTitle(pack.entity.toStr); return pack; })
      .then(pack => this.loadComponent(pack).then(getComponent => this.setState({ getComponent: getComponent })))
      .done();
  }

  loadEntity(props: FramePageProps): Promise<EntityPack<Entity>> {

    if (QueryString.parse(this.props.location.search).waitData) {
      if (window.opener.dataForChildWindow == undefined) {
        throw new Error("No dataForChildWindow in parent found!")
      }

      var pack = window.opener.dataForChildWindow;
      window.opener.dataForChildWindow = undefined;
      this.setState({ pack: pack });
      return Promise.resolve(pack);
    }

    const ti = this.getTypeInfo();

    if (this.props.match.params.id) {

      const lite: Lite<Entity> = {
        EntityType: ti.name,
        id: parseId(ti, props.match.params.id!),
      };

      return Navigator.API.fetchEntityPack(lite)
        .then(pack => {
          this.setState({ pack });
          return Promise.resolve(pack);
        });

    } else {

      return Constructor.constructPack(ti.name)
        .then(pack => {
          this.setState({ pack: pack as EntityPack<Entity> });
          return Promise.resolve(pack as EntityPack<Entity>);
        });
    }
  }


  loadComponent(pack: EntityPack<Entity>): Promise<(ctx: TypeContext<Entity>) => React.ReactElement<any>> {
    const viewName = QueryString.parse(this.props.location.search).viewName;
    return Navigator.getViewPromise(pack.entity, viewName && Array.isArray(viewName) ? viewName[0] : viewName).promise;
  }

  onClose() {
    if (Finder.isFindable(this.props.match.params.type, true))
      Navigator.history.push(Finder.findOptionsPath({ queryName: this.props.match.params.type }));
    else
      Navigator.history.push("~/");
  }


  entityComponent?: React.Component<any, any> | null;

  setComponent(c: React.Component<any, any> | null) {
    if (c && this.entityComponent != c) {
      this.entityComponent = c;
      this.forceUpdate();
    }
  }

  buttonBar?: ButtonBar | null;

  render() {

    if (!this.state.pack) {
      return (
        <div className="normal-control">
          {this.renderTitle()}
        </div>
      );
    }

    const entity = this.state.pack.entity;

    const frame: EntityFrame = {
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: this.state.pack,
      onReload: (pack, reloadComponent, callback) => {

        var packEntity = (pack || this.state.pack) as EntityPack<Entity>;

        if (packEntity.entity.id != null && entity.id == null)
          Navigator.history.push(Navigator.navigateRoute(packEntity.entity));
        else {
          if (reloadComponent)
            this.setState({ getComponent: undefined }, () => this.loadComponent(packEntity)
              .then(getComponent => this.setState({
                pack: packEntity,
                refreshCount: this.state.refreshCount + 1,
                getComponent: getComponent
              }, callback))
              .done());
          else
            this.setState({
              pack: packEntity, refreshCount: this.state.refreshCount + 1
            }, callback);
        } 
      },
      onClose: () => this.onClose(),
      revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
      setError: (ms, initialPrefix) => {
        GraphExplorer.setModelState(entity, ms, initialPrefix || "");
        this.forceUpdate()
      },
      refreshCount: this.state.refreshCount,
      allowChangeEntity: true,
    };

    const ti = this.getTypeInfo();

    const styleOptions: StyleOptions = {
      readOnly: Navigator.isReadOnly(this.state.pack),
      frame: frame
    };

    const ctx = new TypeContext<Entity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(entity, "framePage"));

    const wc: WidgetContext<Entity> = {
      ctx: ctx,
      frame: frame,
    };

    const embeddedWidgets = renderEmbeddedWidgets(wc);

    return (
      <div className="normal-control">
        {this.renderTitle()}
        {renderWidgets(wc)}
        {this.entityComponent && <ButtonBar ref={bb => this.buttonBar = bb} frame={frame} pack={this.state.pack} />}
        <ValidationErrors entity={this.state.pack.entity} ref={ve => this.validationErrors = ve} prefix="framePage" />
        {embeddedWidgets.top}
        <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-main-entity={entityInfo(ctx.value)}>
          <ErrorBoundary>
            {this.state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(this.state.getComponent(ctx), c => this.setComponent(c))}</AutoFocus>}
          </ErrorBoundary>
        </div>
        {embeddedWidgets.bottom}
      </div>
    );
  }

  validationErrors?: ValidationErrors | null;

  renderTitle() {

    if (!this.state.pack)
      return <h3 className="display-6 sf-entity-title">{JavascriptMessage.loading.niceToString()}</h3>;

    const entity = this.state.pack.entity;

    return (
      <h4>
        <span className="display-6 sf-entity-title">{getToString(entity)}</span>
        <br />
        <small className="sf-type-nice-name text-muted">{Navigator.getTypeTitle(entity, undefined)}</small>
      </h4>
    );
  }
}




