import * as React from 'react'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, Lite, is }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SendEmailTaskEntity, EmailTemplateEntity } from '../Signum.Entities.Mailing'

export interface IFrameRendererProps extends React.HTMLAttributes<HTMLIFrameElement> {
    html: string | null | undefined;
}

export default class IFrameRenderer extends React.Component<IFrameRendererProps> {

    componentDidMount() {
        this.load(this.props.html);
    }

    componentWillReceiveProps(newProps: { html: string }) {
        this.load(newProps.html);
    }

    load(html: string | null | undefined) {
        const cd = this.iframe.contentDocument!;

        cd.body.innerHTML = html || "";
    }

    iframe!: HTMLIFrameElement;

    render() {

        var { html, ...props } = this.props;

        return (<iframe {...props} ref={e => this.iframe = e!}></iframe>);
    }
}

