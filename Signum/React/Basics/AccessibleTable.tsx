import React from "react";
import { JSX } from "react/jsx-runtime";

type SectionType = "tbody" | "thead" | "tfoot";
type TableRole = "grid" | "table" | "treegrid";

interface AccessibleTableProps extends React.TableHTMLAttributes<HTMLTableElement> {
  caption: string;
  tableRole?: TableRole;
  multiselectable?: boolean;
  focusCells?: boolean;
  mapCustomComponents?: Map<React.JSXElementConstructor<any>, string>
}

export function AccessibleTable({
  caption,
  tableRole = "grid",
  children,
  multiselectable = true,
  focusCells = true,
  mapCustomComponents,
  ...rest
}: AccessibleTableProps): JSX.Element {

  function enhanceSection(
    section: React.ReactElement<React.HTMLAttributes<HTMLTableSectionElement>>,
    sectionType: SectionType
  ): React.ReactElement {
    const enhancedRows = React.Children.map(section.props.children, (child) => {

      const type = getType(child, mapCustomComponents);
      if (type !== "tr") {
        handleStructureError(sectionType, child);
        return child;
      }
      const element = child as React.ReactElement<React.HTMLAttributes<HTMLTableRowElement>>;
      if (element.type == "tr")
        return React.createElement(WCAGRow, { focusCells, focusHeader: multiselectable, sectionType, tableRole, ...element.props });

      return child;
    });

    return React.cloneElement(section, { children: enhancedRows });
  }

  function enhanceChildren(node: React.ReactNode): React.ReactNode {
    return React.Children.map(node, (child) => {

      const element = child as React.ReactElement<React.HTMLAttributes<HTMLTableSectionElement>>;
      const type = getType(element, mapCustomComponents);
      switch (type) {
        case "thead": return enhanceSection(element, "thead");
        case "tbody": return enhanceSection(element, "tbody");
        case "tfoot": return enhanceSection(element, "tfoot");
        default:
          handleStructureError("A table should have a thead, tbody or tfoot", element);
          return element;
      }
    });
  }

  const enhancedChildren = enhanceChildren(children);

  return (
    <table
      role={tableRole}
      aria-multiselectable={`${multiselectable ? "true" : "false"}`}
      {...rest}>
      <caption>{caption}</caption>
      {enhancedChildren}
    </table>
  );
}

function handleStructureError(message: string, node: React.ReactNode) {

  throw new Error(`[AccessibleTable] Structure error: ${message} instead of ${React.isValidElement(node) ? node.type : typeof node}`);
}

function getType(node: React.ReactNode, mapCustomComponents: Map<React.JSXElementConstructor<any>, string> | undefined): string | null {
  if (!React.isValidElement(node))
    return null;

  if (typeof node.type == "string")
    return node.type;

  var mappedType = mapCustomComponents?.get(node.type);

  if (mappedType == null)
    throw new Error(`Custom Component ${node.type.name} should be registered in mapCustomComponents with the equivalent table tag (tr, td..)`);

  return mappedType;
}
interface WCAGRowProps extends React.HTMLAttributes<HTMLTableRowElement> {
  focusCells?: boolean;
  focusHeader?: boolean;
  sectionType?: SectionType,
  mapCustomComponents?: Map<React.JSXElementConstructor<any>, string>,
  tableRole?: TableRole
}
export function WCAGRow({ focusCells = true, focusHeader = false, sectionType = "tbody", mapCustomComponents, children, tableRole = "grid", ...rest }: WCAGRowProps): React.ReactElement {

  function enhanceHeaderCell(
    th: React.ReactElement<React.ThHTMLAttributes<HTMLTableCellElement>>
  ): React.ReactElement {
    return React.cloneElement(th, {
      role: !tableRole ? "columnheader" : undefined,
      scope: th.props.scope || "col",
      tabIndex: focusHeader ? 0 : -1,
    } as React.ThHTMLAttributes<HTMLTableCellElement>);
  }

  function enhanceCell(
    td: React.ReactElement<React.TdHTMLAttributes<HTMLTableCellElement> | React.ThHTMLAttributes<HTMLTableCellElement>>
  ): React.ReactElement {
    var type = getType(td, mapCustomComponents);

    const renderedChildren = React.Children.toArray(td.props.children)
      .filter(child => child !== "" && child !== null && child !== undefined);
    const isEmptyCell = renderedChildren.length === 0; // needed for condinional rendering

    if (type == "th" && isEmptyCell)
      handleStructureError("tbody > th should always contain content", td);

    if (type == "th")
      return React.cloneElement(td, {
        role: "rowheader",
        scope: td.props.scope || "row",
        tabIndex: focusCells ? 0 : -1,
      } as React.ThHTMLAttributes<HTMLTableCellElement>);


    return React.cloneElement(td, {
      role: (tableRole) ? undefined : "gridcell",
      tabIndex: focusCells ? 0 : -1,
      children: isEmptyCell
        ? <span className="sr-only">Kein Eintrag in diesem Feld</span>
        : td.props.children
    } as React.TdHTMLAttributes<HTMLTableCellElement>);
  }



  const childrenArray = React.Children.toArray(children);
  const enhancedCells = childrenArray.map((child) => {

    var type = getType(child, mapCustomComponents);

    if (sectionType === "thead") {

      if (type !== "th")
        handleStructureError("thead > tr should only contain th", child);

      return enhanceHeaderCell(child as React.ReactElement<React.ThHTMLAttributes<HTMLTableCellElement>>);
    } else {

      if (type !== "td" && type !== "th")
        handleStructureError(`${sectionType} > tr should contains td or th`, child);

      return enhanceCell(child as React.ReactElement<React.TdHTMLAttributes<HTMLTableCellElement> | React.ThHTMLAttributes<HTMLTableCellElement>>);
    }
  });

  function handleKeyDown(e: React.KeyboardEvent<HTMLTableRowElement>) {

    const currentRow = e.currentTarget;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      const nextRow = currentRow.nextElementSibling as HTMLTableRowElement | null;
      nextRow?.focus();
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      const prevRow = currentRow.previousElementSibling as HTMLTableRowElement | null;
      prevRow?.focus();
    }
  }

  return React.cloneElement(<tr role={!tableRole ? "row" : undefined} tabIndex={0} onKeyDown={handleKeyDown}></ tr>, undefined, enhancedCells);
}
