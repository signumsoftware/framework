import * as React from 'react'
import { Dic } from '../Globals'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, JavascriptMessage } from '../Signum.Entities'


export default class ValidationErrors extends React.Component<{ modelState: ModelState }, void>
{
    render() {

        var modelState = this.props.modelState;

        if (!modelState || Dic.getKeys(modelState).length == 0)
            return null;

        return (
            <ul className="validaton-summary alert alert-danger">
                { Dic.getValues(modelState).map(error => <li>{error}</li>) }
            </ul>
        );
    }
}
