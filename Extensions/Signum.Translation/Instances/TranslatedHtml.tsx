import * as React from 'react'
import { softCast } from '@framework/Globals'
import { IBinding, ReadonlyBinding } from '@framework/Reflection'
import { ErrorBoundary } from '@framework/Components'
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor'
import { LinkExtension } from '../../Signum.HtmlEditor/Extensions/LinkExtension'
import '../../Signum.HtmlEditor/HtmlEditorLine.css'

/** Read-only rich rendering for a translatable `Format(FormatAttribute.Html)` property. */
export function TranslatedHtmlViewer(p: { text: string | null | undefined }): React.JSX.Element {
  const binding = new ReadonlyBinding<string | null | undefined>(p.text ?? "", "");
  return (
    <div className="html-viewer">
      <ErrorBoundary>
        <HtmlEditor
          readOnly
          binding={binding}
          toolbarButtons={() => null}
          extensionsMemo={[new LinkExtension()]}
        />
      </ErrorBoundary>
    </div>
  );
}

/** Editable rich rendering for a translatable `Format(FormatAttribute.Html)` property. */
export function TranslatedHtmlEditor(p: { text: string | null | undefined, onChange: (newText: string) => void }): React.JSX.Element {
  // Keep the latest props in a ref so the binding created once below always reads/writes the current value
  // (HtmlEditor reads getValue only on mount and writes via setValue, so the binding must stay stable).
  const propsRef = React.useRef(p);
  propsRef.current = p;

  const binding = React.useMemo(() => softCast<IBinding<string | null | undefined>>({
    getValue: () => propsRef.current.text,
    setValue: v => propsRef.current.onChange(v ?? ""),
    suffix: "",
    getIsReadonly: () => false,
    getIsHidden: () => false,
    getError: () => undefined,
    setError: () => { },
  }), []);

  return (
    <div className="html-editor-line" style={{ width: "90%" }}>
      <ErrorBoundary>
        <HtmlEditor binding={binding} extensionsMemo={[new LinkExtension()]} />
      </ErrorBoundary>
    </div>
  );
}
