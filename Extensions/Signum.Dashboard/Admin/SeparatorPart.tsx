import * as React from 'react';
import { TypeContext, AutoLine } from '@framework/Lines';
import { SeparatorPartEntity } from '../Signum.Dashboard';

export default function SeparatorPart(p: { ctx: TypeContext<SeparatorPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div className="form-inline">
      <AutoLine ctx={ctx.subCtx(c => c.title)} />
    </div>
  );
}
