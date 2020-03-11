import { classes } from '@framework/Globals';
import { ModelState } from '@framework/Signum.Entities';
import * as QueryString from 'query-string';
import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import * as AuthClient from '../AuthClient';
import { AuthMessage } from '../Signum.Entities.Authorization';

interface ResetPasswordProps extends RouteComponentProps<{ queryName: string; }> {
}

interface ResetPasswordState {
  codeSent?: boolean;
  code?: string;
  success?: boolean;
  modelState?: ModelState;
  hasError?: boolean;
}

export default class ResetPassword extends React.Component<ResetPasswordProps, ResetPasswordState> {
  username!: HTMLInputElement;
  password!: HTMLInputElement;
  confirmPassword!: HTMLInputElement;

  constructor(props: ResetPasswordProps) {
    super(props);
    this.state = {};
  }

  async componentWillMount() {
    const query = QueryString.parse(this.props.location.search);
    if (query.code) {
      const code = query.code.toString();
      const req = await AuthClient.API.fetchResetPasswordRequest(code);

      if (req && !req.lapsed) {
        this.setState({ code, hasError: false });
      } else {
        this.setState({ hasError: true });
      }
    }
  }

  async handleSubmit(e: React.FormEvent<any>) {
    e.preventDefault();
    await AuthClient.API.fetchResetPasswordMail(this.username.value);
    this.setState({ codeSent: true });
  }

  async setPassword(e: React.FormEvent<any>) {
    e.preventDefault();

    const request: AuthClient.API.SetPasswordRequest = {
      code: this.state.code!,
      password: this.password.value,
      confirmPassword: this.confirmPassword.value
    };

    try {
      await AuthClient.API.setPassword(request);
      this.setState({ code: undefined, success: true });
    } catch (e) {
      if (e.modelState) {
        this.setState({ modelState: e.modelState });
      } else {
        this.setState({ code: undefined, hasError: true });
      }
    }
  }

  error(field: string): string | undefined {
    const ms = this.state.modelState;
    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  handlePasswordBlur = (event: React.SyntheticEvent<any>) => {
    this.setState({ modelState: { ...this.state.modelState, ...this.validatePassword(event.currentTarget == this.confirmPassword) } as ModelState });
  };

  validatePassword(isSecond: boolean) {
    return {
      ['password']:
        !isSecond ? [] :
          !this.password.value && !this.confirmPassword.value ? [AuthMessage.PasswordMustHaveAValue.niceToString()] :
            this.password.value != this.confirmPassword.value ? [AuthMessage.PasswordsAreDifferent.niceToString()] :
              []
    };
  }

  renderDefault() {
    return (
      <>
        {this.state.hasError && (
          <div className="alert alert-danger alert-dismissible fade show">
            {AuthMessage.ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin.niceToString()}
            <button type="button" className="close" onClick={() => this.setState({ hasError: false })}>
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
        )}
        <form onSubmit={(e) => this.handleSubmit(e)}>
          <div className="row">
            <div className="offset-sm-2 col-sm-6">
              <h2 className="sf-entity-title">{AuthMessage.ResetPassword.niceToString()}</h2>
              <p>{AuthMessage.WeWillSendYouAnEmailWithALinkToResetYourPassword.niceToString()}</p>
            </div>
          </div>
          <div className="form-group row">
            <label className="col-form-label col-sm-2">{AuthMessage.Username.niceToString()}</label>
            <div className="col-sm-4">
              <input type="text" className="form-control" id="username" ref={r => this.username = r!} required />
            </div>
          </div>
          <div className="row">
            <div className="offset-sm-2 col-sm-6">
              <button type="submit" className="btn btn-primary"
                      id="resetPassword">{AuthMessage.ResetPassword.niceToString()}</button>
            </div>
          </div>
        </form>
      </>
    );
  }

  renderCodeSent() {
    return (
      <div className="row">
        <div className="offset-sm-2 col-sm-6">
          <h2 className="sf-entity-title">{AuthMessage.ResetPassword.niceToString()}</h2>
          <p>{AuthMessage.ResetPasswordCodeHasBeenSent.niceToString()}</p>
        </div>
      </div>
    );
  }

  renderSetPassword() {
    return (
      <form onSubmit={(e) => this.setPassword(e)}>
        <div className="row">
          <div className="offset-sm-2 col-sm-6">
            <h2 className="sf-entity-title">{AuthMessage.ResetPassword.niceToString()}</h2>
            <p>{AuthMessage.PleaseEnterYourChosenNewPassword.niceToString()}</p>
          </div>
        </div>
        <div>
          <div className={classes('form-group row', this.error('password') && 'has-error')}>
            <label className="col-form-label col-sm-2">{AuthMessage.EnterTheNewPassword.niceToString()}</label>
            <div className="col-sm-4">
              <input type="password" className="form-control" id="password" ref={r => this.password = r!}
                     onBlur={this.handlePasswordBlur} required />
              {this.error('password') && <span className="help-block">{this.error('password')}</span>}
            </div>
          </div>
          <div className={classes('form-group row', this.error('password') && 'has-error')}>
            <label
              className="col-form-label col-sm-2">{AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString()}</label>
            <div className="col-sm-4">
              <input type="password" className="form-control" id="confirmPassword" ref={r => this.confirmPassword = r!}
                     onBlur={this.handlePasswordBlur} required />
              {this.error('password') && <span className="help-block">{this.error('password')}</span>}
            </div>
          </div>
        </div>
        <div className="row">
          <div className="offset-sm-2 col-sm-6">
            <button type="submit" className="btn btn-primary"
                    id="setPassword">{AuthMessage.ChangePassword.niceToString()}</button>
          </div>
        </div>
      </form>
    );
  }

  renderResetPasswordSuccess() {
    return (
      <div className="row">
        <div className="offset-sm-2 col-sm-6">
          <h2 className="sf-entity-title">{AuthMessage.PasswordChanged.niceToString()}</h2>
          <p>{AuthMessage.ResetPasswordSuccess.niceToString()}</p>
        </div>
      </div>
    );
  }

  render() {
    if (this.state.codeSent) {
      return this.renderCodeSent();
    }

    if (this.state.code) {
      return this.renderSetPassword();
    }

    if (this.state.success) {
      return this.renderResetPasswordSuccess();
    }

    return this.renderDefault();
  }
}
