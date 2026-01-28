import * as React from 'react'
import { Finder } from '@framework/Finder';
import { Constructor } from '@framework/Constructor';
import { Navigator } from '@framework/Navigator';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { WhatsNewClient } from '../WhatsNewClient';
import { WhatsNewEntity, WhatsNewMessage, WhatsNewMessageEmbedded } from '../Signum.WhatsNew';
import { useAPI, useForceUpdate } from '@framework/Hooks';
import { Binding, EntityCombo, EntityLine, EntityTabRepeater, TypeContext, AutoLine } from '@framework/Lines';
import { FileLine } from '../../Signum.Files/Components/FileLine';
import WhatsNewHtmlEditor from './WhatsNewHtmlEditor';
import SelectorModal from '@framework/SelectorModal';
import { getTypeInfos, TypeInfo } from '@framework/Reflection';
import { PermissionSymbol, QueryEntity, TypeEntity } from '@framework/Signum.Basics';
import { OperationSymbol } from '@framework/Signum.Operations';
import { Entity } from '../../../Signum/React/Signum.Entities';

export default function WhatsNew(p: { ctx: TypeContext<WhatsNewEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();

  function selectContentType(filter: (ti: TypeInfo) => boolean) {
    const pr = ctx.memberInfo(wn => wn.related);
    return SelectorModal.chooseType(getTypeInfos(pr.type).filter(filter), {
      size: "def" as any,
      buttonDisplay: ti => {
        var icon = getDefaultIcon(ti);

        if (icon == null)
          return ti.niceName;

        return <><FontAwesomeIcon aria-hidden={true} icon={icon.icon} color={icon.iconColor} /><span className="ms-2">{ti.niceName}</span></>;
      }
    });
  }

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(w => w.status)} readOnly />
      <AutoLine ctx={ctx.subCtx(w => w.name)} />
      <FileLine ctx={ctx.subCtx(w => w.previewPicture)} mandatory />
      <EntityLine ctx={ctx.subCtx(w => w.related)}
        onFind={() => selectContentType(ti => Navigator.isFindable(ti)).then(ti => ti && Finder.find({ queryName: ti.name }))}
        onCreate={() => selectContentType(ti => Navigator.isCreable(ti)).then(ti => ti && Constructor.construct(ti.name) as Promise<Entity | undefined>)}
      />
      <EntityTabRepeater ctx={ctx.subCtx(w => w.messages)} onChange={() => forceUpdate()} getComponent={ctx =>
        <WhatsNewMessageComponent ctx={ctx} invalidate={() => forceUpdate} />} />
    </div>
  );
}

export interface WhatsNewMessageComponentProps
{
  ctx: TypeContext<WhatsNewMessageEmbedded>;
  invalidate: () => void;
}

export function WhatsNewMessageComponent(p: WhatsNewMessageComponentProps): React.JSX.Element {

  const ec = p.ctx.subCtx({labelColumns: 4});
  return (
    <div>
      <EntityCombo ctx={ec.subCtx(e => e.culture)} label={WhatsNewMessage.Language.niceToString()} onChange={p.invalidate} />
      <AutoLine ctx={ec.subCtx(e => e.title)} label={ec.subCtx(e => e.title).niceName()} onChange={p.invalidate} />
      
      <div>
        <p>{ec.subCtx(e => e.description).niceName()}</p>
        <WhatsNewHtmlEditor binding={Binding.create(ec.value, w => w.description)} />
      </div>
    </div>
  );
}

function getDefaultIcon(ti: TypeInfo): WhatsNewClient.IconColor | null {

  if (ti.name == TypeEntity.typeName)
    return ({ icon: "object-subtract", iconColor: "#229954" });

  if (ti.name == QueryEntity.typeName)
    return ({ icon: "rectangle-list", iconColor: "#52BE80" });

  if (ti.name == OperationSymbol.typeName)
    return ({ icon: "key", iconColor: "#F1C40F" });

  if (ti.name == PermissionSymbol.typeName)
    return ({ icon: "key", iconColor: "#F1C40F" });

  var conf = WhatsNewClient.configs[ti.name];
  if (conf == null || conf.length == 0)
    return null;

  return conf.first().getDefaultIcon();
}
