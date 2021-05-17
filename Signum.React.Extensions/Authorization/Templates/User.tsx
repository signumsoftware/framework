import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage, UserOIDMixin } from '../Signum.Entities.Authorization'
import { Binding } from '@framework/Reflection'
import { ValueLine, EntityLine, EntityCombo, FormGroup, TypeContext } from '@framework/Lines'
import { DoublePassword } from './DoublePassword'
import { tryGetMixin } from '@framework/Signum.Entities'

export default function User(p: { ctx: TypeContext<UserEntity> }) {

  const ctx = p.ctx.subCtx({ labelColumns: { sm: 3 } });
  const entity = p.ctx.value;

  var oid = tryGetMixin(entity, UserOIDMixin);

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(e => e.state, { readOnly: true })} />
      <ValueLine ctx={ctx.subCtx(e => e.userName)} readOnly={User.userNameReadonly(ctx.value) ? true : undefined} />
      {!ctx.readOnly && ctx.subCtx(a => a.passwordHash).propertyRoute?.canModify() && User.changePasswordVisible(ctx.value) &&
        <DoublePassword ctx={new TypeContext<string>(ctx, undefined, undefined as any, Binding.create(ctx.value, v => v.newPassword))} isNew={entity.isNew} />}
      <EntityLine ctx={ctx.subCtx(e => e.role)} />
      <ValueLine ctx={ctx.subCtx(e => e.email)} readOnly={User.emailReadonly(ctx.value) ? true : undefined} />
      <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo)} />
    </div>
  );
}

User.changePasswordVisible = (user: UserEntity) => tryGetMixin(user, UserOIDMixin)?.oID == null;
User.userNameReadonly = (user: UserEntity) => tryGetMixin(user, UserOIDMixin)?.oID != null;
User.emailReadonly = (user: UserEntity) => tryGetMixin(user, UserOIDMixin)?.oID != null;
