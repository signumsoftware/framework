import * as React from 'react'
import { classes, Dic, DomUtils, KeyGenerator } from '../Globals'
import { TypeContext } from '../TypeContext'
import { Navigator } from '../Navigator'
import { ModifiableEntity, MList, EntityControlMessage, newMListElement, Entity, Lite, is } from '../Signum.Entities'
import { EntityBaseController } from './EntityBase'
import { EntityListBaseController, EntityListBaseProps, DragConfig, MoveConfig } from './EntityListBase'
import { Property } from 'csstype';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Breakpoints, getBreakpoint, useAPI, useBreakpoint, useForceUpdate } from '../Hooks'
import { useController } from './LineBase'
import { KeyNames } from '../Components'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { GroupHeader, HeaderType } from './GroupHeader'
import { AutoLine } from './AutoLine'


export interface EntityTableProps<V extends ModifiableEntity, RS> extends EntityListBaseProps<V> {
  createAsLink?: boolean | ((er: EntityTableController<V, RS>) => React.ReactElement);
  firstColumnHtmlAttributes?: React.ThHTMLAttributes<any>;
  rowHooks?: (ctx: TypeContext<NoInfer<V>>, row: EntityTableRowHandle<V, unknown>) => RS;
  columns?: (EntityTableColumn<V, NoInfer<RS>> | false | null | undefined)[],
  onRowHtmlAttributes?: (ctx: TypeContext<NoInfer<V>>, row: EntityTableRowHandle<V, NoInfer<RS>>, rowState: any) => React.HTMLAttributes<any> | null | undefined;
  avoidFieldSet?: boolean | HeaderType;
  avoidEmptyTable?: boolean;
  maxResultsHeight?: Property.MaxHeight<string | number> | any;
  scrollable?: boolean;
  rowSubContext?: NoInfer<(ctx: TypeContext<V>) => TypeContext<V>>;
  tableClasses?: string;
  theadClasses?: string;
  createMessage?: string;
  createOnBlurLastRow?: boolean;
  responsive?: boolean;
  customKey?: (entity: V) => string | undefined; 
  afterView?: (ctx: TypeContext<NoInfer<V>>, row: EntityTableRowHandle<V, NoInfer<RS>>, rowState: NoInfer<RS>) => React.ReactElement | boolean | null | undefined;
  afterRow?: (ctx: TypeContext<NoInfer<V>>, row: EntityTableRowHandle<V, NoInfer<RS>>, rowState: NoInfer<RS>) => React.ReactElement | boolean | null | undefined;
  ref?: React.Ref<EntityTableController<V, RS>>;
}

export interface EntityTableColumn<V extends ModifiableEntity, RS> {
  property?: ((a: V) => unknown) | string;
  header?: React.ReactNode | null;
  headerHtmlAttributes?: React.ThHTMLAttributes<any>;
  cellHtmlAttributes?: (ctx: TypeContext<V>, row: EntityTableRowHandle<V, RS>, rowState: RS) => React.TdHTMLAttributes<any> | null | undefined;
  template?: (ctx: TypeContext<V>, row: EntityTableRowHandle<V, RS>, rowState: RS) => React.ReactElement | string | number | null | undefined | false;
  mergeCells?: (boolean | ((a: V) => any) | string);
  footer?: React.ReactNode | null;
  footerHtmlAttributes?: React.ThHTMLAttributes<any>;
}



export class EntityTableController<V extends ModifiableEntity, RS> extends EntityListBaseController<EntityTableProps<V, RS>, V> {
  containerDiv!: React.RefObject<HTMLDivElement |  null>;
  thead!: React.RefObject<HTMLTableSectionElement | null>;
  tfoot!: React.RefObject<HTMLTableSectionElement | null>;
  recentlyCreated!: React.RefObject<Lite<Entity> | ModifiableEntity | null>;

