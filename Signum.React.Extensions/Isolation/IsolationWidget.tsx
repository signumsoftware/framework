
import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import { JavascriptMessage, Lite, is, ModifiableEntity, tryGetMixin } from '@framework/Signum.Entities'
import { NavDropdown, Dropdown } from 'react-bootstrap'
import { useAPI } from '@framework/Hooks';
import { LinkContainer } from '@framework/Components'
import { IsolationEntity, IsolationMessage, IsolationMixin } from './Signum.Entities.Isolation';
import * as IsolationClient from './IsolationClient';
import { WidgetContext } from '../../../Framework/Signum.React/Scripts/Frames/Widgets';


export interface IsolationWidgetProps {
  wc: WidgetContext<ModifiableEntity>
}

export function IsolationWidget(p: IsolationWidgetProps) {

  const entity = p.wc.ctx.value;

  var mixin = tryGetMixin(entity, IsolationMixin);

  if (mixin == null)
    return null;


  return (
    <strong className="badge badge-secondary" style={{ display: "flex" }}>{mixin.isolation?.toStr ?? IsolationMessage.GlobalEntity.niceToString()}</strong>
  );
}
