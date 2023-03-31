import * as React from 'react';
import { TypeContext, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines';
import { ImagePartEntity } from '../Signum.Entities.Dashboard';

export default function ImagePart(p: { ctx: TypeContext<ImagePartEntity> }) {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div className="form-inline">
      <ValueLine ctx={ctx.subCtx(c => c.imageSrcContent)} />
      <ValueLine ctx={ctx.subCtx(c => c.clickActionURL)} />
      <ValueLine ctx={ctx.subCtx(c => c.altText)} />
    </div>
  );
}
