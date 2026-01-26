import * as React from "react";
import { DateTime, DateTimeUnit, Duration } from 'luxon'
import { Navigator } from "./Navigator"
import { Dic, classes } from './Globals'
import {
  FilterOptionParsed,
  QueryToken,
  FilterGroupOptionParsed, FilterConditionOptionParsed, isFilterGroup, isFilterCondition,
  hasToArray, getFilterOperations, getFilterGroupUnifiedFilterType, FilterConditionOption, isList
} from './FindOptions';
import { FilterOperation } from './Signum.DynamicQuery';
import { Entity, Lite, SearchMessage, JavascriptMessage, getToString, MList, newMListElement } from './Signum.Entities';
import {
  TypeReference,
  tryGetTypeInfos, getEnumInfo, toLuxonFormat, toNumberFormat,
  PropertyRoute, tryGetTypeInfo,
  toLuxonDurationFormat, toFormatWithFixes, IsByAll, getTypeInfos, Binding,
  QueryTokenString
} from './Reflection';
import EntityLink from './SearchControl/EntityLink';
import SearchControlLoaded from './SearchControl/SearchControlLoaded';
import { EntityBaseController, EntityCombo, EntityLine, EntityStrip, FormGroup, StyleContext, TypeContext } from "./Lines";
import { similarToken } from "./Search";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { TextHighlighter } from "./Components/Typeahead";
import { Finder } from "./Finder";
import { OverlayTrigger, Popover } from "react-bootstrap";
import { TypeEntity } from "./Signum.Basics";
import { useForceUpdate } from "./Hooks";
import { TextAreaLine } from "./Lines/TextAreaLine";
import { TextBoxLine } from "./Lines/TextBoxLine";
import { AutoLine } from "./Lines/AutoLine";
import { EnumLine } from "./Lines/EnumLine";
import { KeyNames } from "./Components";
import QueryTokenBuilder from "./SearchControl/QueryTokenBuilder";
import { LinkButton } from "./Basics/LinkButton";


export function isMultiline(pr?: PropertyRoute): boolean {
  if (pr == null || pr.member == null)
    return false;

  return pr.member.isMultiline || pr.member.maxLength != null && pr.member.maxLength > 150;
}

