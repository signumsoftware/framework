
import * as React from 'react'
import { classes } from '../Globals'
import { Finder } from '../Finder'
import { FindOptions, ResultRow } from '../FindOptions'
import { mlistItemContext, TypeContext } from '../TypeContext'
import { MListElementBinding, ReadonlyBinding, TypeReference } from '../Reflection'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey, getToString, MListElement } from '../Signum.Entities'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'
import { useController } from './LineBase'
import { normalizeEmptyArray } from './EntityCombo'
import { ResultTable } from '../Search'
import { Navigator } from '../Navigator'
import { getTimeMachineCheckboxIcon, getTimeMachineIcon } from './TimeMachineIcon'
import { EntityOperations } from '../Operations/EntityOperations'
import { GroupHeader, HeaderType } from './GroupHeader'
import { Aprox } from './EntityBase'



export interface RenderCheckboxItemContext<V extends ModifiableEntity | Lite<Entity>> {
  row: ResultRow;
  index: number;
  checked: boolean;
  controller: EntityCheckboxListController<V>;
  resultTable?: ResultTable;
  ectx: TypeContext<V> | null;
  oldCtx: TypeContext<V> | null;
}

export interface EntityCheckboxListProps<V extends ModifiableEntity | Lite<Entity>> extends EntityListBaseProps<V> {
  data?: Lite<Entity>[];
  columnCount?: number | null;
  columnWidth?: number | null;
  elementsHtmlAttributes?: React.HTMLAttributes<any>;
  avoidFieldSet?: boolean | HeaderType;
  deps?: React.DependencyList;
  onRenderCheckbox?: (ric: RenderCheckboxItemContext<V>) => React.ReactElement;
  onRenderItem?: (ric: RenderCheckboxItemContext<V>) => React.ReactElement;

  getLiteFromElement?: (e: V) => Entity | Lite<Entity>;
  createElementFromLite?: (file: Lite<Entity>) => Promise<V>;

  groupElementsBy?: (e: ResultRow) => string;
  renderGroupTitle?: (key: string, i?: number) => React.ReactElement;

  ref?: React.Ref<EntityCheckboxListController<V>>;
}

export class EntityCheckboxListController<V extends Lite<Entity> | ModifiableEntity> extends EntityListBaseController<EntityCheckboxListProps<V>, V> {

  refresh: number = 0;

  override getDefaultProps(state: EntityCheckboxListProps<V>): void {
    super.getDefaultProps(state);

    if (state.ctx.value == null)
      state.ctx.value = [];

    state.remove = false;
    state.create = false;
    state.view = false;
    state.find = false;
    state.columnWidth = 200;
  }

  getKeyEntity(element: V): Lite<Entity> | Entity {
    if (this.props.getLiteFromElement)
      return this.props.getLiteFromElement(element);
    else
      return element as Lite<Entity> | Entity;
  }

  createElementContext<T>(embedded: T): TypeContext<T> {
    var pr = this.props.ctx.propertyRoute!.addMember("Indexer", "", true);
    return new TypeContext(this.props.ctx, undefined, pr, new ReadonlyBinding(embedded, ""));
  }

  handleOnChange = async (event: React.SyntheticEvent, lite: Lite<Entity>): Promise<void> => {
    const ctx = this.props.ctx!;
    const toRemove = this.getMListItemContext(ctx).filter(ctxe => is(this.getKeyEntity(ctxe.value), lite))

    if (toRemove.length) {
      toRemove.forEach(ctxe => ctx.value.remove((ctxe.binding as MListElementBinding<V>).getMListElement()));
      this.setValue(ctx.value, event);
    }
    else {
      var elem = this.props.createElementFromLite ? await this.props.createElementFromLite(lite) :
        await this.convert(lite as Aprox<V>);

      this.addElement(elem);
    }
  }

  override handleCreateClick = async (event: React.SyntheticEvent<any>): Promise<undefined> => {

    event.preventDefault();
    var pr = this.props.ctx.propertyRoute!.addMember("Indexer", "", true);

    if (this.props.getLiteFromElement)
      pr = pr.addLambda(this.props.getLiteFromElement);

    let e = this.props.onCreate ? await this.props.onCreate(pr) : await this.defaultCreate(pr);

    if (e == undefined)
      return undefined;

    if (this.props.viewOnCreate) {
      e = await this.doView(await this.convert(e));
    }

    if (e) {
      this.refresh++;
      this.forceUpdate();
    }
  }
}

export function EntityCheckboxList<V extends ModifiableEntity | Lite<Entity>>(props: EntityCheckboxListProps<V>): React.JSX.Element | null {
  const c = useController < EntityCheckboxListController<V>, EntityCheckboxListProps<V>, MList<V>>(EntityCheckboxListController, props);
  const p = c.props;

  if (c.isHidden)
    return null;

    return (
      <GroupHeader className={classes("sf-checkbox-list", c.getErrorClass("border"))}
      label={p.label}
      labelIcon={p.labelIcon}
      avoidFieldSet={p.avoidFieldSet}
        buttons={renderButtons()}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...c.errorAttributes() }} >
      {renderCheckboxList()}
    </GroupHeader >
  );

  function renderButtons() {
    return (
      <span>
        {p.extraButtonsBefore?.(c)}
        {c.renderCreateButton(false)}
        {c.renderFindButton(false)}
        {p.extraButtons?.(c)}
      </span>
    );
  }

  function renderCheckboxList() {
    return (
      <EntityCheckboxListSelect ctx={p.ctx} controller={c}  />
    );
  }
}


