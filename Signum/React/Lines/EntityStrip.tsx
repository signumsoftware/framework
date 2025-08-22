import * as React from 'react'
import { classes, Dic } from '../Globals'
import { Navigator } from '../Navigator'
import { TypeContext } from '../TypeContext'
import { FormGroup } from '../Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, toLite, is, liteKey, getToString, isEntity, isLite, parseLiteList, MList } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityListBaseController, EntityListBaseProps, DragConfig, MoveConfig } from './EntityListBase'
import { AutocompleteConfig, TypeBadge } from './AutoCompleteConfig'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Aprox, EntityBaseController, NN } from './EntityBase';
import { LineBaseController, LineBaseProps, tasks, useController } from './LineBase'
import { getTypeInfo, getTypeInfos, getTypeName, QueryTokenString, tryGetTypeInfos } from '../Reflection'
import { FindOptions } from '../Search'
import { useForceUpdate } from '../Hooks'
import { TextHighlighter, TypeaheadController } from '../Components/Typeahead'
import { getTimeMachineIcon } from './TimeMachineIcon'


export interface EntityStripProps<V extends ModifiableEntity | Lite<Entity>> extends EntityListBaseProps<V> {
  vertical?: boolean;
  iconStart?: boolean;
  autocomplete?: AutocompleteConfig<any> | null;
  onRenderItem?: (item: NoInfer<V>) => React.ReactNode;
  showType?: boolean;
  onItemHtmlAttributes?: (item: NoInfer<V>) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  onItemContainerHtmlAttributes?: (item: NoInfer<V>) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  avoidDuplicates?: boolean;
  groupElementsBy?: (e: V) => string;
  renderGroupTitle?: (key: string, i?: number) => React.ReactElement;
  inputAttributes?: React.InputHTMLAttributes<HTMLInputElement>;
  ref?: React.Ref<EntityStripController<V>>
}

export class EntityStripController<V extends ModifiableEntity | Lite<Entity>> extends EntityListBaseController<EntityStripProps<V>, V> {

  typeahead!: React.RefObject<TypeaheadController | null>;

  overrideProps(p: EntityStripProps<V>, overridenProps: EntityStripProps<V>): void {
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

          if (types.length == 1) {
            var tn = types.single().name;
            p.findOptions = withAvoidDuplicates(p.findOptions ?? Navigator.entitySettings[tn]?.defaultFindOptions ?? { queryName: tn }, tn);
          }
          else {
            p.findOptionsDictionary = types.toObject(a => a.name, a => withAvoidDuplicates(p.findOptionsDictionary?.[a.name] ?? Navigator.entitySettings[a.name]?.defaultFindOptions ?? { queryName: a.name }, a.name));
          }
        }

        p.autocomplete = Navigator.getAutoComplete(p.type, p.findOptions, p.findOptionsDictionary, p.ctx, p.create!, p.showType);
      }
      if (p.iconStart == undefined && p.vertical)
        p.iconStart = true;

    }

    function withAvoidDuplicates(fo: FindOptions,  typeName: string): FindOptions {

      const compatible = p.ctx.value.map(a => a.element).filter(e => isLite(e) ? e.EntityType == typeName : isEntity(e) ? e.Type == typeName : null).notNull();

      return { ...fo, filterOptions: [...fo?.filterOptions ?? [], { token: "Entity", operation: "IsNotIn", value: compatible, frozen: true }] };
      }
  }

  handleOnSelect = (item: any, event: React.SyntheticEvent<any>) => {
    this.props.autocomplete!.getEntityFromItem(item)
      .then(entity => entity && this.convert(entity as Aprox<V>)
        .then(e => this.addElement(e))
      );

    return "";
  }
}

