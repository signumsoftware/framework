import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { areEqual, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Finder from '../Finder'
import { FindOptions, FindOptionsParsed, SubTokensOptions, QueryToken, QueryValueRequest } from '../FindOptions'
import { Lite, Entity, getToString, EmbeddedEntity } from '../Signum.Entities'
import { getQueryKey, toNumberFormat, toLuxonFormat, getEnumInfo, QueryTokenString, getTypeInfo, getTypeName, toLuxonDurationFormat, timeToString } from '../Reflection'
import { AbortableRequest } from "../Services";
import { SearchControlProps } from "./SearchControl";
import { BsColor, BsSize } from '../Components';
import { toFilterRequests } from '../Finder';
import { PropertyRoute } from '../Lines'
import * as Hooks from '../Hooks'

export interface ValueSearchControlProps extends React.Props<ValueSearchControl> {
  valueToken?: string | QueryTokenString<any>;
  findOptions: FindOptions;
  multipleValues?: { vertical?: boolean, showType?: boolean };
  isLink?: boolean;
  isBadge?: boolean | "MoreThanZero";
  badgeColor?: BsColor | ((value: any | undefined) => BsColor);
  formControlClass?: string;
  avoidAutoRefresh?: boolean;
  onValueChange?: (value: any) => void;
  onExplored?: () => void;
  onTokenLoaded?: () => void;
  initialValue?: any;
  customClass?: string | ((value: any | undefined) => (string | undefined));
  customStyle?: React.CSSProperties;
  format?: string;
  throwIfNotFindable?: boolean;
  avoidNotifyPendingRequest?: boolean;
  deps?: React.DependencyList;
  searchControlProps?: Partial<SearchControlProps>;
  modalSize?: BsSize;
  onRender?: (value: any | undefined, vsc: ValueSearchControl) => React.ReactNode;
  htmlAttributes?: React.HTMLAttributes<HTMLElement>,
  customRequest?: (req: QueryValueRequest, fop: FindOptionsParsed, token?: QueryToken) => Promise<any>,
}

export interface ValueSearchControlState {
  value?: any;
  valueToken?: QueryToken;
}

function getQueryRequestValue(fo: FindOptionsParsed, valueToken?: string | QueryTokenString<any>, multipleValues?: boolean): QueryValueRequest {

  return {
    queryKey: fo.queryKey,
    multipleValues: multipleValues,
    filters: toFilterRequests(fo.filterOptions),
    valueToken: valueToken?.toString(),
    systemTime: fo.systemTime && { ...fo.systemTime }
  };
}

export default class ValueSearchControl extends React.Component<ValueSearchControlProps, ValueSearchControlState> {

  static defaultProps = {
    isLink: false,
    isBadge: "MoreThanZero"
  }

  constructor(props: ValueSearchControlProps) {
    super(props);
    this.state = { value: props.initialValue };
  }

  componentDidMount() {
    if (this.props.initialValue == undefined) {
      this.loadToken(this.props)
        .then(t => this.refreshValue(this.props, t))
        .done();
    }
  }

  componentWillReceiveProps(newProps: ValueSearchControlProps) {

    function toString(token: string | QueryTokenString<any> | undefined) {
      return token?.toString();
    }

    if (Finder.findOptionsPath(this.props.findOptions) == Finder.findOptionsPath(newProps.findOptions) &&
      toString(this.props.valueToken) == toString(newProps.valueToken)) {

      if (!Hooks.areEqual(this.props.deps ?? [], newProps.deps ?? []))
        this.refreshValue(newProps, this.state.valueToken)

    } else {
      this.loadToken(newProps)
        .then(valTok => {
          if (newProps.initialValue == undefined)
            this.refreshValue(newProps, valTok);
        })
        .done();
      
    }
  }

  loadToken(props: ValueSearchControlProps): Promise<QueryToken | undefined> {

    if (props.valueToken?.toString() == this.state.valueToken?.fullKey)
      return Promise.resolve(this.state.valueToken);

    this.setState({ valueToken: undefined, value: undefined });
    if (!props.valueToken)
      return Promise.resolve(undefined);

    return Finder.parseSingleToken(props.findOptions.queryName, props.valueToken.toString(), SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement)
      .then(st => {
        this.setState({ valueToken: st });
        this.props.onTokenLoaded && this.props.onTokenLoaded();
        return st;
      });
  }

  componentWillUnmount() {
    this.abortableQuery.abort();
  }

  abortableQuery = new AbortableRequest<{
    findOptions: FindOptions;
    valueToken?: QueryToken,
    multipleValues: boolean | undefined,
    avoidNotify: boolean | undefined,
    customRequest?: (req: QueryValueRequest, fop: FindOptionsParsed, token: QueryToken | undefined) => any
  }, any>(
    (abortSignal, a) =>
      Finder.getQueryDescription(a.findOptions.queryName)
        .then(qd => Finder.parseFindOptions(a.findOptions, qd, false))
        .then(fop => {
          var req = getQueryRequestValue(fop, a.valueToken?.fullKey, a.multipleValues);

          if (a.customRequest)
            return a.customRequest(req, fop, a.valueToken);
          else
            return Finder.API.queryValue(req, a.avoidNotify, abortSignal);
        }));

  refreshValue(props?: ValueSearchControlProps, token?: QueryToken | null) {

    if (props === undefined)
      props = this.props;

    if (token === undefined)
      token = this.state.valueToken;

    var fo = props.findOptions;

    if (!Finder.isFindable(fo.queryName, false)) {
      if (this.props.throwIfNotFindable)
        throw Error(`Query ${getQueryKey(fo.queryName)} not allowed`);
      return;
    }

    if (Finder.validateNewEntities(fo))
      return;

    this.abortableQuery.getData({
      findOptions: fo,
      valueToken: this.state.valueToken,
      avoidNotify: props!.avoidNotifyPendingRequest,
      multipleValues: Boolean(props.multipleValues),
      customRequest: props.customRequest
    })
      .then(value => {
        const fixedValue = value === undefined ? null : value;
        this.setState({ value: fixedValue });
        this.props.onValueChange && this.props.onValueChange(fixedValue);
      })
      .done();
  }

  isNumeric() {
    let token = this.state.valueToken;
    return token && (token.filterType == "Integer" || token.filterType == "Decimal");
  }

  isMultiLine() {
    let token = this.state.valueToken;
    return token && token.filterType == "String" && token.propertyRoute && PropertyRoute.parseFull(token.propertyRoute).member?.isMultiline;
  }

  render() {

    const p = this.props;
    const s = this.state;

    const fo = p.findOptions;

    if (!Finder.isFindable(fo.queryName, false))
      return null;

    var errorMessage = Finder.validateNewEntities(fo);
    if (errorMessage) {
      return (
        <div className="alert alert-danger" role="alert" >
          <strong>Error in ValueSearchControl ({getQueryKey(fo.queryName)}): </strong>
          {errorMessage}
        </div>
      );
    }

    if (this.props.onRender)
      return this.props.onRender(this.state.value, this)!;

   

    if (p.multipleValues) {
      var value = this.state.value;
      if (value == null)
        return null;

      let token = this.state.valueToken;
      if (!token)
        return null;

      if (token.filterType == "Lite") {
        var showType = p.multipleValues.showType ?? token.type.name.contains(",");
        return (
          <div className="sf-entity-strip sf-control-container">
            <ul className={classes("sf-strip", p.multipleValues.vertical ? "sf-strip-vertical" : "sf-strip-horizontal", p.customClass && typeof p.customClass == "function" ? p.customClass(this.state.value) : p.customClass)}>
              {(value as Lite<Entity>[]).map((lite, i) => {
                const toStr = getToString(lite);
                var tag = !showType ? toStr :
                  <span style={{ wordBreak: "break-all" }} title={toStr}>
                    <span className="sf-type-badge">{getTypeInfo(lite.EntityType).niceName}</span>&nbsp;{toStr}
                  </span>;

                var link = p.isLink && Navigator.isViewable(lite.EntityType) ?
                  <a key={i} href={Navigator.navigateRoute(lite)}
                    className="sf-entitStrip-link" onClick={e => this.onClickLite(e, lite)} {...p.htmlAttributes}>
                    {tag}
                  </a> :
                  <span key={i} className="sf-entitStrip-link"{...p.htmlAttributes}>
                    {tag}
                  </span>;

                return (
                  <li className="sf-strip-element">
                    {link }
                  </li>
                );
              })}
            </ul>
          </div>
        );
      } else {
        return <div className="alert alert-danger">Not implemented</div>
      }
    }

    function bg(color: BsColor) {
      return p.isLink ? "btn-" + color : "bg-" + color;
    }

    let className = classes(
      p.valueToken == undefined && "count-search",
      p.valueToken == undefined && (this.state.value > 0 ? "count-with-results" : "count-no-results"),
      s.valueToken && (s.valueToken.type.isLite || s.valueToken!.type.isEmbedded) && "sf-entity-line-entity",
      p.formControlClass,
      p.formControlClass && this.isNumeric() && "numeric",
      p.formControlClass && this.isMultiLine() && "sf-multi-line",

      p.isBadge == false ? "" :
        "badge badge-pill " +
        (p.isBadge == "MoreThanZero" && (this.state.value == 0 || this.state.value == null) ? bg("light") + " text-muted" :
          p.badgeColor && typeof p.badgeColor == "function" ? bg(p.badgeColor(this.state.value)) :
            p.badgeColor ? bg(p.badgeColor) :
              bg("secondary")),

      p.customClass && typeof p.customClass == "function" ? p.customClass(this.state.value) : p.customClass
    );

    if (p.formControlClass) {
      return (
        <div className={className} style={p.customStyle} {...p.htmlAttributes}>
          {this.renderValue(this.state.value)}
        </div>
      );
    }
     

    if (p.isLink) {
      return (
        <a className={className} onClick={this.handleClick} href="#" style={p.customStyle} {...p.htmlAttributes}>
          {this.renderValue(this.state.value)}
        </a>
      );
    }

    return <span className={className} style={p.customStyle} {...p.htmlAttributes}>{this.renderValue(this.state.value)}</span>
  }

  onClickLite(e: React.MouseEvent<any>, lite: Lite<Entity>) {
    e.preventDefault();
    const s = Navigator.getSettings(lite.EntityType)
    const avoidPopup = s != undefined && s.avoidPopup;

    if (e.ctrlKey || e.button == 1 || avoidPopup) {
      window.open(Navigator.navigateRoute(lite));
      return;
    }

    Navigator.view(lite)
      .then(() => { this.refreshValue(this.props, this.state.valueToken); this.props.onExplored && this.props.onExplored(); })
      .done();
  }

  renderValue(value : any) {

    if (value === undefined)
      return "…";

    if (value === null)
      return null;

    if (!this.props.valueToken)
      return value;

    let token = this.state.valueToken;
    if (!token)
      return null;

    switch (token.filterType) {
      case "Integer":
      case "Decimal":
        {
          const numberFormat = toNumberFormat(this.props.format ?? token.format);
          return numberFormat.format(value);
        }
      case "DateTime":
        {
          const luxonFormat = toLuxonFormat(this.props.format ?? token.format, token.type.name as "DateOnly" | "DateTime");
          return toFormatFixed(DateTime.fromISO(value), luxonFormat);
        }
      case "Time":
        {
          const luxonFormat = toLuxonDurationFormat(this.props.format ?? token.format);
          return Duration.fromISOTime(value).toFormat(luxonFormat ?? "hh:mm:ss");
        }
      case "String": return value;
      case "Lite": return (value as Lite<Entity>).toStr;
      case "Embedded": return getToString(value as EmbeddedEntity);
      case "Boolean": return <input type="checkbox" className="form-check-input" disabled={true} checked={value} />
      case "Enum": return getEnumInfo(token!.type.name, value).niceName;
      case "Guid":
        let str = value as string;
        return <span className="guid">{str.substr(0, 4) + "…" + str.substring(str.length - 4)}</span>;
    }

    return value;
  }

  handleClick = (e: React.MouseEvent<any>) => {
    e.preventDefault();

    if (e.ctrlKey || e.button == 1)
      window.open(Finder.findOptionsPath(this.props.findOptions));
    else
      Finder.explore(this.props.findOptions, { searchControlProps: this.props.searchControlProps, modalSize: this.props.modalSize }).then(() => {
        if (!this.props.avoidAutoRefresh)
          this.refreshValue(this.props, this.state.valueToken);

        if (this.props.onExplored)
          this.props.onExplored();
      }).done();
  }
}

function toFormatFixed(arg0: DateTime, momentFormat: string) {
    throw new Error('Function not implemented.')
}
