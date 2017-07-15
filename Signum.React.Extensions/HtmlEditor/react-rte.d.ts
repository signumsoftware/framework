

declare module 'react-rte' {

    import * as React from "react"

    export type EditorFormat = "html" | "markdown";
    export interface EditorValue {
        toString(format: EditorFormat): string;
    }

    export interface RichTextEditorProps {
        value: EditorValue;
        onChange: (value: EditorValue) => void;
        onBlur?: () => void;
        autoFocus?: boolean;
        placeholder?: string;
        readOnly?: boolean;
    }


    export default class RichTextEditor extends React.Component<RichTextEditorProps>{
        static createEmptyValue(): EditorValue;
        static createValueFromString(markup: string, format: EditorFormat): EditorValue;
    }
}