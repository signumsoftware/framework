import * as React from 'react'
import { classes, Dic, DomUtils } from '../Globals'
import { TypeContext } from '../TypeContext'
import * as Navigator from '../Navigator'
import { ModifiableEntity, MList, EntityControlMessage, newMListElement, Entity, Lite, is } from '../Signum.Entities'
import { EntityBaseController } from './EntityBase'
import { EntityListBaseController, EntityListBaseProps, DragConfig } from './EntityListBase'
import DynamicComponent, { getAppropiateComponent } from './DynamicComponent'
import { MaxHeightProperty } from 'csstype';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useAPI, useForceUpdate } from '../Hooks'

export interface EntityTableProps extends EntityListBaseProps {
  createAsLink?: boolean | ((er: EntityTableController) => React.ReactElement<any>);
  /**Consider using EntityTable.typedColumns to get Autocompletion**/
  columns?: EntityTableColumn<any /*T*/, any>[],
  fetchRowState?: (ctx: TypeContext<any /*T*/>, row: EntityTableRowHandle) => Promise<any>;
  onRowHtmlAttributes?: (ctx: TypeContext<any /*T*/>, row: EntityTableRowHandle, rowState: any) => React.HTMLAttributes<any> | null | undefined;
  avoidFieldSet?: boolean;
  avoidEmptyTable?: boolean;
  maxResultsHeight?: MaxHeightProperty<string | number> | any;
  scrollable?: boolean;
  rowSubContext?: (ctx: TypeContext<any /*T*/>) => TypeContext<any>;
  tableClasses?: string;
  theadClasses?: string;
  createMessage?: string;
  createOnBlurLastRow?: boolean;
}

export interface EntityTableColumn<T, RS> {
  property?: ((a: T) => any) | string;
  header?: React.ReactNode | null;
  headerHtmlAttributes?: React.ThHTMLAttributes<any>;
  cellHtmlAttributes?: (ctx: TypeContext<T>, row: EntityTableRowHandle, rowState: RS) => React.TdHTMLAttributes<any> | null | undefined;
  template?: (ctx: TypeContext<T>, row: EntityTableRowHandle, rowState: RS) => React.ReactChild | null | undefined | false;
}

export class EntityTableController extends EntityListBaseController<EntityTableProps> {
  containerDiv: React.RefObject<HTMLDivElement>;
  thead: React.RefObject<HTMLTableSectionElement>;
  tfoot: React.RefObject<HTMLTableSectionElement>;
  recentlyCreated: React.MutableRefObject<Lite<Entity> | ModifiableEntity | null>;


  constructor(p: EntityTableProps) {
    super(p);
    this.containerDiv = React.useRef<HTMLDivElement>(null);
    this.thead = React.useRef<HTMLTableSectionElement>(null);
    this.tfoot = React.useRef<HTMLTableSectionElement>(null);
    this.recentlyCreated = React.useRef<Lite<Entity> | ModifiableEntity | null>(null);

    React.useEffect(() => {
      this.containerDiv.current!.addEventListener("scroll", (e) => {
        var translate = "translate(0," + this.containerDiv.current!.scrollTop + "px)";
        this.thead.current!.style.transform = translate;
      });
    }, []);
  }

  getDefaultProps(p: EntityTableProps) {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.view = false;
    p.createAsLink = true;
  }

  overrideProps(state: EntityTableProps, overridenProps: EntityTableProps) {
    super.overrideProps(state, overridenProps);

    if (!state.columns) {
      var elementPr = state.ctx.propertyRoute.addLambda(a => a[0].element);

      state.columns = Dic.getKeys(elementPr.subMembers())
        .filter(a => a != "Id")
        .map(memberName => ({
          property: eval("(function(e){ return e." + memberName.firstLower() + "; })")
        }) as EntityTableColumn<ModifiableEntity, any>);
    }
  }