export function initFormatRules(): Finder.FormatRule[] {
  return [
    {
      name: "Object",
      isApplicable: qt => true,
      formatter: (qt, sc) => {

        var hl = new TextHighlighter(getKeywordsSC(qt, sc));

        return new Finder.CellFormatter(cell => cell ? <span className="try-no-wrap">{hl.highlight(cell.toString())}</span> : undefined, true);
      }
    },
    {
      name: "Entity",
      isApplicable: qt => qt.filterType == "Embedded" || qt.filterType == "Model",
      formatter: qt => new Finder.CellFormatter(cell => cell ? <span className="try-no-wrap">{getToString(cell)}</span> : undefined, true)
    },
    {
      name: "MultiLine",
      isApplicable: qt => {
        if (qt.type.name == "string" && qt.propertyRoute != null) {
          var pr = PropertyRoute.tryParseFull(qt.propertyRoute);
          if (pr != null && pr.member != null && isMultiline(pr))
            return true;
        }

        return false;
      },
      formatter: (qt, sc) => {
        var hl = new TextHighlighter(getKeywordsSC(qt, sc));

        return new Finder.CellFormatter(cell => cell ? <span className="multi-line">{hl.highlight(cell.toString())}</span> : undefined, true);
      }
    },
    {
      name: "Snippet",
      isApplicable: qt => qt.key == "Snippet" && qt.parent?.type.name == "string",
      formatter: (qt, sc) => {
        var hl = new TextHighlighter(getKeywordsSC(qt, sc));

        return new Finder.CellFormatter(cell => {
          if (!cell)
            return cell;

          return (cell as string).toString().split("(…)").map(str => hl.highlight(str)).joinHtml(<small className="text-muted">(…)</small>);
        }, true);
      }
    },
    {
      name: "SmallText",
      isApplicable: qt => {
        if (qt.type.name == "string" && qt.propertyRoute != null) {
          var pr = PropertyRoute.tryParseFull(qt.propertyRoute);
          if (pr != null && pr.member != null && !pr.member.isMultiline && pr.member.maxLength != null && pr.member.maxLength <= 150)
            return true;
        }

        return false;
      },
      formatter: (qt, sc) => {

        var hl = new TextHighlighter(getKeywords(qt, sc?.state.resultFindOptions?.filterOptions));

        return new Finder.CellFormatter(cell => cell ? <span className="try-no-wrap">{hl.highlight(cell.toString())}</span> : undefined, false);
      }
    },
    {
      name: "Password",
      isApplicable: qt => qt.format == "Password",
      formatter: qt => new Finder.CellFormatter(cell => cell ? <span className="try-no-wrap">•••••••</span> : undefined, false)
    },
    {
      name: "Enum",
      isApplicable: qt => qt.filterType == "Enum",
      formatter: qt => new Finder.CellFormatter(cell => {
        if (cell == undefined)
          return undefined;

        var ei = getEnumInfo(qt.type.name, cell);

        return <span className="try-no-wrap">{ei ? ei.niceName : cell}</span>
      }, false)
    },
    {
      name: "Lite",
      isApplicable: qt => qt.filterType == "Lite",
      formatter: qt => new Finder.CellFormatter((cell: Lite<Entity> | undefined, ctx) => !cell ? undefined : <EntityLink lite={cell} onNavigated={ctx.refresh} inSearch="related" shy />, true)
    },
    {
      name: "LiteNoFill",
      isApplicable: qt => {
        return qt.filterType == "Lite" && tryGetTypeInfos(qt.type)?.every(ti => ti && Navigator.getSettings(ti)?.avoidFillSearchColumnWidth);
      },
      formatter: qt => new Finder.CellFormatter((cell: Lite<Entity> | undefined, ctx) => !cell ? undefined : <EntityLink lite={cell} onNavigated={ctx.refresh} inSearch="related" shy />, false)
    },
    {
      name: "Guid",
      isApplicable: qt => qt.filterType == "Guid",
      formatter: (qt, sc) => {

        var kw = getKeywordsSC(qt, sc)?.map(a => a.toLowerCase());

        return new Finder.CellFormatter((cell: string | undefined) => {
          if (!cell)
            return cell;

          var small = cell.substring(0, 4) + "…" + cell.substring(cell.length - 4)

          if (kw?.contains(cell.toLowerCase()))
            return <strong className="try-no-wrap">{small}</strong>;

          return <span className="guid try-no-wrap">{small}</span>

        }, false);
      }
    },
    {
      name: "DateTime",
      isApplicable: qt => qt.filterType == "DateTime",
      formatter: qt => {
        const luxonFormat = toLuxonFormat(qt.format, qt.type.name as "DateOnly" | "DateTime");
        return new Finder.CellFormatter((cell: string | undefined) => cell == undefined || cell == "" ? "" : <bdi className="date try-no-wrap">{toFormatWithFixes(DateTime.fromISO(cell), luxonFormat)}</bdi>, false, "date-cell") //To avoid flippig hour and date (L LT) in RTL cultures
      }
    },
    {
      name: "Time",
      isApplicable: qt => qt.filterType == "Time",
      formatter: qt => {
        const durationFormat = toLuxonDurationFormat(qt.format) ?? "hh:mm:ss";

        return new Finder.CellFormatter((cell: string | undefined) => cell == undefined || cell == "" ? "" : <bdi className="date try-no-wrap">{Duration.fromISOTime(cell).toFormat(durationFormat)}</bdi>, false, "date-cell") //To avoid flippig hour and date (L LT) in RTL cultures
      }
    },
    {
      name: "TimeSeries",
      isApplicable: qt => qt.fullKey == QueryTokenString.timeSeries.token,
      formatter: (qt, scl) => {
        let luxonFormat = toLuxonFormat(qt.format, qt.type.name as "DateOnly" | "DateTime");
        const st = scl?.props.findOptions.systemTime;
        if (st) {
          var start = DateTime.fromISO(st.startDate!);

          if (start.equals(start.startOf(st.timeSeriesUnit?.firstLower() as DateTimeUnit))) {
            luxonFormat = toLuxonFormat(st.timeSeriesUnit == "Year" ? "YYYY" :
              st.timeSeriesUnit == "Month" ? "LLLL YYYY" :
                st.timeSeriesUnit == "Day" ? "d" :
                  qt.format, "DateTime");
          }
        }


        return new Finder.CellFormatter((cell: string | undefined) => cell == undefined || cell == "" ? "" : <bdi className="date try-no-wrap">{toFormatWithFixes(DateTime.fromISO(cell), luxonFormat)}</bdi>, false, "date-cell") //To avoid flippig hour and date (L LT) in RTL cultures
      }
    },
    {
      name: "SystemValidFrom",
      isApplicable: qt => qt.fullKey.tryAfterLast(".") == "SystemValidFrom",
      formatter: qt => {
        return new Finder.CellFormatter((cell: string | undefined, ctx) => {

          if (cell == undefined || cell == "")
            return "";

          var c = DateTime.fromISO(cell);

          var className = c.year <= 1 ? "date-start" :
            ctx.systemTime && ctx.systemTime.mode == "Between" && DateTime.fromISO(ctx.systemTime.startDate!) <= c ? "date-created" :
              undefined;

          const luxonFormat = toLuxonFormat(qt.format, qt.type.name as "DateOnly" | "DateTime");
          return (
            <bdi className={classes("date", "try-no-wrap", className)}>
              {toFormatWithFixes(c, luxonFormat)}
            </bdi>);
        }, false, "date-cell"); //To avoid flippig hour and date (L LT) in RTL cultures
      }
    },
    {
      name: "SystemValidTo",
      isApplicable: qt => qt.fullKey.tryAfterLast(".") == "SystemValidTo",
      formatter: qt => {
        return new Finder.CellFormatter((cell: string | undefined, ctx) => {
          if (cell == undefined || cell == "")
            return "";

          var c = DateTime.fromISO(cell);

          var className = c.year >= 9999 ? "date-end" :
            ctx.systemTime && ctx.systemTime.mode == "Between" && c <= DateTime.fromISO(ctx.systemTime.endDate!) ? "date-removed" :
              undefined;

          const luxonFormat = toLuxonFormat(qt.format, qt.type.name as "DateOnly" | "DateTime");
          return <bdi className={classes("date", "try-no-wrap", className)}>{toFormatWithFixes(c, luxonFormat)}</bdi>;
        }, false, "date-cell");//To avoid flippig hour and date (L LT) in RTL cultures
      }
    },
    {
      name: "Integer",
      isApplicable: qt => qt.filterType == "Integer",
      formatter: (qt, sc) => {
        var values = getKeywordsSC(qt, sc)?.map(a => a.toLowerCase());
        const numberFormat = toNumberFormat(qt.format);
        return new Finder.CellFormatter((cell: number | undefined) => {
          if (cell == null)
            return cell;

          var str = numberFormat.format(cell);

          if (values?.contains(str))
            return <strong className="try-no-wrap">{str}</strong>;
          else
            return <span className="try-no-wrap">{str}</span>;

        }, false, "numeric-cell");
      }
    },
    {
      name: "Decimal",
      isApplicable: qt => qt.filterType == "Decimal",
      formatter: (qt, hl) => {
        const numberFormat = toNumberFormat(qt.format);
        return new Finder.CellFormatter((cell: number | undefined) => cell == undefined ? "" : <span className="try-no-wrap">{numberFormat.format(cell)}</span>, false, "numeric-cell");
      }
    },
    {
      name: "Number with Unit",
      isApplicable: qt => (qt.filterType == "Integer" || qt.filterType == "Decimal") && Boolean(qt.unit),
      formatter: qt => {
        const numberFormat = toNumberFormat(qt.format);
        return new Finder.CellFormatter((cell: number | undefined) => cell == undefined ? "" : <span className="try-no-wrap">{numberFormat.format(cell) + "\u00a0" + qt.unit}</span>, false, "numeric-cell");
      }
    },
    {
      name: "Bool",
      isApplicable: qt => qt.filterType == "Boolean",
      formatter: col => new Finder.CellFormatter((cell: boolean | undefined) => cell == undefined ? undefined : <input type="checkbox" className="form-check-input" disabled={true} checked={cell} />, false, "centered-cell")
    },
    {
      name: "Phone",
      isApplicable: qt => {
        if (qt.type.name == "string" && qt.propertyRoute != null) {
          var pr = PropertyRoute.tryParseFull(qt.propertyRoute);
          if (pr != null && pr.member != null && pr.member.isPhone == true)
            return true;
        }

        return false;
      },
      formatter: (qt, sc) => {

        var hl = new TextHighlighter(getKeywords(qt, sc?.state.resultFindOptions?.filterOptions));

        return new Finder.CellFormatter((cell: string | undefined) => {
          if (!cell)
            return cell;

          const multiLineClass = isMultiline(PropertyRoute.tryParseFull(qt.propertyRoute!)) ? "multi-line" : "try-no-wrap";

          return (
            <span className={multiLineClass}>
              {cell.split(",").map((t, i) => <a key={i} href={`tel:${t.trim()}`}>{hl.highlight(t.trim())}</a>).joinCommaHtml(", ")}
            </span>
          );
        }, false, "telephone-link-cell");

      }
    },
    {
      name: "Email",
      isApplicable: qt => {
        if (qt.type.name == "string" && qt.propertyRoute != null) {
          var pr = PropertyRoute.tryParseFull(qt.propertyRoute);
          if (pr != null && pr.member != null && pr.member.isMail == true)
            return true;
        }

        return false;
      },
      formatter: (qt, sc) => {

        var hl = new TextHighlighter(getKeywords(qt, sc?.state.resultFindOptions?.filterOptions));

        return new Finder.CellFormatter((cell: string | undefined) => {
          if (!cell)
            return cell;

          const multiLineClass = isMultiline(PropertyRoute.tryParseFull(qt.propertyRoute!)) ? "multi-line" : "try-no-wrap";

          return (
            <span className={multiLineClass}>
              <a href={`mailto:${cell}`}>{hl.highlight(cell)}</a>
            </span>
          );
        }, false, "email-link-cell");

      }
    },
  ];
}

