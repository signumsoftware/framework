/// <reference path="codemirror.d.ts" />
import * as React from 'react'
import * as CM from 'codemirror'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'


export interface CodeMirrorProps {
    onChange: (value: string) => void,
    onFocusChange: (focused: boolean) => void,
    options: CM.EditorConfiguration,
    path: string,
    value: string,
    className: string,
    defaultValue: string;
}

export default class CodeMirror extends React.Component<CodeMirrorProps, { isFocused: boolean }> {

    codeMirror: CM.EditorFromTextArea;
    _currentCodemirrorValue: string;

    constructor(props) {
        super(props);
        this.state = { isFocused: false, };
    }

    componentDidMount() {
        const textareaNode = this.refs["textarea"] as HTMLTextAreaElement;
        this.codeMirror = CM.fromTextArea(textareaNode, this.props.options);
        this.codeMirror.on('change', this.codemirrorValueChanged);
        this.codeMirror.on('focus', this.focusChanged.bind(this, true));
        this.codeMirror.on('blur', this.focusChanged.bind(this, false));
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
        if (this.codeMirror && nextProps.value !== undefined && this._currentCodemirrorValue !== nextProps.value) {
            this.codeMirror.setValue(nextProps.value);
        }
        if (typeof nextProps.options === 'object') {
            for (let optionName in nextProps.options) {
                if (nextProps.options.hasOwnProperty(optionName)) {
                    this.codeMirror.setOption(optionName, nextProps.options[optionName]);
                }
            }
        }
    }
    getCodeMirror() {
        return this.codeMirror;
    }

    focus() {
        if (this.codeMirror) {
            this.codeMirror.focus();
        }
    }
    focusChanged(focused) {
        this.setState({
            isFocused: focused,
        });
        this.props.onFocusChange && this.props.onFocusChange(focused);
    }

    codemirrorValueChanged(doc, change) {
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
                <textarea ref="textarea" name={this.props.path} defaultValue={this.props.value} autoComplete="off" />
            </div>
        );
    }
}
