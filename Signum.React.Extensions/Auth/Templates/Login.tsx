/// <reference path="../../../../framework/signum.react/typings/react/react.d.ts" />
/// <reference path="../../../../framework/signum.react/typings/react-widgets/react-widgets.d.ts" />

import * as React from 'react'
import { Calendar } from 'react-widgets'
import { Authorization } from 'Signum.Entities.Extensions'
import * as AuthClient from '../Extensions/Signum.React.Extensions/Auth/Scripts/AuthClient'

var AuthMessage = Authorization.AuthMessage;

interface IsLoginProps {
    isSingleSignOn: boolean;
    userIdentity: boolean;
    userTicket: boolean;
}


export default class Login extends React.Component<IsLoginProps, {}> {

    componentDidMount() {

    }

    render() {
        return (
            <div>
                <div className="col-sm-offset-4">
                     <h2>Login</h2>
                     <p>
                        {this.props.isSingleSignOn ?
                            AuthMessage.Welcome0.niceToString().formatHtml(<code>{this.props.userIdentity}</code>) :
                            AuthMessage.IntroduceYourUserNameAndPassword.niceToString()
                        }
                      </p>
                </div>
                <div className="form-horizontal">
                    <div className="form-group">
                        <label for="username" className="col-sm-offset-2 col-sm-2 control-label">{ AuthMessage.Username.niceToString() }</label>
                        <div className="col-sm-4">
                             <input type="textbox" className="form-control" placeholder={AuthMessage.Username.niceToString() }/>
                        </div>
                    </div>
                    <div className="form-group">
                        <label for="username" className="col-sm-offset-2 col-sm-2 control-label">{ AuthMessage.Username.niceToString() }</label>
                        <div className="col-sm-4">
                             <input type="textbox" className="form-control" placeholder={AuthMessage.Username.niceToString() }/>
                        </div>
                    </div>

                    {AuthClient.UserTicket && (
                    <div className="form-group">
                        <div className="col-sm-offset-4 col-sm-6">
                            <div className="checkbox">
                                <label>
                                    <input type="checkbox">{AuthMessage.RememberMe.niceToString() }</input>
                                </label>
                            </div>
                        </div>
                    </div>) }
                </div>
            </div>);
        }
}
