import * as React from 'react'
import { DateTime } from 'luxon'
import { classes, Dic } from '@framework/Globals'
import { AutoLine, EntityLine, EntityCombo, TextBoxLine, EnumLine } from '@framework/Lines'
import { FilterOptionParsed } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { Finder } from '@framework/Finder'
import { Binding, IsByAll, tryGetTypeInfos, TypeReference, getTypeInfos } from '@framework/Reflection'
import {
  QueryDescription, SubTokensOptions, FilterConditionOptionParsed,
  isList, FilterType, FilterGroupOptionParsed, PinnedFilter, PinnedFilterParsed,
	getFilterGroupUnifiedFilterType, getFilterType, isFilterGroup
} from '@framework/FindOptions'
import { Lite, Entity, parseLite, liteKey, liteKeyLong } from "@framework/Signum.Entities";
import { Navigator } from "@framework/Navigator";
import FilterBuilder, { RenderValueContext } from '@framework/SearchControl/FilterBuilder';
import { MList, newMListElement } from '@framework/Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { PinnedQueryFilterEmbedded, QueryFilterEmbedded, QueryTokenEmbedded, UserAssetQueryMessage } from '../Signum.UserAssets.Queries'
import { MultiValue } from '@framework/FinderRules'
import { HeaderType } from '@framework/Lines/GroupHeader'

interface FilterBuilderEmbeddedProps {
  ctx: TypeContext<MList<QueryFilterEmbedded>>;
  avoidFieldSet?: boolean | HeaderType;
  queryKey: string;
  subTokenOptions: SubTokensOptions;
  onChanged?: () => void;
  showPinnedFilterOptions?: boolean;
  showDashboardBehaviour?: boolean;
}

