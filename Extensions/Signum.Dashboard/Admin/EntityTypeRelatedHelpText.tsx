import * as React from 'react'
import { Lite } from '@framework/Signum.Entities'
import { TypeEntity } from '@framework/Signum.Basics'
import { DashboardMessage } from '../Signum.Dashboard'

/**
 * Returns a helpText element for EntityLine when the part's entity type may not match the parent dashboard's entity type.
 *
 * - null:                 no issue (dashboard has no entityType, or selected item matches)
 * - text-warning:         dashboard has entityType but the selected item has entityType=null (global — not filtering by that type)
 * - text-danger:          selected item has an explicit entityType that differs from the dashboard's
 */
export function getEntityTypeHelpText(
  dashboardEntityType: Lite<TypeEntity> | null | undefined,
  selectedEntityType: Lite<TypeEntity> | null | undefined,
): React.ReactElement | undefined {

  if ((dashboardEntityType?.model ?? null) === (selectedEntityType?.model ?? null))
    return undefined;

  if (dashboardEntityType != null && selectedEntityType == null)
    return <span className="text-warning">{DashboardMessage.NotFilteringBy0.niceToString(dashboardEntityType.model as string)}</span>;

  return <span className="text-danger">{DashboardMessage.IncompatibleEntityType.niceToString()}</span>;
}
