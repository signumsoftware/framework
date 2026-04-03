import * as React from 'react'

export interface IFrameRendererProps extends React.HTMLAttributes<HTMLIFrameElement> {
  html: string | null | undefined;
  manipulateDom?: (doc:Document)=> void
}

export default function IFrameRenderer({ html, manipulateDom, ...props }: IFrameRendererProps): React.JSX.Element {

  const iframe = React.useRef<HTMLIFrameElement>(null)

  React.useEffect(() => {
    iframe.current!.contentDocument!.body.innerHTML = html ?? "";

    if (manipulateDom != null) {
      manipulateDom(iframe.current!.contentDocument!)
    }
  }, [html]);

  return <iframe {...props} ref={iframe}></iframe>;
}

