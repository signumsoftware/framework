import * as React from 'react'
import { ResultTable } from '../Search';
import { Entity, is, isEntity, isLite, isMListElement, Lite, liteKey, MList, MListElement, ModifiableEntity, newMListElement, toLite, toMList } from '../Signum.Entities';
import { AutocompleteConfig } from './AutoCompleteConfig';
import { Aprox, EntityBaseController } from './EntityBase';
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase';
import { Navigator } from '../Navigator'
import { Multiselect } from 'react-widgets-up';
import { useController } from './LineBase';
import { number } from 'prop-types';
import { FindOptions, ResultRow } from '../FindOptions'
import { Finder } from '../Finder'
import { normalizeEmptyArray } from './EntityCombo';
import { useMounted } from '../Hooks';
import { FormGroup } from './FormGroup';
import { classes } from '../Globals';
import { getTimeMachineIcon } from './TimeMachineIcon';
import Input from 'react-widgets-up/Input';
import { JSX } from 'react/jsx-runtime';

export interface EntityMultiSelectProps<V extends Lite<Entity> | Entity> extends EntityListBaseProps<V> {
  onRenderItem?: (item: ResultRow) => React.ReactNode;
  showType?: boolean;
  data?: Lite<Entity>[];
  toStringFromData?: boolean;
  delayLoadData?: boolean;
  deps?: React.DependencyList;
  ref?: React.Ref<EntityMultiSelectController<V>>
}

export class EntityMultiSelectController<V extends Lite<Entity> | Entity> extends EntityListBaseController<EntityMultiSelectProps<V>, V> {
  overrideProps(p: EntityMultiSelectProps<V>, overridenProps: EntityMultiSelectProps<V>): void {
    super.overrideProps(p, overridenProps);

    if (p.type) {
      if (p.showType == undefined)
        p.showType = p.type.name.contains(",");
    }
  }

  handleOnSelect = (lites: Aprox<V>[]) => {
    var current = this.props.ctx.value;

    lites.filter(lite => !current.some(mle => is(mle.element, lite))).forEach(lite => {
      this.convert(lite)
        .then(e => this.addElement(e));
    });

    current.filter(mle => !lites.some(lite => is(lite, mle.element))).forEach(mle => {
      this.removeElement(mle);
    });

    this.forceUpdate();

    return "";
  }
}

export function EntityMultiSelect<V extends Lite<Entity> | Entity>(props: EntityMultiSelectProps<V>): JSX.Element | null {
  const c = useController<EntityMultiSelectController<V>, EntityMultiSelectProps<V>, MList<V>>(EntityMultiSelectController, props);
  const p = c.props;

  if (c.isHidden)
    return null;

  const [data, _setData] = React.useState<Lite<Entity>[] | ResultTable | undefined>(p.data);
  const [loadData, setLoadData] = React.useState<boolean>(!p.delayLoadData);
  const requestStarted = React.useRef(false);
  const mounted = useMounted();

  function setData(data: Lite<Entity>[] | ResultTable) {
    if (mounted.current) {
      _setData(data);
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
  }, [normalizeEmptyArray(p.data), p.type!.name, p.deps, loadData, p.findOptions && Finder.findOptionsPath(p.findOptions)]);

  var optionsRows = getOptionRows()

  function getLite(e: unknown) {
    if (isLite(e))
      return e;

    if (isEntity(e))
      return toLite(e);

    throw new Error("Unexpected value " + JSON.stringify(e));
  }

  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  //TODO add TimeMachineIcon
  return (
    <FormGroup ctx={p.ctx!} error={p.error} label={p.label} labelIcon={p.labelIcon}
      labelHtmlAttributes={p.labelHtmlAttributes}
      helpText={helpText}
      helpTextOnTop={helpTextOnTop}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
      {inputId => <div className={classes(p.ctx.rwWidgetClass, c.mandatoryClass ? c.mandatoryClass + "-widget" : undefined)}>
        <Multiselect<any>
          id={inputId}
          readOnly={p.ctx.readOnly}
          dataKey={item => isMListElement(item) ? liteKey(getLite(item.element)) : liteKey((item as ResultRow).entity!)}
          textField="name"
          value={p.ctx.value}
          data={optionsRows as any}
          onChange={((value) => c.handleOnSelect(value.map(e => isMListElement(e) ? e.element : e.entity!)))}
          renderListItem={({ item }) => p.onRenderItem ? p.onRenderItem(item as ResultRow) : Navigator.renderLite(item.entity)}
          renderTagValue={({ item }) => isMListElement(item) ? Navigator.renderLite(getLite(item.element)) :
            p.onRenderItem ? p.onRenderItem(item) : Navigator.renderLite(item.entity!)
          }
        />
      </div>}
    </FormGroup>
  );

  function getOptionRows() {

    // const lite = getLite();

    var rows = Array.isArray(data) ? data.map(lite => ({ entity: lite } as ResultRow)) :
      typeof data == "object" ? data.rows :
        [];

    const elements: ResultRow[] = [...rows];


    p.ctx.value.forEach(mle => {
      const entityOrLite = mle.element;

      const lite: Lite<V & Entity> | (V & Lite<Entity>) | null = isEntity(entityOrLite) ? toLite(entityOrLite) :
        isLite(entityOrLite) ? entityOrLite : null;

      if (lite == null)
        throw new Error("Unexpected " + mle.element);

      var index = elements.findIndex(a => is(a?.entity, lite));
      if (index == -1)
        elements.insertAt(1, { entity: lite } as ResultRow);
      else {
        if (!p.toStringFromData)
          elements[index]!.entity = lite;
      }
    });


    return elements;
  }
}


interface MultiSelectElement {
  row: ResultRow;
  key: string;
}