export function getKeywordsSC(token: QueryToken, sc?: SearchControlLoaded): string[] | undefined {
  return getKeywords(token, sc?.state.resultFindOptions?.filterOptions);
}

export function getKeywords(token: QueryToken, filters?: FilterOptionParsed[]): string[] | undefined {
  if (filters == null)
    return undefined;

  if (token.key == "Snippet" && token.parent?.filterType == "String")
    token = token.parent;

  //Keep in sync with FilterFullText.GetKeywords
  function extractSyntax(regex: RegExp, value: string) {

    var result = value.split(regex).map(a => {
      a = a.trim()
      if (a.startsWith("\"") && a.endsWith("\""))
        a = a.after("\"").beforeLast("\"");

      return a;
    }).filter(a => a.length > 0);

    return result;
  }

  function splitKeywords(value: unknown, splitValue: boolean | undefined, operation: FilterOperation): string[] {
    if (typeof value == "string" && (splitValue || operation == "FreeText"))
      return (value as string).split(/\s+/);

    if (operation == "ComplexCondition")
      return extractSyntax(/AND|OR|NOT|NEAR|\(|\)|\*/g, value as string ?? "");

    if (operation == "TsQuery")
      return extractSyntax(/&|\||!|<->|<\d>|\(|\)|\*/g, value as string ?? "");

    if (operation == "TsQuery_WebSearch")
      return extractSyntax(/OR|-|\*/, value as string ?? "");

    if (operation == "IsIn" || operation == "IsNotIn") {
      if (Array.isArray(value))
        return value.map(a => typeof a == "string" ? a :
          typeof a == "number" ? a.toString() : null)
          .notNull();
    }

    if (typeof value == "string")
      return [value];

    if (typeof value == "number")
      return [value.toString()];

    return [];
  }

  function isNegative(fo: FilterOperation) {
    return fo == "NotStartsWith" || fo == "NotContains" || fo == "NotEndsWith" || fo == "NotLike" || fo == "IsNotIn";
  }

  function getFiltersKeywords(fo: FilterOptionParsed): string[] {

    if (isFilterGroup(fo)) {
      if (fo.value && fo.pinned && typeof fo.value == "string") {

        var filterConditions = fo.filters.filter(sf => sf.token != null && isFilterCondition(sf)) as FilterConditionOptionParsed[];

        var filters = filterConditions.filter(sf => similarTokenToStr(sf.token!, token) && sf.operation && !isNegative(sf.operation))
          .flatMap(sf => splitKeywords(fo.value!, fo.pinned?.splitValue, sf.operation!));

        return filters;
      } else {
        return fo.filters.notNull().flatMap(f => getFiltersKeywords(f)).distinctBy(a => a);
      }
    }
    else {
      if (fo.token && fo.operation && !isNegative(fo.operation) && similarTokenToStr(fo.token, token)) {
        return splitKeywords(fo.value, fo.pinned?.splitValue, fo.operation);
      } else {
        return [];
      }
    }
  }

  return filters.notNull().flatMap(f => getFiltersKeywords(f)).distinctBy(a => a);
}

