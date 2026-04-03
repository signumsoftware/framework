import * as React from 'react'
import { ModifiableEntity, Lite, Entity, toLite, is, liteKey, getToString, isEntity } from '../Signum.Entities'
import { Finder } from '../Finder'
import { FindOptions, ResultRow } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { getTypeInfos, tryGetTypeInfos, TypeReference } from '../Reflection'
import { EntityBaseController, EntityBaseProps, AsLite, AsEntity, Aprox } from './EntityBase'
import { FormGroup } from './FormGroup'
import { Navigator } from '../Navigator'
import { FormControlReadonly } from './FormControlReadonly'
import { classes } from '../Globals';
import { genericMemo, useController } from './LineBase'
import { useMounted } from '../Hooks'
import { DropdownList } from 'react-widgets-up'
import { ResultTable } from '../Search'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { TextHighlighter } from '../Components/Typeahead'



export interface EntityComboProps<V extends Entity | Lite<Entity> | null> extends EntityBaseProps<V> {
  data?: AsLite<V>[];
  labelTextWithData?: (data: Lite<Entity>[] | undefined | null, resultTable?: ResultTable | null) => React.ReactElement | string;
  deps?: React.DependencyList;
  initiallyFocused?: boolean;
  selectHtmlAttributes?: React.SelectHTMLAttributes<any>;
  optionHtmlAttributes?: (lite: ResultRow | undefined) => React.OptionHTMLAttributes<any> | undefined;
  onRenderItem?: (lite: ResultRow | undefined, role: "Value" | "ListItem", searchTerm?: string) => React.ReactElement | string;
  nullPlaceHolder?: string;
  delayLoadData?: boolean;
  toStringFromData?: boolean;
  overrideSelectedLite?: () => Lite<Entity> | null;
  ref?: React.Ref<EntityComboController<V>>
}



export class EntityComboController<V extends Entity | Lite<Entity> | null> extends EntityBaseController<EntityComboProps<V>, V> {

  refresh = 0;

  override getDefaultProps(p: EntityComboProps<V>): void {
    p.remove = false;
    p.create = false;
    p.view = false;
    p.viewOnCreate = true;
    p.find = false;
  }

  override overrideProps(p: EntityComboProps<V>, overridenProps: EntityComboProps<V>): void {
    super.overrideProps(p, overridenProps);
    if (p.onRenderItem === undefined && p.type && tryGetTypeInfos(p.type).some(a => a && Navigator.getSettings(a)?.renderLite)) {
      p.onRenderItem = (row, role, searchTerm) => row == null ? <span className="mx-2">-</span> : (row?.entity && Navigator.renderLite(row.entity, TextHighlighter.fromString(searchTerm))) ?? "";
    }
  }

  override async doView(entity: V): Promise<Aprox<V> | undefined> {
    var val = await super.doView(entity);

    this.refresh++;

    return val;
  }

  handleOnChange = async (e: React.SyntheticEvent | undefined, lite: AsLite<V> | null): Promise<void> => {
    if (lite == null)
      this.setValue(null!);
    else {
      var v = await this.convert(lite)
      this.setValue(v, e);
    }
  }
}

