
import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import { FindOptions, ResultRow } from '../FindOptions'
import { mlistItemContext, TypeContext } from '../TypeContext'
import { TypeReference } from '../Reflection'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey, getToString, MListElement } from '../Signum.Entities'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'
import { useController } from './LineBase'
import { normalizeEmptyArray } from './EntityCombo'
import { ResultTable } from '../Search'
import { renderLite } from '../Navigator'

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
  columnCount?: number;
  columnWidth?: number;
  avoidFieldSet?: boolean;
  deps?: React.DependencyList;
  onRenderCheckbox?: (ric: RenderCheckboxItemContext<any>) => React.ReactElement;
  onRenderItem?: (ric: RenderCheckboxItemContext<any>) => React.ReactElement;

  getLiteFromElement?: (e: any /*ModifiableEntity*/) => Entity | Lite<Entity>;
  createElementFromLite?: (file: Lite<any>) => Promise<ModifiableEntity>;
}

export class EntityCheckboxListController extends EntityListBaseController<EntityCheckboxListProps> {

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

  handleOnChange = (event: React.SyntheticEvent, lite: Lite<Entity>) => {
    const list = this.props.ctx.value!;
    const toRemove = list.filter(mle => is(this.getKeyEntity(mle.element), lite))

    if (toRemove.length) {
      toRemove.forEach(mle => list.remove(mle));
      this.setValue(list, event);
    }
    else {
      (this.props.createElementFromLite ? this.props.createElementFromLite(lite) : this.convert(lite)).then(e => {
        this.addElement(e);
      });
    }
  }

}

export const EntityCheckboxList = React.forwardRef(function EntityCheckboxList(props: EntityCheckboxListProps, ref: React.Ref<EntityCheckboxListController>) {
  const c = useController(EntityCheckboxListController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("sf-checkbox-list", p.ctx.errorClassBorder)} {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
        {renderButtons()}
        {renderCheckboxList()}
      </div>
    );

  return (
    <fieldset className={classes("sf-checkbox-list", p.ctx.errorClass)} {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
      <legend>
        <div>
          <span>{p.label}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderCheckboxList()}
    </fieldset>
  );

  function renderButtons() {
    return (
      <span className="float-end">
        {c.renderCreateButton(false)}
        {c.renderFindButton(false)}
      </span>
    );
  }

  function renderCheckboxList() {
    return (
      <EntityCheckboxListSelect ctx={p.ctx} controller={c} />
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
  }, [normalizeEmptyArray(p.data), p.type!.name, p.deps, p.findOptions && Finder.findOptionsPath(p.findOptions)]);

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

    const list = p.ctx.value!;

    list.forEach(mle => {
      var lite = c.getKeyEntity(mle.element);
      if (!fixedData.some(d => is(d.entity, lite)))
        fixedData.insertAt(0, { entity: maybeToLite(lite) } as ResultRow)
    });

    const resultTable = Array.isArray(data) ? undefined : data;

    var listCtx = mlistItemContext(p.ctx);

    return fixedData.map((row, i) => {
      var ectx = listCtx.firstOrNull(ectx => is(c.getKeyEntity(ectx.value), row.entity));

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
