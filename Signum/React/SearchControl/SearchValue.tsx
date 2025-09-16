import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { classes } from '../Globals'
import { Navigator } from '../Navigator'
import { Finder } from '../Finder'
import { FindOptions, FindOptionsParsed, SubTokensOptions, QueryToken, QueryValueRequest, QueryDescription } from '../FindOptions'
import { Lite, Entity, getToString, EmbeddedEntity, EntityControlMessage } from '../Signum.Entities'
import { getQueryKey, toNumberFormat, toLuxonFormat, getEnumInfo, QueryTokenString, getTypeInfo, getTypeName, toLuxonDurationFormat, timeToString, toFormatWithFixes } from '../Reflection'
import { SearchControlProps } from "./SearchControl";
import { ColumnParsed } from "./SearchControlLoaded";
import { BsColor, BsSize } from '../Components';
import { PropertyRoute, StyleContext } from '../Lines'
import { useAPI, usePrevious, useVersion } from '../Hooks'
import * as Hooks from '../Hooks'
import { TypeBadge } from '../Lines/AutoCompleteConfig'
import { toAbsoluteUrl } from '../AppContext'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { TimeMachineColors } from '../Lines/TimeMachineIcon'

export interface SearchValueProps {
  ctx?: StyleContext;
  id?: string;
  valueToken?: string | QueryTokenString<any> | QueryToken;
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
  htmlAttributes?: React.AllHTMLAttributes<HTMLElement>,
  customRequest?: (req: QueryValueRequest, fop: FindOptionsParsed, token: QueryToken | null, signal: AbortSignal) => Promise<any>,
  avoidRenderTimeMachineIcon?: boolean;
  onExplore?: (vsc: SearchValueController) => Promise<boolean>;
}

export interface SearchValueController {
  props: SearchValueProps;
  valueToken: QueryToken | null | undefined;
  value: unknown | undefined;
  queryDescription: QueryDescription | undefined;
  hasHistoryChanges: boolean | undefined;
  renderValue(): React.ReactElement | string | null;
  refreshValue: () => void;
  handleClick: (e: React.MouseEvent<any>) => void;
}

function getQueryRequestValue(fo: FindOptionsParsed, valueToken?: string | QueryTokenString<any>, multipleValues?: boolean): QueryValueRequest {

  return {
    queryKey: fo.queryKey,
    multipleValues: multipleValues,
    filters: Finder.toFilterRequests(fo.filterOptions),
    valueToken: valueToken?.toString(),
    systemTime: fo.systemTime && { ...fo.systemTime }
  };
}