export function similarTokenToStr(tokenF: QueryToken, tokenC: QueryToken): boolean {
  if (similarToken(tokenF.fullKey, tokenC.fullKey))
    return true;

  if (tokenF.propertyRoute != null && tokenF.propertyRoute == tokenC.propertyRoute)
    return true;

  if (tokenF.filterType == "TsVector" && tokenF.tsVectorFor && tokenC.propertyRoute && tokenF.tsVectorFor.contains(tokenC.propertyRoute)) {
    return true;
  }

  if (tokenF && tokenF.key == "ToString") {
    var steps = getToStringDependencies(tokenF.parent!.type);

    if (steps && steps.some(a => similarToken(tokenF.parent?.fullKey + "." + a, tokenC.fullKey)))
      return true;
  }

  if (tokenC && tokenC.key == "ToString") {
    var steps = getToStringDependencies(tokenC.parent!.type);

    if (steps && steps.some(a => similarToken(tokenC.parent?.fullKey + "." + a, tokenF.fullKey)))
      return true;
  }

  return false;
}

const toStringFunctionTokensCache: { [typeName: string]: string[] | null } = {};

export function getToStringDependencies(tr: TypeReference): string[] | null {

  var ti = tryGetTypeInfo(tr.name);
  if (ti == null)
    return null;

  if (ti.toStringFunction == null)
    return null;

  var cachedResult = toStringFunctionTokensCache[tr.name];
  if (cachedResult !== undefined)
    return cachedResult

  var parts = Array.from(ti.toStringFunction.matchAll(/e(\.(\w+))+/g));

  var result = parts.map(a => a[0].split(".").slice(1).map(a => a.firstUpper()).join("."));

  toStringFunctionTokensCache[tr.name] = result;

  return result;
}