export function FilterBuilderEmbedded(p: FilterBuilderEmbeddedProps): React.JSX.Element {

  const qd = useAPI(() => Finder.getQueryDescription(p.queryKey), [p.queryKey]);
  const filterOptions = useAPI(() => qd == null ? Promise.resolve(null) : FilterBuilderEmbedded.toFilterOptionParsed(qd, p.ctx.value, p.subTokenOptions), [qd, p.ctx.value, p.subTokenOptions]);

  const forceUpdate = useForceUpdate();
  
  function handleFiltersChanged() {
    var ctx = p.ctx;

    ctx.value.clear();


    function pushFilter(fo: FilterOptionParsed, indent: number) {
      if (isFilterGroup(fo)) {
        ctx.value.push(newMListElement(QueryFilterEmbedded.New({
          isGroup: true,
          indentation: indent,
          groupOperation: fo.groupOperation,
          token: fo.token && QueryTokenEmbedded.New({
            token: fo.token,
            tokenString: fo.token.fullKey
          }),
          valueString: fo.value,
          pinned: !fo.pinned ? undefined : toPinnedQueryFilterEmbedded(fo.pinned),
          dashboardBehaviour: fo.dashboardBehaviour,
        })));

        fo.filters.forEach(f => pushFilter(f, indent + 1));
      } else {

        if (Array.isArray(fo.value) && fo.token) {
          fo.value = fo.value.map(v => v == null || v == "" ? "" :
            fo.token!.filterType == "Embedded" || fo.token!.filterType == "Lite" ? liteKeyLong(v) :
              toStringValue(v, fo.token!.filterType))
            .join("|");
        }

        ctx.value.push(newMListElement(QueryFilterEmbedded.New({
          token: fo.token && QueryTokenEmbedded.New({
            tokenString: fo.token.fullKey,
            token: fo.token,
          }),
          operation: fo.operation,
          valueString: fo.value,
          indentation: indent,
          pinned: !fo.pinned ? undefined : toPinnedQueryFilterEmbedded(fo.pinned),
          dashboardBehaviour: fo.dashboardBehaviour,
        })));
      }

      function toPinnedQueryFilterEmbedded(p: PinnedFilter): PinnedQueryFilterEmbedded {
        return PinnedQueryFilterEmbedded.New({
          label: typeof p.label == "function" ? p.label() : p.label,
          column: p.column,
          colSpan: p.colSpan,
          row: p.row,
          active: p.active,
          splitValue: p.splitValue,
        });
      }
    }

    filterOptions!.forEach(fo => pushFilter(fo, 0))

    ctx.binding.setValue(ctx.value); //force change

    if (p.onChanged)
      p.onChanged();

    forceUpdate();
  }

  function handleRenderValue(fc: RenderValueContext) {
    if (isFilterGroup(fc.filter)) {

      const f = fc.filter;

      const readOnly = fc.readonly || f.frozen;

      const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "xs" }, undefined as any, Binding.create(f, a => a.value));

      if (f.filters.some(a => !a.token))
        return <AutoLineOrExpression ctx={ctx} onChange={fc.handleValueChange} filterType={"String"} type={{ name: "string" }} />

      if (f.filters.map(a => getFilterGroupUnifiedFilterType(a.token!.type) ?? "").distinctBy().onlyOrNull() == null && ctx.value)
        ctx.value = undefined;

      var tr = f.filters.map(a => a.token!.type).distinctBy(a => a.name).onlyOrNull();
      var format = (tr && f.filters.map((a, i) => a.token!.format ?? "").distinctBy().onlyOrNull() || null) ?? undefined;
      var unit = (tr && f.filters.map((a, i) => a.token!.unit ?? "").distinctBy().onlyOrNull() || null) ?? undefined;
      const ft = tr && getFilterType(tr);

      return <AutoLineOrExpression ctx={ctx} onChange={fc.handleValueChange} filterType={ft ?? "String"} type={tr != null ? tr : { name: "string" }} format={format} unit={unit} />

    } else {

      const f = fc.filter

      const readOnly = fc.readonly || f.frozen;

      const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "xs" }, undefined as any, Binding.create(f, a => a.value));

      if (isList(f.operation!))
        return <MultiLineOrExpression ctx={ctx} onRenderItem={(ctx, onChange) => handleCreateAppropiateControl(ctx, fc, onChange, true)} onChange={fc.handleValueChange} />;

      return handleCreateAppropiateControl(ctx, fc, fc.handleValueChange, false);
    }
  }

  function handleCreateAppropiateControl(ctx: TypeContext<any>, fc: RenderValueContext, onChange: () => void, mandatory: boolean): React.ReactElement<any> {
    const token = fc.filter.token!;

    switch (token.filterType) {
      case "Lite":
      case "Embedded":
      case "Model":
        return <EntityLineOrExpression ctx={ctx} onChange={() => { onChange(); fc.handleValueChange(); }} filterType={token.filterType} type={token.type} mandatory={mandatory} />;
      default:
        return <AutoLineOrExpression ctx={ctx} onChange={() => { onChange(); fc.handleValueChange(); }} filterType={token.filterType} type={token.type} mandatory={mandatory} />;

    }
  }
  return (
    <div>
      {
        qd != null &&
        <FilterBuilder
          title={p.ctx.niceName()}
          avoidFieldSet={p.avoidFieldSet}
          queryDescription={qd}
          filterOptions={filterOptions ?? []}
          subTokensOptions={p.subTokenOptions}
          readOnly={p.ctx.readOnly}
          onFiltersChanged={handleFiltersChanged}
          showPinnedFiltersOptions={p.showPinnedFilterOptions}
          showDashboardBehaviour={p.showDashboardBehaviour}
          showPinnedFiltersOptionsButton={false}
          renderValue={handleRenderValue}
          avoidPreview />
      }
    </div>
  );
}

export namespace FilterBuilderEmbedded {
  export async function toFilterOptionParsed(qd: QueryDescription, allFilters: MList<QueryFilterEmbedded>, subTokenOptions: SubTokensOptions): Promise<FilterOptionParsed[]> {
    const completer = new Finder.TokenCompleter(qd);

    allFilters.forEach(mle => {
      if (mle.element.token && mle.element.token.tokenString)
        completer.request(mle.element.token.tokenString);
    });

    await completer.finished();

    function toFilterList(filters: QueryFilterEmbedded[], indent: number): FilterOptionParsed[] {
      return filters.groupWhen(f => f.indentation == indent).map(gr => {
        if (!gr.key.isGroup) {
          if (gr.elements.length != 0)
            throw new Error("Unexpected childrens of condition");

          const pinned = gr.key.pinned;

          const filterCondition: FilterConditionOptionParsed = {
            token: completer.get(gr.key.token!.tokenString, subTokenOptions),
            operation: gr.key.operation ?? "EqualTo",
            value: gr.key.valueString,
            frozen: false,
            pinned: !pinned ? undefined : toPinnedFilterParsed(pinned),
            dashboardBehaviour: gr.key.dashboardBehaviour ?? undefined,
          };

          return filterCondition;
        }
        else {

          const pinned = gr.key.pinned;

          const filterGroup: FilterGroupOptionParsed = {
            token: gr.key.token ? completer.get(gr.key.token.tokenString, subTokenOptions) : undefined,
            groupOperation: gr.key.groupOperation!,
            filters: toFilterList(gr.elements, indent + 1),
            value: gr.key.valueString ?? undefined,
            frozen: false,
            pinned: !pinned ? undefined : toPinnedFilterParsed(pinned),
            dashboardBehaviour: gr.key.dashboardBehaviour ?? undefined,
          };

          return filterGroup;
        }
      });

      function toPinnedFilterParsed(pinned: PinnedQueryFilterEmbedded): PinnedFilterParsed {
        return {
          label: pinned.label || undefined,
          column: pinned.column ?? undefined,
          colSpan: pinned.colSpan ?? undefined,
          row: pinned.row ?? undefined,
          active: pinned.active || undefined,
          splitValue: pinned.splitValue || undefined,
        };
      }
    }

    return toFilterList(allFilters.map(a => a.element), 0);
  }
}

