import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { EntityLine } from '@framework/Lines'
import { Navigator } from '@framework/Navigator'
import { Operations } from '@framework/Operations'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { toLite } from '@framework/Signum.Entities'
import { openModal, IModalProps } from '@framework/Modals'
import { Modal } from 'react-bootstrap'
import { AgentSymbol, SkillCustomizationOperation } from '../Signum.Agent'
import { AgentClient, SkillCodeInfo } from '../AgentClient'
import { SkillCodeView } from './SkillCode'

export default function Agent(p: { ctx: TypeContext<AgentSymbol> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();

  const defaultInfo = useAPI(
    () => ctx.value.skillCustomization == null && ctx.value.key != null
      ? AgentClient.API.getDefaultAgentSkillCodeInfo(ctx.value.key)
      : Promise.resolve(null),
    [ctx.value.skillCustomization, ctx.value.key]
  );

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(a => a.skillCustomization)} onChange={forceUpdate} labelColumns={4}
        create={true} viewOnCreate={false}
        onCreate={() => Operations.API.constructFromLite(toLite(ctx.value), SkillCustomizationOperation.CreateFromAgent)
          .then(pack => Operations.API.executeEntity(pack!.entity, SkillCustomizationOperation.Save))
          .then(pack => pack.entity)}
        helpText={defaultInfo
          ? <a href="#" onClick={e => { e.preventDefault(); DefaultSkillInfoModal.show(defaultInfo, ctx.value.key!); }}>View defaults</a>
          : undefined}
      />
    </div>
  );
}

interface DefaultSkillInfoModalProps extends IModalProps<void> {
  info: SkillCodeInfo;
  title: string;
}

function DefaultSkillInfoModal(p: DefaultSkillInfoModalProps): React.JSX.Element {
  const [show, setShow] = React.useState(true);
  return (
    <Modal show={show} onHide={() => setShow(false)} onExited={() => p.onExited!(undefined)} size="lg">
      <Modal.Header closeButton>
        <Modal.Title>{p.title}</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <SkillCodeView info={p.info} />
      </Modal.Body>
    </Modal>
  );
}

DefaultSkillInfoModal.show = (info: SkillCodeInfo, title: string): Promise<void> =>
  openModal<void>(<DefaultSkillInfoModal info={info} title={title} />);
