import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityControlMessage } from '../Signum.Entities';
import { EntityBaseController } from '../Lines/EntityBase';
import { LinkButton } from '../Basics/LinkButton';
import { classes } from '../Globals';
import '../Lines/Lines.css';

export interface DraggableTableColumn<T> {
  header: React.ReactNode;
  template: (item: T, index: number, forceUpdate: () => void) => React.ReactNode;
  headerHtmlAttributes?: React.ThHTMLAttributes<HTMLTableCellElement>;
  cellHtmlAttributes?: React.TdHTMLAttributes<HTMLTableCellElement>;
}

interface DropBorderIndex {
  index: number;
  offset: 0 | 1;
}

interface DraggableTableProps<T> {
  items: T[];
  columns: DraggableTableColumn<T>[];
  forceUpdate: () => void;
  onCreate?: () => T | undefined;
  className?: string;
}

export function DraggableTable<T>(p: DraggableTableProps<T>): React.ReactElement {
  const dragIndex = React.useRef<number | undefined>(undefined);
  const [draggingIndex, setDraggingIndex] = React.useState<number | undefined>(undefined);
  const [dropBorder, setDropBorder] = React.useState<DropBorderIndex | undefined>(undefined);

  function handleDragStart(e: React.DragEvent<any>, index: number) {
    e.dataTransfer.setData('text', 'start');
    e.dataTransfer.effectAllowed = "move";
    dragIndex.current = index;
    setDraggingIndex(index);
  }

  function handleDragEnd() {
    dragIndex.current = undefined;
    setDraggingIndex(undefined);
    setDropBorder(undefined);
  }

  function handleDragEnter(e: React.DragEvent<HTMLTableRowElement>) {
    if (dragIndex.current == null) return;
    e.preventDefault();
  }

  function handleDragOver(e: React.DragEvent<HTMLTableRowElement>, index: number) {
    if (dragIndex.current == null) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";

    const rect = e.currentTarget.getBoundingClientRect();
    const margin = Math.min(50, rect.height / 2);
    const offsetY = e.clientY - rect.top;
    const offset: 0 | 1 | undefined = offsetY < margin ? 0 : offsetY > rect.height - margin ? 1 : undefined;

    const newDropBorder = offset != null ? { index, offset } : undefined;
    if (newDropBorder?.index !== dropBorder?.index || newDropBorder?.offset !== dropBorder?.offset)
      setDropBorder(newDropBorder);
  }

  function handleDrop(e: React.DragEvent<HTMLTableRowElement>) {
    e.preventDefault();
    const from = dragIndex.current;
    dragIndex.current = undefined;
    setDraggingIndex(undefined);
    setDropBorder(undefined);

    if (from == null || dropBorder == null) return;

    let to = dropBorder.index + (dropBorder.offset === 1 ? 1 : 0);
    if (from < to) to--;
    if (from === to) return;

    const temp = p.items[from];
    p.items.splice(from, 1);
    p.items.splice(to, 0, temp);
    p.forceUpdate();
  }

  function getDropClass(index: number): string | undefined {
    if (dropBorder == null) return undefined;
    if (index === dropBorder.index) return dropBorder.offset === 0 ? "drag-top" : "drag-bottom";
    if (dropBorder.index === index - 1 && dropBorder.offset === 1) return "drag-top";
    if (dropBorder.index === index + 1 && dropBorder.offset === 0) return "drag-bottom";
    return undefined;
  }

  function handleRemove(index: number) {
    p.items.splice(index, 1);
    p.forceUpdate();
  }

  function handleAdd() {
    if (!p.onCreate) return;
    const newItem = p.onCreate();
    if (newItem !== undefined) {
      p.items.push(newItem);
      p.forceUpdate();
    }
  }

  return (
    <table className={classes("table table-sm sf-table", p.className)}>
      <thead>
        <tr>
          <th></th>
          {p.columns.map((col, i) => (
            <th key={i} {...col.headerHtmlAttributes}>{col.header}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {p.items.map((item, index) => (
          <tr key={index}
            className={classes(index === draggingIndex && "sf-dragging", getDropClass(index))}
            onDragEnter={handleDragEnter}
            onDragOver={e => handleDragOver(e, index)}
            onDrop={handleDrop}>
            <td style={{ verticalAlign: "middle" }}>
              <div className="item-group">
                <LinkButton
                  title={EntityControlMessage.Remove.niceToString()}
                  className={classes("sf-line-button", "sf-remove", "me-1")}
                  onClick={e => { e.stopPropagation(); handleRemove(index); }}>
                  {EntityBaseController.getRemoveIcon()}
                </LinkButton>
                <LinkButton
                  title={EntityControlMessage.MoveWithDragAndDropOrCtrlUpDown.niceToString()}
                  className={classes("sf-line-button", "sf-move")}
                  onClick={e => e.stopPropagation()}
                  draggable={true}
                  onDragStart={e => handleDragStart(e, index)}
                  onDragEnd={handleDragEnd}>
                  {EntityBaseController.getMoveIcon()}
                </LinkButton>
              </div>
            </td>
            {p.columns.map((col, ci) => (
              <td key={ci} style={{ verticalAlign: "middle" }} {...col.cellHtmlAttributes}>
                {col.template(item, index, p.forceUpdate)}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
      {p.onCreate && (
        <tfoot>
          <tr>
            <td colSpan={p.columns.length + 1}>
              <LinkButton className="sf-line-button sf-create" title={undefined} onClick={handleAdd}>
                <FontAwesomeIcon aria-hidden={true} icon="plus" /> {EntityControlMessage.Add.niceToString()}
              </LinkButton>
            </td>
          </tr>
        </tfoot>
      )}
    </table>
  );
}
