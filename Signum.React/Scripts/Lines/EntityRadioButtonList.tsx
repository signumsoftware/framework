import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { TypeReference } from '../Reflection'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey } from '../Signum.Entities'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { useController } from './LineBase'

export interface EntityRadioButtonListProps extends EntityBaseProps{
  data?: Lite<Entity>[];
  columnCount?: number;
  columnWidth?: number;
  avoidFieldSet?: boolean;
  refreshKey?: string;
  renderRadio?: (lite: Lite<Entity>, index: number, checked: boolean, controller: EntityRadioButtonListController) => React.ReactElement;
  groupKey: string;
}

export class EntityRadioButtonListController extends EntityBaseController<EntityRadioButtonListProps> {

  getDefaultProps(state: EntityRadioButtonListProps) {
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
    if (lite == null)
      this.setValue(lite);
    else
      this.convert(lite)
        .then(v => this.setValue(v))
        .done();
  }

}

export const EntityRadioButtonList = React.forwardRef(function EntityRadioButtonList(props: EntityRadioButtonListProps, ref: React.Ref<EntityRadioButtonListController>) {
  const c = useController(EntityRadioButtonListController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("SF-RadioButton-list", p.ctx.errorClassBorder)} {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
        {renderButtons()}
        {renderRadioList()}
      </div>
    );

  return (
    <fieldset className={classes("SF-RadioButton-list", p.ctx.errorClass)} {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
      <legend>
        <div>
          <span>{p.labelText}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderRadioList()}
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

  function renderRadioList() {
    return (
      <EntityRadioButtonListSelect ctx={p.ctx} controller={c} groupKey={p.groupKey} />
    );
  }
});


interface EntityRadioButtonListSelectProps {
  ctx: TypeContext<MList<Lite<Entity> | ModifiableEntity>>;
  controller: EntityRadioButtonListController;
  groupKey: string;
}

export function EntityRadioButtonListSelect(props: EntityRadioButtonListSelectProps) {

  const c = props.controller;
  const p = c.props;

  var [data, setData] = React.useState<Lite<Entity>[] | undefined>(p.data);
  var requestStarted = React.useRef(false);

  React.useEffect(() => {
    if (p.data) {
      if (requestStarted.current)
        console.warn(`The 'data' was set too late. Consider using [] as default value to avoid automatic query. EntityRadioButtonList: ${p.type!.name}`);
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
  }, [p.data, p.type!.name, p.refreshKey, p.findOptions && Finder.findOptionsPath(p.findOptions)]);

  return (
    <div className="sf-radiobutton-elements switch-field" style={getColumnStyle()}>
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



    list.forEach((mle: Entity | Lite<Entity>) => {
      if (!fixedData.some(d => is(d, mle as Entity | Lite<Entity>)))
        fixedData.insertAt(0, maybeToLite(mle as Entity | Lite<Entity>))
    });

    if (p.renderRadio)
      return fixedData.map((lite, i) => p.renderRadio!(lite, i, list.some((mle: Entity | Lite<Entity>) => is(mle as Entity | Lite<Entity>, lite)), c));

    return fixedData.map((lite, i) =>
      <label className="sf-radiobutton-element" key={i}>
        <input type="radio"
          checked={list.some((mle: Entity | Lite<Entity>) => is(mle as Entity | Lite<Entity>, lite))}
          disabled={p.ctx.readOnly}
          name={liteKey(lite) + props.groupKey}
          onChange={e => c.handleOnChange(lite)} />
        &nbsp;
        <span className="sf-entitStrip-link">{lite.toStr}</span>
      </label>);
  }
}