export const EntityCombo: <V extends Entity | Lite<Entity> | null>(props: EntityComboProps<V>) => React.ReactNode | null 
  = genericMemo(function EntityCombo<V extends Entity | Lite<Entity> | null>(props: EntityComboProps<V>) {

    const c = useController<EntityComboController<V>, EntityComboProps<V>, V>(EntityComboController, props);
    const p = c.props;
    const hasValue = !!c.props.ctx.value;
    const comboRef = React.useRef<EntityComboSelectHandle>(null);

    React.useEffect(() => {
      if (p.initiallyFocused)
        window.setTimeout(() => {
          let select = comboRef.current && comboRef.current.getSelect();
          if (select) {
            select.focus();
          }
        }, 0);
    }, []);

    if (c.isHidden)
      return null;

    const buttons = (
      <>
        {c.props.extraButtonsBefore && c.props.extraButtonsBefore(c)}
        {!hasValue && c.renderCreateButton(true)}
        {!hasValue && c.renderFindButton(true)}
        {hasValue && c.renderViewButton(true)}
        {hasValue && c.renderRemoveButton(true)}
        {c.props.extraButtons && c.props.extraButtons(c)}
      </>
    );

    function getLabelText() {

      if (p.labelTextWithData == null)
        return p.label;

      var data = c.props.data || comboRef.current && comboRef.current.getData();

      return p.labelTextWithData(data == null ? null : Array.isArray(data) ? data : data.rows.map(a => a.entity!), data && (Array.isArray(data) ? undefined : data));
    }

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

    return (
      <FormGroup ctx={c.props.ctx} error={p.error} label={getLabelText()} labelIcon={p.labelIcon}
        helpText={helpText}
        helpTextOnTop={helpTextOnTop}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes }}
        labelHtmlAttributes={p.labelHtmlAttributes}>
        {inputId => <div className="sf-entity-combo">
          <div className={EntityBaseController.hasChildrens(buttons) ? p.ctx.inputGroupClass : undefined}>
            {getTimeMachineIcon({ ctx: p.ctx })}
            <EntityComboSelect<V>
              id={inputId}
              ref={comboRef}
              ctx={p.ctx}
              onChange={c.handleOnChange}
              type={p.type!}
              data={p.data}
              findOptions={p.findOptions}
              findOptionsDictionary={p.findOptionsDictionary}
              onDataLoaded={p.labelTextWithData == null ? undefined : () => c.forceUpdate()}
              mandatoryClass={c.mandatoryClass}
              deps={p.deps ? [c.refresh, ...p.deps] : [c.refresh]}
              delayLoadData={p.delayLoadData}
              toStringFromData={p.toStringFromData}
              selectHtmlAttributes={p.selectHtmlAttributes}
              optionHtmlAttributes={p.optionHtmlAttributes}
              liteToString={p.liteToString as (e: Entity) => string}
              nullPlaceHolder={p.nullPlaceHolder}
              onRenderItem={p.onRenderItem}
              overrideSelectedLite={p.overrideSelectedLite}
            />
            {EntityBaseController.hasChildrens(buttons) ? buttons : undefined}
          </div>
        </div>}
      </FormGroup>
    );
  }, (prev, next): boolean => EntityBaseController.propEquals(prev, next));

export interface EntityComboSelectProps<V extends ModifiableEntity | Lite<Entity> | null> {
  ctx: TypeContext<V>;
  onChange: (e: React.SyntheticEvent | undefined, lite: AsLite<V> | null) => void;
  type: TypeReference;
  findOptions?: FindOptions;
  findOptionsDictionary?: { [typeName: string]: FindOptions };
  data?: AsLite<V>[];
  mandatoryClass: string | null;
  onDataLoaded?: (data: AsLite<V>[] | ResultTable | undefined) => void;
  deps?: React.DependencyList;
  selectHtmlAttributes?: React.SelectHTMLAttributes<any>;
  optionHtmlAttributes?: (lite: ResultRow | undefined) => React.OptionHTMLAttributes<any> | undefined;
  onRenderItem?: (lite: ResultRow | undefined, role: "Value" | "ListItem", searchTerm?: string) => React.ReactNode;
  liteToString?: (e: Entity) => string;
  nullPlaceHolder?: string;
  delayLoadData?: boolean;
  toStringFromData?: boolean;
  overrideSelectedLite?: () => Lite<Entity> | null;
  id: string;
  ref?: React.Ref<EntityComboSelectHandle>
}


const __normalized: Lite<Entity>[] = [];
export function normalizeEmptyArray(data: Lite<Entity>[] | undefined): Lite<Entity>[] | undefined {
  if (data == undefined)
    return undefined;

  if (data.length == 0)
    return __normalized;

  return data;
}

