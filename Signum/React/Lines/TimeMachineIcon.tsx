import React from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { TypeContext } from "../Lines";
import { EntityControlMessage, EnumEntity, getToString, is } from "../Signum.Entities";
import { Prev } from "react-bootstrap/esm/PageItem";
import { EnumType, TypeInfo } from "../Reflection";

export interface TimeMachineIconProps {
  ctx: TypeContext<any>;
  isContainer?: boolean;
  translateX?: string;
  translateY?: string;
}


export function getTimeMachineIcon(p: TimeMachineIconProps): React.ReactElement | null {
  if (!p.ctx.previousVersion)
    return null;

  return (
    <TimeMachineIcon ctx={p.ctx} isContainer={p.isContainer} translateX={p.translateX} translateY={p.translateY} />
  );
}


function TimeMachineIcon(p: TimeMachineIconProps) {
  if (!p.ctx.previousVersion)
    return null;

  var previous = p.ctx.previousVersion.value;
  var current = p.ctx.value;

  var change: ChangeType  =
    previous == null && current != null ? "New" :
      previous != null && current == null ? "Removed" :
        p.ctx.previousVersion.isMoved ? "Moved" :
          previous == current ? "NoChange" :
            is(previous, current, false, false) ? "NoChange" :
              p.ctx.propertyRoute?.member?.type.isEmbedded && previous != null && current != null ? "NoChange" :
              "Changed";

  if (change == undefined)
    return null;

  var color = change == "Changed" || change == "Moved" ? TimeMachineColors.changed :
    change == "New" ? TimeMachineColors.created :
      change == "Removed" ? TimeMachineColors.removed :
        change == "NoChange" ?  TimeMachineColors.noChange : "magenta";

  var title = change == "Changed" ? EntityControlMessage.PreviousValueWas0.niceToString(`${previous}`) :
    change == "Moved" ? EntityControlMessage.Moved.niceToString() :
      change == "New" ? EntityControlMessage.Added.niceToString() :
        change == "Removed" ? EntityControlMessage.Removed0.niceToString(p.isContainer ? "" : typeof previous == "object" ? getToString(previous) : `${previous}`) :
          EntityControlMessage.NoChanges.niceToString();

  return (
    <FontAwesomeIcon
      icon="circle"
      title={title}
      fontSize={14}
      style={{
        position: p.isContainer ? undefined : 'absolute',
        zIndex: p.isContainer ? undefined : 2,
        minWidth: "14px",
        minHeight: "14px",
        transform: p.isContainer && !(p.translateX || p.translateY) ? undefined : `translate(${p.translateX ?? "-40%"}, ${p.translateY ?? "-40%"})`,
        color: color,
      }}
    />
  );
}

export const TimeMachineColors = {
  changed: "orange",
  created: "#2ECC71",
  removed: "red",
  noChange: "#ddd",
}

export interface TimeMachineIconCheckboxProps {
  newCtx: TypeContext<any> | null;
  oldCtx: TypeContext<any> | null;
  translateX?: string;
  translateY?: string;
  type?: TypeInfo;
}

export function getTimeMachineCheckboxIcon(p: TimeMachineIconCheckboxProps): React.ReactElement | null {
  
  if ((p.newCtx == null && p.oldCtx == null) || (p.newCtx != null && !p.newCtx.previousVersion))
    return null;

  return (
    <TimeMachineCheckboxIcon newCtx={p.newCtx} oldCtx={p.oldCtx} translateX={p.translateX} translateY={p.translateY} />
  );
}

function TimeMachineCheckboxIcon(p: TimeMachineIconCheckboxProps) {

  var change: ChangeType =
    p.oldCtx == null && p.newCtx == null ? "NoChange" :
      p.oldCtx == null && p.newCtx != null ? "New" :
        p.oldCtx != null && p.newCtx == null ? "Removed" :
          (p.oldCtx === p.newCtx ? "NoChange" : "Changed");


  var color = change == "Changed" ? TimeMachineColors.changed :
    change == "New" ? TimeMachineColors.created :
      change == "Removed" ? TimeMachineColors.removed :
        change == "NoChange" ? TimeMachineColors.noChange :
        "magenta";

  var title = change == "Changed" ? EntityControlMessage.RemovedAndSelectedAgain.niceToString() :
    change == "New" ? EntityControlMessage.Selected.niceToString() :
      change == "Removed" ? EntityControlMessage.Removed0.niceToString(typeof p.oldCtx?.value.element == "string" ? p.type!.members[p.oldCtx!.value].niceName : getToString(p.oldCtx?.value.element)) :
        EntityControlMessage.NoChanges.niceToString();
  return (
    <FontAwesomeIcon icon="circle" title={title}
      fontSize={14}
      style={{
        position: 'absolute',
        zIndex: 2,
        minWidth: "14px",
        minHeight: "14px",
        transform: `translate(${p.translateX ?? "-70%"}, ${p.translateY ?? "0%"})`,
        color: color,
      }}
    />
  );
}

type ChangeType = "New" | "Removed" | "Changed" | "Moved" | "NoChange"; 
