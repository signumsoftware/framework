import * as React from 'react'
import { TourEntity } from '../Signum.Tour'
import { useForceUpdate } from '@framework/Hooks';
import { AutoLine, CheckboxLine, EntityAccordion, TypeContext } from '@framework/Lines';
import TourStep from './TourStep';
import { Navigator } from '@framework/Navigator';
import { TypeEntity } from '@framework/Signum.Basics';

export default function Tour(p: { ctx: TypeContext<TourEntity> }): React.ReactElement {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  const type = Navigator.useFetchInState(TypeEntity.isLite(p.ctx.value.trigger) ? p.ctx.value.trigger : null);
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(a => a.trigger)} />

      <EntityAccordion ctx={ctx.subCtx(a => a.steps)}
        getComponent={ctx => <TourStep ctx={ctx} invalidate={forceUpdate} type={type} />}
        getTitle={ctx => ctx.value.title || ""} />

      <div className="row">
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(a => a.showProgress)} inlineCheckbox={true} />
        </div>
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(a => a.animate)} inlineCheckbox={true} />
        </div>
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(a => a.showCloseButton)} inlineCheckbox={true} />
        </div>
      </div>
    </div>
  );
}