export function EntityStrip<V extends ModifiableEntity | Lite<Entity>>(props: EntityStripProps<V>): React.JSX.Element | null {
  const c = useController<EntityStripController<V>, EntityStripProps<V>, MList<V>>(EntityStripController, props);
  const p = c.props;

  if (c.isHidden)
    return null;

  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  const readOnly = p.ctx.readOnly;
  return (
    <FormGroup ctx={p.ctx!} error={p.error} label={p.label} labelIcon={p.labelIcon}
      labelHtmlAttributes={p.labelHtmlAttributes}
      helpText={helpText}
      helpTextOnTop={helpTextOnTop}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
      {inputId => <div className="sf-entity-strip sf-control-container">
        {p.groupElementsBy == undefined ?
          <ul id={inputId} className={classes("sf-strip", p.vertical ? "sf-strip-vertical" : "sf-strip-horizontal", p.ctx.labelClass)}>
            {c.getMListItemContext(p.ctx).map((mlec, i) => renderElement(mlec, i))}
            {renderLastElement()}
          </ul>
          :

          <>
            {c.getMListItemContext(p.ctx).groupBy(a => (a.binding == null && a.previousVersion) ? p.groupElementsBy!(a.previousVersion.value) : p.groupElementsBy!(a.value)).map((gr, i) =>
              <div className={classes("mb-2")} key={i} >
                <small className="text-muted">{p.renderGroupTitle != undefined ? p.renderGroupTitle(gr.key, i) : gr.key}</small>
                <ul className={classes("sf-strip", p.vertical ? "sf-strip-vertical" : "sf-strip-horizontal", p.ctx.labelClass)}>
                  {gr.elements.map((mlec, i) => renderElement(mlec, i))}
                </ul>
              </div>)}
            {renderLastElement()}
          </>
        }

      </div>}
    </FormGroup>
  );

  function renderElement(mlec: TypeContext<V>, index: number): React.ReactElement {
    return <EntityStripElement<V> key={index}
      ctx={mlec}
      iconStart={p.iconStart}
      autoComplete={p.autocomplete}
      onRenderItem={p.onRenderItem}
      move={c.canMove(mlec.value) && p.moveMode == "MoveIcons" && !readOnly ? c.getMoveConfig(false, mlec.index!, p.vertical ? "v" : "h") : undefined}
      drag={c.canMove(mlec.value) && p.moveMode == "DragIcon" && !readOnly ? c.getDragConfig(mlec.index!, p.vertical ? "v" : "h") : undefined}
      onItemHtmlAttributes={p.onItemHtmlAttributes}
      onItemContainerHtmlAttributes={p.onItemContainerHtmlAttributes}
      onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
      onView={c.canView(mlec.value) ? e => c.handleViewElement(e, mlec.index!) : undefined}
      showType={p.showType!}
      vertical={p.vertical}
    />
  }

  function renderLastElement() {

    const buttons = (
      <>
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {c.renderCreateButton(true)}
        {c.renderFindButton(true)}
        {c.renderPasteButton(true)}
        {p.extraButtons && p.extraButtons(c)}
      </>
    );
    var autocomplete = !EntityBaseController.hasChildrens(buttons) ?
      renderAutoComplete() :
      renderAutoComplete(input => <div className={p.ctx.inputGroupClass}>
        {input}
        {buttons}
      </div>);

    if (p.groupElementsBy == null)
      return (
        <li className={"sf-strip-input"}>
          {autocomplete}
        </li>
      );

    return (
      <div className={"sf-strip-input"}>
        {autocomplete}
      </div>
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
    });
  }

  function renderAutoComplete(renderInput?: (input: React.ReactElement | null) => React.ReactElement) {
    var ac = p.autocomplete;

    if (p.ctx!.readOnly)
      return undefined;

    if (ac == null)
      return renderInput == null ? null : renderInput(null);

    return (
      <Typeahead ref={c.typeahead}
        inputAttrs={{
          className: classes(p.ctx.formControlClass, "sf-entity-autocomplete", c.mandatoryClass),
          placeholder: EntityControlMessage.Add.niceToString(),
          onPaste: p.paste == false ? undefined : handleOnPaste,
          ...p.inputAttributes
        }}
        getItems={q => ac!.getItems(q)}
        itemsDelay={ac.getItemsDelay()}
        renderItem={(e, hl) => ac!.renderItem(e, hl)}
        itemAttrs={item => ({ 'data-entity-key': ac!.getDataKeyFromItem(item) }) as React.HTMLAttributes<HTMLButtonElement>}
        onSelect={c.handleOnSelect}
        renderInput={renderInput}
      />
    );
  }
}

export interface EntityStripElementProps<V extends ModifiableEntity | Lite<Entity>> {
  iconStart?: boolean;
  onRemove?: (event: React.MouseEvent<any>) => void;
  onView?: (event: React.MouseEvent<any>) => void;
  ctx: TypeContext<V>;
  autoComplete?: AutocompleteConfig<unknown> | null;
  onRenderItem?: (item: V) => React.ReactNode;
  onItemHtmlAttributes?: (item: V) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  onItemContainerHtmlAttributes?: (item: V) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  drag?: DragConfig;
  move?: MoveConfig;
  showType: boolean;
  vertical?: boolean;
}

