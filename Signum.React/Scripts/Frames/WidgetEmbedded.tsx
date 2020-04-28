import * as React from 'react'
import { EntityPack, ModifiableEntity, NormalWindowMessage } from '../Signum.Entities'
import { TypeContext, EntityFrame } from '../TypeContext'
import "./Widgets.css"
import { ErrorBoundary } from '../Components';
import { renderWidgets, WidgetContext, renderEmbeddedWidgets } from './Widgets'
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



export default function WidgetEmbedded(p: WidgetEmbeddedProps) {
  const embeddedWidgets = renderEmbeddedWidgets(p.widgetContext);
  const est = Navigator.getSettings(p.widgetContext.frame.pack.entity.Type)!;
 
  debugger;
  if (embeddedWidgets.tab.length > 0 && est.supportsAdditionalTabs!==true) {
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
