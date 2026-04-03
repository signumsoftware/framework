import * as React from 'react';


export function AutoFocus(p: { disabled?: boolean, delay?: number, children: React.ReactNode }): React.ReactElement {
  var ref = React.useRef<HTMLDivElement | null>(null);
  React.useEffect(() => {
    if (!p.disabled) {

      var timer = window.setTimeout(() => {
        
        var input = Array.from(ref.current!.querySelectorAll("input, select, textarea"))
          .firstOrNull(e => {
            var html = e as HTMLInputElement; return html.tabIndex >= 0 && html.disabled != true;
          });

        if (input)
          (input as HTMLInputElement).focus();

      }, p.delay == null ? 200 : p.delay);

      return () => clearTimeout(timer);
    }
  }, [p.disabled]);

  return (
    <div ref={ref}>
      {p.children}
    </div>
  );
}
