import * as React from 'react'
import { Dic } from '../Globals'
import { ModifiableEntity } from '../Signum.Entities'
import { GraphExplorer } from '../Reflection'

export default class ValidationErrors extends React.Component<{ entity: ModifiableEntity, prefix: string }>
{
  render() {
    const modelState = GraphExplorer.collectModelState(this.props.entity, this.props.prefix);

    if (!modelState || Dic.getKeys(modelState).length == 0)
      return null;

    return (
      <ul className="validaton-summary alert alert-danger">
        {Dic.map(modelState, (key, value) => <li
          key={key}
          style={{ cursor: "pointer" }}
          onClick={() => this.handleOnClick(key)}
          title={key.after(this.props.prefix + ".")}>
          {value.join("\n")}
        </li>)}
      </ul>
    );
  }

  handleOnClick = (key: string) => {

    var result = document.querySelector(`[data-error-path='${key}']`);
    if (result != null) {
      result.scrollIntoView();
      var input = result.querySelector("button, [href], input, select, textarea, [tabindex]:not([tabindex='-1'])");
      if (input)
        (input as HTMLInputElement).focus();
    }
  }
}
