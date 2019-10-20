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

  render() {
    const { autoResize, innerRef, ...props } = this.props;
    return (
      <textarea onInput={autoResize ? (e => this.handleResize(e.currentTarget)) : undefined} style={
        {
          ...(autoResize ? { display: "block", overflow: "hidden", resize: "none" } : {}),
          ...props.style
        }
      } {...props} ref={a => {
        a && this.handleResize(a);
        innerRef && innerRef(a);
      }} />
    );
  }
}