interface EntityCheckboxListSelectProps<V extends ModifiableEntity | Lite<Entity>> {
  ctx: TypeContext<MList<V>>;
  controller: EntityCheckboxListController<V>;
}

export function EntityCheckboxListSelect<V extends ModifiableEntity | Lite<Entity>>(props: EntityCheckboxListSelectProps<V>): React.ReactElement {

  const c = props.controller;
  const p = c.props;

  var [data, setData] = React.useState<Lite<Entity>[] | ResultTable | undefined>(p.data);
  var requestStarted = React.useRef(false);

  React.useEffect(() => {
    if (p.data) {
      if (requestStarted.current)
        console.warn(`The 'data' was set too late. Consider using [] as default value to avoid automatic query. EntityCheckboxList: ${p.type!.name}`);
      setData(p.data);
    } else {
      requestStarted.current = true;
      const fo = p.findOptions;
      if (fo) {
        Finder.getResultTable(Finder.defaultNoColumnsAllRows(fo, undefined))
          .then(data => setData(data));
      }
      else {

        var type = p.getLiteFromElement ? p.ctx.propertyRoute!.addMember("Indexer", "", true).addLambda(p.getLiteFromElement).typeReference() : p.type;
        Finder.API.fetchAllLites({ types: type!.name })
          .then(data => setData(data.orderBy(a => getToString(a))));
      } 
    }
  }, [normalizeEmptyArray(p.data), p.type!.name, p.deps, p.findOptions && Finder.findOptionsPath(p.findOptions), c.refresh]);

  
  return (
    <div {...p.elementsHtmlAttributes}
      className={classes("sf-checkbox-elements", p.elementsHtmlAttributes?.className)}
      style={{ ...p.elementsHtmlAttributes?.style, ...getColumnStyle()}} >
      {renderContent()}
    </div>
  );

  function getColumnStyle(): React.CSSProperties | undefined {
    if (p.columnCount && p.columnWidth)
      return {
        columns: `${p.columnCount} ${p.columnWidth}px`,
      };

    if (p.columnCount)
      return {
        columnCount: p.columnCount,
      };

    if (p.columnWidth)
      return {
        columnWidth: p.columnWidth,
      };

    return undefined;
  }

  function maybeToLite(entityOrLite: Entity | Lite<Entity>): Lite<Entity> {
    const entity = entityOrLite as Entity;

    if (entity.Type)
      return toLite(entity, entity.isNew);

    return entityOrLite as Lite<Entity>;
  }

  function renderContent() {
    if (data == undefined)
      return undefined;

    const fixedData = Array.isArray(data) ? data.map(lite => ({ entity: lite } as ResultRow)) :
      typeof data == "object" ? data.rows :
        [];

    var listCtx = mlistItemContext(p.ctx);

    if (p.filterRows)
      listCtx = p.filterRows(listCtx);

    listCtx.forEach(ctx => {
      var lite = c.getKeyEntity(ctx.value);
      if (!fixedData.some(d => is(d.entity, lite)))
        fixedData.insertAt(0, { entity: maybeToLite(lite) } as ResultRow)
    });

    const resultTable = Array.isArray(data) ? undefined : data;

    function renderRow(row: ResultRow, i: number) {
      var ectx = listCtx.firstOrNull(ectx => is(c.getKeyEntity(ectx.value), row.entity));
      var oldCtx = p.ctx.previousVersion == null || p.ctx.previousVersion.value == null ? null :
        listCtx.firstOrNull(ectx => is(c.getKeyEntity(ectx.previousVersion!.value), row.entity));

      var ric: RenderCheckboxItemContext<any> = {
        row,
        index: i,
        checked: ectx != null,
        controller: c,
        resultTable: resultTable,
        ectx: ectx,
        oldCtx: oldCtx
      };

      if (p.onRenderCheckbox)
        return p.onRenderCheckbox(ric);

      return (
        <label className="sf-checkbox-element" key={i}>
          {getTimeMachineCheckboxIcon({ newCtx: ectx, oldCtx: oldCtx })}
          <input type="checkbox"
            className="form-check-input"
            checked={ectx != null}
            disabled={p.ctx.readOnly}
            name={liteKey(row.entity!)}
            onChange={e => c.handleOnChange(e, row.entity!)} />
          &nbsp;
          {p.onRenderItem ? p.onRenderItem(ric) : <span>{Navigator.renderLite(row.entity!)}</span>}
        </label>
      );
    }

    return p.groupElementsBy == undefined ? fixedData.map((row, i) => renderRow(row, i)) :
      <>
        {fixedData.groupBy(a => p.groupElementsBy!(a)).map((gr, i) => <div className={classes("mb-2")} key={i} >
          <small className="text-muted">{p.renderGroupTitle != undefined ? p.renderGroupTitle(gr.key, i) : gr.key}</small>
          {gr.elements.map((mle, j) => renderRow(mle, j)) }          
        </div>)}
      </>;
  }
}
