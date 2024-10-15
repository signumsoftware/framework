import * as React from 'react';
import * as draftjs from 'draft-js';
import { IBinding } from '@framework/Reflection';
import './HtmlEditor.css';
import 'draft-js/dist/Draft.css';
export interface IContentStateConverter {
    contentStateToText(content: draftjs.ContentState): string;
    textToContentState(html: string): draftjs.ContentState;
}
export interface HtmlEditorProps {
    binding: IBinding<string | null | undefined>;
    readOnly?: boolean;
    small?: boolean;
    mandatory?: boolean | "warning";
    converter?: IContentStateConverter;
    innerRef?: React.Ref<draftjs.Editor>;
    decorators?: draftjs.DraftDecorator[];
    plugins?: HtmlEditorPlugin[];
    toolbarButtons?: (c: HtmlEditorController) => React.ReactElement | React.ReactFragment | null;
    htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
    initiallyFocused?: boolean | number;
    onEditorFocus?: (e: React.SyntheticEvent, controller: HtmlEditorController) => void;
    onEditorBlur?: (e: React.SyntheticEvent, controller: HtmlEditorController) => void;
}
export interface HtmlEditorControllerProps {
    binding: IBinding<string | null | undefined>;
    readOnly?: boolean;
    small?: boolean;
    converter: IContentStateConverter;
    decorators?: draftjs.DraftDecorator[];
    plugins?: HtmlEditorPlugin[];
    innerRef?: React.Ref<draftjs.Editor>;
    initiallyFocused?: boolean | number;
}
export declare class HtmlEditorController {
    editor: draftjs.Editor;
    editorState: draftjs.EditorState;
    setEditorState: (newState: draftjs.EditorState) => void;
    overrideToolbar: React.ReactFragment | React.ReactElement | undefined;
    setOverrideToolbar: (newState: React.ReactFragment | React.ReactElement | undefined) => void;
    converter: IContentStateConverter;
    decorators: draftjs.DraftDecorator[];
    plugins: HtmlEditorPlugin[];
    binding: IBinding<string | null | undefined>;
    readOnly?: boolean;
    small?: boolean;
    initialContentState: draftjs.ContentState;
    lastSavedString?: {
        str: string | null;
    };
    createWithContentAndDecorators(contentState: draftjs.ContentState): draftjs.EditorState;
    init(p: HtmlEditorControllerProps): void;
    saveHtml(): void;
    extraButtons(): React.ReactElement | null;
    setRefs: (editor: draftjs.Editor | null) => void;
}
declare const HtmlEditor: React.ForwardRefExoticComponent<HtmlEditorProps & Partial<draftjs.EditorProps> & React.RefAttributes<HtmlEditorController>>;
export default HtmlEditor;
export interface HtmlEditorPlugin {
    getDecorators?(controller: HtmlEditorController): draftjs.DraftDecorator[];
    getToolbarButtons?(controller: HtmlEditorController): React.ReactChild;
    expandConverter?(converter: IContentStateConverter): void;
    expandEditorProps?(props: draftjs.EditorProps, controller: HtmlEditorController): void;
}
//# sourceMappingURL=HtmlEditor.d.ts.map