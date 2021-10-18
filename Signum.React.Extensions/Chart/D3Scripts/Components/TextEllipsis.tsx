import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './ChartUtils';
import { translate } from './ChartUtils';


export interface TextEllipsisProps extends React.SVGProps<SVGTextElement>{
  maxWidth: number;
  padding?: number;
  etcText?: string;
}

export default function TextEllipsis({ maxWidth, padding, children, etcText, ...atts } :  TextEllipsisProps) {

  const txt = React.useRef<SVGTextElement>(null);

  React.useEffect(() => {
    var width = maxWidth;
    if (padding)
      width -= padding * 2;

    let txtElem = txt.current!;
    txtElem.textContent = getString(children);
    let textLength = txtElem.getComputedTextLength();
    let text = txtElem.textContent!;
    while (textLength > width && text.length > 0) {
      text = text.slice(0, -1);
      while (text[text.length - 1] == ' ' && text.length > 0)
        text = text.slice(0, -1);
      txtElem.textContent = text + (etcText ??  "…");
      textLength = txtElem.getComputedTextLength();
    }

  }, [maxWidth, padding, etcText, getString(children)]);

  return (
    <text ref={txt} {...atts} >
      {children ?? ""}
    </text>
  );
}

function getString(children: React.ReactNode) {
  return React.Children.toArray(children)[0] as string;
}
