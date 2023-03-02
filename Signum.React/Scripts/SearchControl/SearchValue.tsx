import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Finder from '../Finder'
import { FindOptions, FindOptionsParsed, SubTokensOptions, QueryToken, QueryValueRequest } from '../FindOptions'
import { Lite, Entity, getToString, EmbeddedEntity } from '../Signum.Entities'
import { getQueryKey, toNumberFormat, toLuxonFormat, getEnumInfo, QueryTokenString, getTypeInfo, getTypeName, toLuxonDurationFormat, timeToString, toFormatWithFixes } from '../Reflection'
import { SearchControlProps } from "./SearchControl";
import { BsColor, BsSize } from '../Components';
import { toFilterRequests } from '../Finder';
import { PropertyRoute } from '../Lines'
import { useAPI, usePrevious } from '../Hooks'
import * as Hooks from '../Hooks'
import { TypeBadge } from '../Lines/AutoCompleteConfig'

export interface SearchValueProps {
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
  onInitialValueLoaded?: () => void;
  customClass?: string | ((value: any | undefined) => (string | undefined));
  customStyle?: React.CSSProperties;
  unit?: string | null;
  format?: string;
  throwIfNotFindable?: boolean;
  avoidNotifyPendingRequest?: boolean;
  deps?: React.DependencyList;
  searchControlProps?: Partial<SearchControlProps>;
  modalSize?: BsSize;
  onRender?: (value: any | undefined, vsc: SearchValueController) => React.ReactElement | null | undefined | false;
  htmlAttributes?: React.HTMLAttributes<HTMLElement>,
  customRequest?: (req: QueryValueRequest, fop: FindOptionsParsed, token: QueryToken | null, signal: AbortSignal) => Promise<any>,
}

