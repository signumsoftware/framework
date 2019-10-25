import * as React from 'react';

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  innerRef?: (ta: HTMLTextAreaElement | null) => void;
  autoResize?: boolean;
  minHeight?: string
}

export default class TextArea extends React.Component<TextAreaProps> {

  static defaultProps = { autoResize: true, minHeight: "50px" };

  handleResize = (ta: HTMLTextAreaElement) => {
    ta.style.height = "0";
    ta.style.height = ta.scrollHeight + 'px';
    ta.style.minHeight = this.props.minHeight!;
    ta.scrollTop = ta.scrollHeight;
    //window.scrollTo(window.scrollX, (ta.scrollTop + ta.scrollHeight));
  }

  handleRef = (a: HTMLTextAreaElement | null) => {
    a && this.handleResize(a);
    this.props.innerRef && this.props.innerRef(a);
  }

  render() {
    const { autoResize, innerRef, minHeight, ...props } = this.props;
    return (
      <textarea onInput={autoResize ? (e => this.handleResize(e.currentTarget)) : undefined} style={
        {
          ...(autoResize ? { display: "block", overflow: "hidden", resize: "none" } : {}),
          ...props.style
        }
      } {...props} ref={this.handleRef} />
    );
  }
}
