import * as React from 'react'
import { BigStringEmbedded, ExceptionEntity } from '../Signum.Entities.Basics'
import { EntitySettings } from '../Navigator'
import * as Navigator from '../Navigator'
import { customTypeComponent } from '../Lines/DynamicComponent';
import { ValueLine } from '../Lines';
import { TypeContext } from '../TypeContext';

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(ExceptionEntity, e => import('./Exception')));
  customTypeComponent[BigStringEmbedded.typeName] = (ctx: TypeContext<BigStringEmbedded>)=> {
    return <ValueLine ctx={ctx.subCtx(a => a.text)} valueLineType="TextArea" readOnly />;
  };
}
