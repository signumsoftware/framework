import * as React from 'react'
import { TourEntity } from '../Signum.Tour'
import { useForceUpdate } from '@framework/Hooks';
import { AutoLine, EntityAccordion, TypeContext } from '@framework/Lines';
import TourStep from './TourStep';

export default function Tour(p: { ctx: TypeContext<TourEntity> }): React.ReactElement {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(a => a.name)} />
      <EntityAccordion ctx={ctx.subCtx(a => a.steps)} onChange={forceUpdate} 
        getComponent={ctx => <TourStep ctx={ctx} />} />
      <div className="row">
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(a => a.showProgress)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(a => a.animate)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(a => a.showCloseButton)} />
        </div>
      </div>
    </div>
  );
}
