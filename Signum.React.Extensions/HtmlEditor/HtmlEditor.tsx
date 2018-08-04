/// <reference path="react-rte.d.ts" />
import * as React from 'react'
import RichTextEditor, { EditorValue } from 'react-rte';
import { IBinding } from '@framework/Reflection';

export interface HtmlEditorProps {
    binding: IBinding<string | null | undefined>;
    readonly?: boolean;
}

export default class HtmlEditor extends React.Component<HtmlEditorProps, { editorValue: EditorValue }>{
    constructor(props: HtmlEditorProps) {
        super(props);

        this.state = { editorValue: RichTextEditor.createValueFromString(props.binding.getValue() || "", "html") };
    }

    componentWillUnmount() {
        this.saveHtml();
    }

    saveHtml() {
        if (!this.props.readonly)
            this.props.binding.setValue(this.state.editorValue.toString("html") || "");
    }

    render() {
        return (
            <RichTextEditor
                value={this.state.editorValue}
                readOnly={this.props.readonly}
                onChange={ev => this.setState({ editorValue: ev })}
                onBlur={() => this.saveHtml()}
                />
        );
    }
}