  handleBlur = (sender: EntityTableRowHandle, e: React.FocusEvent<HTMLTableRowElement>) => {

    const p = this.props;
    var tr = DomUtils.closest(e.target, "tr")!;

    if (tr == DomUtils.closest(e.relatedTarget as HTMLElement, "tr")) {
      if (this.recentlyCreated.current && sender.props.ctx.value == this.recentlyCreated.current)
        this.recentlyCreated.current = null;

      return;
    }

    if (this.recentlyCreated && sender.props.ctx.value == this.recentlyCreated.current) {
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
        var pr = this.props.ctx.propertyRoute.addLambda(a => a[0]);
        const promise = p.onCreate ? p.onCreate(pr) : this.defaultCreate(pr);
        if (promise == null)
          return;

        promise.then(entity => {
          if (!entity)
            return;

          this.recentlyCreated.current = entity;
          this.addElement(entity);
        }).done();
      }
    }
  }




  handleViewElement = (event: React.MouseEvent<any>, index: number) => {

    event.preventDefault();

    const p = this.props;
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

export function EntityTable(props: EntityTableProps) {
  const c = new EntityTableController(props);
  const p = c.props;

  if (p.type!.isLite)
    throw new Error("Lite not supported");

  if (c.isHidden)
    return null;

  let ctx = (p.ctx as TypeContext<MList<ModifiableEntity>>).subCtx({ formGroupStyle: "SrOnly" });

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("SF-table-field SF-control-container", ctx.errorClassBorder)} {...c.baseHtmlAttributes()} {...p.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
        {renderButtons()}
        {renderTable()}
      </div>
    );

  return (
    <fieldset className={classes("SF-table-field SF-control-container", ctx.errorClass)} {...c.baseHtmlAttributes()} {...p.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
      <legend>
        <div>
          <span>{p.labelText}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderTable()}
    </fieldset>
  );

  function renderButtons() {
    const buttons = (
      <span className="ml-2">
        {p.createAsLink == false && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
      </span>
    );

    return (EntityBaseController.hasChildrens(buttons) ? buttons : undefined);
  }

  function renderTable() {

    const readOnly = ctx.readOnly;
    const elementPr = ctx.propertyRoute.addLambda(a => a[0].element);

    var isEmpty = p.avoidEmptyTable && ctx.value.length == 0;

    return (
      <div ref={c.containerDiv}
        className={p.scrollable ? "sf-scroll-table-container table-responsive" : undefined}
        style={{ maxHeight: p.scrollable ? p.maxResultsHeight : undefined }}>
        <table className={classes("table table-sm sf-table", p.tableClasses)} >
          {
            !isEmpty &&
            <thead ref={c.thead}>
              <tr className={p.theadClasses || "bg-light"}>
                <th></th>
                {
                  p.columns!.map((c, i) => <th key={i} {...c.headerHtmlAttributes}>
                    {c.header === undefined && c.property ? elementPr.addLambda(c.property).member!.niceName : c.header}
                  </th>)
                }
              </tr>
            </thead>
          }
          <tbody>
            {
              c.getMListItemContext(ctx)
                .map(mlec => <EntityTableRow key={c.keyGenerator.getKey(mlec.value)}
                  index={mlec.index!}
                  onRowHtmlAttributes={p.onRowHtmlAttributes}
                  fetchRowState={p.fetchRowState}
                  onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
                  onView={c.canView(mlec.value) && !readOnly ? e => c.handleViewElement(e, mlec.index!) : undefined}
                  draggable={c.canMove(mlec.value) && !readOnly ? c.getDragConfig(mlec.index!, "v") : undefined}
                  columns={p.columns!}
                  ctx={p.rowSubContext ? p.rowSubContext(mlec) : mlec}
                  onBlur={p.createOnBlurLastRow && p.create && !readOnly ? c.handleBlur : undefined}
                />
                )
            }
          </tbody>
          {
            p.createAsLink && p.create && !readOnly &&
            <tfoot ref={c.tfoot}>
              <tr>
                <td colSpan={1 + p.columns!.length} className={isEmpty ? "border-0" : undefined}>
                  {typeof p.createAsLink == "function" ? p.createAsLink(c) :
                    <a href="#" title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}
                      className="sf-line-button sf-create"
                      onClick={c.handleCreateClick}>
                      <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{p.createMessage || EntityControlMessage.Create.niceToString()}
                    </a>}
                </td>
              </tr>
            </tfoot>
          }
        </table>
      </div >
    );
  }
}

