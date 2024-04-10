import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import { Navigator } from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Authorization'
import { AuthClient } from '../AuthClient'
import { useStateWithPromise } from '@framework/Hooks'

export default function ChangePasswordSucessPage() {
  return (
    <div className="container sf-change-password-success">
      <div className="row">
        <div className="col-md-6 offset-md-3">
          <h2 className="sf-entity-title">{LoginAuthMessage.PasswordChanged.niceToString()}</h2>
          <p>{LoginAuthMessage.PasswordHasBeenChangedSuccessfully.niceToString()}</p>
        </div>
      </div>
    </div>
  );
}
  
