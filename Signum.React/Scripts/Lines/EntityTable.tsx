import * as React from 'react'
import { classes, Dic, DomUtils } from '../Globals'
import { TypeContext } from '../TypeContext'
import * as Navigator from '../Navigator'
import { ModifiableEntity, MList, EntityControlMessage, newMListElement, Entity, Lite, is } from '../Signum.Entities'
import { EntityBase, TitleManager } from './EntityBase'
import { EntityListBase, EntityListBaseProps, DragConfig } from './EntityListBase'
import DynamicComponent from './DynamicComponent'
import { MaxHeightProperty } from 'csstype';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export interface EntityTableProps extends EntityListBaseProps {
  createAsLink?: boolean | ((er: EntityTable) => React.ReactElement<any>);
  /**Consider using EntityTable.typedColumns to get Autocompletion**/
  columns?: EntityTableColumn<any /*T*/, any>[],
  fetchRowState?: (ctx: TypeContext<any /*T*/>, row: EntityTableRow) => Promise<any>;
  onRowHtmlAttributes?: (ctx: TypeContext<any /*T*/>, row: EntityTableRow, rowState: any) => React.HTMLAttributes<any> | null | undefined;
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
  cellHtmlAttributes?: (ctx: TypeContext<T>, row: EntityTableRow, rowState: RS) => React.TdHTMLAttributes<any> | null | undefined;
  template?: (ctx: TypeContext<T>, row: EntityTableRow, rowState: RS) => React.ReactChild | null | undefined | false;
}

export class EntityTable extends EntityListBase<EntityTableProps, EntityTableProps> {

  static defaultProps = {
    maxResultsHeight: "400px",
    scrollable: false
  };

  static typedColumns<T extends ModifiableEntity>(columns: (EntityTableColumn<T, any> | false | null | undefined)[]): EntityTableColumn<ModifiableEntity, any>[] {
    return columns.filter(a => a != null && a != false) as EntityTableColumn<ModifiableEntity, any>[];
  }

  static typedColumnsWithRowState<T extends ModifiableEntity, RS>(columns: (EntityTableColumn<T, RS> | false | null | undefined)[]): EntityTableColumn<ModifiableEntity, RS>[] {
    return columns.filter(a => a != null && a != false) as EntityTableColumn<ModifiableEntity, RS>[];
  }

