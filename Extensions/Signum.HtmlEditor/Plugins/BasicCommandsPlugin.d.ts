import * as draftjs from 'draft-js';
import { HtmlEditorPlugin, HtmlEditorController } from '../HtmlEditor';
export default class BasicCommandsPlugin implements HtmlEditorPlugin {
    expandEditorProps?(props: draftjs.EditorProps, controller: HtmlEditorController): draftjs.EditorProps;
}
//# sourceMappingURL=BasicCommandsPlugin.d.ts.map