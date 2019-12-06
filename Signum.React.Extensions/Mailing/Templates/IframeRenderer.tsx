import * as React from 'react'

export interface IFrameRendererProps extends React.HTMLAttributes<HTMLIFrameElement> {
  html: string | null | undefined;
}

export default function IFrameRenderer({ html, ...props }: IFrameRendererProps) {

  const iframe = React.useRef<HTMLIFrameElement>(null)

  React.useEffect(() => {
    iframe.current!.contentDocument!.body.innerHTML = html ?? "";
  }, [html]);

  return <iframe {...props} ref={iframe}></iframe>;
}