export interface EntityComboSelectHandle {
  getSelect(): HTMLSelectElement | null;
  getData(): Lite<Entity>[] | ResultTable | undefined;
}
//Extracted to another component
export function EntityComboSelect<V extends Entity | Lite<Entity> | null>(p: EntityComboSelectProps<V>): React.JSX.Element {

  const [data, _setData] = React.useState<Lite<Entity>[] | ResultTable | undefined>(p.data);
  const requestStarted = React.useRef(false);

  const [loadData, setLoadData] = React.useState<boolean>(!p.delayLoadData);

  const selectRef = React.useRef<HTMLSelectElement>(null);
  const mounted = useMounted();

  React.useImperativeHandle(p.ref, () => ({
    getData: () => data,
    getSelect: () => selectRef.current
  }));

  function setData(data: AsLite<V>[] | ResultTable) {
    if (mounted.current) {
      _setData(data);
      if (p.onDataLoaded)
        p.onDataLoaded(data);
    }
  }

  React.useEffect(() => {
    if (p.data) {
      if (requestStarted.current)
        console.warn(`The 'data' was set too late. Consider using [] as default value to avoid automatic query. EntityCombo: ${p.type!.name}`);
      setData(p.data);
    } else if (!p.ctx.readOnly && loadData) {
      requestStarted.current = true;

      if (p.type.name.contains(",") && !p.findOptions) {
        Promise.all(getTypeInfos(p.type.name).map(t => {
          var fo = p.findOptionsDictionary?.[t.name] ?? { queryName: t!.name };
          return Finder.getResultTable(Finder.defaultNoColumnsAllRows(fo, undefined))
        })).then(array => setData(array.flatMap(a => a.rows.map(a => a.entity! as AsLite<V>))));
      } else {
        const fo = p.findOptions ?? { queryName: p.type!.name };;
        Finder.getResultTable(Finder.defaultNoColumnsAllRows(fo, undefined))
          .then(data => setData(data));
      }
    }
  }, [normalizeEmptyArray(p.data), p.type.name, loadData, p.ctx.readOnly, p.findOptions && Finder.findOptionsPath(p.findOptions), ...(p.deps ?? [])]);

  const lite = getLite();

  const ctx = p.ctx;

  if (ctx.readOnly)
    return (
      <FormControlReadonly id={p.id} ctx={ctx} htmlAttributes={p.selectHtmlAttributes}>
        {ctx.value &&
          (p.onRenderItem ? p.onRenderItem({ entity: lite } as ResultRow, "Value", undefined) :
            p.liteToString ? getToString(lite!, p.liteToString) :
              Navigator.renderLite((p.toStringFromData ? p.data?.singleOrNull(a => is(a, lite)) : null) ?? lite!))
        }
      </FormControlReadonly>
    );

  if (p.onRenderItem) {
    return (
      <DropdownList
        className={classes(ctx.formControlClass, p.mandatoryClass)} data={getOptionRows()}
        onChange={(row, e) => p.onChange(e.originalEvent, row?.entity as AsLite<V> ?? null)}
        value={getResultRow(lite)}
        title={getToString(lite)}
        filter={(e, query) => {
          var toStr = getToString((e as ResultRow).entity).toLowerCase();
          return query.toLowerCase().split(' ').every(part => toStr.contains(part));
        }}
        renderValue={a => p.onRenderItem!(a.item?.entity == null ? undefined : a.item, "Value")}
        renderListItem={a => p.onRenderItem!(a.item?.entity == null ? undefined : a.item, "ListItem", a.searchTerm)}
      />
    );
  } else {
    return (
      <select {...p.selectHtmlAttributes}
        className={classes(ctx.formSelectClass, p.mandatoryClass, p.selectHtmlAttributes?.className)}
        onChange={handleOnChange} value={lite ? liteKey(lite) : ""}
        title={getToString(lite)}
        id={p.id}
        onClick={() => setLoadData(true)}
        disabled={ctx.readOnly} ref={selectRef} >
        {getOptionRows().map((r, i) =>
          <option key={i} value={r?.entity ? liteKey(r.entity!) : ""} {...p.optionHtmlAttributes?.(r)}>{r?.entity ? getToString(r.entity, p.liteToString) : (p.nullPlaceHolder ?? " - ")}</option>)}
      </select>
    );
  }

  function handleOnChange(event: React.ChangeEvent<HTMLSelectElement>) {
    const current = event.currentTarget as HTMLSelectElement;

    const lite = getLite();

    if (current.value != (lite ? liteKey(lite) : undefined)) {
      if (!current.value) {
        p.onChange(event, null);
      } else {
        const liteFromData = Array.isArray(data) ? data!.single(a => liteKey(a) == current.value) :
          data?.rows.single(a => liteKey(a.entity!) == current.value).entity!;
        p.onChange(event, liteFromData as AsLite<V>);
      }
    }
  }

  function getResultRow(lite: Lite<Entity> | undefined): ResultRow {

    if (lite == null)
      return ({ entity: undefined }) as ResultRow;

    if (Array.isArray(data))
      return ({ entity: lite }) as ResultRow;

    if (typeof data == "object")
      return data.rows.singleOrNull(a => is(lite, a.entity)) ?? ({ entity: lite }) as ResultRow;

    return ({ entity: lite }) as ResultRow;
  }

  function getLite() {
    const v = p.ctx.value;
    if (v == undefined) {
      if (p.overrideSelectedLite) {
        return (p.overrideSelectedLite() ?? undefined);
      }
      return undefined;
    }

    if (isEntity(v))
      return toLite(v, v.isNew, p.liteToString && p.liteToString(v));

    return v as Lite<Entity>;
  }

  function getOptionRows(): ResultRow[] {

    const lite = getLite();

    var rows = Array.isArray(data) ? data.map(lite => ({ entity: lite } as ResultRow)) :
      typeof data == "object" ? data.rows :
        [];

    const elements: ResultRow[] = [{ entity: undefined } as ResultRow/*Because DropDownList*/, ...rows];

    if (lite) {
      var index = elements.findIndex(a => is(a?.entity, lite));
      if (index == -1)
        elements.insertAt(1, { entity: lite } as ResultRow);
      else {
        if (!p.toStringFromData)
          elements[index]!.entity = lite;
      }
    }

    return elements;
  }
}





