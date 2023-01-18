import * as React from 'react'
import { ModifiableEntity, Lite, Entity, toLite, is, liteKey, getToString, isEntity } from '../Signum.Entities'
import * as Finder from '../Finder'
import { FindOptions, ResultRow } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { getTypeInfos, tryGetTypeInfos, TypeReference } from '../Reflection'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { FormGroup } from './FormGroup'
import * as Navigator from '../Navigator'
import { FormControlReadonly } from './FormControlReadonly'
import { classes } from '../Globals';
import { useController } from './LineBase'
import { useMounted } from '../Hooks'
import { DropdownList } from 'react-widgets'
import { ResultTable } from '../Search'


export interface EntityComboProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
  data?: Lite<Entity>[];
  labelTextWithData?: (data: Lite<Entity>[] | undefined | null, resultTable?: ResultTable | null) => React.ReactChild;
  deps?: React.DependencyList;
  initiallyFocused?: boolean;
  selectHtmlAttributes?: React.AllHTMLAttributes<any>;
  onRenderItem?: (lite: ResultRow | undefined, role: "Value" | "ListItem", searchTerm?: string) => React.ReactChild;
  nullPlaceHolder?: string;
  delayLoadData?: boolean;
  toStringFromData?: boolean;
  overrideSelectedLite?: () => Lite<Entity> | null;
}

export class EntityComboController extends EntityBaseController<EntityComboProps> {

  getDefaultProps(p: EntityComboProps) {
    p.remove = false;
    p.create = false;
    p.view = false;
    p.viewOnCreate = true;
    p.find = false;
  }

  overrideProps(p: EntityComboProps, overridenProps: EntityComboProps) {
    super.overrideProps(p, overridenProps);
    if (p.onRenderItem === undefined && p.type && tryGetTypeInfos(p.type).some(a => a && Navigator.getSettings(a)?.renderLite)) {
      p.onRenderItem = (row, role, searchTerm) => (row?.entity && Navigator.renderLite(row.entity, searchTerm)) ?? "";
    }
  }

  doView(entity: ModifiableEntity | Lite<Entity>) {
    var promise = super.doView(entity) ?? Promise.resolve(undefined);

    if (this.props.deps == null) {
      promise = promise.then(a => {
        this.props.deps = [new Date().getTime().toString()];
        this.forceUpdate();
        return a;
      });
    }

    return promise;
  }

  handleOnChange = (e: React.SyntheticEvent | undefined, lite: Lite<Entity> | null) => {
    if (lite == null)
      this.setValue(lite);
    else
      this.convert(lite)
        .then(v => this.setValue(v, e));
  }
}

export const EntityCombo = React.memo(React.forwardRef(function EntityCombo(props: EntityComboProps, ref: React.Ref<EntityComboController>) {

  const c = useController(EntityComboController, props, ref);
  const p = c.props;
  const hasValue = !!c.props.ctx.value;
  const comboRef = React.useRef<EntityComboSelectHandle>(null);

  React.useEffect(() => {
    if (p.initiallyFocused)
      setTimeout(() => {
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
      {hasValue && c.renderViewButton(true, c.props.ctx.value!)}
      {hasValue && c.renderRemoveButton(true, c.props.ctx.value!)}
      {c.props.extraButtonsAfter && c.props.extraButtonsAfter(c)}
    </>
  );

  function getLabelText() {

    if (p.labelTextWithData == null)
      return p.label;

    var data = c.props.data || comboRef.current && comboRef.current.getData();

    return p.labelTextWithData(data == null ? null : Array.isArray(data) ? data : data.rows.map(a => a.entity!), data && (Array.isArray(data) ? undefined : data));
  }

  return (
    <FormGroup ctx={c.props.ctx}
      label={getLabelText()}
      helpText={p.helpText}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes }}
      labelHtmlAttributes={p.labelHtmlAttributes} >
      <div className="sf-entity-combo">
        <div className={EntityBaseController.hasChildrens(buttons) ? p.ctx.inputGroupClass : undefined}>
          <EntityComboSelect
            ref={comboRef}
            ctx={p.ctx}
            onChange={c.handleOnChange}
            type={p.type!}
            data={p.data}
            findOptions={p.findOptions}
            onDataLoaded={p.labelTextWithData == null ? undefined : () => c.forceUpdate()}
            mandatoryClass={c.mandatoryClass}
            deps={p.deps}
            delayLoadData={p.delayLoadData}
            toStringFromData={p.toStringFromData}
            selectHtmlAttributes={p.selectHtmlAttributes}
            liteToString={p.liteToString}
            nullPlaceHolder={p.nullPlaceHolder}
            onRenderItem={p.onRenderItem}
            overrideSelectedLite={p.overrideSelectedLite}
          />
          {EntityBaseController.hasChildrens(buttons) ? buttons : undefined}
        </div>
      </div>
    </FormGroup>
  );
}), (prev, next) => EntityBaseController.propEquals(prev, next));

export interface EntityComboSelectProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
  onChange: (e: React.SyntheticEvent | undefined, lite: Lite<Entity> | null) => void;
  type: TypeReference;
  findOptions?: FindOptions;
  data?: Lite<Entity>[];
  mandatoryClass: string | null;
  onDataLoaded?: (data: Lite<Entity>[] | ResultTable | undefined) => void;
  deps?: React.DependencyList;
  selectHtmlAttributes?: React.AllHTMLAttributes<any>;
  onRenderItem?: (lite: ResultRow | undefined, role: "Value" | "ListItem", searchTerm?: string) => React.ReactNode;
  liteToString?: (e: Entity) => string;
  nullPlaceHolder?: string;
  delayLoadData?: boolean;
  toStringFromData?: boolean;
  overrideSelectedLite?: () => Lite<Entity> | null
}