export interface SearchValueController {
  props: SearchValueProps;
  valueToken: QueryToken | null | undefined;
  value: unknown | undefined;
  renderValue(): React.ReactChild | null;
  refreshValue: () => void;
  handleClick: (e: React.MouseEvent<any>) => void;
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

const SearchValue = React.forwardRef(function SearchValue(p: SearchValueProps, ref: React.Ref<SearchValueController>): React.ReactElement | null {

  const fo = p.findOptions;

  const valueToken = useAPI(() => {
    if (p.valueToken == null)
      return Promise.resolve(null);

    return Finder.parseSingleToken(p.findOptions.queryName, p.valueToken.toString(), SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement)
      .then(st => {
        controller.valueToken = st;
        p.onTokenLoaded && p.onTokenLoaded();
        return st;
      });
  }, [p.valueToken?.toString()]);

  const [reloadTicks, setReloadTicks] = React.useState(0);
  var deps = [reloadTicks, ...(p.deps ?? [])];

  var initialDeps = React.useRef(deps);

  const value = useAPI(signal => {

    function makeRequest() {

      if (!Finder.isFindable(fo.queryName, false)) {
        if (p.throwIfNotFindable)
          throw Error(`Query ${getQueryKey(fo.queryName)} not allowed`);
      }

      return Finder.getQueryDescription(p.findOptions.queryName)
        .then(qd => Finder.parseFindOptions(p.findOptions, qd, false))
        .then(fop => {
          const req = getQueryRequestValue(fop, valueToken?.fullKey, Boolean(p.multipleValues));
          if (p.customRequest)
            return p.customRequest(req, fop, valueToken ?? null, signal);
          else
            return Finder.API.queryValue(req, p.avoidNotifyPendingRequest, signal);
        })
        .then(val => {
          const fixedValue = val === undefined ? null : val;
          controller.value = fixedValue;
          p.onValueChange && p.onValueChange(fixedValue);
          return fixedValue;
        });
    }

    if (valueToken === undefined)
      return Promise.resolve(undefined);

    if (p.initialValue !== undefined) {
      if (Hooks.areEqual(deps ?? [], initialDeps.current ?? [])) {
        controller.value = p.initialValue;
        p.onInitialValueLoaded?.();
        return Promise.resolve(p.initialValue);
      }
      else
        return makeRequest();
    } else {
      return makeRequest();
    }

  }, [p.initialValue, valueToken, Finder.findOptionsPath(p.findOptions), ...(deps || [])], { avoidReset: true });

  function refreshValue() {
    setReloadTicks(a => a + 1);
  }

  var controller = React.useMemo(() => ({} as any as SearchValueController), []);
  controller.props = p;
  controller.value = value;
  controller.valueToken = valueToken;
  controller.renderValue = renderValue;
  controller.refreshValue = refreshValue;
  controller.handleClick = handleClick;

  React.useImperativeHandle(ref, () => controller, []);

  function isNumeric() {
    let token = valueToken;
    return token && (token.filterType == "Integer" || token.filterType == "Decimal");
  }

  function isMultiLine() {
    let token = valueToken;
    return token && token.filterType == "String" && token.propertyRoute && PropertyRoute.parseFull(token.propertyRoute).member?.isMultiline;
  }

  if (!Finder.isFindable(fo.queryName, false))
    return null;

  var errorMessage = Finder.validateNewEntities(fo);
  if (errorMessage) {
    return (
      <div className="alert alert-danger" role="alert" >
        <strong>Error in SearchValue ({getQueryKey(fo.queryName)}): </strong>
        {errorMessage}
      </div>
    );
  }

  if (p.onRender) {
    var result = p.onRender(value, controller)!;
    if (result == null || result == false)
      return null;

    return result;
  }

  if (p.multipleValues) {
    if (value == null)
      return null;

    let token = valueToken;
    if (!token)
      return null;

    if (token.filterType == "Lite") {
      var showType = p.multipleValues.showType ?? token.type.name.contains(",");
      return (
        <div className="sf-entity-strip sf-control-container">
          <ul className={classes("sf-strip", p.multipleValues.vertical ? "sf-strip-vertical" : "sf-strip-horizontal", p.customClass && typeof p.customClass == "function" ? p.customClass(value) : p.customClass)}>
            {(value as Lite<Entity>[]).map((lite, i) => {
              const toStr = getToString(lite);
              var tag = !showType ? toStr :
                <span style={{ wordBreak: "break-all" }} title={toStr}>
                  {toStr}<TypeBadge entity={lite} />
                </span>;

              var link = p.isLink && Navigator.isViewable(lite.EntityType) ?
                <a key={i} href={Navigator.navigateRoute(lite)}
                  className="sf-entitStrip-link" onClick={e => onClickLite(e, lite)} {...p.htmlAttributes}>
                  {tag}
                </a> :
                <span key={i} className="sf-entitStrip-link"{...p.htmlAttributes}>
                  {tag}
                </span>;

              return (
                <li className="sf-strip-element">
                  {link}
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

    if (p.isLink)
      return "bg-" + color + (color == "light" ? " text-dark" : "");
    
    return "bg-" + color;
  }

  let className = classes(
    p.valueToken == undefined && "count-search",
    p.valueToken == undefined && (value > 0 ? "count-with-results" : "count-no-results"),
    valueToken && (valueToken.type.isLite || valueToken!.type.isEmbedded) && "sf-entity-line-entity",
    p.formControlClass,
    p.formControlClass && isNumeric() && "numeric",
    p.formControlClass && isMultiLine() && "sf-multi-line",

    p.isBadge == false ? "" :
      "badge badge-pill " +
      (p.isBadge == "MoreThanZero" && (value == 0 || value == null) ? bg("light") + " text-muted" :
        p.badgeColor && typeof p.badgeColor == "function" ? bg(p.badgeColor(value)) :
          p.badgeColor ? bg(p.badgeColor) :
            bg("secondary")),

    p.customClass && typeof p.customClass == "function" ? p.customClass(value) : p.customClass
  );

  if (p.formControlClass) {
    return (
      <div className={className} style={p.customStyle} {...p.htmlAttributes}>
        {renderValue()}
      </div>
    );
  }


  if (p.isLink) {
    return (
      <a className={className} onClick={handleClick} href="#" style={p.customStyle} {...p.htmlAttributes}>
        {renderValue()}
      </a>
    );
  }

  return <span className={className} style={p.customStyle} {...p.htmlAttributes}>{renderValue()}</span>


  function onClickLite(e: React.MouseEvent<any>, lite: Lite<Entity>) {
    e.preventDefault();
    const s = Navigator.getSettings(lite.EntityType)
    const avoidPopup = s != undefined && s.avoidPopup;

    if (e.ctrlKey || e.button == 1 || avoidPopup) {
      window.open(Navigator.navigateRoute(lite));
      return;
    }

    Navigator.view(lite)
      .then(() => { refreshValue(); p.onExplored && p.onExplored(); });
  }

  function renderValue(): React.ReactChild | null{

    if (value === undefined)
      return "…";

    if (value === null)
      return null;

    if (!valueToken)
      return value;

    let token = valueToken;
    if (!token)
      return null;

    switch (token.filterType) {
      case "Integer":
      case "Decimal":
        {
          const numberFormat = toNumberFormat(p.format ?? token.format);

          var unit = p.unit === null ? p.unit : p.unit ?? token.unit;
          if (unit)
            return numberFormat.format(value) + " " + unit;
          else
            return numberFormat.format(value);
        }
      case "DateTime":
        {
          const luxonFormat = toLuxonFormat(p.format ?? token.format, token.type.name as "DateOnly" | "DateTime");
          return toFormatWithFixes(DateTime.fromISO(value), luxonFormat);
        }
      case "Time":
        {
          const luxonFormat = toLuxonDurationFormat(p.format ?? token.format);
          return Duration.fromISOTime(value).toFormat(luxonFormat ?? "hh:mm:ss");
        }
      case "String": return value;
      case "Lite": return value && Navigator.renderLite(value as Lite<Entity>);
      case "Embedded": return getToString(value as EmbeddedEntity);
      case "Boolean": return <input type="checkbox" className="form-check-input" disabled={true} checked={value} />
      case "Enum": return getEnumInfo(token!.type.name, value).niceName;
      case "Guid":
        let str = value as string;
        return <span className="guid">{str.substr(0, 4) + "…" + str.substring(str.length - 4)}</span>;
    }

    return value;
  }

  function handleClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    var fo: FindOptions;
    if (p.findOptions.columnOptions == undefined && valueToken && valueToken.parent)
      fo = {
        ...p.findOptions,
        columnOptions: [{
          token: valueToken.queryTokenType == "Aggregate" ? valueToken.parent!.fullKey : valueToken.fullKey,
          summaryToken: valueToken.queryTokenType == "Aggregate" ? valueToken.fullKey : undefined,
        }],
        columnOptionsMode: "ReplaceOrAdd",
      }
    else
      fo = p.findOptions;

    if (e.ctrlKey || e.button == 1)
      window.open(Finder.findOptionsPath(fo));
    else
      Finder.explore(fo, { searchControlProps: p.searchControlProps, modalSize: p.modalSize }).then(() => {
        if (!p.avoidAutoRefresh)
          refreshValue();

        if (p.onExplored)
          p.onExplored();
      });
  }
});


(SearchValue as any).defaultProps = {
  isLink: false,
  isBadge: "MoreThanZero"
} as Partial<SearchValueProps>;

export default SearchValue;
