import * as React from 'react'
import { CustomPartEntity, DashboardEntity, DashboardMessage } from '../Signum.Dashboard';
import { EnumLine, TypeContext } from '../../../Signum/React/Lines';
import { DashboardClient } from '../DashboardClient';

export default function CustomPart(p: { ctx: TypeContext<CustomPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  const entityType = ctx.findParentCtx(DashboardEntity).value.entityType;

  var registeredNames = entityType && Object.keys(DashboardClient.Options.customPartRenderers[entityType.model as string] ?? {});

  return (
    <div>
      {entityType == null ? <div className="alert alert-danger" role="alert">
        {DashboardMessage._0CanOnlyBeUserInA1With2.niceToString(CustomPartEntity.niceName(), DashboardEntity.niceName(), DashboardEntity.nicePropertyName(a => a.entityType))}
      </div> :
        registeredNames!.length == 0 ?
          <div className="alert alert-danger" role="alert">
            No renderer for <code>{entityType!.model as string}</code> registered in <code>DashboardClient.Options.customPartRenderers</code>
          </div> :
          <EnumLine ctx={ctx.subCtx(p => p.customPartName)} optionItems={Object.values(registeredNames!)} />
      }
    </div>
  );
}

