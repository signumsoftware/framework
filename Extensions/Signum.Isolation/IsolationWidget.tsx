
import * as React from 'react'
import { getToString, ModifiableEntity, tryGetMixin } from '@framework/Signum.Entities'
import { IsolationMessage, IsolationMixin } from './Signum.Isolation';
import { IsolationClient } from './IsolationClient';
import { WidgetContext } from '@framework/Frames/Widgets';

export interface IsolationWidgetProps {
  wc: WidgetContext<ModifiableEntity>
}

export function IsolationWidget(p: IsolationWidgetProps): React.JSX.Element | null {

  const entity = p.wc.ctx.value;

  var mixin = tryGetMixin(entity, IsolationMixin);

  if (mixin == null)
    return null;

  const isolation = entity.isNew ? IsolationClient.getOverridenIsolation() : mixin.isolation;

  return (
    <strong className="badge btn-tertiary" style={{ display: "flex" }}>{isolation == null ? IsolationMessage.GlobalEntity.niceToString() : getToString(isolation)}</strong>
  );
}
