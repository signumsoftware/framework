import * as React from 'react'
import { Dic } from '../Globals'
import { ModifiableEntity } from '../Signum.Entities'
import { GraphExplorer } from '../Reflection'
import { useForceUpdate } from '../Hooks';


export interface ValidationErrorsHandle {
  forceUpdate() : void; 
}

export const ValidationErrors: React.ForwardRefExoticComponent<{ entity: ModifiableEntity; prefix: string; } & React.RefAttributes<ValidationErrorsHandle>>
  = React.forwardRef(function ValidationErrors(p: { entity: ModifiableEntity, prefix: string }, ref: React.Ref<ValidationErrorsHandle>) {

  const forceUpdate = useForceUpdate();

  React.useImperativeHandle(ref, () => ({ forceUpdate }), []);

  const modelState = GraphExplorer.collectModelState(p.entity, p.prefix);

  if (!modelState || Dic.getKeys(modelState).length == 0)
    return null;

  return (
    <ul className="validaton-summary alert alert-danger">
      {Dic.map(modelState, (key, value) => <li
        key={key}
        style={{ cursor: "pointer", whiteSpace: "pre-wrap" }}
        onClick={() => handleOnClick(key)}
        title={key.after(p.prefix + ".")}>
        {value.join("\n")}
      </li>)}
    </ul>
  );

  function handleOnClick(key: string) {

    var result = document.querySelector(`[data-error-path='${key}']`);
    if (result != null && result.checkVisibility()) {
      result.scrollIntoView();
      var input = result.querySelector("button, [href], input, select, textarea, [tabindex]:not([tabindex='-1'])");
      if (input)
        (input as HTMLInputElement).focus();
    } else {
      var subKey = key.tryBeforeLast(".");
      while (subKey) {
        var container = document.querySelector(`[data-error-container='${subKey}']`);
        if (container) {
          (container as HTMLElement).dispatchEvent(new Event("openError"));
          setTimeout(() => handleOnClick(key), 200);

          return;
        }

        subKey = subKey.tryBeforeLast(".");
      }
    }
  }
});
