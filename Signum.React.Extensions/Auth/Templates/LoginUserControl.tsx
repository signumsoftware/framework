/// <reference path="../../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Link } from 'react-router'
import { Authorization } from 'Signum.Entities.Extensions'
import * as AuthClient from 'Extensions/Signum.React.Extensions/Auth/Scripts/AuthClient'

var AuthMessage = Authorization.AuthMessage;

export default class LoginUserControl extends React.Component<{}, {}> {

    componentDidMount() {

    }

    render() {

        if (!AuthClient.currentUser())
            return <Link to={"auth/login"}></Link>;
        
        return <div></div>;
    }
}
