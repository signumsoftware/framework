import * as React from 'react'
import { EntityPack, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, EntityFrame } from '../TypeContext'
import "./Widgets.css"
import { ErrorBoundary } from '../Components';

export interface WidgetContext<T extends ModifiableEntity> {
  ctx: TypeContext<T>;
  frame: EntityFrame;
}

export const onWidgets: Array<(ctx: WidgetContext<ModifiableEntity>) => React.ReactElement<any> | undefined> = [];
export const onEmbeddedWidgets: Array<(ctx: WidgetContext<ModifiableEntity>) => EmbeddedWidget[] | undefined> = [];


export function clearWidgets() {
  onWidgets.clear();
  onEmbeddedWidgets.clear();
}

export function renderWidgets(wc: WidgetContext<ModifiableEntity>): React.ReactNode | undefined {
  const widgets = onWidgets.map(a => a(wc)).filter(a => a != undefined);

  if (widgets.length == 0)
    return undefined;

  return (
    <ErrorBoundary>
      <ul className="sf-widgets">
        {widgets.map((w, i) => <li key={i}>{w}</li>)}
      </ul>
    </ErrorBoundary>
  );
}

export interface EmbeddedWidget {
  embeddedWidget: React.ReactElement<any>;
  position: EmbeddedWidgetPosition;
  title: string;
  eventKey: string;
}

export type EmbeddedWidgetPosition = "Top" | "Bottom" | "Tab";


