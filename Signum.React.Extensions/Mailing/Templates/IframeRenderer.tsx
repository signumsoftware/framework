import * as React from 'react'

export interface IFrameRendererProps extends React.HTMLAttributes<HTMLIFrameElement> {
  html: string | null | undefined;
}

export default class IFrameRenderer extends React.Component<IFrameRendererProps> {

  componentDidMount() {
    this.load(this.props.html);
  }

  componentWillReceiveProps(newProps: { html: string }) {
    this.load(newProps.html);
  }

  load(html: string | null | undefined) {
    const cd = this.iframe.contentDocument!;

    cd.body.innerHTML = html || "";
  }

  iframe!: HTMLIFrameElement;

  render() {
    var { html, ...props } = this.props;

    return <iframe {...props} ref={e => this.iframe = e!}></iframe>;
  }
}

