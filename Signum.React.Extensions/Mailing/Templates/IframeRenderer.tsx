import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail } from '@framework/Lines'
import { SearchControl }  from '@framework/Search'
import { getToString, Lite, is }  from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
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

