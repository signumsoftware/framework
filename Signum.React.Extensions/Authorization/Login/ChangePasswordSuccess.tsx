import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { useStateWithPromise } from '@framework/Hooks'

export default function ChangePasswordSucess() {
  return (
    <div>
      <h2 className="sf-entity-title">{LoginAuthMessage.PasswordChanged.niceToString()}</h2>
      <p>{LoginAuthMessage.PasswordHasBeenChangedSuccessfully.niceToString()}</p>
    </div>
  );
}
  
