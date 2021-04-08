import * as React from 'react';

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  innerRef?: React.Ref<HTMLTextAreaElement>;
  autoResize?: boolean;
  minHeight?: string
}

export default function TextArea(p: TextAreaProps) {

  var textAreaRef = React.useRef<HTMLTextAreaElement | null | undefined>();

    function handleResize(ta: HTMLTextAreaElement) {
    if (ta.style.height == ta.scrollHeight + 'px') { // do not move to a variable
        return;
    }
    ta.style.height = "0";
    ta.style.height = ta.scrollHeight + 'px';
    ta.style.minHeight = p.minHeight!;
    ta.scrollTop = ta.scrollHeight;
  }

  const { autoResize, innerRef, minHeight, ...props } = p;

  const handleRef = React.useCallback((ta: HTMLTextAreaElement | null) => {
    textAreaRef.current = ta;
    if (ta && p.autoResize) {
      if (ta.offsetParent != null)
        handleResize(ta);
      else
        whenVisible(ta, visible => visible && handleResize(ta));
    }
    innerRef && (typeof innerRef == "function" ? innerRef(ta) : (innerRef as any).current = ta);
  }, [innerRef, minHeight]);

  React.useEffect(() => {
    if (p.autoResize && textAreaRef.current && p.value != null)
      handleResize(textAreaRef.current);
  }, [p.value]);

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
