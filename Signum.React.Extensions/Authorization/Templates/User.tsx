import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage } from '../Signum.Entities.Authorization'
import { Binding } from '@framework/Reflection'
import { ValueLine, EntityLine, EntityCombo, FormGroup, TypeContext } from '@framework/Lines'
import { DoublePassword } from './DoublePassword'

export default function User(p: { ctx: TypeContext<UserEntity> }) {

  const ctx = p.ctx.subCtx({ labelColumns: { sm: 3 } });
  const entity = p.ctx.value;

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(e => e.state, { readOnly: true })} />
      <ValueLine ctx={ctx.subCtx(e => e.userName)} />
      {!ctx.readOnly && ctx.subCtx(a => a.passwordHash).propertyRoute?.canModify() &&
        <DoublePassword ctx={new TypeContext<string>(ctx, undefined, undefined as any, Binding.create(ctx.value, v => v.newPassword))} isNew={entity.isNew} />}
      <EntityLine ctx={ctx.subCtx(e => e.role)} />
      <ValueLine ctx={ctx.subCtx(e => e.email)} />
      <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo)} />
    </div>
  );
}
