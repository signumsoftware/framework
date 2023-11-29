
import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import { FindOptions, ResultRow } from '../FindOptions'
import { mlistItemContext, TypeContext } from '../TypeContext'
import { MListElementBinding, ReadonlyBinding, TypeReference } from '../Reflection'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey, getToString, MListElement } from '../Signum.Entities'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'
import { useController } from './LineBase'
import { normalizeEmptyArray } from './EntityCombo'
import { ResultTable } from '../Search'
import { renderLite } from '../Navigator'
import { getTimeMachineCheckboxIcon, getTimeMachineIcon } from './TimeMachineIcon'
import { getEntityOperationButtons } from '../Operations/EntityOperations'
import { GroupHeader, HeaderType } from './GroupHeader'



export interface RenderCheckboxItemContext<T> {
  row: ResultRow;
  index: number;
  checked: boolean;
  controller: EntityCheckboxListController;
  resultTable?: ResultTable;
  ectx: TypeContext<T> | null;
}

export interface EntityCheckboxListProps extends EntityListBaseProps {
  data?: Lite<Entity>[];
  columnCount?: number | null;
  columnWidth?: number | null;
  avoidFieldSet?: boolean | HeaderType;
  deps?: React.DependencyList;
  onRenderCheckbox?: (ric: RenderCheckboxItemContext<any>) => React.ReactElement;
  onRenderItem?: (ric: RenderCheckboxItemContext<any>) => React.ReactElement;

  getLiteFromElement?: (e: any /*ModifiableEntity*/) => Entity | Lite<Entity>;
  createElementFromLite?: (file: Lite<any>) => Promise<ModifiableEntity>;
}

export class EntityCheckboxListController extends EntityListBaseController<EntityCheckboxListProps> {

  refresh: number = 0;

  getDefaultProps(state: EntityCheckboxListProps) {
    super.getDefaultProps(state);

    if (state.ctx.value == null)
      state.ctx.value = [];

    state.remove = false;
    state.create = false;
    state.view = false;
    state.find = false;
    state.columnWidth = 200;
  }

  getKeyEntity(element: Lite<Entity> | ModifiableEntity): Lite<Entity> | Entity {
    if (this.props.getLiteFromElement)
      return this.props.getLiteFromElement(element);
    else
      return element as Lite<Entity> | Entity;
  }

  createElementContext<T>(embedded: T): TypeContext<T> {
    var pr = this.props.ctx.propertyRoute!.addMember("Indexer", "", true);
    return new TypeContext(this.props.ctx, undefined, pr, new ReadonlyBinding(embedded, ""));
  }

  handleOnChange = (event: React.SyntheticEvent, lite: Lite<Entity>) => {
    const ctx = this.props.ctx!;
    const toRemove = this.getMListItemContext(ctx).filter(ctxe => is(this.getKeyEntity(ctxe.value), lite))

    if (toRemove.length) {
      toRemove.forEach(ctxe => ctx.value.remove((ctxe.binding as MListElementBinding<any>).getMListElement()));
      this.setValue(ctx.value, event);
    }
    else {
      (this.props.createElementFromLite ? this.props.createElementFromLite(lite) : this.convert(lite)).then(e => {
        this.addElement(e);
      });
    }
  }

  handleCreateClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    var pr = this.props.ctx.propertyRoute!.addMember("Indexer", "", true);

    if (this.props.getLiteFromElement)
      pr = pr.addLambda(this.props.getLiteFromElement);

    const promise = this.props.onCreate ?
      this.props.onCreate(pr) : this.defaultCreate(pr);

    if (!promise)
      return;

    promise.then<ModifiableEntity | Lite<Entity> | undefined>(e => {

      if (e == undefined)
        return undefined;

      if (!this.props.viewOnCreate)
        return Promise.resolve(e);

      return this.doView(e);

    }).then(e => {
      if (e) {
        this.refresh++;
        this.forceUpdate();
      }
    });

  };

}

export const EntityCheckboxList = React.forwardRef(function EntityCheckboxList(props: EntityCheckboxListProps, ref: React.Ref<EntityCheckboxListController>) {
  const c = useController(EntityCheckboxListController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  return (
    <GroupHeader className={classes("sf-checkbox-list", p.ctx.errorClassBorder)}
      label={p.label}
      labelIcon={p.labelIcon}
      avoidFieldSet={p.avoidFieldSet}
      buttons={renderButtons()}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} >
      {renderCheckboxList()}
    </GroupHeader >
  );

  function renderButtons() {
    return (
      <span>
        {c.renderCreateButton(false)}
        {c.renderFindButton(false)}
      </span>
    );
  }

  function renderCheckboxList() {
    return (
      <EntityCheckboxListSelect ctx={p.ctx} controller={c}  />
    );
  }
});


interface EntityCheckboxListSelectProps {
  ctx: TypeContext<MList<Lite<Entity> | ModifiableEntity>>;
  controller: EntityCheckboxListController;
}

export function EntityCheckboxListSelect(props: EntityCheckboxListSelectProps) {

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
    <div className="sf-checkbox-elements" style={getColumnStyle()}>
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

    return fixedData.map((row, i) => {
      var ectx = listCtx.firstOrNull(ectx => is(c.getKeyEntity(ectx.value), row.entity));
      var oldCtx = p.ctx.previousVersion == null || p.ctx.previousVersion.value == null ? null :
        listCtx.firstOrNull(ectx => is(c.getKeyEntity(ectx.previousVersion?.value), row.entity));

      var ric: RenderCheckboxItemContext<any> = {
        row,
        index: i,
        checked: ectx != null,
        controller: c,
        resultTable: resultTable,
        ectx: ectx
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
          {p.onRenderItem ? p.onRenderItem(ric) : <span>{renderLite(row.entity!)}</span>}
        </label>
      );
    }
    );
  }
}
