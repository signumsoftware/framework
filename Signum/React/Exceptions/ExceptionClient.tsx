import * as React from 'react'
import { RouteObject } from 'react-router'
import { EntitySettings } from '../Navigator'
import * as Navigator from '../Navigator'
import { customTypeComponent } from '../Lines/DynamicComponent';
import { ValueLine } from '../Lines';
import { TypeContext } from '../TypeContext';
import { ExceptionEntity } from '../Signum.Basics';
import { BigStringEmbedded } from '../Signum.Entities';

export function start(options: { routes: RouteObject[] }) {
  Navigator.addSettings(new EntitySettings(ExceptionEntity, e => import('./Exception'), { allowWrapEntityLink: true }));
  customTypeComponent[BigStringEmbedded.typeName] = (ctx: TypeContext<BigStringEmbedded>)=> {
    return <ValueLine ctx={ctx.subCtx(a => a.text)} valueLineType="TextArea" readOnly />;
  };
}
