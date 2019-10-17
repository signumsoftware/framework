import * as React from 'react';

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  innerRef?: React.Ref<HTMLTextAreaElement>;
  autoResize?: boolean;
}

export default function TextArea(p: TextAreaProps) {
  function handleResize(ta: HTMLTextAreaElement) {
    ta.style.height = "0";
    ta.style.height = ta.scrollHeight + 'px';
    ta.style.minHeight = "50px";
    ta.scrollTop = ta.scrollHeight;
    //window.scrollTo(window.scrollX, (ta.scrollTop + ta.scrollHeight));
  }

  const { autoResize, innerRef, ...props } = p;
  return (
    <textarea onInput={autoResize ? (e => handleResize(e.currentTarget)) : undefined} style={
      {
        ...(autoResize ? { display: "block", overflow: "hidden", resize: "none" } : {}),
        ...props.style
      }
    } {...props} ref={a => {
      a && handleResize(a);
      if (innerRef) {
        if (typeof innerRef == "function")
          innerRef(a);
        else
          (innerRef as React.MutableRefObject<HTMLTextAreaElement | null>).current = a;
      }
    }} />
  );
}

TextArea.defaultProps = { autoResize: true };

