import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { EntityLine, RenderEntity } from '@framework/Lines'
import { Navigator } from '@framework/Navigator'
import { Operations } from '@framework/Operations'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { toLite } from '@framework/Signum.Entities'
import { AgentSymbol, SkillCustomizationOperation } from '../Signum.Agent'
import { AgentClient } from '../AgentClient'
import { SkillCodeView } from './SkillCode'

export default function Agent(p: { ctx: TypeContext<AgentSymbol> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();

  const defaultInfo = useAPI(
    () => ctx.value.skillCustomization == null
      ? AgentClient.API.getDefaultAgentSkillCodeInfo(ctx.value.key)
      : Promise.resolve(null),
    [ctx.value.skillCustomization, ctx.value.key]
  );

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(a => a.skillCustomization)} onChange={forceUpdate}
        onCreate={() => Operations.API.constructFromLite(toLite(ctx.value), SkillCustomizationOperation.CreateFromAgent)
          .then(pack => pack ? Navigator.view(pack) : undefined)}
      />
      {ctx.value.skillCustomization
        ? <RenderEntity ctx={ctx.subCtx(a => a.skillCustomization)} />
        : defaultInfo && <SkillCodeView info={defaultInfo} />
      }
    </div>
  );
}