export default FilterBuilderEmbedded;

interface MultiLineOrExpressionProps {
  ctx: TypeContext<string | null | undefined>;
  onChange: () => void;
  onRenderItem: (ctx: TypeContext<any>, onChange: () => void) => React.ReactElement<any>;
}

export function MultiLineOrExpression(p: MultiLineOrExpressionProps): React.JSX.Element {

  const [values, setValues] = React.useState<string[]>([]);

  React.useEffect(() => {
    setValues((p.ctx.value ?? "").split("|").filter(a => Boolean(a)));
  }, [p.ctx.value]);

  const handleChangeValue = () => {
    p.ctx.value = values.join("|");
    if (p.onChange)
      p.onChange();
  }

  return <MultiValue values={values} onChange={handleChangeValue} readOnly={p.ctx.readOnly} onRenderItem={ctx => p.onRenderItem(ctx, handleChangeValue)} />;
}

interface EntityLineOrExpressionProps {
  ctx: TypeContext<string | null>;
  onChange: () => void;
  type: TypeReference;
  filterType: FilterType;
  mandatory?: boolean;
}

export function EntityLineOrExpression(p: EntityLineOrExpressionProps): React.JSX.Element {

  const forceUpdate = useForceUpdate();

  const liteRef = React.useRef<Lite<Entity> | null | undefined>(undefined);

  React.useEffect(() => {
    var lite = p.ctx.value == null ? null :
      p.ctx.value.contains(";") ? parseLite(p.ctx.value) :
        undefined;

    liteRef.current = lite;

    if (lite != null) {
      Navigator.API.fillLiteModels(lite)
        .then(() => forceUpdate());
    }
  }, [p.ctx.value]);

  function getSwitchModelButton(isValue: boolean): React.ReactElement<any> {
    return (<a href="#" role="button" className={classes("sf-line-button", "sf-remove", "btn input-group-text", p.ctx.readOnly  && "disabled")}
      onClick={e => { e.preventDefault(); liteRef.current = isValue ? undefined : null; forceUpdate() }}
      title={isValue ? UserAssetQueryMessage.SwitchToExpression.niceToString() : UserAssetQueryMessage.SwitchToValue.niceToString()}>
      <FontAwesomeIcon aria-hidden={true} icon={[isValue ? "far" : "fas", "pen-to-square"]} />
    </a>)
  }

  if (liteRef.current === undefined)
    return <TextBoxLine ctx={p.ctx} type={{ name: "string" }} onChange={p.onChange} extraButtons={() => getSwitchModelButton(false)} mandatory={p.mandatory} />;

  const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: p.ctx.readOnly, formSize: "xs" }, undefined as any, Binding.create(liteRef, a => a.current));

  const handleChangeValue = () => {
    p.ctx.value = ctx.value ? liteKeyLong(ctx.value) : null;
    if (p.onChange)
      p.onChange();
  }

  const type = p.type;

  if (p.filterType == "Lite") {
    if (type.name == IsByAll || getTypeInfos(type).some(ti => !ti.isLowPopulation))
      return <EntityLine ctx={ctx} type={type} create={false} onChange={handleChangeValue} extraButtons={() => getSwitchModelButton(true)} mandatory={p.mandatory} />;
    else
      return <EntityCombo ctx={ctx} type={type} create={false} onChange={handleChangeValue} extraButtons={() => getSwitchModelButton(true)} mandatory={p.mandatory} />;
  }
  else if (p.filterType == "Embedded" || p.filterType == "Model") {
    return <EntityLine ctx={ctx} type={type} create={false} autocomplete={null} onChange={handleChangeValue} extraButtons={() => getSwitchModelButton(true)} mandatory={p.mandatory} />;
  }
  else
    throw new Error("Unexpected Filter Type");
}


