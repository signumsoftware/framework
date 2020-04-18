import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey } from '../Signum.Entities'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { useController } from './LineBase'

export interface EntityRadioButtonListProps extends EntityBaseProps {
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
      state.ctx.value = null;

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
    <div className="sf-radiobutton-elements switch-field">
      {renderContent()}
    </div>
  );

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

    const value = p.ctx.value!;

    var groupString = new Date().getTime().toString();
    return fixedData.map((lite, i) =>
      <label className="sf-radiobutton-element" key={i}>
        <div className={"buttonRadioItem" + (value && is(value as Lite<Entity>, lite) ? " buttonRadioItemChecked" : "")} onClick={e => c.handleOnChange(lite)}>
          <input type="radio" style={{ marginLeft:"10px" }}
            checked={value && is(value as Lite<Entity>, lite)}
            disabled={p.ctx.readOnly}
            name={"RadioGroup" + groupString} />
          <span className="sf-entitStrip-link">{lite.toStr}</span>
        </div>
      </label>);
  }
}
