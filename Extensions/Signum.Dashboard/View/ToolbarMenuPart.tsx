
import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import { ToolbarMenuPartEntity } from '../Signum.Dashboard'
import { PanelPartContentProps } from '../DashboardClient';
import { ToolbarMenuItems } from '../../Signum.Toolbar/Renderers/ToolbarRenderer';
import { ToolbarClient } from '../../Signum.Toolbar/ToolbarClient';
import { useAPI } from '../../../Signum/React/Hooks';
import { JavascriptMessage } from '@framework/Signum.Entities';

export default function ToolbarPart(p: PanelPartContentProps<ToolbarMenuPartEntity>): React.ReactNode {

  const response = useAPI(() => ToolbarClient.API.getToolbarMenu(p.content.toolbarMenu), [p.content.toolbarMenu], { avoidReset: true }); 

  return (
    <div className="sidebar sidebar-nav wide" style={{ zIndex: 0 }}>
      {!response ? JavascriptMessage.loading.niceToString() :
        <ToolbarMenuItems response={response} ctx={{ active: null, onRefresh: () => { }, onAutoClose: () => { } }} selectedEntity={p.entity ?? null} />
      }
      </div>
  );
}