const SearchValue: React.ForwardRefExoticComponent<SearchValueProps & React.RefAttributes<SearchValueController>> =
  React.forwardRef(function SearchValue(p: SearchValueProps, ref: React.Ref<SearchValueController>): React.ReactElement | null {

    const fo = p.findOptions;

    const valueToken = useAPI(() => {
      if (p.valueToken == null)
        return Promise.resolve(null);

      if ((p.valueToken as QueryToken).key)
        return p.valueToken as QueryToken;

      return Finder.parseSingleToken(p.findOptions.queryName, p.valueToken.toString(), SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement)
        .then(st => {
          controller.valueToken = st;
          p.onTokenLoaded && p.onTokenLoaded();
          return st;
        });
    }, [p.valueToken?.toString()]);

    const [version, updateVersion] = useVersion();
    var deps = [version, ...(p.deps ?? [])];

    var initialDeps = React.useRef(deps);

    const value = useAPI(signal => {

      async function makeRequest() {

        if (!Finder.isFindable(fo.queryName, false)) {
          if (p.throwIfNotFindable)
            throw Error(`Query ${getQueryKey(fo.queryName)} not allowed`);
        }

        var qd = await Finder.getQueryDescription(p.findOptions.queryName);
        controller.queryDescription = qd;

        var fop = await Finder.parseFindOptions(p.findOptions, qd, false);

        const systemVersioned = p.findOptions.systemTime == undefined && p.ctx?.frame?.currentDate && Finder.isSystemVersioned(qd.columns["Entity"].type);
        if (systemVersioned)
          fop.systemTime = { mode: 'AsOf', startDate: p.ctx?.frame!.currentDate };

        const req = getQueryRequestValue(fop, valueToken?.fullKey, Boolean(p.multipleValues));
        const valPromise = p.customRequest ?
          p.customRequest(req, fop, valueToken ?? null, signal) :
          Finder.API.queryValue(req, p.avoidNotifyPendingRequest, signal);

        if (systemVersioned && p.ctx?.frame?.previousDate) {
          var sv = new QueryTokenString("Entity");

          controller.hasHistoryChanges = (await Finder.API.queryValue({
            queryKey: qd.queryKey,
            systemTime: { mode: "Between", startDate: p.ctx?.frame?.previousDate, endDate: p.ctx?.frame?.currentDate, joinMode: "FirstCompatible" },
            filters: [
              ...req.filters,
              {
                groupOperation: "Or",
                filters: [
                  {
                    groupOperation: "And",
                    filters: [
                      { token: sv.systemValidFrom().token, operation: "GreaterThanOrEqual", value: p.ctx?.frame?.previousDate },
                      { token: sv.systemValidFrom().token, operation: "LessThanOrEqual", value: p.ctx?.frame?.currentDate },
                    ]
                  },
                  {
                    groupOperation: "And",
                    filters: [
                      { token: sv.systemValidTo().token, operation: "GreaterThanOrEqual", value: p.ctx?.frame?.previousDate },
                      { token: sv.systemValidTo().token, operation: "LessThanOrEqual", value: p.ctx?.frame?.currentDate },
                    ]
                  }
                ]
              }
            ],
            valueToken: "Count"
          })) > 0;
        } else {
          controller.hasHistoryChanges = undefined;
        }

        const val = await valPromise;

        const fixedValue = val === undefined ? null : val;
        controller.value = fixedValue;
        p.onValueChange && p.onValueChange(fixedValue);
        return fixedValue;

      }

      if (valueToken === undefined)
        return Promise.resolve(undefined);

      if (p.initialValue !== undefined) {
        if (Hooks.areEqualDeps(deps ?? [], initialDeps.current ?? [])) {
          controller.value = p.initialValue;
          controller.hasHistoryChanges = undefined;
          p.onInitialValueLoaded?.();
          return Promise.resolve(p.initialValue);
        }
        else
          return makeRequest();
      } else {
        return makeRequest();
      }

    }, [p.initialValue, valueToken, Finder.findOptionsPath(p.findOptions), p.ctx?.frame?.currentDate, p.ctx?.frame?.previousDate, ...(deps || [])], { avoidReset: true });



    var controller = React.useMemo(() => ({} as any as SearchValueController), []);
    controller.props = p;
    controller.value = value;
    controller.valueToken = valueToken;
    controller.renderValue = renderValue;
    controller.refreshValue = updateVersion;
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
            {!p.avoidRenderTimeMachineIcon && renderTimeMachineIcon(controller.hasHistoryChanges, `translate(-100%, -80%)`)}
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


    let className = classes(
      p.valueToken == undefined && "count-search",
      p.valueToken == undefined && (value > 0 ? "count-with-results" : "count-no-results"),
      valueToken && (valueToken.type.isLite || valueToken!.type.isEmbedded) && "sf-entity-line-entity",
      p.formControlClass,
      p.formControlClass && isNumeric() && "numeric",
      p.formControlClass && isMultiLine() && "sf-multi-line",

      p.isBadge == false ? "" :
        "badge badge-pill " +
        (p.isBadge == "MoreThanZero" && (value == 0 || value == null) ? "text-bg-light":
          p.badgeColor && typeof p.badgeColor == "function" ? "text-bg-" + p.badgeColor(value) :
            p.badgeColor ? "text-bg-" + p.badgeColor :
              "text-bg-secondary"),

      p.customClass && typeof p.customClass == "function" ? p.customClass(value) : p.customClass
    );

    const { onClick, ...htmlAttrs } = p.htmlAttributes ?? {};

    if (p.formControlClass) {
      return (
        <div id={p.id} className={className} style={p.customStyle} {...htmlAttrs}>
          {!p.avoidRenderTimeMachineIcon && renderTimeMachineIcon(controller.hasHistoryChanges, `translate(-100%, -80%)`)}
          {renderValue()}
        </div>
      );
    }


    if (p.isLink) {
      return (
        <a id={p.id} className={className} onClick={handleClick} href="#" style={p.customStyle} {...htmlAttrs}>
          {!p.avoidRenderTimeMachineIcon && renderTimeMachineIcon(controller.hasHistoryChanges, `translate(-100%, -80%)`)}
          {renderValue()}
        </a>
      );
    }

    return <span id={p.id} className={className} style={p.customStyle} {...htmlAttrs}>
      {!p.avoidRenderTimeMachineIcon && renderTimeMachineIcon(controller.hasHistoryChanges, `translate(-100%, -80%)`)}
      {renderValue()}
    </span>


    function onClickLite(e: React.MouseEvent<any>, lite: Lite<Entity>) {

      e.preventDefault();
      const s = Navigator.getSettings(lite.EntityType)
      const avoidPopup = s != undefined && s.avoidPopup;

      if (e.ctrlKey || e.button == 1 || avoidPopup) {
        window.open(toAbsoluteUrl(Navigator.navigateRoute(lite)));
        return;
      }

      Navigator.view(lite)
        .then(() => { updateVersion(); p.onExplored && p.onExplored(); });
    }

    function renderValue(): React.ReactElement | string | null {

      if (value === undefined)
        return "…";

      if (value === null)
        return null;

      if (!valueToken)
        return value;

      let token = valueToken;
      if (!token)
        return null;

      var qs = Finder.getSettings(p.findOptions.queryName);

      var formatter = Finder.getCellFormatter(qs, token, undefined);

      const cfc: Finder.CellFormatterContext = { columns: [token.fullKey], row: { entity: undefined, columns: [value] }, rowIndex: 0, refresh: () => updateVersion() };
      const cp: ColumnParsed = { column: { token: token }, columnIndex: 0, resultIndex: 0 };
      return formatter.formatter(value, cfc, cp) ?? null;

      //switch (token.filterType) {
      //  case "Integer":
      //  case "Decimal":
      //    {
      //      const numberFormat = toNumberFormat(p.format ?? token.format);

      //      var unit = p.unit === null ? p.unit : p.unit ?? token.unit;
      //      if (unit)
      //        return numberFormat.format(value) + " " + unit;
      //      else
      //        return numberFormat.format(value);
      //    }
      //  case "DateTime":
      //    {
      //      const luxonFormat = toLuxonFormat(p.format ?? token.format, token.type.name as "DateOnly" | "DateTime");
      //      return toFormatWithFixes(DateTime.fromISO(value), luxonFormat);
      //    }
      //  case "Time":
      //    {
      //      const luxonFormat = toLuxonDurationFormat(p.format ?? token.format);
      //      return Duration.fromISOTime(value).toFormat(luxonFormat ?? "hh:mm:ss");
      //    }
      //  case "String": return value;
      //  case "Lite": return value && Navigator.renderLite(value as Lite<Entity>);
      //  case "Embedded": return getToString(value as EmbeddedEntity);
      //  case "Boolean": return <input type="checkbox" className="form-check-input" disabled={true} checked={value} />
      //  case "Enum": return getEnumInfo(token!.type.name, value).niceName;
      //  case "Guid":
      //    let str = value as string;
      //    return <span className="guid">{str.substring(0, 4) + "…" + str.substring(str.length - 4)}</span>;
      //}

      //return value;
    }


    function handleClick(e: React.MouseEvent<any>) {
      e.preventDefault();

      if (p.onExplore) {
        p.onExplore(controller).then(r => {
          if (r && !p.avoidAutoRefresh)
            updateVersion();

          if (p.onExplored)
            p.onExplored();
        });
        return;
      }

      p.htmlAttributes?.onClick?.(e);

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
        fo = { ...p.findOptions };

      if (fo.systemTime == null && p.ctx?.frame?.currentDate && Finder.isSystemVersioned(controller.queryDescription!.columns["Entity"].type)) {
        if (p.ctx?.frame?.previousDate)
          fo.systemTime = { mode: 'Between', startDate: p.ctx?.frame!.previousDate, endDate: p.ctx?.frame!.currentDate, joinMode: "FirstCompatible" };
        else
          fo.systemTime = { mode: 'AsOf', startDate: p.ctx?.frame!.currentDate };

        if (fo.columnOptionsMode == null)
          fo.columnOptionsMode = "Add";

        if (fo.columnOptionsMode != "Remove") {
          if (fo.columnOptions == null)
            fo.columnOptions = [];

          fo.columnOptions.push(...[
            { token: QueryTokenString.entity().systemValidFrom(), hiddenColumn: true },
            { token: QueryTokenString.entity().systemValidTo(), hiddenColumn: true }
          ]);

          if (fo.orderOptions == null)
            fo.orderOptions = [];

          fo.orderOptions.push({ token: QueryTokenString.entity().systemValidFrom(), orderType: "Descending" });
        }
      }

      if (e.ctrlKey || e.button == 1)
        window.open(toAbsoluteUrl(Finder.findOptionsPath(fo)));
      else
        Finder.explore(fo, { searchControlProps: p.searchControlProps, modalSize: p.modalSize }).then(() => {
          if (!p.avoidAutoRefresh)
            updateVersion();

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

export function renderTimeMachineIcon(hasHistoryChanges: boolean | undefined, transform: string): React.ReactElement | null {

  if (hasHistoryChanges === undefined)
    return null;

  return <FontAwesomeIcon icon="circle"
    title={hasHistoryChanges ? EntityControlMessage.Changed.niceToString() : EntityControlMessage.NoChanges.niceToString()}
    fontSize={14}
    style={{
      position: 'absolute',
      zIndex: 2,
      minWidth: "14px",
      minHeight: "14px",
      transform: transform,
      color: hasHistoryChanges ? TimeMachineColors.changed : TimeMachineColors.noChange,
    }}
  />;
}