  calculateDefaultState(state: EntityTableProps) {
    super.calculateDefaultState(state);
    state.viewOnCreate = false;
    state.view = false;
    state.createAsLink = true;
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


  renderInternal() {

    if (this.state.type!.isLite)
      throw new Error("Lite not supported");

    let ctx = (this.state.ctx as TypeContext<MList<ModifiableEntity>>).subCtx({ formGroupStyle: "SrOnly" });

    if (this.props.avoidFieldSet == true)
      return (
        <div className={classes("SF-table-field SF-control-container", ctx.errorClassBorder)} {...this.baseHtmlAttributes()} {...this.state.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
          {this.renderButtons()}
          {this.renderTable(ctx)}
        </div>
      );

    return (
      <fieldset className={classes("SF-table-field SF-control-container", ctx.errorClass)} {...this.baseHtmlAttributes()} {...this.state.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
        <legend>
          <div>
            <span>{this.state.labelText}</span>
            {this.renderButtons()}
          </div>
        </legend>
        {this.renderTable(ctx)}
      </fieldset>
    );
  }

  renderButtons() {
    const buttons = (
      <span className="ml-2">
        {this.state.createAsLink == false && this.renderCreateButton(false, this.props.createMessage)}
        {this.renderFindButton(false)}
      </span>
    );

    return (EntityBase.hasChildrens(buttons) ? buttons : undefined);
  }

  componentDidMount() {
    this.containerDiv!.addEventListener("scroll", (e) => {
      var translate = "translate(0," + this.containerDiv!.scrollTop + "px)";
      this.thead!.style.transform = translate;
    });
  }


  containerDiv?: HTMLDivElement | null;
  thead?: HTMLTableSectionElement | null;
  tfoot?: HTMLTableSectionElement | null;

  recentlyCreated?: Lite<Entity> | ModifiableEntity | null;
  handleBlur = (sender: EntityTableRow, e: React.FocusEvent<HTMLTableRowElement>) => {

    var tr = DomUtils.closest(e.target, "tr")!;

    if (tr == DomUtils.closest(e.relatedTarget as HTMLElement, "tr")) {
      if (this.recentlyCreated && sender.props.ctx.value == this.recentlyCreated)
        this.recentlyCreated = null;

      return;
    }

    if (this.recentlyCreated && sender.props.ctx.value == this.recentlyCreated) {
      this.props.ctx.value.extract(a => a.element == this.recentlyCreated);
      this.setValue(this.props.ctx.value);

      return;
    }

    var last = this.props.ctx.value.last();

    if (sender.props.ctx.value == last.element && DomUtils.closest(e.relatedTarget as HTMLElement, "tfoot") == this.tfoot) {
      var focusable = Array.from(tr.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'))
        .filter(e => {
          var html = e as HTMLInputElement;
          return html.tabIndex >= 0 && html.disabled != true;
        });

      if (focusable.last() == e.target) {
        var pr = this.state.ctx.propertyRoute.addLambda(a => a[0]);
        const promise = this.props.onCreate ? this.props.onCreate(pr) : this.defaultCreate(pr);
        if (promise == null)
          return;

        promise.then(entity => {

          if (!entity)
            return;

          this.recentlyCreated = entity;
          this.addElement(entity);
        }).done();
      }
    }
  }

  renderTable(ctx: TypeContext<MList<ModifiableEntity>>) {

    const readOnly = ctx.readOnly;
    const elementPr = ctx.propertyRoute.addLambda(a => a[0].element);

    var isEmpty = this.props.avoidEmptyTable && ctx.value.length == 0;

    return (
      <div ref={d => this.containerDiv = d}
        className={this.props.scrollable ? "sf-scroll-table-container table-responsive" : undefined}
        style={{ maxHeight: this.props.scrollable ? this.props.maxResultsHeight : undefined }}>
        <table className={classes("table table-sm sf-table", this.props.tableClasses)} >
          {
            !isEmpty &&
            <thead ref={th => this.thead = th}>
              <tr className={this.props.theadClasses || "bg-light"}>
                <th></th>
                {
                  this.state.columns!.map((c, i) => <th key={i} {...c.headerHtmlAttributes}>
                    {c.header === undefined && c.property ? elementPr.addLambda(c.property).member!.niceName : c.header}
                  </th>)
                }
              </tr>
            </thead>
          }
          <tbody>
            {
              this.getMListItemContext(ctx)
                .map(mlec => <EntityTableRow key={this.keyGenerator.getKey(mlec.value)}
                  index={mlec.index!}
                  onRowHtmlAttributes={this.props.onRowHtmlAttributes}
                  fetchRowState={this.props.fetchRowState}
                  onRemove={this.canRemove(mlec.value) && !readOnly ? e => this.handleRemoveElementClick(e, mlec.index!) : undefined}
                  onView={this.canView(mlec.value) && !readOnly ? e => this.handleViewElement(e, mlec.index!) : undefined}
                  draggable={this.canMove(mlec.value) && !readOnly ? this.getDragConfig(mlec.index!, "v") : undefined}
                  columns={this.state.columns!}
                  ctx={this.props.rowSubContext ? this.props.rowSubContext(mlec) : mlec}
                  onBlur={this.props.createOnBlurLastRow && this.state.create && !readOnly? this.handleBlur : undefined}
                />
                )
            }
          </tbody>
          {
            this.state.createAsLink && this.state.create && !readOnly &&
            <tfoot ref={tf => this.tfoot = tf}>
              <tr>
                <td colSpan={1 + this.state.columns!.length} className={isEmpty ? "border-0" : undefined}>
                  {typeof this.state.createAsLink == "function" ? this.state.createAsLink(this) :
                    <a href="#" title={TitleManager.useTitle ? EntityControlMessage.Create.niceToString() : undefined}
                      className="sf-line-button sf-create"
                      onClick={this.handleCreateClick}>
                      <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{this.props.createMessage || EntityControlMessage.Create.niceToString()}
                    </a>}
                </td>
              </tr>
            </tfoot>
          }
        </table>
      </div>);
  }


  handleViewElement = (event: React.MouseEvent<any>, index: number) => {

    event.preventDefault();

    const ctx = this.state.ctx;
    const list = ctx.value!;
    const mle = list[index];
    const entity = mle.element;

    const openWindow = (event.button == 1 || event.ctrlKey) && !this.state.type!.isEmbedded;
    if (openWindow) {
      event.preventDefault();
      const route = Navigator.navigateRoute(entity as Lite<Entity> /*or Entity*/);
      window.open(route);
    }
    else {
      const pr = ctx.propertyRoute.addLambda(a => a[0]);

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


export interface EntityTableRowProps {
  ctx: TypeContext<ModifiableEntity>;
  index: number;
  columns: EntityTableColumn<ModifiableEntity, any>[],
  onRemove?: (event: React.MouseEvent<any>) => void;
  onView?: (event: React.MouseEvent<any>) => void;
  draggable?: DragConfig;
  fetchRowState?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRow) => Promise<any>;
  onRowHtmlAttributes?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRow, rowState: any) => React.HTMLAttributes<any> | null | undefined;
  onBlur?: (sender: EntityTableRow, e: React.FocusEvent<HTMLTableRowElement>) => void;
}

export class EntityTableRow extends React.Component<EntityTableRowProps, { rowState?: any }> {

  constructor(props: EntityTableRowProps) {
    super(props);

    this.state = {};

    if (props.fetchRowState)
      props.fetchRowState(this.props.ctx, this)
        .then(val => this.setState({ rowState: val }))
        .done();
  }
  
  render() {
    var ctx = this.props.ctx;
    var rowAtts = this.props.onRowHtmlAttributes && this.props.onRowHtmlAttributes(ctx, this, this.state.rowState);
    const drag = this.props.draggable;
    return (
      <tr style={{ backgroundColor: rowAtts && rowAtts.style && rowAtts.style.backgroundColor || undefined }}
        onDragEnter={drag && drag.onDragOver}
        onDragOver={drag && drag.onDragOver}
        onDrop={drag && drag.onDrop}
        className={drag && drag.dropClass}
        onBlur={this.props.onBlur && (e => this.props.onBlur!(this, e))}>
        <td>
          <div className="item-group">
            {this.props.onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
              onClick={this.props.onRemove}
              title={TitleManager.useTitle ? EntityControlMessage.Remove.niceToString() : undefined}>
              {EntityBase.removeIcon}
            </a>}
            &nbsp;
          {drag && <a href="#" className={classes("sf-line-button", "sf-move")}
              draggable={true}
              onDragStart={drag.onDragStart}
              onDragEnd={drag.onDragEnd}
              title={TitleManager.useTitle ? EntityControlMessage.Move.niceToString() : undefined}>
              {EntityBase.moveIcon}
            </a>}
            {this.props.onView && <a href="#" className={classes("sf-line-button", "sf-view")}
              onClick={this.props.onView}
              title={TitleManager.useTitle ? EntityControlMessage.View.niceToString() : undefined}>
              {EntityBase.viewIcon}
            </a>}
          </div>
        </td>
        {this.props.columns.map((c, i) => <td key={i} {...c.cellHtmlAttributes && c.cellHtmlAttributes(ctx, this, this.state.rowState)}>{this.getTemplate(c)}</td>)}
      </tr>
    );
  }


  getTemplate(col: EntityTableColumn<ModifiableEntity, any>): React.ReactChild | undefined | null | false {

    if (col.template === null)
      return null;

    if (col.template !== undefined)
      return col.template(this.props.ctx, this, this.state.rowState);

    if (col.property == null)
      throw new Error("Column has no property and no template");

    if (typeof col.property == "string")
      return DynamicComponent.getAppropiateComponent(this.props.ctx.subCtx(col.property)); /*string overload*/
    else
      return DynamicComponent.getAppropiateComponent(this.props.ctx.subCtx(col.property)); /*lambda overload*/

  }
}
