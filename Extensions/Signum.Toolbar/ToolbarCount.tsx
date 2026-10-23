import * as React from 'react'
import { classes } from '@framework/Globals'
import { ShowCount } from './Signum.Toolbar'

export function ToolbarCount(p: { num: number | null | undefined, showCount: ShowCount }): React.JSX.Element | null {

  if (!p.num && p.showCount == "MoreThan0")
    return null;

  return (
    <div className="sf-toolbar-count-container">
      <div className={classes("badge badge-pill sf-toolbar-count", !p.num ? "text-bg-tertiary" : "text-bg-danger")}>{p.num ?? "…"}</div>
    </div>
  );
}
