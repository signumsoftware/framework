import * as React from 'react'
import { EntityPack, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, EntityFrame } from '../TypeContext'
import "./Widgets.css"
import { ErrorBoundary } from '../Components';
import { classes } from "../Globals";

export interface WidgetContext<T extends ModifiableEntity> {
  ctx: TypeContext<T>;
  frame: EntityFrame;
}

export const onWidgets: Array<(ctx: WidgetContext<ModifiableEntity>) => React.ReactElement | undefined> = [];
export const onEmbeddedWidgets: Array<(ctx: WidgetContext<ModifiableEntity>) => EmbeddedWidget[] | undefined> = [];


export function clearWidgets(): void {
  onWidgets.clear();
  onEmbeddedWidgets.clear();
}

export function renderWidgets(wc: WidgetContext<ModifiableEntity>, stickyHeader?: boolean): React.ReactNode | undefined {
  const widgets = onWidgets.map(a => a(wc)).filter(a => a != undefined);

  if (widgets.length == 0)
    return undefined;

  return (
    <ErrorBoundary>
      <div className={classes("sf-widgets", stickyHeader && "sf-sticky-header")}>
        {widgets.slice().reverse().map((w, i) => React.cloneElement((w as React.ReactElement), { key: i }))}
      </div>
    </ErrorBoundary>
  );
}

export interface EmbeddedWidget {
  embeddedWidget: React.ReactElement;
  position: EmbeddedWidgetPosition;
  title: React.ReactNode;
  eventKey: string;
}

export type EmbeddedWidgetPosition = "Top" | "Bottom" | "Tab";


