import * as React from 'react'

export interface TextIfFitsProps extends React.SVGProps<SVGTextElement>{
  maxWidth: number;
  padding?: number;
  etcText?: string;
}

export default function TextIfFits({ maxWidth, padding, children, etcText, ...atts } :  TextIfFitsProps): React.JSX.Element {

  const txt = React.useRef<SVGTextElement>(null);

  React.useEffect(() => {
    var width = maxWidth;
    if (padding)
      width -= padding * 2;

    let txtElem = txt.current!;
    txtElem.textContent = getString(children);
    let textLength = txtElem.getComputedTextLength();
    console.log("Width:", width, " textLength:", textLength, " text: ", txtElem.textContent);
    if (textLength > width)
      txtElem.textContent = "";
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