  init(p: EntityTableProps<V, RS>): void {
    super.init(p);
    this.containerDiv = React.useRef<HTMLDivElement>(null);
    this.thead = React.useRef<HTMLTableSectionElement>(null);
    this.tfoot = React.useRef<HTMLTableSectionElement>(null);
    this.recentlyCreated = React.useRef<Lite<Entity> | ModifiableEntity | null>(null);

    React.useEffect(() => {
      this.containerDiv.current && this.containerDiv.current.addEventListener("scroll", (e) => {
        var translate = "translate(0," + this.containerDiv.current!.scrollTop + "px)";
        this.thead.current!.style.transform = translate;
      });
    }, []);
  }

  getDefaultProps(p: EntityTableProps<V, RS>): void {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.view = false;
    p.createAsLink = true;
  }

  overrideProps(state: EntityTableProps<V, RS>, overridenProps: EntityTableProps<V, RS>): void {
    super.overrideProps(state, overridenProps);

    if (state.ctx.propertyRoute) {
      var pr = state.ctx.propertyRoute!.addMember("Indexer", "", true)!;

      if (!state.columns) {
        var elementPr = state.ctx.propertyRoute!.addLambda(a => a[0].element);

        state.columns = Dic.getKeys(elementPr.subMembers())
          .filter(a => a != "Id" && !a.startsWith("["))
          .map(memberName => ({
            property: eval("(function(e){ return e." + memberName.firstLower() + "; })")
          }) as EntityTableColumn<V, RS>);
      }
      else {
        state.columns = state.columns.filter(c => c && (c.property == null ||
          (c.property == "string" ? pr.addMember("Member", c.property, false) : pr.tryAddLambda(c.property!)) != null));
      }

      (state.columns as EntityTableColumn<V, RS>[]).forEach(c => {
        if (c.template === undefined) {
          if (c.property == null)
            throw new Error("Column has no property and no template");

          var propertyRoute = c.property == "string" ? pr.addMember("Member", c.property, true) : pr.addLambda(c.property!);
          var factory = AutoLine.getComponentFactory(propertyRoute.typeReference(), propertyRoute);

          c.template = (ctx, row, state): React.ReactElement<any, string | React.JSXElementConstructor<any>> => {
            var subCtx = typeof c.property == "string" ? ctx.subCtx(c.property) : ctx.subCtx(c.property!);
            return factory({ ctx: subCtx });
          };
        }

        if (c.mergeCells == true) {
          if (c.property == null)
            throw new Error("Column has no property but mergeCells is true");

          c.mergeCells = c.property;
        }

        if (typeof c.mergeCells == "string") {
          const prop = c.mergeCells;
          c.mergeCells = a => (a as any)[prop];
        }
      });
    }

    if (state.responsive === undefined) {
      state.responsive = getBreakpoint() <= Breakpoints.sm;
    }
  }

  handleKeyDown = (sender: EntityTableRowHandle<V, RS>, e: React.KeyboardEvent<HTMLTableRowElement>): void => {

    if (e.key != KeyNames.tab) {
      if (this.recentlyCreated.current && sender.props.ctx.value == this.recentlyCreated.current)
        this.recentlyCreated.current = null;

      return;
    }
  }

