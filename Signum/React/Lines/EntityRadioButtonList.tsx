import * as React from 'react'
import { classes } from '../Globals'
import { Finder } from '../Finder'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, MList, toLite, is, liteKey, getToString } from '../Signum.Entities'
import { Aprox, AsLite, EntityBaseController, EntityBaseProps } from './EntityBase'
import { useController } from './LineBase'
import { ResultTable } from '../Search'
import { ResultRow } from '../FindOptions'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { GroupHeader, HeaderType } from './GroupHeader'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'


export interface EntityRadioButtonListProps<V extends Entity | Lite<Entity> | null> extends EntityBaseProps<V> {
  data?: AsLite<V>[];
  columnCount?: number;
  columnWidth?: number;
  avoidFieldSet?: boolean | HeaderType;
  deps?: React.DependencyList;
  nullPlaceHolder?: string;
  onRenderItem?: (lite: AsLite<V> | null) => React.ReactElement | null;
  nullElement?: "No" | "Always" | "Initially";
  ref?: React.Ref<EntityRadioButtonListController<V>>
}

export class EntityRadioButtonListController<V extends Entity | Lite<Entity> | null> extends EntityBaseController<EntityRadioButtonListProps<V>, V> {

  getDefaultProps(state: EntityRadioButtonListProps<V>): void {
    super.getDefaultProps(state);

    if (state.ctx.value == null)
      state.ctx.value = null!;

    state.remove = false;
    state.create = false;
    state.view = false;
    state.find = false;
    state.columnWidth = 200;
  }

  handleOnChange = (lite: AsLite<V> | null): void => {
    if (lite == null)
      this.setValue(null!);
    else
      this.convert(lite)
        .then(v => this.setValue(v));
  }

}

export function EntityRadioButtonList<V extends Entity | Lite<Entity>>(props: EntityRadioButtonListProps<V>): React.JSX.Element | null {
  const c = useController<EntityRadioButtonListController<V>, EntityRadioButtonListProps<V>, V>(EntityRadioButtonListController, props);
  const p = c.props;

  if (c.isHidden)
    return null;

  return (
    <GroupHeader className={classes("sf-radiobutton-list", c.getErrorClass("border"), c.mandatoryClass)}
      label={<>{getTimeMachineIcon({ ctx: p.ctx, translateY: "100%" })}{p.label}</>}
      labelIcon={p.labelIcon}
      avoidFieldSet={p.avoidFieldSet}
      buttons={renderButtons()}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...c.errorAttributes() }} >
      {renderRadioList()}
    </GroupHeader >
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
};


interface EntityRadioButtonListSelectProps<V extends Lite<Entity> | Entity | null> {
  ctx: TypeContext<V>;
  controller: EntityRadioButtonListController<V>;
  nullElement?: "No" | "Always" | "Initially";
}

export function EntityRadioButtonListSelect<V extends Lite<Entity> | Entity | null>(props: EntityRadioButtonListSelectProps<V>): React.ReactElement {

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

    const value = p.ctx.value ;
    if (!fixedData.some(d => is(d.entity, value)) && (p.nullElement == "Initially" || value != null))
      fixedData.insertAt(0, { entity: value && maybeToLite(value) } as ResultRow);

    return fixedData.map((row, i) =>
      <label className="sf-radio-element" key={i}>
        <input type="radio" style={{ marginLeft: "10px" }}
          checked={is(value, row.entity)}
          onClick={e => c.handleOnChange(row.entity as AsLite<V>)}
          disabled={p.ctx.readOnly}/>
        &nbsp;
        {c.props.onRenderItem ? c.props.onRenderItem(row.entity as AsLite<V>) : <span>{getToString(row.entity) ?? p.nullPlaceHolder ?? " - "}</span>}
      </label>);
  }
}
