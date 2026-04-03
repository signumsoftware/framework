import * as React from 'react'
import { DashboardEntity, DashboardMessage, CustomPartEntity, PanelPartEmbedded, TextPartEntity } from '../Signum.Dashboard';
import { PanelPartContentProps, DashboardClient, CustomPartProps } from '../DashboardClient';
import { useForceUpdate } from '../../../Signum/React/Hooks';
import { ImportComponent } from '../../../Signum/React/ImportComponent';
import { DashboardController } from './DashboardFilterController';
import { Entity, Lite } from '@framework/Signum.Entities';
import { Type } from '@framework/Reflection';
import { softCast } from '../../../Signum/React/Globals';

export default function CustomPart(p: PanelPartContentProps<CustomPartEntity>): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  const cpr = DashboardClient.Options.getCustomPartRenderer(p.entity?.EntityType)?.[p.content.customPartName];

  if (!cpr)
    return (
      <div className="alert alert-danger" role="alert">
        No renderer for <code>{p.entity?.EntityType ?? "global"}</code> with name <code>{p.content.customPartName}</code> registered in <code>DashboardClient.Options.customPartRenderers</code>
      </div>
    );

  return <ImportComponent onImport={cpr.renderer} componentProps={softCast<CustomPartProps<Entity>>({
    partEmbedded: p.partEmbedded,
    content: p.content,
    dashboardController: p.dashboardController,
    entity: p.entity,
  })} />;
}

