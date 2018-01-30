/// <reference path="codemirror.d.ts" />
import * as React from 'react'
import * as CodeMirror from 'codemirror'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'

import "codemirror/lib/codemirror.css"


export interface CodeMirrorProps {
    onChange?: (value: string) => void,
    onFocusChange?: (focused: boolean) => void,
    options?: CodeMirror.EditorConfiguration,
    path?: string,
    value?: string | null,
    className?: string,
    errorLineNumber?: number;
}

export default class CodeMirrorComponent extends React.Component<CodeMirrorProps, { isFocused: boolean }> {

    codeMirror!: CodeMirror.EditorFromTextArea;

    constructor(props: CodeMirrorProps) {
        super(props);
        this.state = { isFocused: false, };
    }

    textArea!: HTMLTextAreaElement; 

    componentDidMount() {
        this.codeMirror = CodeMirror.fromTextArea(this.textArea!, this.props.options);
        if (this.props.onChange)
            this.codeMirror.on('change', this.codemirrorValueChanged);
        this.codeMirror.on('focus', () => this.focusChanged(true));
        this.codeMirror.on('blur', () => this.focusChanged.bind(false));
        this.codeMirror.setValue(this.props.value || '');
        if (this.props.errorLineNumber != null)
            this.lineHandle = this.codeMirror.addLineClass(this.props.errorLineNumber - 1, undefined, "exceptionLine");
    }
    componentWillUnmount() {
        // todo: is there a lighter-weight way to remove the cm instance?
        if (this.codeMirror) {
            this.codeMirror.toTextArea();
        }
    }

    lineHandle?: CodeMirror.LineHandle;
    componentWillReceiveProps(nextProps: CodeMirrorProps) {
        if (this.codeMirror) {
            if (nextProps.value != undefined && this.codeMirror.getValue() !== nextProps.value) {
                this.codeMirror.off('change', this.codemirrorValueChanged);
                this.codeMirror.setValue(nextProps.value);
                this.codeMirror.on('change', this.codemirrorValueChanged);
            }

            if (typeof nextProps.options === 'object') {
                for (let optionName in nextProps.options) {
                    if (nextProps.options.hasOwnProperty(optionName)) {
                        this.codeMirror.setOption(optionName, (nextProps.options as any)[optionName]);
                    }
                }
            }
            
            if (this.lineHandle != undefined)
                this.codeMirror.removeLineClass(this.lineHandle, undefined, undefined);

            if (nextProps.errorLineNumber != null)
                this.lineHandle = this.codeMirror.addLineClass(nextProps.errorLineNumber - 1, undefined, "exceptionLine");
            
        }
    }
    
    focus() {
        if (this.codeMirror) {
            this.codeMirror.focus();
        }
    }

    focusChanged(focused: boolean) {
        this.setState({
            isFocused: focused,
        });
        this.props.onFocusChange && this.props.onFocusChange(focused);
    }

    codemirrorValueChanged = (doc: CodeMirror.Editor, change: CodeMirror.EditorChangeLinkedList) => {
        const newValue = doc.getValue();
        if (newValue != this.props.value && this.props.onChange)
            this.props.onChange(newValue);
    }
    render() {
        const editorClassName = classes(
            'ReactCodeMirror',
            this.state.isFocused ? 'ReactCodeMirror--focused' : undefined,
            this.props.className
        );

        const css = ".exceptionLine { background: pink }";
        return (
            <div className={editorClassName}>
                <style>{css}</style>
                <textarea ref={ta => this.textArea = ta!} name={this.props.path} defaultValue={this.props.value || undefined} autoComplete="off" />
            </div>
        );
    }
}
