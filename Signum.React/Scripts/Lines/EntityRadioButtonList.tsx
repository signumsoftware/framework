import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey, getToString } from '../Signum.Entities'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { useController } from './LineBase'
import { ResultTable } from '../Search'
import { ResultRow } from '../FindOptions'

export interface EntityRadioButtonListProps extends EntityBaseProps {
  data?: Lite<Entity>[];
  columnCount?: number;
  columnWidth?: number;
  avoidFieldSet?: boolean;
  deps?: React.DependencyList;
  nullPlaceHolder?: string;
  onRenderItem?: (lite: Lite<Entity> | null) => React.ReactChild;
  nullElement?: "No" | "Always" | "Initially";
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

  handleOnChange = (lite: Lite<Entity> | null) => {
    if (lite == null)
      this.setValue(lite);
    else
      this.convert(lite)
        .then(v => this.setValue(v));
  }

}

export const EntityRadioButtonList = React.forwardRef(function EntityRadioButtonList(props: EntityRadioButtonListProps, ref: React.Ref<EntityRadioButtonListController>) {
  const c = useController(EntityRadioButtonListController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("sf-radiobutton-list", p.ctx.errorClassBorder, c.mandatoryClass)} {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
        {renderButtons()}
        {renderRadioList()}
      </div>
    );

  return (
    <fieldset className={classes("sf-radiobutton-list", p.ctx.errorClass, c.mandatoryClass)} {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
      <legend>
        <div>
          <span>{p.label}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderRadioList()}
    </fieldset>
  );

  function renderButtons() {
    return (
      <span className="ms-2">
        {c.renderCreateButton(false)}
        {c.renderFindButton(false)}
      </span>
    );
  }

  function renderRadioList() {
    return (
      <EntityRadioButtonListSelect ctx={p.ctx} controller={c} nullElement={p.nullElement} />
    );
  }
});


interface EntityRadioButtonListSelectProps {
  ctx: TypeContext<MList<Lite<Entity> | ModifiableEntity>>;
  controller: EntityRadioButtonListController;
  nullElement?: "No" | "Always" | "Initially";
}

export function EntityRadioButtonListSelect(props: EntityRadioButtonListSelectProps) {

  const c = props.controller;
  const p = c.props;

  var [data, setData] = React.useState<Lite<Entity>[] | ResultTable | undefined>(p.data);
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
        Finder.getResultTable(Finder.defaultNoColumnsAllRows(fo, undefined))
          .then(data => setData(data));
      }
      else
        Finder.API.fetchAllLites({ types: p.type!.name })
          .then(data => setData(data.orderBy(a => getToString(a))));
    }
  }, [p.data, p.type!.name, p.deps, p.findOptions && Finder.findOptionsPath(p.findOptions)]);

  return (
    <div className="sf-radiobutton-elements" style={getColumnStyle()}>
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

    if (p.nullElement == "Always")
      fixedData.insertAt(0, { entity: null as any } as ResultRow);

    const value = p.ctx.value as Entity | Lite<Entity> | null;
    if (!fixedData.some(d => is(d.entity, value)) && (p.nullElement == "Initially" || value != null))
      fixedData.insertAt(0, { entity: value && maybeToLite(value) } as ResultRow);

    return fixedData.map((row, i) =>
      <label className="sf-radio-element" key={i}>
        <input type="radio" style={{ marginLeft: "10px" }}
          checked={is(value, row.entity)}
          onClick={e => c.handleOnChange(row.entity!)}
          disabled={p.ctx.readOnly}/>
        &nbsp;
        {c.props.onRenderItem ? c.props.onRenderItem(row.entity!) : <span>getToString(lite) ?? p.nullPlaceHolder ?? " - "</span>}
      </label>);
  }
}