  handleCreateLastRowBlur = (sender: EntityTableRowHandle<V, RS>, e: React.FocusEvent<HTMLTableRowElement>): void => {
    const p = this.props;
    var tr = DomUtils.closest(e.target, "tr")!;

    if (e.relatedTarget == null || tr == DomUtils.closest(e.relatedTarget as HTMLElement, "tr")) {
      if (this.recentlyCreated.current && sender.props.ctx.value == this.recentlyCreated.current)
        this.recentlyCreated.current = null;

      return;
    }

    if (this.recentlyCreated.current && sender.props.ctx.value == this.recentlyCreated.current) {

      p.ctx.value.extract(a => a.element == this.recentlyCreated.current);
      this.setValue(p.ctx.value);

      return;
    }

    var last = p.ctx.value.last();

    if (sender.props.ctx.value == last.element && DomUtils.closest(e.relatedTarget as HTMLElement, "tfoot") == this.tfoot.current) {
      var focusable = Array.from(tr.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'))
        .filter(e => {
          var html = e as HTMLInputElement;
          return html.tabIndex >= 0 && html.disabled != true;
        });

      if (focusable.last() == e.target) {
        this.createLastRow();
      }
    }
  }

  async createLastRow(): Promise<void> {
    const p = this.props;
    var pr = this.props.ctx.propertyRoute!.addLambda(a => a[0]);
    const entity = p.onCreate ? await p.onCreate(pr) : await this.defaultCreate(pr);

    if (!entity)
      return;

    this.recentlyCreated.current = entity;
    var c = await this.convert(entity);
    this.addElement(c);
  }
}

//interface WithTypeColumns {
//  typedColumns<T extends ModifiableEntity, RS = undefined>(columns: (EntityTableColumn<T, RS> | false | null | undefined)[]): EntityTableColumn<ModifiableEntity, RS>[]
//}

export function EntityTable<V extends ModifiableEntity, RS>(props: EntityTableProps<V, RS>): React.JSX.Element | null {
  const c = useController<EntityTableController<V, RS>, EntityTableProps<V, RS>, MList<V>>(EntityTableController, props);
  const p = c.props;

  if (p.type && p.type.isLite)
    throw new Error("Lite not supported");

  if (c.isHidden)
    return null;

  let ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly" });

  return (
    <GroupHeader className={classes("sf-table-field sf-control-container", c.getErrorClass("border"))}
      label={p.label}
      labelIcon={p.labelIcon}
      avoidFieldSet={p.avoidFieldSet}
      buttons={renderButtons()}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...c.errorAttributes() }}>
      {renderTable()}
    </GroupHeader >
  );

  function renderButtons() {
    const buttons = (
      <span className="ms-2">
        {c.props.extraButtonsBefore && c.props.extraButtonsBefore(c)}
        {p.createAsLink == false && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {c.props.extraButtons && c.props.extraButtons(c)}
      </span>
    );

    return (EntityBaseController.hasChildrens(buttons) ? buttons : undefined);
  }

  function renderTable() {

    const readOnly = ctx.readOnly;
    const elementPr = ctx.propertyRoute!.addLambda(a => a[0].element);

    var elementCtxs = c.getMListItemContext(ctx);
    var isEmpty = p.avoidEmptyTable && elementCtxs.length == 0;
    var firstColumnVisible = !(p.readOnly || p.remove == false && p.move == false && p.view == false);

    var showCreateRow = p.createAsLink && p.create && !readOnly;
    var cleanColumns = p.columns as EntityTableColumn<V, RS>[];
    var hasFooters = cleanColumns.some(a => a.footer != null);

    return (
      <div ref={c.containerDiv}
        className={classes(
          p.scrollable ? "sf-scroll-table-container position-relative" /*Fix chrome double scroll bar (in div and in page)*/ : undefined,
          p.responsive && "table-responsive")}
        style={{ maxHeight: p.scrollable ? p.maxResultsHeight : undefined }}>
        <table className={classes("table table-sm sf-table", p.tableClasses, c.mandatoryClass)} >
          {
            !isEmpty &&
            <thead ref={c.thead}>
              <tr className={p.theadClasses}>
                {firstColumnVisible && <th {...p.firstColumnHtmlAttributes}></th>}
                {
                  cleanColumns.map((c, i) => <th key={i} {...c.headerHtmlAttributes}>
                    {c.header === undefined && c.property ? elementPr.addLambda(c.property).member!.niceName : c.header}
                  </th>)
                }
              </tr>
            </thead>
          }
          <tbody>
            {
              elementCtxs
                .map((mlec, i, array) => <EntityTableRow key={p.customKey?.(mlec.value) ?? c.keyGenerator.getKey(mlec.value)}
                  ctx={p.rowSubContext ? p.rowSubContext(mlec) : mlec}
                  array={array}
                  index={i}
                  firstColumnVisible={firstColumnVisible}
                  onRowHtmlAttributes={p.onRowHtmlAttributes}
                  rowHooks={p.rowHooks}
                  onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
                  onView={c.canView(mlec.value) ? e => c.handleViewElement(e, mlec.index!) : undefined}
                  move={c.canMove(mlec.value) && p.moveMode == "MoveIcons" && !readOnly ? c.getMoveConfig(false, mlec.index!, "v") : undefined}
                  drag={c.canMove(mlec.value) && p.moveMode == "DragIcon" && !readOnly ? c.getDragConfig(mlec.index!, "v") : undefined}
                  columns={cleanColumns}
                  onCreateLastRowBlur={p.createOnBlurLastRow && p.create && !readOnly ? c.handleCreateLastRowBlur : undefined}
                  onKeyDown={p.createOnBlurLastRow && p.create && !readOnly ? c.handleKeyDown : undefined}
                  afterRow={p.afterRow}
                  afterView={p.afterView}
                />
                )
            }
          </tbody>
          {
            (showCreateRow || hasFooters) &&
            <tfoot ref={c.tfoot}>
              {
                showCreateRow && <tr>
                  <td colSpan={1 + p.columns!.length} className={isEmpty ? "border-0" : undefined}>
                      {typeof p.createAsLink == "function" ? p.createAsLink(c) :
                        <a href="#" title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}
                        className="sf-line-button sf-create"
                        onClick={c.handleCreateClick}>
                        <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{p.createMessage ?? EntityControlMessage.Create.niceToString()}
                      </a>}
                  </td>
                </tr>
              }
              {
                hasFooters && <tr>
                    {firstColumnVisible && <td></td>}
                  {cleanColumns.map((c, i) =>
                    <td key={i} {...c.footerHtmlAttributes}>{c.footer}</td>)}
                </tr>
              }
            </tfoot>
          }
        </table>
      </div >
    );
  }
};

