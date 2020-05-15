import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage } from '../Signum.Entities.Authorization'
import { ValueLine, EntityLine, EntityCombo, FormGroup, TypeContext } from '@framework/Lines'

export function DoublePassword(p: { ctx: TypeContext<string>, isNew: boolean }) {

  const [withPassword, setWithPassword] = React.useState(p.isNew);
  var newPass = React.useRef<HTMLInputElement>(null);
  var newPass2 = React.useRef<HTMLInputElement>(null);

  function handlePasswordBlur(e: React.SyntheticEvent<any>) {
    const ctx = p.ctx;

    if (newPass.current!.value && newPass2.current!.value && newPass.current!.value != newPass2.current!.value) {
      ctx.error = LoginAuthMessage.PasswordsAreDifferent.niceToString()
    }
    else {
      ctx.error = undefined;
      ctx.value = newPass.current!.value;
    }

    ctx.frame!.revalidate();
  }

  if (!withPassword) {
    return (
      <FormGroup labelText={LoginAuthMessage.NewPassword.niceToString()} ctx={p.ctx}>
        <a className="btn btn-light btn-sm" onClick={() => setWithPassword(true)}>
          <FontAwesomeIcon icon="key" /> {LoginAuthMessage.ChangePassword.niceToString()}
        </a>
      </FormGroup>
    );
  }

  return (
    <div>
      <FormGroup ctx={p.ctx} labelText={LoginAuthMessage.NewPassword.niceToString()}>
        <input type="password" ref={newPass} autoComplete="asdfasdf" className={p.ctx.formControlClass} onBlur={handlePasswordBlur} />
      </FormGroup>
      <FormGroup ctx={p.ctx} labelText={LoginAuthMessage.ConfirmNewPassword.niceToString()}>
        <input type="password" ref={newPass2} autoComplete="asdfasdf" className={p.ctx.formControlClass} onBlur={handlePasswordBlur} />
      </FormGroup>
    </div>
  );
}
