import * as React from 'react'
import { TextPartEntity } from '../Signum.Dashboard';
import { DashboardClient, PanelPartContentProps } from '../DashboardClient';
import Markdown from 'react-markdown';
import { HtmlViewer } from '../Admin/TextPart';
import { useForceUpdate } from '../../../Signum/React/Hooks';
import { translated } from '@framework/Signum.Entities';

export default function TextPart(p: PanelPartContentProps<TextPartEntity>): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  React.useEffect(() => {
    if (p.content.textContent) {
      p.content.textContent = translated(p.content, p => p.textContent) ?? p.content.textContent;
      p.content.textContent = p.content.textContent?.replace(/\$(\w+)\$/g, (_, key) => {
        return DashboardClient.GlobalVariables.get(key)?.() ?? `$${key}$`; // Keep it as-is if not found
      });
      forceUpdate();
    }
  }, []);

  function PreviewType(): React.JSX.Element {
    if (p.content?.textPartType == "Text")
      return (<text>{p.content.textContent}</text>)

    if (p.content?.textPartType == "Markdown")
      return (<Markdown components={{ a: LinkRenderer }}>{p.content.textContent}</Markdown>)

    if (p.content?.textPartType == "HTML" && p.content?.textContent != null)
      return (<HtmlViewer text={p.content.textContent} />)

    return (<text>{p.content?.textContent}</text>)
  }

  return (
    <div>
      <div className="row">
        <div className="col-sm-12">
          <PreviewType />
        </div>
      </div>
    </div>
  );
}

function LinkRenderer(props: React.AnchorHTMLAttributes<HTMLAnchorElement>) {
  return (
    <a href={props.href} target="_blank" rel="noreferrer">
      {props.children}
    </a>
  );
}