(EntityTable as any).defaultProps = {
  maxResultsHeight: "400px",
  scrollable: false
};

export interface EntityTableRowProps<V extends ModifiableEntity, RS> {
  ctx: TypeContext<V>;
  array: TypeContext<V>[];
  index: number;
  firstColumnVisible: boolean;
  columns: EntityTableColumn<V, RS>[],
  onRemove?: (event: React.MouseEvent<any>) => void;
  onView?: (event: React.MouseEvent<any>) => void;
  drag?: DragConfig;
  move?: MoveConfig;
  rowHooks?: (ctx: TypeContext<V>, row: EntityTableRowHandle<V, unknown>) => RS;
  onRowHtmlAttributes?: (ctx: TypeContext<V>, row: EntityTableRowHandle<V, RS>, rowState: RS) => React.HTMLAttributes<any> | null | undefined;
  onCreateLastRowBlur?: (sender: EntityTableRowHandle<V, RS>, e: React.FocusEvent<HTMLTableRowElement>) => void;
  onKeyDown?: (sender: EntityTableRowHandle<V, RS>, e: React.KeyboardEvent<HTMLTableRowElement>) => void;
  afterView?: (ctx: TypeContext<NoInfer<V>>, row: EntityTableRowHandle<V, RS>, rowState: RS) => React.ReactElement | boolean | null | undefined;
  afterRow?: (ctx: TypeContext<NoInfer<V>>, row: EntityTableRowHandle<V, RS>, rowState: RS) => React.ReactElement | boolean | null | undefined;
}

export interface EntityTableRowHandle<V extends ModifiableEntity, RS = unknown> {
  props: EntityTableRowProps<V, RS>;
  rowState?: RS;
  forceUpdate(): void;
}