const __normalized: Lite<Entity>[] = [];
export function normalizeEmptyArray(data: Lite<Entity>[] | undefined) {
  if (data == undefined)
    return undefined;

  if (data.length == 0)
    return __normalized;

  return data;
}

export interface  EntityComboSelectHandle {
  getSelect(): HTMLSelectElement | null;
  getData(): Lite<Entity>[] | ResultTable | undefined;
}
//Extracted to another component
export const EntityComboSelect = React.forwardRef(function EntityComboSelect(p: EntityComboSelectProps, ref: React.Ref<EntityComboSelectHandle>) {

  const [data, _setData] = React.useState<Lite<Entity>[] | ResultTable | undefined>(p.data);
  const requestStarted = React.useRef(false);

  const [loadData, setLoadData] = React.useState<boolean>(!p.delayLoadData);

  const selectRef = React.useRef<HTMLSelectElement>(null);
  const mounted = useMounted();

  React.useImperativeHandle(ref, () => ({
    getData: () => data,
    getSelect: () => selectRef.current
  }));

  function setData(data: Lite<Entity>[] | ResultTable) {
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
    } else if (loadData) {
      requestStarted.current = true;
      const fo = p.findOptions;
      if (fo) {
        Finder.getResultTable(Finder.defaultNoColumnsAllRows(fo, undefined))
          .then(data => setData(data));
      }
      else
        Finder.API.fetchAllLites({ types: p.type!.name })
          .then(data => setData(data.orderBy(a => a)));
    }
  }, [normalizeEmptyArray(p.data), p.type.name, p.deps, loadData, p.findOptions && Finder.findOptionsPath(p.findOptions)]);

  const lite = getLite();

  const ctx = p.ctx;

  if (ctx.readOnly)
    return (
      <FormControlReadonly ctx={ctx} htmlAttributes={p.selectHtmlAttributes}>
        {ctx.value &&
          (p.onRenderItem ? p.onRenderItem({ entity: lite } as ResultRow, "Value", undefined) :
          p.liteToString ? getToString(lite!, p.liteToString) :
            Navigator.renderLite(lite!))
        }
      </FormControlReadonly>
    );

  if (p.onRenderItem) {
    return (
      <DropdownList
        className={classes(ctx.formControlClass, p.mandatoryClass)} data={getOptionRows()}
        onChange={(row, e) => p.onChange(e.originalEvent, row?.entity ?? null)}
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
      <select className={classes(ctx.formSelectClass, p.mandatoryClass)} onChange={handleOnChange} value={lite ? liteKey(lite) : ""}
        title={getToString(lite)}
        onClick={() => setLoadData(true)}
        disabled={ctx.readOnly} {...p.selectHtmlAttributes} ref={selectRef} >
        {getOptionRows().map((r, i) => <option key={i} value={r?.entity ? liteKey(r.entity!) : ""}>{r?.entity ? getToString(r.entity, p.liteToString) : (p.nullPlaceHolder ?? " - ")}</option>)}
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
        p.onChange(event, liteFromData);
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
});