export namespace EntityTable {
  export const defaultProps = {
    maxResultsHeight: "400px",
    scrollable: false
  }

  export function typedColumns<T extends ModifiableEntity>(columns: (EntityTableColumn<T, any> | false | null | undefined)[]): EntityTableColumn<ModifiableEntity, any>[] {
    return columns.filter(a => a != null && a != false) as EntityTableColumn<ModifiableEntity, any>[];
  }

  export function typedColumnsWithRowState<T extends ModifiableEntity, RS>(columns: (EntityTableColumn<T, RS> | false | null | undefined)[]): EntityTableColumn<ModifiableEntity, RS>[] {
    return columns.filter(a => a != null && a != false) as EntityTableColumn<ModifiableEntity, RS>[];
  }
}

export interface EntityTableRowProps {
  ctx: TypeContext<ModifiableEntity>;
  index: number;
  columns: EntityTableColumn<ModifiableEntity, any>[],
  onRemove?: (event: React.MouseEvent<any>) => void;
  onView?: (event: React.MouseEvent<any>) => void;
  draggable?: DragConfig;
  fetchRowState?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRowHandle) => Promise<any>;
  onRowHtmlAttributes?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRowHandle, rowState: any) => React.HTMLAttributes<any> | null | undefined;
  onBlur?: (sender: EntityTableRowHandle, e: React.FocusEvent<HTMLTableRowElement>) => void;
}


export interface EntityTableRowHandle {
  props: EntityTableRowProps;
  rowState?: any;
  forceUpdate() : void;
}

export function EntityTableRow(p: EntityTableRowProps) {
  const forceUpdate = useForceUpdate();

  const rowState = useAPI(undefined,
    (signal, oldState) => !p.fetchRowState ? Promise.resolve(undefined) : p.fetchRowState(p.ctx, { props: p, rowState: oldState, forceUpdate }),
    []);

  const rowHandle = { props: p, rowState, forceUpdate };

  var ctx = p.ctx;
  var rowAtts = p.onRowHtmlAttributes && p.onRowHtmlAttributes(ctx, rowHandle, rowState);
  const drag = p.draggable;
  return (
    <tr style={{ backgroundColor: rowAtts && rowAtts.style && rowAtts.style.backgroundColor || undefined }}
      onDragEnter={drag && drag.onDragOver}
      onDragOver={drag && drag.onDragOver}
      onDrop={drag && drag.onDrop}
      className={drag && drag.dropClass}
      onBlur={p.onBlur && (e => p.onBlur!(rowHandle, e))}>
      <td>
        <div className="item-group">
          {p.onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={p.onRemove}
            title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
            {EntityBaseController.removeIcon}
          </a>}
          &nbsp;
          {drag && <a href="#" className={classes("sf-line-button", "sf-move")}
            draggable={true}
            onDragStart={drag.onDragStart}
            onDragEnd={drag.onDragEnd}
            title={ctx.titleLabels ? EntityControlMessage.Move.niceToString() : undefined}>
            {EntityBaseController.moveIcon}
          </a>}
          {p.onView && <a href="#" className={classes("sf-line-button", "sf-view")}
            onClick={p.onView}
            title={ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
            {EntityBaseController.viewIcon}
          </a>}
        </div>
      </td>
      {p.columns.map((c, i) => <td key={i} {...c.cellHtmlAttributes && c.cellHtmlAttributes(ctx, rowHandle, rowState)}>{getTemplate(c)}</td>)}
    </tr>
  );

  function getTemplate(col: EntityTableColumn<ModifiableEntity, any>): React.ReactChild | undefined | null | false {

    if (col.template === null)
      return null;

    if (col.template !== undefined)
      return col.template(p.ctx, rowHandle, rowState);

    if (col.property == null)
      throw new Error("Column has no property and no template");

    if (typeof col.property == "string")
      return getAppropiateComponent(p.ctx.subCtx(col.property)); /*string overload*/
    else
      return getAppropiateComponent(p.ctx.subCtx(col.property)); /*lambda overload*/

  }
}
