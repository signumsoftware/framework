import * as React from 'react'

export interface TextIfFitsProps extends React.SVGProps<SVGTextElement>{
  maxWidth: number;
  padding?: number;
  etcText?: string;
}

export default function TextIfFits({ maxWidth, padding, children, etcText, onClick, ...atts } :  TextIfFitsProps): React.JSX.Element {

  const txt = React.useRef<SVGTextElement>(null);

  React.useEffect(() => {
    var width = maxWidth;
    if (padding)
      width -= padding * 2;

    let txtElem = txt.current!;
    txtElem.textContent = getString(children);
    let textLength = txtElem.getComputedTextLength();
    if (textLength > width)
      txtElem.textContent = "";
  }, [maxWidth, padding, etcText, getString(children)]);

  const interactive = typeof onClick === "function";
  const accessibilityPropsOnClick = interactive
    ? {
      role: "button",
      tabIndex: 0,
      cursor: "pointer",
      onKeyDown: (e: React.KeyboardEvent<SVGTextElement>) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          (onClick as any)?.(e);
        }
      },
    }
    : {};

  return (
    <text ref={txt} {...atts} {...accessibilityPropsOnClick}>
      {children ?? ""}
    </text>
  );
}

function getString(children: React.ReactNode) {
  return React.Children.toArray(children)[0] as string;
}