export function EntityTableRow<V extends ModifiableEntity, RS>(p: EntityTableRowProps<V, RS>): React.ReactElement {
  const forceUpdate = useForceUpdate();

  const rowState = p.rowHooks?.(p.ctx, { props: p as EntityTableRowProps<V, unknown>, forceUpdate })!;

  const rowHandle = { props: p, rowState, forceUpdate } as EntityTableRowHandle<V, RS>;

  var ctx = p.ctx;
  
  if (ctx.binding == null && ctx.previousVersion) {
    return (<tr style={{backgroundColor: "var(--bs-danger-bg-subtle)" }}>
      <td className="align-items-center p-0 ps-1" >{getTimeMachineIcon({ ctx: ctx, isContainer: true })}</td>
      {p.columns.map((c, i) => <td key={i} ></td>)}
    </tr>);
  }
  var rowAtts = p.onRowHtmlAttributes && p.onRowHtmlAttributes(ctx, rowHandle, rowState);
  const drag = p.drag;
  var row =  (
    <tr
      onBlur={p.onCreateLastRowBlur && (e => p.onCreateLastRowBlur!(rowHandle, e))}
      {...rowAtts}
      onDragEnter={drag?.onDragOver}
      onDragOver={drag?.onDragOver}
      onDrop={drag?.onDrop}
      onKeyDown={p.onKeyDown && (e => p.onKeyDown!(rowHandle, e))}
      className={classes(drag?.dropClass, rowAtts?.className)}
    >
      {p.firstColumnVisible && <td style={{ verticalAlign: "middle" }}>
        <div className="item-group">
          {getTimeMachineIcon({ ctx: ctx, isContainer: true })}
          {p.onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={p.onRemove}
            title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
            {EntityBaseController.getRemoveIcon()}
          </a>}
          &nbsp;
          {drag && <a href="#" className={classes("sf-line-button", "sf-move")} onClick={e => { e.preventDefault(); e.stopPropagation(); }}
            draggable={true}
            onKeyDown={drag.onKeyDown}
            onDragStart={drag.onDragStart}
            onDragEnd={drag.onDragEnd}
            title={drag.title}>
            {EntityBaseController.getMoveIcon()}
          </a>}
          {p.move?.renderMoveUp()}
          {p.move?.renderMoveDown()}
          {p.onView && <a href="#" className={classes("sf-line-button", "sf-view")}
            onClick={p.onView}
            title={ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
            {EntityBaseController.getViewIcon()}
          </a>}
          {p.afterView?.(p.ctx, rowHandle, rowState)}
        </div>
      </td>}      
      {p.columns.map((c, i) => {

        var td = <td style={{ verticalAlign: "middle" }} key={i} {...c.cellHtmlAttributes && c.cellHtmlAttributes(ctx, rowHandle, rowState)}>{getTemplate(c)}</td>;

        var mc = c.mergeCells as ((a: any) => any) | undefined

        if (!mc)
          return td;

        var equals = (a: any, b: any) => {
          var ka = mc!(a);
          var kb = mc!(b);
          return ka == kb || is(ka, kb, false, false);
        }

        var current = p.ctx.value;
        if (p.index > 0 && equals(p.array[p.index - 1].value, current))
          return null;

        var rowSpan = 1;
        for (var i = p.index + 1; i < p.array.length; i++) {
          if (equals(p.array[i].value, current))
            rowSpan++;
          else
            break;
        }

        if (rowSpan == 1)
          return td;

        return React.cloneElement(td, { rowSpan });
      })}
    </tr>
  );

  if (!p.afterRow)
    return row;
  else
    return (
      <>
        {row}
        {p.afterRow(p.ctx, rowHandle, rowState)}
      </>
    );


  function getTemplate(col: EntityTableColumn<V, RS>): React.ReactElement | string | number | undefined | null | false {

    if (col.template === null)
      return null;

    if (col.template !== undefined)
      return col.template(p.ctx, rowHandle, rowState);

    if (col.property == null)
      throw new Error("Column has no property and no template");

    if (typeof col.property == "string")
      return <AutoLine ctx={p.ctx.subCtx(col.property)} />; /*string overload*/
    else
      return AutoLine.getComponentFactory(col.property)({ ctx: p.ctx.subCtx(col.property) }); /*lambda overload*/

  }
}