export function initEntityFormatRules(): Finder.EntityFormatRule[] {
  return [
    {
      name: "View",
      isApplicable: sc => true,
      formatter: new Finder.EntityFormatter(({ row, columns, searchControl: sc }) => !row.entity || !Navigator.isViewable(row.entity.EntityType, { isSearch: "main" }) ? undefined :
        <EntityLink lite={row.entity}
          inSearch="main"
          hideIfNotViable
          onNavigated={sc?.handleOnNavigated}
          getViewPromise={sc && (sc.props.getViewPromise ?? sc.props.querySettings?.getViewPromise)}
          inPlaceNavigation={sc?.props.view == "InPlace"} className="sf-line-button sf-view">
          {sc?.state.isMobile == true && sc?.state.viewMode == "Mobile" ? undefined :
            <span title={SearchMessage.View.niceToString()}>
              {EntityBaseController.getViewIcon()}
            </span>}
        </EntityLink>, "centered-cell")
    },
    {
      name: "View",
      isApplicable: sc => sc?.state.resultFindOptions?.groupResults == true,
      formatter: new Finder.EntityFormatter(({ row, columns, searchControl: sc }) =>
        <LinkButton
          title={JavascriptMessage.ShowGroup.niceToString()}
          className="sf-line-button sf-view"
          onClick={e => { sc!.openRowGroup(row, e); }}>
          <FontAwesomeIcon aria-hidden={true} icon="layer-group" />
        </LinkButton>, "centered-cell")
    },
  ]
}



export function initQuickFilterRules(): Finder.QuickFilterRule[] {
  return ([

    {
      name: "Default",
      applicable: qt => true,
      execute: async (qt, cellValue, sc) => {

        return sc.addQuickFilter(qt, getFilterOperations(qt).first(), cellValue);
      },
    },
    {
      name: "PreferEquals",
      applicable: qt => Boolean(qt.preferEquals),
      execute: async (qt, cellValue, sc) => {

        return sc.addQuickFilter(qt, "EqualTo", cellValue);
      },
    },

    {
      name: "Model",
      applicable: qt => qt.filterType == "Model",
      execute: async (qt, cellValue, sc) => {

        return sc.addQuickFilter(qt, cellValue == null ? "EqualTo" : "DistinctTo", null);
      },
    },
    {
      name: "Embedded",
      applicable: qt => qt.filterType == "Embedded",
      execute: async (qt, cellValue, sc) => {

        return sc.addQuickFilter(qt, cellValue == null ? "EqualTo" : "DistinctTo", null);
      },
    },
    {
      name: "Snippet",
      applicable: qt => qt.key == "Snippet" && qt.parent?.filterType == "String",
      execute: async (qt, cellValue, sc) => {

        return sc.addQuickFilter(qt.parent!, getFilterOperations(qt.parent!).first(), cellValue);
      },
    },

    {
      name: "ToArray",
      applicable: qt => hasToArray(qt) != null,
      execute: async (qt, cellValue, sc) => {
        var toArray = hasToArray(qt)!;
        const newToken = await sc.parseSingleFilterToken(qt!.fullKey.split(".").map(p => p == toArray!.key ? "Any" : p).join("."));
        return sc.addQuickFilter(newToken, "IsIn", cellValue ?? []);
      },
    },

  ]);
}



