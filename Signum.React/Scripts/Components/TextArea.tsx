import * as React from 'react';

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  innerRef?: React.Ref<HTMLTextAreaElement>;
  autoResize?: boolean;
  minHeight?: string
}

export default function TextArea(p: TextAreaProps) {

  function handleResize(ta: HTMLTextAreaElement) {
    ta.style.height = "0";
    ta.style.height = ta.scrollHeight + 'px';
    ta.style.minHeight = p.minHeight!;
    ta.scrollTop = ta.scrollHeight;
  }

  const { autoResize, innerRef, minHeight, ...props } = p;

  const handleRef = React.useCallback((ta: HTMLTextAreaElement | null) => {
    if (ta && p.autoResize) {
      if (ta.offsetParent != null)
        handleResize(ta);
      else
        whenVisible(ta, visible => visible && handleResize(ta));
    }

    ta && p.autoResize && handleResize(ta);
    innerRef && (typeof innerRef == "function" ? innerRef(ta) : (innerRef as any).current = ta);
  }, [innerRef, minHeight]);

  return (
    <textarea {...props} onInput={e => {
      if (p.autoResize) {
        handleResize(e.currentTarget);
      }
      if (p.onInput)
        p.onInput(e);
    }} style={
      {
        ...(autoResize ? { display: "block", overflow: "hidden", resize: "none" } : {}),
        ...props.style
      }
    } ref={handleRef} />
  );
}

TextArea.defaultProps = { autoResize: true, minHeight: "50px" };

function whenVisible(element: HTMLElement, callback: (visible: boolean) => void) {
  var options = {
    root: document.documentElement
  }

  var observer = new IntersectionObserver((entries, observer) => {
    entries.forEach(entry => {
      callback(entry.intersectionRatio > 0);
    });
  }, options);

  observer.observe(element);
}
