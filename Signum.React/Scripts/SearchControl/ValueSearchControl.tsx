import * as React from 'react'
import { DateTime } from 'luxon'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Finder from '../Finder'
import { FindOptions, FindOptionsParsed, SubTokensOptions, QueryToken, QueryValueRequest } from '../FindOptions'
import { Lite, Entity, getToString, EmbeddedEntity } from '../Signum.Entities'
import { getQueryKey, toNumberFormat, toLuxonFormat, getEnumInfo, QueryTokenString, getTypeInfo, getTypeName, toDurationFormat, durationToString } from '../Reflection'
import { AbortableRequest } from "../Services";
import { SearchControlProps } from "./SearchControl";
import { BsColor } from '../Components';
import { toFilterRequests } from '../Finder';
import { PropertyRoute } from '../Lines'

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
  refreshKey?: any;
  searchControlProps?: Partial<SearchControlProps>;
  onRender?: (value: any | undefined, vsc: ValueSearchControl) => React.ReactNode;
  htmlAttributes?: React.HTMLAttributes<HTMLElement>,
}

export interface ValueSearchControlState {
  value?: any;
  token?: QueryToken;
}

function getQueryRequest(fo: FindOptionsParsed, valueToken?: string | QueryTokenString<any>, multipleValues?: boolean): QueryValueRequest {

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
      this.loadToken(this.props);
      this.refreshValue(this.props);
    }
  }

  componentWillReceiveProps(newProps: ValueSearchControlProps) {

    function toString(token: string | QueryTokenString<any> | undefined) {
      return token?.toString();
    }

    if (Finder.findOptionsPath(this.props.findOptions) == Finder.findOptionsPath(newProps.findOptions) &&
      toString(this.props.valueToken) == toString(newProps.valueToken)) {

      if (this.props.refreshKey != newProps.refreshKey)
        this.refreshValue(newProps)

    } else {
      this.loadToken(newProps);

      if (newProps.initialValue == undefined)
        this.refreshValue(newProps);
    }
  }

  loadToken(props: ValueSearchControlProps) {

    if (props.valueToken == (this.state.token && this.state.token.fullKey))
      return;

    this.setState({ token: undefined, value: undefined });
    if (props.valueToken)
      Finder.parseSingleToken(props.findOptions.queryName, props.valueToken.toString(), SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll)
        .then(st => {
          this.setState({ token: st });
          this.props.onTokenLoaded && this.props.onTokenLoaded();
        })
        .done();
  }

  componentWillUnmount() {
    this.abortableQuery.abort();
  }

  abortableQuery = new AbortableRequest<{ findOptions: FindOptions; valueToken?: string | QueryTokenString<any>, multipleValues: boolean | undefined, avoidNotify: boolean | undefined }, number>(
    (abortSignal, a) =>
      Finder.getQueryDescription(a.findOptions.queryName)
        .then(qd => Finder.parseFindOptions(a.findOptions, qd, false))
        .then(fop => Finder.API.queryValue(getQueryRequest(fop, a.valueToken, a.multipleValues), a.avoidNotify, abortSignal)));

  refreshValue(props?: ValueSearchControlProps) {
    if (!props)
      props = this.props;

    var fo = props.findOptions;

    if (!Finder.isFindable(fo.queryName, false)) {
      if (this.props.throwIfNotFindable)
        throw Error(`Query ${getQueryKey(fo.queryName)} not allowed`);
      return;
    }

    if (Finder.validateNewEntities(fo))
      return;

    this.abortableQuery.getData({ findOptions: fo, valueToken: props.valueToken, avoidNotify: props!.avoidNotifyPendingRequest, multipleValues: Boolean(props.multipleValues) })
      .then(value => {
        const fixedValue = value === undefined ? null : value;
        this.setState({ value: fixedValue });
        this.props.onValueChange && this.props.onValueChange(fixedValue);
      })
      .done();
  }

  isNumeric() {
    let token = this.state.token;
    return token && (token.filterType == "Integer" || token.filterType == "Decimal");
  }

  isMultiLine() {
    let token = this.state.token;
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

      let token = this.state.token;
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

    let className = classes(
      p.valueToken == undefined && "count-search",
      p.valueToken == undefined && (this.state.value > 0 ? "count-with-results" : "count-no-results"),
      s.token && (s.token.type.isLite || s.token!.type.isEmbedded) && "sf-entity-line-entity",
      p.formControlClass,
      p.formControlClass && this.isNumeric() && "numeric",
      p.formControlClass && this.isMultiLine() && "sf-multi-line",

      p.isBadge == false ? "" :
        "badge badge-pill " +
        (p.isBadge == "MoreThanZero" && (this.state.value == 0 || this.state.value == null) ? "badge-light text-muted" :
          p.badgeColor && typeof p.badgeColor == "function" ? "badge-" + p.badgeColor(this.state.value) :
            p.badgeColor ? "badge-" + p.badgeColor :
              "badge-secondary"),

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
      .then(() => { this.refreshValue(); this.props.onExplored && this.props.onExplored(); })
      .done();
  }

  renderValue(value : any) {

    if (value === undefined)
      return "…";

    if (value === null)
      return null;

    if (!this.props.valueToken)
      return value;

    let token = this.state.token;
    if (!token)
      return null;

    switch (token.filterType) {
      case "Integer":
      case "Decimal":
        const numbroFormat = toNumberFormat(this.props.format ?? token.format);
        return numbroFormat.format(value);
      case "DateTime":
        const momentFormat = toLuxonFormat(this.props.format ?? token.format, token.type.name as "Date" | "DateTime");
        return DateTime.fromISO(value).toFormatFixed(momentFormat);
      case "Time":
        const durationFormat = toDurationFormat(this.props.format ?? token.format);
        return durationToString(value, durationFormat);
      case "String": return value;
      case "Lite": return (value as Lite<Entity>).toStr;
      case "Embedded": return getToString(value as EmbeddedEntity);
      case "Boolean": return <input type="checkbox" disabled={true} checked={value} />
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
      Finder.explore(this.props.findOptions, { searchControlProps: this.props.searchControlProps }).then(() => {
        if (!this.props.avoidAutoRefresh)
          this.refreshValue(this.props);

        if (this.props.onExplored)
          this.props.onExplored();
      }).done();
  }
}
