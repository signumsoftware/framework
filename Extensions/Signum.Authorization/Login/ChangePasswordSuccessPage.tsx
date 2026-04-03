import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import { Navigator } from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Authorization'
import { AuthClient } from '../AuthClient'
import { useStateWithPromise } from '@framework/Hooks'

export default function ChangePasswordSucessPage(): React.JSX.Element {
  return (
    <div className="container sf-change-password-success">
      <div className="row">
        <div className="col-md-6 offset-md-3">
          <h1 className="sf-entity-title h2">{LoginAuthMessage.PasswordChanged.niceToString()}</h1>
          <p>{LoginAuthMessage.PasswordHasBeenChangedSuccessfully.niceToString()}</p>
        </div>
      </div>
    </div>
  );
}
  
