import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as _fontawesome_svg_core from '@fortawesome/fontawesome-svg-core'; //throwaway reference to avoid error the inferred type cannot be named without a reference -> https://github.com/microsoft/TypeScript/issues/5938
import { classes, getColorContrasColorBWByHex } from '@framework/Globals'
import { TypeContext } from '@framework/TypeContext'
import { ModifiableEntity, EntityControlMessage } from '@framework/Signum.Entities'
import { EntityListBaseProps, EntityListBaseController } from '@framework/Lines/EntityListBase'
import { isModifiableEntity } from '@framework/Signum.Entities';
import { useController } from '@framework/Lines/LineBase';
import { BootstrapStyle } from '../../Basics/Signum.Entities.Basics';


export interface IGridEntity {
  row: number;
  startColumn: number;
  columns: number
}

export interface EntityGridRepeaterProps extends EntityListBaseProps {
  getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
  createAsLink?: boolean;
  move?: boolean;
  resize?: boolean;
}

export interface EntityGridRepaterDragging {
  dragMode: "move" | "left" | "right";
  initialPageX?: number;
  originalStartColumn?: number;
  //currentItem: TypeContext<ModifiableEntity & IGridEntity>;
  currentItem: ModifiableEntity & IGridEntity;
  currentRow?: number;
}


export class EntityGridRepeaterController extends EntityListBaseController<EntityGridRepeaterProps> {

  drag!: EntityGridRepaterDragging | undefined;
  setDrag!: React.Dispatch<EntityGridRepaterDragging | undefined>;

  init(p: EntityGridRepeaterProps) {
    super.init(p);
    [this.drag, this.setDrag] = React.useState<EntityGridRepaterDragging | undefined>(undefined);
  }
  
  getDefaultProps(state: EntityGridRepeaterProps) {
    super.getDefaultProps(state);
    state.viewOnCreate = false;
    state.move = true;
    state.resize = true;
    state.remove = true;
  }

  handleCreateClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    const p = this.props;
    const pr = p.ctx.propertyRoute!.addLambda(a => a[0]);
    const promise = p.onCreate ?
      p.onCreate(pr) : this.defaultCreate(pr);

    if (!promise)
      return;

