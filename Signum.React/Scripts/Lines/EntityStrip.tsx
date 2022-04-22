import * as React from 'react'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { TypeContext } from '../TypeContext'
import { FormGroup } from '../Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, toLite, is, liteKey, getToString, isEntity, isLite, parseLiteList } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityListBaseController, EntityListBaseProps, DragConfig } from './EntityListBase'
import { AutocompleteConfig, TypeBadge } from './AutoCompleteConfig'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityBaseController } from './EntityBase';
import { LineBaseController, LineBaseProps, tasks, useController } from './LineBase'
import { getTypeInfo, getTypeInfos, getTypeName, tryGetTypeInfos } from '../Reflection'
import { FilterOperation } from '../Signum.Entities.DynamicQuery'
import { FindOptions } from '../Search'
import { useForceUpdate } from '../Hooks'
import { TypeaheadController } from '../Components/Typeahead'


export interface EntityStripProps extends EntityListBaseProps {
  vertical?: boolean;
  iconStart?: boolean;
  autocomplete?: AutocompleteConfig<any> | null;
  onRenderItem?: (item: any /*T*/) => React.ReactNode;
  showType?: boolean;
  onItemHtmlAttributes?: (item: any /*T*/) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  onItemContainerHtmlAttributes?: (item: any /*T*/) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  avoidDuplicates?: boolean;
}

export class EntityStripController extends EntityListBaseController<EntityStripProps> {

  typeahead!: React.RefObject<TypeaheadController>;

