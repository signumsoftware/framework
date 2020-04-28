import * as React from 'react'
import { EntityPack, ModifiableEntity, NormalWindowMessage } from '../Signum.Entities'
import { TypeContext, EntityFrame } from '../TypeContext'
import "./Widgets.css"
import { ErrorBoundary } from '../Components';
import { renderWidgets, WidgetContext, EmbeddedWidget } from './Widgets'
import { Tabs, Tab } from 'react-bootstrap';
import * as Navigator from "../Navigator"


export interface WidgetEmbeddedProps {
  widgetContext: WidgetContext<ModifiableEntity>;
  children?: React.ReactNode;
}

export function toTab(e: any) {
  return <Tab eventKey={e.props.dashboard.ticks} mountOnEnter="true" title={e.props.dashboard.toStr}>
    {e}
  </Tab>;
}

export function addAdditionalTabs(frame: EntityFrame | undefined) {
  if (frame === undefined || frame!.tabs === undefined)
    return undefined;

  return frame!.tabs!.map(e => toTab(e)); 
}

export const onEmbeddedWidgets: Array<(ctx: WidgetContext<ModifiableEntity>) => EmbeddedWidget[] | undefined> = [];

export function renderEmbeddedWidgets(wc: WidgetContext<ModifiableEntity>): { top: React.ReactElement<any>[]; tab: React.ReactElement<any>[]; bottom: React.ReactElement<any>[] } {
  const widgets = onEmbeddedWidgets.map(a => a(wc)).filter(a => a !== undefined).map(a => a!).flatMap(a => a);

  return {
    top: widgets.filter(ew => ew.position === "Top").map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i })),
    tab: widgets.filter(ew => ew.position === "Tab").map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i })),
    bottom: widgets.filter(ew => ew.position === "Bottom").map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i }))
  };
}


export default function WidgetEmbedded(p: WidgetEmbeddedProps) {
  const embeddedWidgets = renderEmbeddedWidgets(p.widgetContext);
  const est = Navigator.getSettings(p.widgetContext.frame.pack.entity.Type)!;
 
  debugger;
  if (embeddedWidgets.tab.length > 0 && (!est || est.supportsAdditionalTabs!==true)) {
    return (
      <>
        {embeddedWidgets.top}
        <Tabs id="appTabs">
          <Tab eventKey="tabMain1" title={NormalWindowMessage.Main.niceToString()}>
            {p.children}
          </Tab>
          {embeddedWidgets.tab.map(e => toTab(e))}
        </Tabs>
        {embeddedWidgets.bottom}
      </>);
  }
  else {
    p.widgetContext.frame.tabs = embeddedWidgets.tab
    return (
      <>
        {embeddedWidgets.top}
        {p.children}
        {embeddedWidgets.bottom}
      </>);
  }
}
