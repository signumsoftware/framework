import * as React from 'react'
import { classes, Dic } from '../Globals'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { ModifiableEntity, MList, EntityControlMessage } from '../Signum.Entities'
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
  isRowVisible?: (ctx: TypeContext<any /*T*/>) => boolean;
  rowSubContext?: (ctx: TypeContext<any /*T*/>) => TypeContext<any>;
  tableClasses?: string;
  theadClasses?: string;
  createMessage?: string;
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
              mlistItemContext(ctx)
                .map((mlec, i) => ({ mlec, i }))
                .filter(a => this.props.isRowVisible == null || this.props.isRowVisible(a.mlec))
                .map(a => <EntityTableRow key={a.i}
                  index={a.i}
                  onRowHtmlAttributes={this.props.onRowHtmlAttributes}
                  fetchRowState={this.props.fetchRowState}
                  onRemove={this.canRemove(a.mlec.value) && !readOnly ? e => this.handleRemoveElementClick(e, a.i) : undefined}
                  draggable={this.canMove(a.mlec.value) && !readOnly ? this.getDragConfig(a.i, "v") : undefined}
                  columns={this.state.columns!}
                  ctx={this.props.rowSubContext ? this.props.rowSubContext(a.mlec) : a.mlec} />)
            }
          </tbody>
          {
            this.state.createAsLink && this.state.create && !readOnly &&
            <tfoot>
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
}


export interface EntityTableRowProps {
  ctx: TypeContext<ModifiableEntity>;
  index: number;
  columns: EntityTableColumn<ModifiableEntity, any>[],
  onRemove?: (event: React.MouseEvent<any>) => void;
  draggable?: DragConfig;
  fetchRowState?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRow) => Promise<any>;
  onRowHtmlAttributes?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRow, rowState: any) => React.HTMLAttributes<any> | null | undefined;
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
        className={drag && drag.dropClass}>
        <td>
          <div className="item-group">
            {this.props.onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
              onClick={this.props.onRemove}
              title={TitleManager.useTitle ? EntityControlMessage.Remove.niceToString() : undefined}>
              <FontAwesomeIcon icon="times" />
            </a>}
            &nbsp;
                        {drag && <a href="#" className={classes("sf-line-button", "sf-move")}
              draggable={true}
              onDragStart={drag.onDragStart}
              onDragEnd={drag.onDragEnd}
              title={TitleManager.useTitle ? EntityControlMessage.Move.niceToString() : undefined}>
              <FontAwesomeIcon icon="bars" />
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
