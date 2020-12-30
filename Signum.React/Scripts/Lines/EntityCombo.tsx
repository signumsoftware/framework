import * as React from 'react'
import { ModifiableEntity, Lite, Entity, toLite, is, liteKey, getToString, isEntity } from '../Signum.Entities'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { TypeReference } from '../Reflection'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { FormGroup } from './FormGroup'
import { FormControlReadonly } from './FormControlReadonly'
import { classes } from '../Globals';
import { useController } from './LineBase'
import { useMounted } from '../Hooks'


export interface EntityComboProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
  data?: Lite<Entity>[];
  labelTextWithData?: (data: Lite<Entity>[] | undefined | null) => React.ReactChild;
  refreshKey?: string;
  initiallyFocused?: boolean;
  selectHtmlAttributes?: React.AllHTMLAttributes<any>;
  delayLoadData?: boolean;
  toStringFromData?: boolean;
}

export class EntityComboController extends EntityBaseController<EntityComboProps> {

  getDefaultProps(p: EntityComboProps) {
    p.remove = false;
    p.create = false;
    p.view = false;
    p.viewOnCreate = true;
    p.find = false;
  }


  doView(entity: ModifiableEntity | Lite<Entity>) {
    var promise = super.doView(entity) ?? Promise.resolve(undefined);

    if (this.props.refreshKey == null) {
      promise = promise.then(a => {
        this.props.refreshKey = new Date().getTime().toString();
        this.forceUpdate();
        return a;
      });
    }

    return promise;
  }

  handleOnChange = (lite: Lite<Entity> | null) => {
    if (lite == null)
      this.setValue(lite);
    else
      this.convert(lite)
        .then(v => this.setValue(v))
        .done();
  }
}

export const EntityCombo = React.memo(React.forwardRef(function EntityCombo(props: EntityComboProps, ref: React.Ref<EntityComboController>) {

  const c = useController(EntityComboController, props, ref);
  const p = c.props;
  const hasValue = !!c.props.ctx.value;
  const comboRef = React.useRef<EntityComboHandle>(null);

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
    <span className="input-group-append">
      {!hasValue && c.renderCreateButton(true)}
      {!hasValue && c.renderFindButton(true)}
      {hasValue && c.renderViewButton(true, c.props.ctx.value!)}
      {hasValue && c.renderRemoveButton(true, c.props.ctx.value!)}
      {c.props.extraButtons && c.props.extraButtons(c)}
    </span>
  );

  return (
    <FormGroup ctx={c.props.ctx}
      labelText={p.labelTextWithData == null ? p.labelText : p.labelTextWithData(c.props.data || comboRef.current && comboRef.current.getData())}
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
            refreshKey={p.refreshKey}
            delayLoadData={p.delayLoadData}
            toStringFromData={p.toStringFromData}
            selectHtmlAttributes={p.selectHtmlAttributes}
            liteToString={p.liteToString}
          />
          {EntityBaseController.hasChildrens(buttons) ? buttons : undefined}
        </div>
      </div>
    </FormGroup>
  );
}), (prev, next) => EntityBaseController.propEquals(prev, next));

export interface EntityComboSelectProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
  onChange: (lite: Lite<Entity> | null) => void;
  type: TypeReference;
  findOptions?: FindOptions;
  data?: Lite<Entity>[];
  mandatoryClass: string | null; 
  onDataLoaded?: (data: Lite<Entity>[] | undefined) => void;
  refreshKey?: string;
  selectHtmlAttributes?: React.AllHTMLAttributes<any>;
  liteToString?: (e: Entity) => string;
  delayLoadData?: boolean;
  toStringFromData?: boolean;
}


const __normalized: Lite<Entity>[] = [];
export function normalizeEmptyArray(data: Lite<Entity>[] | undefined) {
  if (data == undefined)
    return undefined;

  if (data.length == 0)
    return __normalized;

  return data;
}

export interface  EntityComboHandle {
  getSelect(): HTMLSelectElement | null;
  getData(): Lite<Entity>[] | undefined;
}
//Extracted to another component
export const EntityComboSelect = React.forwardRef(function EntityComboSelect(p: EntityComboSelectProps, ref: React.Ref<EntityComboHandle>) {

  const [data, _setData] = React.useState<Lite<Entity>[] | undefined>(p.data);
  const requestStarted = React.useRef(false);

  const [loadData, setLoadData] = React.useState<boolean>(!p.delayLoadData);

  const selectRef = React.useRef<HTMLSelectElement>(null);
  const mounted = useMounted();

  React.useImperativeHandle(ref, () => ({
    getData: () => data,
    getSelect: () => selectRef.current
  }));

  function setData(data: Lite<Entity>[]) {
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
    } else if (loadData){
      requestStarted.current = true;
      const fo = p.findOptions;
      if (fo) {
        Finder.expandParentColumn(fo);
        var limit = fo?.pagination?.elementsPerPage ?? 999;
        Finder.fetchEntitiesWithFilters(fo.queryName, fo.filterOptions ?? [], fo.orderOptions ?? [], limit)
          .then(data => setData(fo.orderOptions && fo.orderOptions.length ? data : data.orderBy(a => a.toStr)))
          .done();
      }
      else
        Finder.API.fetchAllLites({ types: p.type!.name })
          .then(data => setData(data.orderBy(a => a)))
          .done();
    }
  }, [normalizeEmptyArray(p.data), p.type.name, p.refreshKey, loadData, p.findOptions && Finder.findOptionsPath(p.findOptions)]);

  const lite = getLite();

  const ctx = p.ctx;

  if (ctx.readOnly)
    return <FormControlReadonly ctx={ctx} htmlAttributes={p.selectHtmlAttributes}>{ctx.value && getToString(lite, p.liteToString)}</FormControlReadonly>;

  return (
    <select className={classes(ctx.formControlClass, p.mandatoryClass)} onChange={handleOnChange} value={lite ? liteKey(lite) : ""} onClick={() => setLoadData(true)}
      disabled={ctx.readOnly} {...p.selectHtmlAttributes} ref={selectRef} >
      {renderOptions()}
    </select>
  );


  function handleOnChange(event: React.ChangeEvent<HTMLSelectElement>) {
    const current = event.currentTarget as HTMLSelectElement;

    const lite = getLite();

    if (current.value != (lite ? liteKey(lite) : undefined)) {
      if (!current.value) {
        p.onChange(null);
      } else {
        const liteFromData = data!.filter(a => liteKey(a) == current.value).single();
        p.onChange(liteFromData);
      }
    }
  }

  function getLite() {
    const v = p.ctx.value;
    if (v == undefined)
      return undefined;

    if (isEntity(v))
      return toLite(v, v.isNew, p.liteToString && p.liteToString(v));

    return v as Lite<Entity>;
  }

  function renderOptions() {

    const lite = getLite();

    const elements = [undefined, ...data ?? []];

    if (lite) {
      var index = elements.findIndex(a => is(a, lite));
      if (index == -1)
        elements.insertAt(1, lite);
      else {
        if (!p.toStringFromData)
          elements[index] = lite;
      }
    }

    return (
      elements.map((e, i) => <option key={i} value={e ? liteKey(e) : ""}>{e ? getToString(e, p.liteToString) : " - "}</option>)
    );
  }
});





