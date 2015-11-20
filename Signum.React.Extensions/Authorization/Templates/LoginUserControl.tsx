/// <reference path="../../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Link } from 'react-router'
import { AuthMessage } from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import * as AuthClient from 'Extensions/Signum.React.Extensions/Authorization/AuthClient'
import WebMenuItem from 'Framework/Signum.React/Templates/WebMenuItem'

export default class LoginUserControl extends React.Component<{}, {}> {

    componentDidMount() {

    }

    render() {

        if (!AuthClient.currentUser())
            return <Link to={"auth/login"} className="sf-login"></Link>;

        return <WebMenuItem liAtts={{ className: "sf-user" }} text={AuthClient.currentUser().userName}>
            <WebMenuItem text={AuthMessage.ChangePassword.niceToString() } link={"auth/changPassword"} />
            <WebMenuItem text={AuthMessage.Logout.niceToString() } link={"auth/logout"} />
            </WebMenuItem>;
    }
}
