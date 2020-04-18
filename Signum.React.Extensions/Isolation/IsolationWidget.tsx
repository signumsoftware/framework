
import * as React from 'react'
import { ModifiableEntity, tryGetMixin } from '@framework/Signum.Entities'
import { IsolationMessage, IsolationMixin } from './Signum.Entities.Isolation';
import * as IsolationClient from './IsolationClient';
import { WidgetContext } from '@framework/Frames/Widgets';

export interface IsolationWidgetProps {
  wc: WidgetContext<ModifiableEntity>
}

export function IsolationWidget(p: IsolationWidgetProps) {

  const entity = p.wc.ctx.value;

  var mixin = tryGetMixin(entity, IsolationMixin);

  if (mixin == null)
    return null;

  const isolation = entity.isNew ? IsolationClient.getOverridenIsolation().current?.toStr ?? IsolationMessage.GlobalEntity.niceToString() :
    mixin.isolation?.toStr ?? IsolationMessage.GlobalEntity.niceToString();

  return (
    <strong className="badge badge-secondary" style={{ display: "flex" }}>{isolation}</strong>
  );
}
