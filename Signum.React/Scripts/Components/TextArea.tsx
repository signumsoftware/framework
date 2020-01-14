import * as React from 'react';

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  innerRef?: React.Ref<HTMLTextAreaElement>;
  autoResize?: boolean;
  minHeight?: string
}

export default function TextArea(p: TextAreaProps) {

  handleResize = (ta: HTMLTextAreaElement) => {
    if (ta.style.height == ta.scrollHeight + 'px') { // do not move to a variable
      return;
    }

    ta.style.height = "0";
    ta.style.height = ta.scrollHeight + 'px';
    ta.style.minHeight = p.minHeight!;
    ta.scrollTop = ta.scrollHeight;
  }

  const { autoResize, innerRef, minHeight, ...props } = p;

  const handleRef = React.useCallback((a: HTMLTextAreaElement | null) => {
    a && handleResize(a);
    innerRef && (typeof innerRef == "function" ? innerRef(a) : (innerRef as any).current = a);
  }, [innerRef, minHeight]);

  return (
    <textarea onInput={autoResize ? (e => handleResize(e.currentTarget)) : undefined} style={
      {
        ...(autoResize ? { display: "block", overflow: "hidden", resize: "none" } : {}),
        ...props.style
      }
    } {...props} ref={handleRef} />
  );
}

TextArea.defaultProps = { autoResize: true, minHeight: "50px" };

