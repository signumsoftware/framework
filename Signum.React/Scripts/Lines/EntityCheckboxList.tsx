import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { TypeReference } from '../Reflection'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey } from '../Signum.Entities'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'
import { useController } from './LineBase'
import { normalizeEmptyArray } from './EntityCombo'

export interface EntityCheckboxListProps extends EntityListBaseProps {
  data?: Lite<Entity>[];
  columnCount?: number;
  columnWidth?: number;
  avoidFieldSet?: boolean;
  refreshKey?: string;
  renderCheckbox?: (lite: Lite<Entity>, index: number, checked: boolean, controller: EntityCheckboxListController) => React.ReactElement; 
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

  handleOnChange = (lite: Lite<Entity>) => {
    const list = this.props.ctx.value!;
    const toRemove = list.filter(mle => is(mle.element as Lite<Entity> | Entity, lite))

    if (toRemove.length) {
      toRemove.forEach(mle => list.remove(mle));
      this.setValue(list);
    }
    else {
      this.convert(lite).then(e => {
        this.addElement(e);
      }).done();
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
          <span>{p.labelText}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderCheckboxList()}
    </fieldset>
  );

  function renderButtons() {
    return (
      <span className="float-right">
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

  var [data, setData] = React.useState<Lite<Entity>[] | undefined>(p.data);
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
        Finder.expandParentColumn(fo);
        var limit = fo?.pagination?.elementsPerPage ?? 999;
        Finder.fetchEntitiesWithFilters(fo.queryName, fo.filterOptions ?? [], fo.orderOptions ?? [], limit)
          .then(data => setData(fo.orderOptions && fo.orderOptions.length ? data : data.orderBy(a => a.toStr)))
          .done();
      }
      else
        Finder.API.fetchAllLites({ types: p.type!.name })
          .then(data => setData(data.orderBy(a => a.toStr)))
          .done();
    }
  }, [normalizeEmptyArray(p.data), p.type!.name, p.refreshKey, p.findOptions && Finder.findOptionsPath(p.findOptions)]);

  return (
    <div className="sf-checkbox-elements" style={getColumnStyle()}>
      {renderContent()}
    </div>
  );

  function getColumnStyle(): React.CSSProperties | undefined {
    if (p.columnCount && p.columnWidth)
      return {
        columns: `${p.columnCount} ${p.columnWidth}px`,
        MozColumns: `${p.columnCount} ${p.columnWidth}px`,
        WebkitColumns: `${p.columnCount} ${p.columnWidth}px`,
      };

    if (p.columnCount)
      return {
        columnCount: p.columnCount,
        MozColumnCount: p.columnCount,
        WebkitColumnCount: p.columnCount,
      };

    if (p.columnWidth)
      return {
        columnWidth: p.columnWidth,
        MozColumnWidth: p.columnWidth,
        WebkitColumnWidth: p.columnWidth,
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

    const fixedData = [...data];

    const list = p.ctx.value!;

    list.forEach(mle => {
      if (!fixedData.some(d => is(d, mle.element as Entity | Lite<Entity>)))
        fixedData.insertAt(0, maybeToLite(mle.element as Entity | Lite<Entity>))
    });

    if (p.renderCheckbox)
      return fixedData.map((lite, i) => p.renderCheckbox!(lite, i, list.some(mle => is(mle.element as Entity | Lite<Entity>, lite)), c));

    return fixedData.map((lite, i) =>
      <label className="sf-checkbox-element" key={i}>
        <input type="checkbox"
          checked={list.some(mle => is(mle.element as Entity | Lite<Entity>, lite))}
          disabled={p.ctx.readOnly}
          name={liteKey(lite)}
          onChange={e => c.handleOnChange(lite)} />
        &nbsp;
        <span className="sf-entitStrip-link">{lite.toStr}</span>
      </label>);
  }
}
