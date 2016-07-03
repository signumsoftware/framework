import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, Lite, is }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SendEmailTaskEntity, EmailTemplateEntity } from '../Signum.Entities.Mailing'

export default class IFrameRenderer extends React.Component<{ html: string }, void> {

    componentDidMount() {
        this.load();
    }

    componentWillUpdate() {
        this.load();
    }

    load() {
        var cd = this.iframe.contentDocument;

        cd.body.innerHTML = this.props.html;
    }

    iframe: HTMLIFrameElement;

    render() {
        return (<iframe style={{ width: "100%" }} ref={e => this.iframe= e}></iframe>);
    }


}

