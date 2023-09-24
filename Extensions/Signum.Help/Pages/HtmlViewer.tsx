import * as React from 'react';
import * as HelpClient from '../HelpClient';
import { ReadonlyBinding } from '@framework/Lines';
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor';
import LinksPlugin from '../../Signum.HtmlEditor/Plugins/LinksPlugin';
import BasicCommandsPlugin from '../../Signum.HtmlEditor/Plugins/BasicCommandsPlugin';
import { ErrorBoundary } from '@framework/Components';


export default function HtmlViewer(p: { text: string | null | undefined; htmlAttributes?: React.HTMLAttributes<HTMLDivElement>; }) {

    var htmlText = React.useMemo(() => HelpClient.replaceHtmlLinks(p.text ?? ""), [p.text]);
    if (!htmlText)
      return null;

    var binding = new ReadonlyBinding(htmlText, "");

    return (
        <div className="html-viewer">
            <ErrorBoundary>
                <HtmlEditor readOnly
                    binding={binding}
                    htmlAttributes={p.htmlAttributes}
                    toolbarButtons={c => null} plugins={[
                        new LinksPlugin(),
                        new BasicCommandsPlugin()
                    ]} />
            </ErrorBoundary>
        </div>
    );
}
