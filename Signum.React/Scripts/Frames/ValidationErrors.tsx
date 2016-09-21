﻿import * as React from 'react'
import { Dic } from '../Globals'
import { ModifiableEntity, getToString, EntityPack, ModelState, JavascriptMessage } from '../Signum.Entities'
import { GraphExplorer } from '../Reflection'


export default class ValidationErrors extends React.Component<{ entity: ModifiableEntity }, void>
{
    render() {

        const modelState = GraphExplorer.collectModelState(this.props.entity, "");

        if (!modelState || Dic.getKeys(modelState).length == 0)
            return null;

        return (
            <ul className="validaton-summary alert alert-danger">
                { Dic.getValues(modelState).map((error, i) => <li key={i}>{error}</li>) }
            </ul>
        );
    }
}
