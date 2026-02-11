import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage, RoleEntity } from '../Signum.Authorization'
import { Binding } from '@framework/Reflection'
import { AutoLine, EntityLine, EntityCombo, FormGroup, TypeContext, CheckboxLine } from '@framework/Lines'
import { DoublePassword } from './DoublePassword'
import { tryGetMixin } from '@framework/Signum.Entities'
import * as AppContext from "@framework/AppContext"
import { useEffect, useState } from 'react'
import ProfilePhoto from './ProfilePhoto'
import { Finder } from '@framework/Finder'
import { AuthAdminClient } from '../AuthAdminClient'
import { CultureClient } from '@framework/Basics/CultureClient'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { Dic } from '@framework/Globals'
import { AuthClient } from '../AuthClient'

export default function User(p: { ctx: TypeContext<UserEntity> }): React.JSX.Element {

  const ctx = p.ctx.subCtx({
    labelColumns: { sm: 3 },
    readOnly: p.ctx.value.state == "Deactivated" ? true : undefined
  });
  const entity = p.ctx.value;
  var cultures = useAPI(signal => CultureClient.getCultures(false), []);
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <div className="row">
        <div className="col-sm-3 d-flex">
          <div className="mx-auto mt-3">
            <ProfilePhoto user={ctx.value} size={150} />
          </div>
        </div>
        <div className="col-sm-8">
          <AutoLine ctx={ctx.subCtx(e => e.state, { readOnly: true })} />
          <AutoLine ctx={ctx.subCtx(e => e.userName)} readOnly={userNameReadonly(ctx.value) ? true : undefined} />
          {!ctx.readOnly && !ctx.subCtx(a => a.passwordHash).isMemberReadOnly() && changePasswordVisible(ctx.value) &&
            <div className="mb-2">
              <DoublePassword ctx={new TypeContext<string>(ctx, undefined, undefined as any, Binding.create(ctx.value, v => v.newPassword))} initialOpen={Boolean(entity.isNew)} mandatory onChange={() => {
                if (!is(ctx.value, AuthClient.currentUser())) {
                  ctx.value.mustChangePassword = true;
                  forceUpdate();
                }
              }} />
              <div className="row">
                <div className="offset-sm-3 col-sm-9">
              <CheckboxLine ctx={ctx.subCtx(e => e.mustChangePassword)} inlineCheckbox />
              </div>
              </div>
            </div>
          }
          <EntityLine ctx={ctx.subCtx(e => e.role)} onFind={() =>
            Finder.findMany(RoleEntity).then(rs => {
              if (rs == null)
                return undefined;

              if (rs.length == 1)
                return rs[0];

              return AuthAdminClient.API.trivialMergeRole(rs);
            })} />

          <AutoLine ctx={ctx.subCtx(e => e.email)} readOnly={emailReadonly(ctx.value) ? true : undefined} />
          <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo)} data={cultures ? Dic.getValues(cultures) : []} />
        </div>
      </div>
    </div>
  );
}

export let changePasswordVisible = (user: UserEntity) => true;
export function setChangePasswordVisibleFunction(newFunction: (user: UserEntity) => boolean): void {
  changePasswordVisible = newFunction;
}

export let userNameReadonly = (user: UserEntity) => false;
export function setUserNameReadonlyFunction(newFunction: (user: UserEntity) => boolean): void {
  userNameReadonly = newFunction;
}

export let emailReadonly = (user: UserEntity) => false;
export function setEmailReadonlyFunction(newFunction: (user: UserEntity) => boolean): void {
  emailReadonly = newFunction;
}
