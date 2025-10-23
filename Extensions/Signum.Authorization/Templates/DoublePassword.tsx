import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage } from '../Signum.Authorization'
import { FormGroup } from '@framework/Lines/FormGroup'
import { TypeContext } from '@framework/TypeContext'
import { classes } from '@framework/Globals'
import { useForceUpdate } from '@framework/Hooks'
import { LinkButton } from '@framework/Basics/LinkButton'

export function DoublePassword(p: { ctx: TypeContext<string>, initialOpen: boolean, mandatory: boolean }): React.JSX.Element {

  const [isOpen, setIsOpen] = React.useState(p.initialOpen);
  var newPass = React.useRef<HTMLInputElement>(null);
  var newPass2 = React.useRef<HTMLInputElement>(null);
  const forceUpdate = useForceUpdate();


  function handlePasswordBlur(e: React.SyntheticEvent<any>) {
    const ctx = p.ctx;

    if (newPass.current!.value && newPass2.current!.value) {
      if (newPass.current!.value != newPass2.current!.value) {
        ctx.error = LoginAuthMessage.PasswordsAreDifferent.niceToString()
      }
      else {
        ctx.error = undefined;
        ctx.value = newPass.current!.value;
      }
    }
    forceUpdate();
    ctx.frame!.revalidate();
  }

  if (!isOpen) {
    return (
      <FormGroup label={LoginAuthMessage.NewPassword.niceToString()} ctx={p.ctx}>
        {() => <LinkButton title={undefined} className="btn btn-tertiary btn-sm" onClick={() => setIsOpen(true)}>
          <FontAwesomeIcon aria-hidden={true} icon="key" /> {LoginAuthMessage.ChangePassword.niceToString()}
        </LinkButton>}
      </FormGroup>
    );
  }

  return (
    <div>
      <FormGroup ctx={p.ctx} label={LoginAuthMessage.NewPassword.niceToString()}>
        {inputId => <input id={inputId} type="password" ref={newPass} autoComplete="off" placeholder={LoginAuthMessage.NewPassword.niceToString()} className={classes(p.ctx.formControlClass, p.mandatory && !newPass.current?.value ? "sf-mandatory" : null)} onBlur={handlePasswordBlur} />}
      </FormGroup>
      <FormGroup ctx={p.ctx} label={LoginAuthMessage.ConfirmNewPassword.niceToString()}>
        {inputId => <input id={inputId} type="password" ref={newPass2} autoComplete="off" placeholder={LoginAuthMessage.ConfirmNewPassword.niceToString()} className={classes(p.ctx.formControlClass, p.mandatory && !newPass2.current?.value ? "sf-mandatory" : null)} onBlur={handlePasswordBlur} />}
      </FormGroup>
    </div>
  );
}