    promise
      .then(e => {

        if (!e)
          return;

        if (!isModifiableEntity(e))
          throw new Error("Should be an entity");

        let ge = e as ModifiableEntity & IGridEntity;

        const list = p.ctx.value!;
        if (ge.row == undefined)
          ge.row = list.length == 0 ? 0 : list.map(a => (a.element as IGridEntity).row).max()! + 1;
        if (ge.startColumn == undefined)
          ge.startColumn = 0;
        if (ge.columns == undefined)
          ge.columns = 12;

        list.push({ rowId: null, element: e });
        this.setValue(list);
      });
  };

  handleRowDragOver = (e: React.DragEvent<any>, row: number) => {
    e.dataTransfer.dropEffect = "move";
    e.preventDefault();
    if (this.drag!.currentRow != row) {
      this.setDrag({ ...this.drag!, currentRow: row });
    }
  };

  handleRowDragLeave = () => {
    this.setDrag({ ...this.drag!, currentRow: undefined });
  };

  handleRowDrop = (e: React.DragEvent<any>, row: number) => {
    e.preventDefault();

    const list = this.props.ctx.value!.map(a => a.element as ModifiableEntity & IGridEntity);

    const c = this.drag!.currentItem;
    
    list.filter(a => a != c && a.row >= row).forEach(a => { a.row++; a.modified = true; });
    
    c.row = row;
    c.startColumn = 0;
    c.columns = 12;
    c.modified = true;
    
    if (!list.find(a => a == c)) {
      this.props.ctx.value!.push({ rowId: null, element: c });
      this.setValue(this.props.ctx.value);
    }

    this.setDrag(undefined);
  };

  handleOnDrop = (event: React.SyntheticEvent<any>) => {
    this.setDrag(undefined);
  };

  handleResizeDragStart: (resizer: "left" | "right", e: React.DragEvent<any>, mlec: ModifiableEntity & IGridEntity) => void = (resizer, e, mlec) => {
    e.dataTransfer.effectAllowed = "move";
    const de = e.nativeEvent as DragEvent;
    this.setDrag({ currentItem: mlec, dragMode: resizer });
  };

  handleMoveDragStart = (e: React.DragEvent<any>, mlec: ModifiableEntity & IGridEntity) => {
    e.dataTransfer.effectAllowed = "move";
    const de = e.nativeEvent as DragEvent;

    this.setDrag({ dragMode: "move", initialPageX: de.pageX, originalStartColumn: mlec.startColumn, currentItem: mlec });
  };

  handleMoveDragEnd = (e: React.DragEvent<any>, mlec: ModifiableEntity & IGridEntity) => {
    e.dataTransfer.effectAllowed = "move";
    const de = e.nativeEvent as DragEvent;
    this.setDrag(undefined);
  };

  handleItemsRowDragOver = (e: React.DragEvent<any>, row: number) => {
    if (this.drag == null)
      return;
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
    const de = e.nativeEvent as DragEvent;
    const list = this.props.ctx.value!.map(a => a.element as ModifiableEntity & IGridEntity);
    const rect = (e.currentTarget as HTMLDivElement).getBoundingClientRect();
    const d = this.drag!;
    const item = d.currentItem;
    if (d.dragMode == "move") {
      const offset = de.pageX - d.initialPageX!;
      const dCol = Math.round((offset / rect.width) * 12);
      let newCol = d.originalStartColumn! + dCol;
      let start = list.filter(a => a != item && a.row == row && a.startColumn <= newCol).map(a => a.startColumn + a.columns).max();
      if (start == null)
        start = 0;

      let end = list.filter(a => a != item && a.row == row && a.startColumn > newCol).map(a => a.startColumn - item.columns).min();
      if (end == null)
        end = 12 - item.columns;

      if (start > end) {
        e.dataTransfer.dropEffect = "none";
        return; //Doesn't fit
      }

      newCol = Math.max(start, Math.min(newCol, end));

      if (newCol != item.startColumn || item.row != row) {
        item.startColumn = newCol;
        item.row = row;
        item.modified = true;
        this.forceUpdate();
      }
    } else {
      const offsetX = (de.pageX + (d.dragMode == "right" ? 15 : -15)) - rect.left;
      let col = Math.round((offsetX / rect.width) * 12);

      if (d.dragMode == "left") {
        const max = list.filter(a => a != item && a.row == item.row && a.startColumn < item.startColumn).map(a => a.startColumn + a.columns).max();
        col = max == null ? col : Math.max(col, max);

        const cx = item.startColumn - col;
        if (cx != 0) {
          item.startColumn = col;
          item.columns += cx;
          item.modified = true;

          this.forceUpdate();
        }
      }
      else if (d.dragMode == "right") {
        const min = list.filter(a => a != item && a.row == item.row && a.startColumn > item.startColumn).map(a => a.startColumn).min();
        col = min == null ? col : Math.min(col, min);
        if (col != item.startColumn + item.columns) {
          item.columns = col - item.startColumn;
          item.modified = true;

          this.forceUpdate();
        }
      }
    }
  }
}


