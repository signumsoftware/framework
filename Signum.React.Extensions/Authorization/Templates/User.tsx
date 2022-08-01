import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage, UserADMixin } from '../Signum.Entities.Authorization'
import { Binding } from '@framework/Reflection'
import { ValueLine, EntityLine, EntityCombo, FormGroup, TypeContext } from '@framework/Lines'
import { DoublePassword } from './DoublePassword'
import { tryGetMixin } from '@framework/Signum.Entities'
import * as ActiveDirectoryClient from "../ActiveDirectoryClient";
import { useAPI } from '../../../Signum.React/Scripts/Hooks'
import * as AppContext from "@framework/AppContext"
import { useEffect, useState } from 'react'
import ProfilePhoto from './ProfilePhoto'

export default function User(p: { ctx: TypeContext<UserEntity> }) {

  const ctx = p.ctx.subCtx({ labelColumns: { sm: 3 } });
  const entity = p.ctx.value;

  var oid = tryGetMixin(entity, UserADMixin);

  return (
    <div>
      <div className="row">
        <div className="col-sm-2">
          <ProfilePhoto user={ctx.value } />
        </div>
        <div className="col-sm-8">
          <ValueLine ctx={ctx.subCtx(e => e.state, { readOnly: true })} />
          <ValueLine ctx={ctx.subCtx(e => e.userName)} readOnly={User.userNameReadonly(ctx.value) ? true : undefined} />
          {!ctx.readOnly && ctx.subCtx(a => a.passwordHash).propertyRoute?.canModify() && User.changePasswordVisible(ctx.value) &&
            <DoublePassword ctx={new TypeContext<string>(ctx, undefined, undefined as any, Binding.create(ctx.value, v => v.newPassword))} isNew={entity.isNew} mandatory />}
          <EntityLine ctx={ctx.subCtx(e => e.role)} />
          <ValueLine ctx={ctx.subCtx(e => e.email)} readOnly={User.emailReadonly(ctx.value) ? true : undefined} />
          <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo)} />
        </div>
      </div>
    </div>
  );
}

User.changePasswordVisible = (user: UserEntity) => tryGetMixin(user, UserADMixin)?.oID == null;
User.userNameReadonly = (user: UserEntity) => tryGetMixin(user, UserADMixin)?.oID != null;
User.emailReadonly = (user: UserEntity) => tryGetMixin(user, UserADMixin)?.oID != null;
