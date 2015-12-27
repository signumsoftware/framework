import * as React from 'react'
import * as moment from 'moment'
import { Input, Tab } from 'react-bootstrap'
//import { DatePicker } from 'react-widgets'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from 'Framework/Signum.React/Scripts/TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo} from 'Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from 'Framework/Signum.React/Scripts/Lines/LineBase'


export interface EntityLineProps extends LineBaseProps {

}

export class EntityLine extends LineBase<EntityLineProps, {}> {
    render() {

        var props = Dic.extend({}, this.props);

        runTasks(this, props);

        return <FormGroup ctx={props.ctx} title={props.labelText}>

            </FormGroup>
    }
}