  overrideProps(p: EntityStripProps, overridenProps: EntityStripProps) {
    super.overrideProps(p, overridenProps);
    this.typeahead = React.useRef<TypeaheadController>(null);

    if (p.type) {
      if (p.showType == undefined)
        p.showType = p.type.name.contains(",");


      if (p.autocomplete === undefined) {

        var avoidDuplicates = p.avoidDuplicates ?? p.ctx.propertyRoute?.member?.avoidDuplicates;
        if (avoidDuplicates) {
          var types = tryGetTypeInfos(p.type).notNull();
          if (types.length == 0)
            return;

          if (types.length == 1)
            p.findOptions = withAvoidDuplicates(p.findOptions ?? { queryName: types.single().name }, types.single().name);
          else {
            p.findOptionsDictionary = types.toObject(a => a.name, a => withAvoidDuplicates(p.findOptionsDictionary?.[a.name] ?? { queryName: a.name }, a.name));
          }
        }

        p.autocomplete = Navigator.getAutoComplete(p.type, p.findOptions, p.findOptionsDictionary, p.ctx, p.create!, p.showType);
      }
      if (p.iconStart == undefined && p.vertical)
        p.iconStart = true;

    }

    function withAvoidDuplicates(fo: FindOptions,  typeName: string): FindOptions {

      const compatible = p.ctx.value.map(a => a.element).filter(e => isLite(e) ? e.EntityType == typeName : isEntity(e) ? e.Type == typeName : null).notNull();

      return { ...fo, filterOptions: [...fo?.filterOptions ?? [], { token: "Entity", operation: "IsNotIn", value: compatible }] };
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

export const EntityStrip = React.forwardRef(function EntityStrip(props: EntityStripProps, ref: React.Ref<EntityStripController>) {
  const c = useController(EntityStripController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  const readOnly = p.ctx.readOnly;
  return (
    <FormGroup ctx={p.ctx!}
      labelText={p.labelText}
      labelHtmlAttributes={p.labelHtmlAttributes}
      helpText={p.helpText}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
      <div className="sf-entity-strip sf-control-container">
        <ul className={classes("sf-strip", p.vertical ? "sf-strip-vertical" : "sf-strip-horizontal", p.ctx.labelClass)}>
          {
            c.getMListItemContext(p.ctx).map(mlec =>
              (<EntityStripElement key={c.keyGenerator.getKey(mlec.value)}
                ctx={mlec}
                iconStart={p.iconStart}
                autoComplete={p.autocomplete}
                onRenderItem={p.onRenderItem}
                drag={c.canMove(mlec.value) && !readOnly ? c.getDragConfig(mlec.index!, p.vertical ? "v" : "h") : undefined}
                onItemHtmlAttributes={p.onItemHtmlAttributes}
                onItemContainerHtmlAttributes={p.onItemContainerHtmlAttributes}
                onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
                onView={c.canView(mlec.value) ? e => c.handleViewElement(e, mlec.index!) : undefined}
                showType={p.showType!}
              />))
          }
          {renderLastElement()}
        </ul>
      </div>
    </FormGroup>
  );

  function renderLastElement() {
    
    const buttons = (
      <>
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {c.renderCreateButton(true)}
        {c.renderFindButton(true)}
        {c.renderPasteButton(true)}
        {p.extraButtonsAfter && p.extraButtonsAfter(c)}
      </>
    );

    return (
      <li className={"sf-strip-input"}>
        {
          !EntityBaseController.hasChildrens(buttons) ?
            renderAutoComplete() :
            renderAutoComplete(input => <div className={p.ctx.inputGroupClass}>
              {input}
              {buttons}
            </div>)
        }
      </li>
    );
  }

  function handleOnPaste(e: React.ClipboardEvent<HTMLInputElement>) {
    const text = e.clipboardData.getData("text");
    const lites = parseLiteList(text);
    if (lites.length == 0)
      return;
    
    e.preventDefault();
    c.paste(text)?.then(() => {
      c.typeahead.current?.writeInInput("");
    }).done();
  }

  function renderAutoComplete(renderInput?: (input: React.ReactElement<any> | null) => React.ReactElement<any>) {
    var ac = p.autocomplete;

    if (p.ctx!.readOnly)
      return undefined;

    if (ac == null)
      return renderInput == null ? null : renderInput(null);

    return (
      <Typeahead ref={c.typeahead}
        inputAttrs={{ className: classes(p.ctx.formControlClass, "sf-entity-autocomplete", c.mandatoryClass), placeholder: EntityControlMessage.Add.niceToString(), onPaste: p.paste == false ? undefined : handleOnPaste }}
        getItems={q => ac!.getItems(q)}
        itemsDelay={ac.getItemsDelay()}
        renderItem={(e, str) => ac!.renderItem(e, str)}
        itemAttrs={item => ({ 'data-entity-key': ac!.getDataKeyFromItem(item) }) as React.HTMLAttributes<HTMLButtonElement>}
        onSelect={c.handleOnSelect}
        renderInput={renderInput}
      />
    );
  }
});

export interface EntityStripElementProps {
  iconStart?: boolean;
  onRemove?: (event: React.MouseEvent<any>) => void;
  onView?: (event: React.MouseEvent<any>) => void;
  ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
  autoComplete?: AutocompleteConfig<any> | null;
  onRenderItem?: (item: Lite<Entity> | ModifiableEntity) => React.ReactNode;
  onItemHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  onItemContainerHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  drag?: DragConfig;
  showType: boolean;
}

export function EntityStripElement(p: EntityStripElementProps) {
  var currentEntityRef = React.useRef<{ entity: ModifiableEntity | Lite<Entity>, item?: unknown } | undefined>(undefined);
  const forceUpdate = useForceUpdate();

  React.useEffect(() => {

    if (p.autoComplete) {
      var newEntity = p.ctx.value;
      if (!currentEntityRef.current || currentEntityRef.current.entity !== newEntity) {
        var ci = { entity: newEntity!, item: undefined }
        currentEntityRef.current = ci;
        var fillItem = (newEntity: ModifiableEntity | Lite<Entity>) => {
          const autocomplete = p.autoComplete;
          autocomplete?.getItemFromEntity(newEntity)
            .then(item => {
              if (autocomplete == p.autoComplete) {
                ci.item = item;
                forceUpdate();
              } else {
                fillItem(newEntity);
              }
            })
            .done();
        };
        fillItem(newEntity);
      }
    }

  }, [p.ctx.value]);

  const toStr =
    p.onRenderItem ? p.onRenderItem(p.ctx.value) :
      currentEntityRef.current?.item ? p.autoComplete!.renderItem(currentEntityRef.current.item) :
        getToStr();

  function getToStr() {
    const toStr = getToString(p.ctx.value);
    return !p.showType || !(isEntity(p.ctx.value) || isLite(p.ctx.value)) ? toStr :
      <span style={{ wordBreak: "break-all" }} title={toStr}>
        <TypeBadge entity={p.ctx.value}/>{toStr}
      </span>;
  }


  var drag = p.drag;
  const htmlAttributes = p.onItemHtmlAttributes && p.onItemHtmlAttributes(p.ctx.value);

  var val = p.ctx.value;

  //Till https://github.com/facebook/react/issues/8529 gets fixed
  var url = isEntity(val) && !val.isNew ? Navigator.navigateRoute(val) :
    isLite(val) && !(val.entity && val.entity.isNew) ? Navigator.navigateRoute(val) : "#";

  var hasIcon = p.onRemove || p.drag;

  var containerHtmlAttributes = (p.onItemContainerHtmlAttributes && p.onItemContainerHtmlAttributes(p.ctx.value));

  return (
    <li className={classes("sf-strip-element", containerHtmlAttributes?.className)}
      {...EntityListBaseController.entityHtmlAttributes(p.ctx.value)}
      {...containerHtmlAttributes}>
      <div className={classes(drag && "sf-strip-dropable", drag?.dropClass)}
        onDragEnter={drag?.onDragOver}
        onDragOver={drag?.onDragOver}
        onDrop={drag?.onDrop}

      >
        {hasIcon && p.iconStart && <span style={{ marginRight: "5px" }}>{removeIcon()}&nbsp;{dragIcon()}</span>}
        {
          p.onView ?
            <a href={url} className={classes("sf-strip-link", htmlAttributes?.className)} onClick={p.onView} {...htmlAttributes}>
              {toStr}
            </a>
            :
            <span className={classes("sf-strip-link", htmlAttributes?.className)} {...htmlAttributes}>
              {toStr}
            </span>
        }
        {hasIcon && !p.iconStart && <span>{removeIcon()}&nbsp;{dragIcon()}</span>}
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

//tasks.push(taskSetAvoidDuplicates);
//export function taskSetAvoidDuplicates(lineBase: LineBaseController<any>, state: LineBaseProps) {
//  if (lineBase instanceof EntityStripController &&
//    (state as EntityStripProps).avoidDuplicates == undefined &&
//    state.ctx.propertyRoute &&
//    state.ctx.propertyRoute.propertyRouteType == "Field" &&
//    state.ctx.propertyRoute.member!.avoidDuplicates) {
//    (state as EntityStripProps).avoidDuplicates = true;
//  }
//}
