import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage } from '../Signum.Authorization'
import { FormGroup } from '@framework/Lines/FormGroup'
import { TypeContext } from '@framework/TypeContext'
import { classes } from '@framework/Globals'
import { useForceUpdate } from '@framework/Hooks'
import { LinkButton } from '@framework/Basics/LinkButton'
import { AuthClient } from '../AuthClient'

export function DoublePassword(p: { ctx: TypeContext<string>, initialOpen: boolean, mandatory: boolean, onChange?: ()=> void }): React.JSX.Element {

  const [isOpen, setIsOpen] = React.useState(p.initialOpen);
  const [passValidation, setPassValidation] = React.useState<AuthClient.PasswordValidationResult | null>(null);
  var newPass = React.useRef<HTMLInputElement>(null);
  var newPass2 = React.useRef<HTMLInputElement>(null);
  const forceUpdate = useForceUpdate();

  async function handlePasswordChange(e: React.SyntheticEvent<any>) {
    const ctx = p.ctx;
    const user = ctx.frame!.pack.entity as UserEntity;
    user.passwordIsChanging = true;
    user.modified = true;

    if (newPass.current!.value && AuthClient.validatePassword) {
      const result = await AuthClient.validatePassword(newPass.current!.value, user);

      setPassValidation(result);
        
      if (result?.level == "error") {
        ctx.error = result.message;
      } else {
        ctx.error = undefined;
        if (newPass.current?.value && newPass2.current?.value && newPass.current.value != newPass2.current.value) {
          ctx.error = LoginAuthMessage.PasswordsAreDifferent.niceToString();
        }
      }
    } else {
      setPassValidation(null);
      ctx.error = undefined;
    }
    forceUpdate();
    ctx.frame!.revalidate();
  }

  function handlePasswordBlur(e: React.SyntheticEvent<any>) {
    const ctx = p.ctx;

    const firstValue = newPass.current!.value;
    const secondValue = newPass2.current!.value;

    if (passValidation?.level == 'error') {
      ctx.error = passValidation.message;
      }
    else if (firstValue && secondValue && firstValue == secondValue) {
        ctx.error = undefined;
      ctx.value = firstValue;
      const user = ctx.frame!.pack.entity as UserEntity;
      user.passwordIsChanging = false;
      setPassValidation(null);
        p.onChange?.();
      }
    else if (firstValue || secondValue) {
      ctx.error = LoginAuthMessage.PasswordsAreDifferent.niceToString();
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
      <FormGroup label={LoginAuthMessage.NewPassword.niceToString()} ctx={p.ctx} error={null} >
        {inputId => (
          <>
            <input 
              id={inputId} 
              type="password" 
              ref={newPass} 
              autoComplete="off" 
              placeholder={LoginAuthMessage.NewPassword.niceToString()} 
              className={classes(p.ctx.formControlClass, p.mandatory && !newPass.current?.value ? "sf-mandatory" : null, passValidation && "is-invalid")} 
              onChange={handlePasswordChange} 
              onBlur={handlePasswordBlur} 
            />
            {passValidation && <span className={classes('help-block', passValidation.level == 'error' ? 'text-danger' : 'text-warning')}>{passValidation.message}</span>}
          </>
        )}
      </FormGroup>
      <FormGroup ctx={p.ctx} label={LoginAuthMessage.ConfirmNewPassword.niceToString()} error={null}>
        {inputId => (
          <>
            <input 
              id={inputId} 
              type="password" 
              ref={newPass2} 
              autoComplete="off" 
              placeholder={LoginAuthMessage.ConfirmNewPassword.niceToString()} 
              className={classes(p.ctx.formControlClass, p.mandatory && !newPass2.current?.value ? "sf-mandatory" : null)} 
              onBlur={handlePasswordBlur} 
            />           
          </>
        )}
      </FormGroup>
    </div>
  );
}

