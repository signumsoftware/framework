import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '../Navigator'
import { TypeContext } from '../TypeContext';
import { ExceptionEntity } from '../Signum.Basics';
import { BigStringEmbedded } from '../Signum.Entities';
import { AutoLine, TextAreaLine } from '../Lines';

export namespace ExceptionClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(ExceptionEntity, e => import('./Exception'), { allowWrapEntityLink: true }));
    AutoLine.registerComponent(BigStringEmbedded.typeName, (tr, pr) => {
      if (tr.isCollection)
        return undefined;
  
      return ({ ctx, ...rest }) => <TextAreaLine ctx={(ctx as TypeContext<BigStringEmbedded>).subCtx(a => a.text)}  {...rest as any} readOnly />;
    });
  }
}
