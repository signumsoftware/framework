import * as React from 'react';
import { TypeContext, AutoLine } from '@framework/Lines';
import { ImagePartEntity } from '../Signum.Dashboard';

export default function ImagePart(p: { ctx: TypeContext<ImagePartEntity> }) {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div className="form-inline">
      <AutoLine ctx={ctx.subCtx(c => c.imageSrcContent)} />
      <AutoLine ctx={ctx.subCtx(c => c.clickActionURL)} />
      <AutoLine ctx={ctx.subCtx(c => c.altText)} />
    </div>
  );
}