export const EntityGridRepeater = React.forwardRef(function EntityGridRepeater(props: EntityGridRepeaterProps, ref: React.Ref<EntityGridRepeaterController>) {
  const c = useController(EntityGridRepeaterController, props, ref)
  const p = c.props;

  if (c.isHidden)
    return null;

  return (
    <fieldset className={classes("sf-grid-repeater-field sf-control-container", p.ctx.errorClass)} {...p.ctx.errorAttributes()}>
      <legend>
        <div>
          <span>{p.label}</span>
          <span className="float-end ms-2">
            {p.extraButtonsBefore && p.extraButtonsBefore(c)}
            {c.renderCreateButton(false)}
            {c.renderFindButton(false)}
            {p.extraButtonsAfter && p.extraButtonsAfter(c)}
          </span>
        </div>
      </legend>
      <div className="row rule">
        {Array.range(0, 12).map(i =>
          <div className="col-sm-1" key={i}>
            <div className="ruleItem" />
          </div>
        )}
      </div>
      <div className={classes("sf-grid-container", c.drag?.dragMode == "move" ? "sf-dragging" : undefined)} onDrop={c.handleOnDrop}>
        {(!p.ctx.value || p.ctx.value.length == 0) && renderSeparator(1)}
        {
          c.getMListItemContext<ModifiableEntity & IGridEntity>(p.ctx)
            .groupBy(ctx => { return ctx.value.row.toString(); })
            .orderBy(gr => parseInt(gr.key))
            .flatMap((gr, i, groups) => [
              renderSeparator(parseInt(gr.key)),
              <div className="row items-row" key={"row" + gr.key} onDragOver={e => c.handleItemsRowDragOver(e, parseInt(gr.key))}>
                {gr.elements.orderBy(ctx => ctx.value.startColumn).map((ctx, j, list) => {
                  let item = p.getComponent!(ctx);
                  const s = p;
                  item = React.cloneElement(item, {
                    onResizerDragStart: ctx.readOnly || !s.resize ? undefined : (resizer, e) => c.handleResizeDragStart(resizer, e, ctx.value),
                    onTitleDragStart: ctx.readOnly || !s.move ? undefined : (e) => c.handleMoveDragStart(e, ctx.value),
                    onTitleDragEnd: ctx.readOnly || !s.move ? undefined : (e) => c.handleMoveDragEnd(e, ctx.value),
                    onRemove: ctx.readOnly || !s.remove ? undefined : (e) => c.handleRemoveElementClick(e, ctx.index!),
                  } as EntityGridItemProps);

                  const last = j == 0 ? undefined : list[j - 1].value;

                  const offset = ctx.value.startColumn - (last ? (last.startColumn + last.columns) : 0);

                  return (
                    <div key={j} className={`sf-grid-element col-sm-${ctx.value.columns} offset-sm-${offset}`}>
                      {item}
                      {/*StartColumn: {p.ctx.value.startColumn} | Columns: {p.ctx.value.columns} | Row: {p.ctx.value.row}*/}
                    </div>
                  );
                })}
              </div>,
              i == groups.length - 1 && renderSeparator(parseInt(gr.key) + 1)
            ])

        }
      </div>
    </fieldset>
  );


  function renderSeparator(rowIndex: number) {
    return (
      <div className={classes("row separator-row", c.drag?.currentRow == rowIndex ? "sf-over" : undefined)} key={"sep" + rowIndex}
        onDragOver={e => c.handleRowDragOver(e, rowIndex)}
        onDragEnter={e => c.handleRowDragOver(e, rowIndex)}
        onDragLeave={() => c.handleRowDragLeave()}
        onDrop={e => c.handleRowDrop(e, rowIndex)} />
    );
  }
});



export interface EntityGridItemProps {
  title?: React.ReactElement<any>;
  children?: React.ReactNode;
  customColor?: string;
  onResizerDragStart?: (resizer: "left" | "right", e: React.DragEvent<any>) => void;
  onTitleDragStart?: (e: React.DragEvent<any>) => void;
  onTitleDragEnd?: (e: React.DragEvent<any>) => void;
  onRemove?: (e: React.MouseEvent<any>) => void;
}


export function EntityGridItem(p : EntityGridItemProps){
  var style = p.customColor == null ? "light" : "customColor";

    return (
      <div className={classes("card", "shadow-sm")}>
        <div className={classes("card-header",
          style != "customColor" && ("bg-" + style)
        )} style={{ backgroundColor: p.customColor ?? undefined}}
        draggable={!!p.onTitleDragStart}
        onDragStart={p.onTitleDragStart}
        onDragEnd={p.onTitleDragEnd} >
        {p.onRemove &&
          <a href="#" className="sf-line-button sf-remove float-end" onClick={p.onRemove}
              title={EntityControlMessage.Remove.niceToString()}>
              <FontAwesomeIcon icon="xmark" />
            </a>
          }
        {p.title}
        </div>
        <div className="card-body">
        {p.children}
        </div>
      {p.onResizerDragStart &&
          <div className="sf-leftHandle" draggable={true}
          onDragStart={e => p.onResizerDragStart!("left", e)}>
          </div>
        }
      {p.onResizerDragStart &&
          <div className="sf-rightHandle" draggable={true}
          onDragStart={e => p.onResizerDragStart!("right", e)}>
          </div>
        }
      </div>
    );

  }


