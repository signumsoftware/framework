import * as React from 'react';
import { useWindowEvent, whenVisible } from '../Hooks';

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  innerRef?: React.Ref<HTMLTextAreaElement>;
  autoResize?: boolean;
  minHeight?: string;
}

export default function TextArea(p: TextAreaProps): React.ReactElement {

  var textAreaRef = React.useRef<HTMLTextAreaElement | null | undefined>(undefined);
  const visibleObserver = React.useRef<IntersectionObserver | null>(null);

  function handleResize(ta: HTMLTextAreaElement) {
    if (ta.style.height == ta.scrollHeight + 'px') { // do not move to a variable
      return;
    }
    ta.style.height = "0";
    ta.style.height = ta.scrollHeight + 'px';
    ta.style.minHeight = p.minHeight!;
    ta.scrollTop = ta.scrollHeight;
  }

  const { autoResize = true, innerRef, minHeight = "50px", ...props } = p;

  const handleRef = React.useCallback((ta: HTMLTextAreaElement | null) => {
    textAreaRef.current = ta;

    if (visibleObserver.current)
      visibleObserver.current.disconnect();

    if (ta && p.autoResize) {
      if (ta.offsetParent != null)
        handleResize(ta);
      else
        visibleObserver.current = whenVisible(ta, visible => visible && handleResize(ta), { /*root: document.documentElement*/ });
    }
    innerRef && (typeof innerRef == "function" ? innerRef(ta) : (innerRef as any).current = ta);
  }, [innerRef, minHeight]);

  useWindowEvent("resize", () => textAreaRef.current && handleResize(textAreaRef.current), [textAreaRef]);

  React.useEffect(() => {
    if (p.autoResize && textAreaRef.current && p.value != null)
      handleResize(textAreaRef.current);
  }, [p.value]);

  React.useEffect(() => {
    return () => {
      if (visibleObserver.current)
        visibleObserver.current.disconnect();
    };
  }, []);

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
