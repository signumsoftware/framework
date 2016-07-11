/// <reference path="codemirror.d.ts" />
import * as React from 'react'
import * as CodeMirror from 'codemirror'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'

require("!style!css!codemirror/lib/codemirror.css");


export interface CodeMirrorProps {
    onChange?: (value: string) => void,
    onFocusChange?: (focused: boolean) => void,
    options?: CodeMirror.EditorConfiguration,
    path?: string,
    value?: string,
    className?: string,
    defaultValue?: string;
}

export default class CodeMirrorComponent extends React.Component<CodeMirrorProps, { isFocused: boolean }> {

    codeMirror: CodeMirror.EditorFromTextArea;
    _currentCodemirrorValue: string;

    constructor(props: CodeMirrorProps) {
        super(props);
        this.state = { isFocused: false, };
    }

    textArea: HTMLTextAreaElement; 

    componentDidMount() {
            this.codeMirror = CodeMirror.fromTextArea(this.textArea, this.props.options);
            this.codeMirror.on('change', this.codemirrorValueChanged);
            this.codeMirror.on('focus', () => this.focusChanged(true));
            this.codeMirror.on('blur', () => this.focusChanged.bind(false));
            this._currentCodemirrorValue = this.props.defaultValue || this.props.value || '';
            this.codeMirror.setValue(this._currentCodemirrorValue);
    }
    componentWillUnmount() {
        // todo: is there a lighter-weight way to remove the cm instance?
        if (this.codeMirror) {
            this.codeMirror.toTextArea();
        }
    }
    componentWillReceiveProps(nextProps: CodeMirrorProps) {
        if (this.codeMirror) {
            if (nextProps.value !== undefined && this._currentCodemirrorValue !== nextProps.value) {
                this.codeMirror.setValue(nextProps.value);
            }

            if (typeof nextProps.options === 'object') {
                for (let optionName in nextProps.options) {
                    if (nextProps.options.hasOwnProperty(optionName)) {
                        this.codeMirror.setOption(optionName, (nextProps.options as any)[optionName]);
                    }
                }
            }
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
        this._currentCodemirrorValue = newValue;
        this.props.onChange && this.props.onChange(newValue);
    }
    render() {
        const editorClassName = classes(
            'ReactCodeMirror',
            this.state.isFocused ? 'ReactCodeMirror--focused' : null,
            this.props.className
        );
        return (
            <div className={editorClassName}>
                <textarea ref={ta => this.textArea = ta} name={this.props.path} defaultValue={this.props.value} autoComplete="off" />
            </div>
        );
    }
}
