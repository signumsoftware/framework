import React from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { TypeContext } from "../Lines";
import { EntityControlMessage, getToString, is } from "../Signum.Entities";
import { Prev } from "react-bootstrap/esm/PageItem";

export interface TimeMachineProps {
  ctx: TypeContext<any>;
  isContainer?: boolean;
  transferX?: string;
  transferY?: string;
  oldElement?: any;
}

export function getTimeMachineIcon(p: TimeMachineProps) {
  if (p.ctx != null && !p.ctx.previousVersion)
    return null;

  return (p.oldElement === undefined ? <TimeMachineIcon
    ctx={p.ctx}
    isContainer={p.isContainer}
    transferX={p.transferX}
    transferY={p.transferY}
  /> : <TimeMachineCheckboxIcon
    ctx={p.ctx}
    isContainer={p.isContainer}
    transferX={p.transferX}
    transferY={p.transferY}
    oldElement={p.oldElement}
  />);
}


function TimeMachineIcon(p: TimeMachineProps) {
  if (!p.ctx.previousVersion)
    return null;

  var previous = p.ctx.previousVersion.value;
  var current = p.ctx.value;

  var change: ChangeType | null =
    previous == null && current != null ? "New" :
      previous != null && current == null ? "Removed" :
        p.ctx.previousVersion.isMoved ? "Moved" :
        previous == current ? null:
          is(previous, current, false, false) ? null :
            p.ctx.propertyRoute?.member?.type.isEmbedded && previous != null && current != null ? null :
              "Changed";

  if (change == undefined)
    return null;

  var color = change == "Changed" || change == "Moved" ? "orange" :
    change == "New" ? "#2ECC71" :
      change == "Removed" ? "red" :        
        "lightblue";

  var title = change == "Changed" ? EntityControlMessage.PreviousValueWas0.niceToString(`${previous}`) :
    change == "Moved" ? EntityControlMessage.Moved.niceToString() :
      change == "New" ? EntityControlMessage.Added.niceToString() :
        change == "Removed" ? EntityControlMessage.Removed0.niceToString(p.isContainer ? "" : typeof previous == "object" ? getToString(previous) : `${previous}`) :
          undefined;
  return (
    <FontAwesomeIcon icon="circle" title={title}
      fontSize={14}
      style={{
        position: p.isContainer ? undefined : 'absolute',
        zIndex: p.isContainer ? undefined : 2,
        minWidth: "14px",
        minHeight: "14px",
        transform: p.isContainer && !(p.transferX || p.transferY) ? undefined : `translate(${p.transferX ?? "-40%"}, ${p.transferY ?? "-40%"})`,
        color: color,
      }}
    />
  );
}

function TimeMachineCheckboxIcon(p: TimeMachineProps) {
  if (p.oldElement === undefined)
    return null;

  var previous = p.oldElement;
  var current = p.ctx?.value;

  var change: ChangeType | null =
    previous == null && current == null ? null :
      previous == null && current != null ? "New" :
        previous != null && current == null ? "Removed" :
          typeof previous.element == "string" ? (previous.element == current ? null : "Changed") : (previous.element === current ? null : "Changed");

  if (change == undefined)
    return null;

  var color = change == "Changed" ? "orange" :
    change == "New" ? "#2ECC71" :
      change == "Removed" ? "red" :
        "lightblue";

  var title = change == "Changed" ? EntityControlMessage.RemovedAndSelectedAgain.niceToString() :
    change == "New" ? EntityControlMessage.Selected.niceToString() :
      change == "Removed" ? EntityControlMessage.Removed0.niceToString(typeof previous.element == "string" ? previous.element : getToString(previous.element)) :
          undefined;
  return (
    <FontAwesomeIcon icon="circle" title={title}
      fontSize={14}
      style={{
        position: 'absolute',
        zIndex: 2,
        minWidth: "14px",
        minHeight: "14px",
        transform: `translate(${p.transferX ?? "-70%"}, ${p.transferY ?? "0%"})`,
        color: color,
      }}
    />
  );
}

type ChangeType = "New" | "Removed" | "Changed" | "Moved"; 