export function EntityStripElement<V extends ModifiableEntity | Lite<Entity>>(p: EntityStripElementProps<V>): React.ReactElement {
  var currentEntityRef = React.useRef<{ entity: ModifiableEntity | Lite<Entity>, item?: unknown } | undefined>(undefined);
  const forceUpdate = useForceUpdate();

  if (p.ctx.binding == null && p.ctx.previousVersion) {
    return (
      <li className="sf-strip-element" >
        <div style={{ padding: 0, minHeight: p.vertical ? 10 : 12, minWidth: p.vertical ? undefined : 30, backgroundColor: "#ff000021" }}>
          {p.vertical ? getTimeMachineIcon({ ctx: p.ctx, translateX: "-90%", translateY: "-10%" }) : getTimeMachineIcon({ ctx: p.ctx, translateX: "-80%", translateY: "-60%" })}
        </div>
      </li>
    );
  }
    

  React.useEffect(() => {

    if (p.autoComplete) {
      var newEntity = p.ctx.value;
      if (!currentEntityRef.current || currentEntityRef.current.entity !== newEntity) {
        var ci = { entity: newEntity!, item: undefined as unknown }
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
            });
        };
        fillItem(newEntity);
      }
    }

  }, [p.ctx.value]);

  const toStr =
    p.onRenderItem ? p.onRenderItem(p.ctx.value) :
      currentEntityRef.current?.item ? p.autoComplete!.renderItem(currentEntityRef.current.item, new TextHighlighter(undefined)) :
        getToStr();

  function getToStr() {
    const toStr = getToString(p.ctx.value);
    return !p.showType || !(isEntity(p.ctx.value) || isLite(p.ctx.value)) ? toStr :
      <span style={{ wordBreak: "break-all" }} title={toStr}>
        {toStr}<TypeBadge entity={p.ctx.value} />
      </span>;
  }


  var drag = p.drag;
  const htmlAttributes = p.onItemHtmlAttributes && p.onItemHtmlAttributes(p.ctx.value);

  var val = p.ctx.value;

  //Till https://github.com/facebook/react/issues/8529 gets fixed
  var url = isEntity(val) && !val.isNew ? Navigator.navigateRoute(val) :
    isLite(val) && !(val.entity && val.entity.isNew) ? Navigator.navigateRoute(val) : "#";

  var hasIcon = p.onRemove || p.drag || p.move;

  var containerHtmlAttributes = (p.onItemContainerHtmlAttributes && p.onItemContainerHtmlAttributes(p.ctx.value));

  return (
    <li className={classes("sf-strip-element", containerHtmlAttributes?.className, drag?.dropClass)}
      {...EntityBaseController.entityHtmlAttributes(p.ctx.value)}
      {...containerHtmlAttributes}>
      <div className="sf-strip-dropable"
        onDragEnter={drag?.onDragOver}
        onDragOver={drag?.onDragOver}
        onDrop={drag?.onDrop}
      >
        {p.vertical ? getTimeMachineIcon({ ctx: p.ctx, translateX: "-90%", translateY: "20%" }) : getTimeMachineIcon({ ctx: p.ctx, translateX: "-75%", translateY: "-50%" })}
        {hasIcon && p.iconStart && <span style={{ marginRight: "5px", whiteSpace: "nowrap" }}>{removeIcon()}&nbsp;{dragIcon()}{p.move?.renderMoveUp()}{p.move?.renderMoveDown()}</span>}
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
        {hasIcon && !p.iconStart && <span>{removeIcon()}&nbsp;{dragIcon()}{p.move?.renderMoveUp()}{p.move?.renderMoveDown()}</span>}
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
          {EntityBaseController.getRemoveIcon()}
        </a>
      </span>
  }

  function dragIcon() {
    return drag && <span className={classes("sf-line-button", "sf-move")} onClick={e => { e.preventDefault(); e.stopPropagation(); }}
      draggable={true}
      onDragStart={drag.onDragStart}
      onDragEnd={drag.onDragEnd}
      onKeyDown={drag.onKeyDown}
      title={drag.title}>
      {EntityBaseController.getMoveIcon()}
    </span>;
  }
}

//tasks.push(taskSetAvoidDuplicates);
//export function taskSetAvoidDuplicates(lineBase: LineBaseController<any>, state: LineBaseProps): React.ReactElement {
//  if (lineBase instanceof EntityStripController &&
//    (state as EntityStripProps).avoidDuplicates == undefined &&
//    state.ctx.propertyRoute &&
//    state.ctx.propertyRoute.propertyRouteType == "Field" &&
//    state.ctx.propertyRoute.member!.avoidDuplicates) {
//    (state as EntityStripProps).avoidDuplicates = true;
//  }
//}
