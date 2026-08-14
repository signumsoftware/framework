
import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import { ToolbarMenuPartEntity } from '../Signum.Dashboard'
import { PanelPartContentProps } from '../DashboardClient';
import { ToolbarMenuItems, simplifyForEntity } from '../../Signum.Toolbar/Renderers/ToolbarRenderer';
import { ToolbarClient } from '../../Signum.Toolbar/ToolbarClient';
import { useAPI } from '../../../Signum/React/Hooks';
import { JavascriptMessage, liteKey } from '@framework/Signum.Entities';

export default function ToolbarPart(p: PanelPartContentProps<ToolbarMenuPartEntity>): React.ReactNode {

  const response = useAPI(() => ToolbarClient.API.getToolbarMenu(p.content.toolbarMenu), [p.content.toolbarMenu], { avoidReset: true });

  // Menus fetched by id carry no entityType (GetToolbarMenuResponse doesn't set one), so ToolbarMenuItems can not
  // filter them by itself. Here the entity is known — it is the dashboard's — so apply the per-entity filtering
  // that the entity-bound toolbar menus get: hidden elements plus the ones not allowed in the entity's domain.
  const entity = p.entity;
  const entityFilter = entity && ToolbarClient.entityElementFilters[entity.EntityType];
  const hiddenGuids = useAPI(() => entity && entityFilter ? entityFilter(entity) : null,
    [entity && entity.EntityType, entity && liteKey(entity)]);

  // Wait for the filter rather than rendering unfiltered elements first: an element that flashes and then
  // disappears is exactly what the filter is there to prevent.
  const loading = !response || (entityFilter != null && hiddenGuids === undefined);

  const filtered = React.useMemo(
    () => !response || !entity ? response : { ...response, elements: simplifyForEntity(response.elements ?? [], entity, hiddenGuids ?? undefined) },
    [response, entity && liteKey(entity), hiddenGuids]);

  return (
    <div className="sidebar sidebar-nav wide" style={{ zIndex: 0 }}>
      {loading || !filtered ? JavascriptMessage.loading.niceToString() :
        <ToolbarMenuItems response={filtered} ctx={{ active: null, onRefresh: () => { }, onAutoClose: () => { } }} selectedEntity={p.entity ?? null} />
      }
      </div>
  );
}