interface ValueLineOrExpressionProps {
  ctx: TypeContext<string | null>;
  onChange: () => void;
  type: TypeReference;
  format?: string;
  unit?: string;
  filterType?: FilterType;
  mandatory?: boolean;
}

export function AutoLineOrExpression(p: ValueLineOrExpressionProps): React.JSX.Element {

  const foceUpdate = useForceUpdate();
  const valueRef = React.useRef<string | number | boolean | null | undefined>(undefined);

  React.useEffect(() => {
    valueRef.current = parseValue(p.ctx.value, p.filterType)
  }, [p.ctx.value, p.filterType]);


  function getSwitchModelButton(isValue: boolean) : React.ReactElement<any> {
    return (
      <a href="#" role="button" className={classes("sf-line-button", "sf-remove", "btn input-group-text")}
        onClick={e => {
          e.preventDefault();
          if (p.filterType == "DateTime")
            p.ctx.value = "yyyy/mm/dd hh:mm:ss";

          if (p.filterType == "Lite")
            p.ctx.value = "[CurrentEntity]";

          valueRef.current = isValue ? undefined : null;
          foceUpdate();
        }}
        title={isValue ? UserAssetQueryMessage.SwitchToExpression.niceToString() : UserAssetQueryMessage.SwitchToValue.niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon={[isValue ? "far" : "fas", "pen-to-square"]} />
      </a>
    );
  }
  if (valueRef.current === undefined)
    return <TextBoxLine ctx={p.ctx} type={{ name: "string" }} onChange={p.onChange} extraButtons={() => getSwitchModelButton(false)} mandatory={p.mandatory} />;

  const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: p.ctx.readOnly, formSize: "xs" }, undefined as any, Binding.create(valueRef, a => a.current));

  const handleChangeValue = () => {
    p.ctx.value = toStringValue(ctx.value, p.filterType);
    if (p.onChange)
      p.onChange();
  }

  const type = p.type;

  if (p.filterType == "Enum") {
    const ti = tryGetTypeInfos(type).single();
    if (!ti)
      throw new Error(`EnumType ${type.name} not found`);
    const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
    return <EnumLine ctx={ctx} type={type} unit={p.unit} onChange={handleChangeValue} extraButtons={() => getSwitchModelButton(true)} optionItems={members} mandatory={p.mandatory} />;
  }else {
    return <AutoLine ctx={ctx} type={type} unit={p.unit} onChange={handleChangeValue} extraButtons={() => getSwitchModelButton(true)} mandatory={p.mandatory} />;
  }
}

const serverDateTimeFormat = "yyyy/MM/dd HH:mm:ss";
const serverTimeFormat = "HH:mm:ss.u";

function parseValue(str: string | null | undefined, filterType: FilterType | undefined): string | number | boolean | null | undefined {
  return str == null ? null :
    filterType == "Integer" ? parseInt(str) :
      filterType == "Decimal" ? parseFloat(str) :
        filterType == "Boolean" ? (str == "True" ? true : str == "False" ? false : undefined) :
          filterType == "DateTime" ? parseDate(str) :
            filterType == "Time" ? parseTime(str) :
              filterType == "Enum" || filterType == "Guid" || filterType == "String" ? str :
                undefined;
}

function parseDate(str: string) {
  const parsed = DateTime.fromFormat(str, serverDateTimeFormat).toISO()!;

  return parsed ?? undefined;
}

function parseTime(str: string) {
  const parsed = DateTime.fromFormat(str, serverTimeFormat).toFormat(serverTimeFormat);

  return parsed ?? undefined;
}

function toStringValue(value: string | number | boolean | null | undefined, filterType: FilterType | undefined): string | null {
  return value == null ? null :
    filterType == "Integer" ? value.toString() :
      filterType == "Decimal" ? value.toString() :
        filterType == "Boolean" ? (value ? "True" : "False") :
          filterType == "DateTime" ? DateTime.fromISO(value as string).toFormat(serverDateTimeFormat) :
            filterType == "Time" ? DateTime.fromFormat(value as string, serverTimeFormat).toFormat(serverTimeFormat) :
              filterType == "Enum" || filterType == "Guid" || filterType == "String" ? value as string :
                null;
}
