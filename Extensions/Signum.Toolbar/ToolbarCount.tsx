import * as React from 'react'
import { classes } from '@framework/Globals'
import { ShowCount, ToolbarMessage } from './Signum.Toolbar'

export function ToolbarCount(p: { num: number | null | undefined, showCount: ShowCount }): React.JSX.Element | null {

  if (!p.num && (p.showCount == "MoreThan0" || p.showCount == "Indicator"))
    return null;

  if (p.showCount == "Indicator") {
    // Only a dot, so the screen reader text says what it means.
    return (
      <div className="sf-toolbar-count-container">
        <div className="badge rounded-circle sf-toolbar-count sf-toolbar-indicator text-bg-danger">
          <span className="visually-hidden">{ToolbarMessage.HasEntries.niceToString()}</span>
        </div>
      </div>
    );
  }

  return (
    <div className="sf-toolbar-count-container">
      <div className={classes("badge badge-pill sf-toolbar-count", !p.num ? "text-bg-tertiary" : "text-bg-danger")}>{p.num ?? "…"}</div>
    </div>
  );
}
