import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import { TypeContext } from '../TypeContext'
import { FormGroup } from '../Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, toLite, is, liteKey, getToString, isEntity, isLite } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityListBaseController, EntityListBaseProps, DragConfig } from './EntityListBase'
import { AutocompleteConfig } from './AutoCompleteConfig'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityBaseController } from './EntityBase';

export interface EntityStripProps extends EntityListBaseProps {
  vertical?: boolean;
  iconStart?: boolean;
  autocomplete?: AutocompleteConfig<any> | null;
  onRenderItem?: (item: Lite<Entity> | ModifiableEntity) => React.ReactNode;
  showType?: boolean;
  onItemHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
}

export class EntityStripController extends EntityListBaseController<EntityStripProps> {
  overrideProps(p: EntityStripProps, overridenProps: EntityStripProps) {
    super.overrideProps(p, overridenProps);
    if (p.autocomplete === undefined) {
      p.autocomplete = Navigator.getAutoComplete(p.type!, p.findOptions, p.showType);
    }
  }
}

export function EntityStrip(props: EntityStripProps) {
  const c = new EntityStripController(props);
  const p = c.props;


  const readOnly = p.ctx.readOnly;
  return (
    <FormGroup ctx={p.ctx!}
      labelText={p.labelText}
      labelHtmlAttributes={p.labelHtmlAttributes}
      helpText={p.helpText}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
      <div className="SF-entity-strip SF-control-container">
        <ul className={classes("sf-strip", p.vertical ? "sf-strip-vertical" : "sf-strip-horizontal")}>
          {
            c.getMListItemContext(p.ctx).map(mlec =>
              (<EntityStripElement key={c.keyGenerator.getKey(mlec.value)}
                ctx={mlec}
                iconStart={p.iconStart}
                autoComplete={p.autocomplete}
                onRenderItem={p.onRenderItem}
                drag={c.canMove(mlec.value) && !readOnly ? c.getDragConfig(mlec.index!, p.vertical ? "v" : "h") : undefined}
                onItemHtmlAttributes={p.onItemHtmlAttributes}
                onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
                onView={c.canView(mlec.value) ? e => handleViewElement(e, mlec.index!) : undefined}
              />))
          }
          <li className={classes(p.ctx.inputGroupClass, "sf-strip-input")}>
            {renderAutoComplete()}
            <span>
              {c.renderCreateButton(false)}
              {c.renderFindButton(false)}
              {p.extraButtons && p.extraButtons(c)}
            </span>
          </li>
        </ul>
      </div>
    </FormGroup>
  );

  function handleOnSelect(item: any, event: React.SyntheticEvent<any>) {
    p.autocomplete!.getEntityFromItem(item)
      .then(entity => entity && c.convert(entity)
        .then(e => c.addElement(e))
      ).done();

    return "";
  }

  function handleViewElement(event: React.MouseEvent<any>, index: number) {

    event.preventDefault();

    const ctx = p.ctx;
    const list = ctx.value!;
    const mle = list[index];
    const entity = mle.element;

    const openWindow = (event.button == 1 || event.ctrlKey) && !p.type!.isEmbedded;
    if (openWindow) {
      event.preventDefault();
      const route = Navigator.navigateRoute(entity as Lite<Entity> /*or Entity*/);
      window.open(route);
    }
    else {
      const pr = ctx.propertyRoute.addLambda(a => a[0]);

      const promise = p.onView ?
        p.onView(entity, pr) :
        c.defaultView(entity, pr);

      if (promise == null)
        return;

      promise.then(e => {
        if (e == undefined)
          return;

        c.convert(e).then(m => {
          if (is(list[index].element as Entity, e as Entity)) {
            list[index].element = m;
            if (e.modified)
              c.setValue(list);
            c.forceUpdate();
          } else {
            list[index] = { rowId: null, element: m };
            c.setValue(list);
          }
        }).done();
      }).done();
    }
  }

  function renderAutoComplete() {

    var ac = p.autocomplete;

    if (!ac || p.ctx!.readOnly)
      return undefined;

    return (
      <Typeahead
        inputAttrs={{ className: "sf-entity-autocomplete" }}
        getItems={q => ac!.getItems(q)}
        getItemsDelay={ac.getItemsDelay}
        renderItem={(e, str) => ac!.renderItem(e, str)}
        itemAttrs={item => ({ 'data-entity-key': ac!.getDataKeyFromItem(item) }) as React.HTMLAttributes<HTMLButtonElement>}
        onSelect={handleOnSelect} />
    );
  }
}