export function initFilterValueFormatRules(): Finder.FilterValueFormatter[] {
  return [

    {
      name: "Value",
      applicable: (f, ffc) => isFilterCondition(f) && (
        f.token?.filterType == "Boolean" ||
        f.token?.filterType == "DateTime" ||
        f.token?.filterType == "Decimal" ||
        f.token?.filterType == "Guid" ||
        f.token?.filterType == "Integer" ||
        f.token?.filterType == "Time" ||
        f.token?.filterType == "String"),
      renderValue: (f, ffc) => {
        var tokenType = f.token!.type;
        if (ffc.forceNullable)
          tokenType = { ...tokenType, isNotNullable: false };
        return <AutoLine ctx={ffc.ctx} type={tokenType} format={f.token!.format} unit={f.token!.unit} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "String",
      applicable: (f, ffc) => isFilterCondition(f) && (
        f.token?.filterType == "String"),
      renderValue: (f, ffc) => {
        var tokenType = f.token!.type;
        if (ffc.forceNullable)
          tokenType = { ...tokenType, isNotNullable: false };
        return <TextBoxLine ctx={ffc.ctx} type={tokenType} autoTrimString={false} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "Enum",
      applicable: (f, ffc) => isFilterCondition(f) && f.token?.filterType == "Enum",
      renderValue: (f, ffc) => {
        var tokenType = f.token!.type;
        if (ffc.forceNullable)
          tokenType = { ...tokenType, isNotNullable: false };

        const ti = tryGetTypeInfos(tokenType).single();
        if (!ti)
          throw new Error(`EnumType ${tokenType.name} not found`);
        const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
        return <EnumLine ctx={ffc.ctx} type={tokenType} unit={f.token!.unit} optionItems={members} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "Embedded",
      applicable: (f, ffc) => isFilterCondition(f) && f.token?.filterType == "Embedded",
      renderValue: (f, ffc) => {
        return <EntityLine ctx={ffc.ctx} type={f.token!.type} create={false} autocomplete={null} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "Model",
      applicable: (f, ffc) => isFilterCondition(f) && f.token?.filterType == "Model",
      renderValue: (f, ffc) => {
        return <EntityLine ctx={ffc.ctx} type={f.token!.type} create={false} autocomplete={null} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "Lite",
      applicable: (f, ffc) => isFilterCondition(f) && f.token?.filterType == "Lite",
      renderValue: (f, ffc) => {
        return <EntityLine ctx={ffc.ctx} type={f.token!.type} create={false} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "Lite_LowPopulation",
      applicable: (f, ffc) => isFilterCondition(f) && f.token?.filterType == "Lite" && tryGetTypeInfos(f.token!.type).every(ti => ti == null || ti.isLowPopulation),
      renderValue: (f, ffc) => {
        return <EntityCombo ctx={ffc.ctx} type={f.token!.type} create={false} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "Lite_IsByAll",
      applicable: (f, ffc) => isFilterCondition(f) && f.token?.filterType == "Lite" && f.token!.type.name == IsByAll,
      renderValue: (f, ffc) => {
        return <EntityLine ctx={ffc.ctx} type={f.token!.type} create={false} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    },
    {
      name: "Lite_TypeEntity",
      applicable: (f, ffc) => isFilterCondition(f) && f.token?.filterType == "Lite" && f.token.key == "[EntityType]" && f.token.parent!.type.name != IsByAll,
      renderValue: (f, ffc) => {
        return <EntityCombo ctx={ffc.ctx} type={f.token!.type} create={false} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} findOptions={{
          queryName: TypeEntity,
          filterOptions: [{ token: TypeEntity.token(a => a.cleanName), operation: "IsIn", value: f.token!.parent!.type.name.split(", ") }]
        }} />
      }
    },
    {
      name: "MultiValue",
      applicable: (f, ffc) => isFilterCondition(f) && isList(f.operation!),
      renderValue: (f, ffc) => {
        const fc = f as FilterConditionOptionParsed;

        var pseudoFilter = { ...f, operation: "EqualTo" } as FilterConditionOptionParsed
        var rule = Finder.filterValueFormatRules.filter(r => r.applicable(pseudoFilter, ffc)).last();
        return (
          <FormGroup ctx={ffc.ctx} label={ffc.label}>
            {inputId => <MultiValue values={f.value} readOnly={f.frozen} onChange={() => ffc.handleValueChange(f)}
              onRenderItem={ctx => rule.renderValue(f, { ...ffc, ctx, mandatory: true, })} />}
          </FormGroup>
        );
      }
    },
    {
      name: "MultiEntity",
      applicable: (f, ffc) => isFilterCondition(f) && isList(f.operation!) && f.token!.filterType == "Lite",
      renderValue: (f, ffc) => {
        const fc = f as FilterConditionOptionParsed;

        return (
          <FormGroup ctx={ffc.ctx} label={ffc.label}>
            {inputId => <MultiEntity values={f.value} readOnly={f.frozen} type={f.token!.type.name} onChange={() => ffc.handleValueChange(f)} />}
          </FormGroup>
        );
      }
    },
    {
      name: "TextArea",
      applicable: (f, ffc) => isFilterCondition(f) && isFullTextSearch(f.operation),
      renderValue: (f, ffc) => {
        const fc = f as FilterConditionOptionParsed;
        return <FilterTextArea ctx={ffc.ctx}
          filterOperation={fc.operation ?? null}
          onChange={(() => ffc.handleValueChange(f, isComplexFullTextSearch(fc.operation)))}
          label={ffc.label || SearchMessage.Search.niceToString()} />
      }
    },
    {
      name: "FilterGroup",
      applicable: (f, ffc) => isFilterGroup(f),
      renderValue: (f, ffc) => {
        var fg = f as FilterGroupOptionParsed;
        if (fg.filters.some(a => !a.token))
          return <TextBoxLine ctx={ffc.ctx} type={{ name: "string" }} onChange={() => ffc.handleValueChange(f)} label={ffc.label || SearchMessage.Search.niceToString()} />

        if (fg.filters.map(a => getFilterGroupUnifiedFilterType(a.token!.type) ?? "").distinctBy().onlyOrNull() == null && ffc.ctx.value)
          ffc.ctx.value = undefined;

        var tr = fg.filters.map(a => a.token!.type).distinctBy(a => a.name).onlyOrNull();
        var format = (tr && fg.filters.map((a, i) => a.token!.format ?? "").distinctBy().onlyOrNull() || null) ?? undefined;
        var unit = (tr && fg.filters.map((a, i) => a.token!.unit ?? "").distinctBy().onlyOrNull() || null) ?? undefined;

        return <AutoLine ctx={ffc.ctx} type={tr ?? { name: "string" }} format={format} unit={unit} onChange={() => ffc.handleValueChange(f)} label={ffc.label || SearchMessage.Search.niceToString()} />
      }
    },
    {
      name: "FilterGroup_TextArea",
      applicable: (f, ffc) => isFilterGroup(f) && f.filters.some(sf => isFilterCondition(sf) && isFullTextSearch(sf.operation)),
      renderValue: (f, ffc) => {
        var fg = f as FilterGroupOptionParsed;
        const isComplex = fg.filters.some(sf => isFilterCondition(sf) && isComplexFullTextSearch(sf.operation));
        var filterOperation = fg.filters.map(sf => isFilterCondition(sf) && isFullTextSearch(sf.operation) ? sf.operation : null).notNull().distinctBy(a => a).onlyOrNull();
        return <FilterTextArea ctx={ffc.ctx}
          filterOperation={filterOperation}
          onChange={(() => ffc.handleValueChange(f, isComplex))}
          label={ffc.label || SearchMessage.Search.niceToString()} />;
      }
    },
  ]
}


export interface MultiValueProps {
  values: any[],
  onRenderItem: (ctx: TypeContext<any>) => React.ReactElement;
  readOnly: boolean;
  onChange: () => void;
}

export function MultiValue(p: MultiValueProps): React.ReactElement {

  const forceUpdate = useForceUpdate();

  function handleDeleteValue(e: React.MouseEvent<any>, index: number) {
    e.preventDefault();
    p.values.removeAt(index);
    p.onChange();
    forceUpdate();
  }

  function handleAddValue(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.values.push(undefined);
    p.onChange();
    forceUpdate();
  }

  return (
    <table style={{ marginBottom: "0px" }} className="sf-multi-value">
      <tbody>
        {
          p.values.map((v, i) =>
            <tr key={i}>
              <td>
                {!p.readOnly &&
                  <LinkButton title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
                    className="sf-line-button sf-remove"
                    onClick={e => handleDeleteValue(e, i)}
                    tabIndex={0}>
                    <FontAwesomeIcon aria-hidden={true} icon="xmark" />
                  </LinkButton>}
              </td>
              <td>
                {
                  p.onRenderItem(new TypeContext<any>(undefined,
                    {
                      formGroupStyle: "None",
                      formSize: "xs",
                      readOnly: p.readOnly
                    }, undefined, new Binding<any>(p.values, i)))
                }
              </td>
            </tr>)
        }
        <tr >
          <td colSpan={4}>
            {!p.readOnly &&
              <LinkButton title={StyleContext.default.titleLabels ? SearchMessage.AddValue.niceToString() : undefined}
                className="sf-line-button sf-create"
                onClick={handleAddValue}
                tabIndex={0}>
                <FontAwesomeIcon aria-hidden={true} icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddValue.niceToString()}
              </LinkButton>}
          </td>
        </tr>
      </tbody>
    </table>
  );
}


export function MultiEntity(p: { values: Lite<Entity>[], readOnly: boolean, type: string, onChange: () => void, vertical?: boolean }): React.ReactElement {
  const mListEntity = React.useRef<MList<Lite<Entity>>>([]);


  mListEntity.current.clear();
  mListEntity.current.push(...p.values.map(lite => newMListElement(lite)));

  var ctx = new TypeContext<MList<Lite<Entity>>>(undefined, { formGroupStyle: "None", readOnly: p.readOnly, formSize: "xs" }, undefined, Binding.create(mListEntity, a => a.current));


  return <EntityStrip ctx={ctx} type={{ name: p.type, isLite: true, isCollection: true }} create={false} vertical={p.vertical} onChange={() => {
    p.values.clear();
    p.values.push(...mListEntity.current.map(a => a.element));
    p.onChange();
  }} />
}



export function FilterTextArea(p: { ctx: TypeContext<string>, filterOperation: FilterOperation | null, onChange: () => void, label?: string }): React.ReactElement {
  return <TextAreaLine ctx={p.ctx}
    type={{ name: "string" }}
    label={p.label}
    valueHtmlAttributes={p.filterOperation != null && p.filterOperation != "FreeText" ? {
      onKeyDown: e => {
        console.log(e);
        if (e.key == KeyNames.enter && !e.shiftKey) {
          e.preventDefault();
        }
      },
      onKeyUp: e => {
        console.log(e);
        if (e.key == KeyNames.enter && e.shiftKey) {
          e.stopPropagation()
        }
      }
    } : undefined}
    extraButtons={vlc => p.filterOperation && <ComplexConditionSyntax fo={p.filterOperation} />}
    onChange={p.onChange}
  />
}

export function ComplexConditionSyntax(p: { fo: FilterOperation }): React.ReactElement | null {
  var help = FullTextSearchHelps[p.fo];

  if (help == null)
    return null;

  const popover = (
    <Popover id="popover-basic">
      <Popover.Header as="h3">{ }</Popover.Header>
      <Popover.Body>
        <ul className="ps-3">
          {help?.examples.map((a, i) => <li key={i} style={{ whiteSpace: "nowrap" }}><code>{a}</code></li>)}
        </ul>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="click" placement="right" overlay={popover} >
      <button className="sf-line-button sf-view btn input-group-text">{help?.icon}</button>
    </OverlayTrigger>
  );

}


export interface FullTextSearchHelp {
  icon: React.ReactElement;
  title: string;
  url: React.ReactElement;
  examples: string[];
}


const FullTextSearchHelps: Partial<Record<FilterOperation, FullTextSearchHelp>> = {
  "ComplexCondition": {
    icon: <span>OR</span>,
    title: "Full-Text Search Syntax",
    url: <a href="https://learn.microsoft.com/en-us/sql/relational-databases/search/query-with-full-text-search" target="_blank">Microsoft Docs <FontAwesomeIcon aria-hidden={true} icon="arrow-up-right-from-square" /></a>,
    examples: [
      "banana AND strawberry",
      "banana OR strawberry",
      "apple AND NOT (banana OR strawberry)",
      "\"Dragon Fruit\" OR \"Passion Fruit\"",
      "*berry",
      "NEAR(\"apple\", \"orange\")",
      "NEAR((\"apple\", \"orange\"), 3)",
    ]
  },
  "TsQuery": {
    icon: <span>&</span>,
    title: "Full-Text Search to_tsquery Syntax",
    url: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html" target="_blank">PostgreSQL Docs <FontAwesomeIcon aria-hidden={true} icon="arrow-up-right-from-square" /></a>,
    examples: [
      "banana & strawberry",
      "banana | strawberry",
      "apple & !(banana | strawberry)",
      "Dragon_Fruit | Passion_Fruit",
      "grape*",
      "apple <-> orange",
      "apple <3> orange",
    ]
  },
  "TsQuery_Plain": {
    icon: <FontAwesomeIcon icon="quote-left" />,
    title: "Full-Text Search to_simpletsquery Syntax",
    url: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html" target="_blank">PostgreSQL Docs <FontAwesomeIcon aria-hidden={true} icon="arrow-up-right-from-square" /></a>,
    examples: [
      "banana strawberry",
      "\"banana strawberry\" <3>",
    ]
  },
  "TsQuery_Phrase": {
    icon: <FontAwesomeIcon icon="text" />,
    title: "Full-Text Search plainto_tsquery Syntax",
    url: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html" target="_blank">PostgreSQL Docs <FontAwesomeIcon aria-hidden={true} icon="arrow-up-right-from-square" /></a>,
    examples: [
      "banana strawberry",
    ]
  },
  "TsQuery_WebSearch": {
    icon: <FontAwesomeIcon icon="globe" />,
    title: "Full-Text Search to_tsquery Syntax",
    url: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html" target="_blank">PostgreSQL Docs <FontAwesomeIcon aria-hidden={true} icon="arrow-up-right-from-square" /></a>,
    examples: [
      "banana strawberry",
      "banana OR strawberry",
      "apple -banana -strawberry",
      "\"Dragon Fruit\" OR \"Passion Fruit\"",
      "grape*"
    ]
  },
};

function isFullTextSearch(filterOperation?: FilterOperation) {
  return filterOperation == "ComplexCondition" ||
    filterOperation == "FreeText" ||
    filterOperation == "TsQuery" ||
    filterOperation == "TsQuery_Phrase" ||
    filterOperation == "TsQuery_Plain" ||
    filterOperation == "TsQuery_WebSearch";
}

function isComplexFullTextSearch(filterOperation?: FilterOperation) {
  return filterOperation == "ComplexCondition" ||
    filterOperation == "TsQuery";
}

