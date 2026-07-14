import * as React from 'react'
import { AutoLine, CheckboxLine, EntityLine, EnumLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { BigValuePartEntity, UserQueryEntity } from '../../Signum.UserQueries';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import QueryTokenEmbeddedBuilder from '../../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder';
import { BigValueClient } from '../../BigValueClient';
import { SubTokensOptions } from '@framework/QueryToken';
import { getEntityTypeHelpText } from '../../../Signum.Dashboard/Admin/EntityTypeRelatedHelpText';

export default function BigValuePart(p: { ctx: TypeContext<BigValuePartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic" });
  const forceUpdate = useForceUpdate();
  const dashboardEntityType = ctx.findParentCtx(DashboardEntity).value.entityType;
  const entityType = dashboardEntityType?.model as string | undefined;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userQuery)} mandatory={entityType ? "warning" : true} create={false}
        findOptions={dashboardEntityType ? {
          queryName: UserQueryEntity,
          filterOptions: [{
            token: UserQueryEntity.token(a => a.entity.entityType),
            value: dashboardEntityType,
            pinned: { active: "Checkbox_Checked" }
          }]
        } : undefined}
        helpText={getEntityTypeHelpText(dashboardEntityType, ctx.value.userQuery?.entityType)}
        onChange={() => {
          ctx.value.valueToken = null;
          ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate();
        }} />
      {
        ctx.value.userQuery ? <QueryTokenEmbeddedBuilder ctx={ctx.subCtx(a => a.valueToken)} queryKey={ctx.value.userQuery.query.key} subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} /> :
          entityType ? <QueryTokenEmbeddedBuilder ctx={ctx.subCtx(a => a.valueToken)} queryKey={entityType} subTokenOptions={0 as SubTokensOptions} /> :
          null
      }
      <EnumLine
        ctx={ctx.subCtx(a => a.customBigValue)}
        optionItems={BigValueClient.getKeys(entityType)}
        lineType="ComboBoxText"
      />
      <CheckboxLine ctx={ctx.subCtx(a => a.navigate)} onChange={forceUpdate} inlineCheckbox="block" />
      {ctx.value.navigate && <AutoLine ctx={ctx.subCtx(a => a.customUrl)} />}
      <CheckboxLine ctx={ctx.subCtx(a => a.isClickable)} inlineCheckbox="block" />
    </div>
  );
}