export interface EntityStripElementProps {
  iconStart?: boolean;
  onRemove?: (event: React.MouseEvent<any>) => void;
  onView?: (event: React.MouseEvent<any>) => void;
  ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
  autoComplete?: AutocompleteConfig<any> | null;
  onRenderItem?: (item: Lite<Entity> | ModifiableEntity) => React.ReactNode;
  onItemHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  drag?: DragConfig;
}

export function EntityStripElement(p: EntityStripElementProps) {
  var [currentItem, setCurrentItem] = React.useState<{ entity: ModifiableEntity | Lite<Entity>, item?: unknown } | undefined>(undefined);

  React.useEffect(() => {

    if (p.autoComplete) {
      var newEntity = p.ctx.value;
      if (!currentItem || currentItem.entity !== newEntity) {
        var ci = { entity: newEntity!, item: undefined }
        setCurrentItem(ci);
        var fillItem = (newEntity: ModifiableEntity | Lite<Entity>) => {
          const autocomplete = p.autoComplete;
          autocomplete && autocomplete.getItemFromEntity(newEntity)
            .then(item => {
              if (autocomplete == p.autoComplete) {
                ci.item = item;
                setCurrentItem(ci);
              } else {
                fillItem(newEntity);
              }
            })
            .done();
        };
        fillItem(newEntity);
        p.autoComplete.getItemFromEntity(newEntity)
          .then(item => {
            ci.item = item;
            setCurrentItem(ci);
          })
          .done();
      }
    }

  }, [p.ctx.value]);



  const toStr =
    p.onRenderItem ? p.onRenderItem(p.ctx.value) :
      currentItem && currentItem.item ? p.autoComplete!.renderItem(currentItem.item) :
        getToString(p.ctx.value);

  var drag = p.drag;
  const htmlAttributes = p.onItemHtmlAttributes && p.onItemHtmlAttributes(p.ctx.value);

  var val = p.ctx.value;

  //Till https://github.com/facebook/react/issues/8529 gets fixed
  var url = isEntity(val) && !val.isNew ? Navigator.navigateRoute(val) :
    isLite(val) && !(val.entity && val.entity.isNew) ? Navigator.navigateRoute(val) : "#";

  return (
    <li className="sf-strip-element"
      {...EntityListBaseController.entityHtmlAttributes(p.ctx.value)}>
      <div className={classes(drag && "sf-strip-dropable", drag && drag.dropClass)}
        onDragEnter={drag && drag.onDragOver}
        onDragOver={drag && drag.onDragOver}
        onDrop={drag && drag.onDrop}

      >
        {p.iconStart && <span style={{ marginRight: "5px" }}>{removeIcon()}&nbsp;{dragIcon()}</span>}
        {
          p.onView ?
            <a href={url} className="sf-entitStrip-link" onClick={p.onView} {...htmlAttributes}>
              {toStr}
            </a>
            :
            <span className="sf-entitStrip-link" {...htmlAttributes}>
              {toStr}
            </span>
        }
        {!p.iconStart && <span>{removeIcon()}&nbsp;{dragIcon()}</span>}
      </div>
    </li>
  );


  function removeIcon() {
    return p.onRemove &&
      <span>
        <a className="sf-line-button sf-remove"
          onClick={p.onRemove}
          href="#"
          title={p.ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
          {EntityBaseController.removeIcon}
        </a>
      </span>
  }

  function dragIcon() {
    return drag && <span className={classes("sf-line-button", "sf-move")}
      draggable={true}
      onDragStart={drag.onDragStart}
      onDragEnd={drag.onDragEnd}
      title={p.ctx.titleLabels ? EntityControlMessage.Move.niceToString() : undefined}>
      {EntityBaseController.moveIcon}
    </span>;
  }
}

