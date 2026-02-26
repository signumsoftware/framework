import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage, RoleEntity } from '../Signum.Authorization'
import { FormGroup } from '@framework/Lines/FormGroup'
import { TypeContext } from '@framework/TypeContext'
import { classes } from '@framework/Globals'
import { useForceUpdate } from '@framework/Hooks'
import { LinkButton } from '@framework/Basics/LinkButton'
import { AuthClient } from '../AuthClient'
import { Lite } from '@framework/Signum.Entities'

export function DoublePassword(p: { ctx: TypeContext<string>, initialOpen: boolean, mandatory: boolean, onChange: ()=> void, userRole?: Lite<RoleEntity> }): React.JSX.Element {

  const [isOpen, setIsOpen] = React.useState(p.initialOpen);
  const [complexityWarning, setComplexityWarning] = React.useState<string | undefined>(undefined);
  var newPass = React.useRef<HTMLInputElement>(null);
  var newPass2 = React.useRef<HTMLInputElement>(null);
  const forceUpdate = useForceUpdate();


  async function handlePasswordBlur(e: React.SyntheticEvent<any>) {
    const ctx = p.ctx;

    // Validate password complexity and length first (for first password field)
    if (newPass.current!.value && e.currentTarget === newPass.current) {
      try {
        const validationResult = await AuthClient.API.validatePassword(newPass.current!.value);
        if (validationResult.errorMessage) {
          ctx.error = validationResult.errorMessage;
        } else {
          ctx.error = undefined;
        }
        // Always set complexity warning if it exists, regardless of other errors
        setComplexityWarning(validationResult.complexityWarning);
      } catch (error) {
        console.error("Password validation error:", error);
      }
    }

    // Check if passwords match
    if (newPass.current!.value && newPass2.current!.value) {
      if (newPass.current!.value != newPass2.current!.value) {
        ctx.error = LoginAuthMessage.PasswordsAreDifferent.niceToString();
      }
      else {
        // Only clear error and set value if no validation error exists
        if (!ctx.error || ctx.error === LoginAuthMessage.PasswordsAreDifferent.niceToString()) {
          ctx.error = undefined;
          ctx.value = newPass.current!.value;
          p.onChange();
        }
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
        {inputId => 
          <>
            <input id={inputId} type="password" ref={newPass} autoComplete="off" placeholder={LoginAuthMessage.NewPassword.niceToString()} className={classes(p.ctx.formControlClass, p.mandatory && !newPass.current?.value ? "sf-mandatory" : null)} onBlur={handlePasswordBlur} />
            {complexityWarning && (
              <div className="alert alert-warning mt-2" role="alert">
                {complexityWarning}
              </div>
            )}
          </>
        }
      </FormGroup>
      <FormGroup ctx={p.ctx} label={LoginAuthMessage.ConfirmNewPassword.niceToString()}>
        {inputId => <input id={inputId} type="password" ref={newPass2} autoComplete="off" placeholder={LoginAuthMessage.ConfirmNewPassword.niceToString()} className={classes(p.ctx.formControlClass, p.mandatory && !newPass2.current?.value ? "sf-mandatory" : null)} onBlur={handlePasswordBlur} />}
      </FormGroup>
    </div>
  );
}
