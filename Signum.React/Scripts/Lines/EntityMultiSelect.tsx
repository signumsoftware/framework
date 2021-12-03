import * as React from 'react'
import { ResultTable } from '../Search';
import { Entity, is, Lite } from '../Signum.Entities';
import { AutocompleteConfig } from './AutoCompleteConfig';
import { EntityBaseController } from './EntityBase';
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase';
import * as Navigator from '../Navigator'
import { Multiselect } from 'react-widgets/cjs';
import { useController } from './LineBase';
import { number } from 'prop-types';

export interface EntityMultiSelectProps extends EntityListBaseProps {
  vertical?: boolean;
  iconStart?: boolean;
  autocomplete?: AutocompleteConfig<any> | null;
  onRenderItem?: (item: any /*T*/) => React.ReactNode;
  showType?: boolean;
  onItemHtmlAttributes?: (item: any /*T*/) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  onItemContainerHtmlAttributes?: (item: any /*T*/) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
}

export interface EntityMultiSelectSelectHandle {
  getSelect(): HTMLSelectElement | null;
  getData(): Lite<Entity>[] | ResultTable | undefined;
}


export class EntityMultiSelectSelectController extends EntityListBaseController<EntityMultiSelectProps> {
  overrideProps(p: EntityMultiSelectProps, overridenProps: EntityMultiSelectProps) {
    super.overrideProps(p, overridenProps);

    if (p.type) {
      if (p.showType == undefined)
        p.showType = p.type.name.contains(",");

      if (p.autocomplete === undefined) {
        p.autocomplete = Navigator.getAutoComplete(p.type, p.findOptions, p.ctx, p.create!, p.showType);
      }
      if (p.iconStart == undefined && p.vertical)
        p.iconStart = true;
    }
  }

  handleOnSelect = (item: any, event: React.SyntheticEvent<any>) => {
    this.props.autocomplete!.getEntityFromItem(item)
      .then(entity => entity && this.convert(entity)
        .then(e => this.addElement(e))
      ).done();

    return "";
  }

  handleViewElement = (event: React.MouseEvent<any>, index: number) => {

    event.preventDefault();

    const ctx = this.props.ctx;
    const list = ctx.value!;
    const mle = list[index];
    const entity = mle.element;

    const openWindow = (event.button == 1 || event.ctrlKey) && !this.props.type!.isEmbedded;
    if (openWindow) {
      event.preventDefault();
      const route = Navigator.navigateRoute(entity as Lite<Entity> /*or Entity*/);
      window.open(route);
    }
    else {
      const pr = ctx.propertyRoute!.addLambda(a => a[0]);

      const promise = this.props.onView ?
        this.props.onView(entity, pr) :
        this.defaultView(entity, pr);

      if (promise == null)
        return;

      promise.then(e => {
        if (e == undefined)
          return;

        this.convert(e).then(m => {
          if (is(list[index].element as Entity, e as Entity)) {
            list[index].element = m;
            if (e.modified)
              this.setValue(list);
            this.forceUpdate();
          } else {
            list[index] = { rowId: null, element: m };
            this.setValue(list);
          }
        }).done();
      }).done();
    }
  }
}

export const EntityMultiSelect = React.forwardRef(function EntityMultiSelect(props: EntityMultiSelectProps, ref: React.Ref<EntityMultiSelectSelectController>) {
  const c = useController(EntityMultiSelectSelectController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;


  var newData = c.getMListItemContext(p.ctx).map((mlec, i) => ({ id:i, rule: mlec}))
        
  const readOnly = p.ctx.readOnly;

  return (
    <Multiselect
      dataKey="id"
      textField="rule"
      defaultValue={[1]}
      data={newData}
    />
    );
});
