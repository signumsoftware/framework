import * as React from "react";
import { TypeContext, LineBaseProps, LineBase, FormGroup } from "../Lines";
import { SearchMessage, MList, newMListElement } from "../Signum.Entities";
import { mlistItemContext } from "../TypeContext";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import DynamicComponent from "./DynamicComponent";
import { ErrorBoundary } from "../Components";

interface MultiValueLineProps extends LineBaseProps {
  ctx: TypeContext<MList<any>>;
  onRenderItem?: (ctx: TypeContext<any>) => React.ReactElement<any>;
  onCreate?: () => Promise<any[] | any | undefined>;
  addValueText?: string;
}

export class MultiValueLine extends LineBase<MultiValueLineProps, MultiValueLineProps> {
  calculateDefaultState(state: MultiValueLineProps) {
    if (state.ctx.value == undefined)
      state.ctx.value = [];

    super.calculateDefaultState(state);
  }

  handleDeleteValue = (index: number) => {
    const list = this.state.ctx.value;
    list.removeAt(index);
    this.setValue(list);
  }

  handleAddValue = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    const list = this.state.ctx.value;
    const newValuePromise = this.state.onCreate == null ? this.defaultCreate() : this.state.onCreate();

    newValuePromise.then(v => {
      if (v === undefined)
        return;

      if (Array.isArray(v)) {
        list.push(...v.map(e => newMListElement(e)));
      }
      else {
        list.push(newMListElement(v));
      }

      this.setValue(list);
    }).done();
  }

  defaultCreate() {
    return Promise.resolve(null);
  }

  renderInternal() {

    const s = this.state;
    const list = this.state.ctx.value!;

    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText}
        htmlAttributes={{ ...this.baseHtmlAttributes(), ...this.state.formGroupHtmlAttributes }}
        helpText={this.state.helpText}
        labelHtmlAttributes={s.labelHtmlAttributes}>
        <table className="sf-multi-value">
          <tbody>
            {
              mlistItemContext(s.ctx.subCtx({ formGroupStyle: "None" })).map((mlec, i) =>
                (<ErrorBoundary>
                  <MultiValueLineElement key={i}
                    ctx={mlec}
                    onRemove={e => { e.preventDefault(); this.handleDeleteValue(i); }}
                    onRenderItem={this.props.onRenderItem} />
                </ErrorBoundary>))
            }
            <tr >
              <td colSpan={4}>
                {!s.ctx.readOnly &&
                  <a href="#" title={this.props.addValueText || SearchMessage.AddValue.niceToString()}
                    className="sf-line-button sf-create"
                    onClick={this.handleAddValue}>
                    <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{this.props.addValueText || SearchMessage.AddValue.niceToString()}
                  </a>}
              </td>
            </tr>
          </tbody>
        </table>
      </FormGroup>
    );
  }
}

export interface MultiValueLineElementProps {
  ctx: TypeContext<any>;
  onRemove: (event: React.MouseEvent<any>) => void;
  onRenderItem?: (ctx: TypeContext<any>) => React.ReactElement<any>;
}

export class MultiValueLineElement extends React.Component<MultiValueLineElementProps> {
  render() {
    const ctx = this.props.ctx;

    return (
      <tr>
        <td>
          {!ctx.readOnly &&
            <a href="#" title={SearchMessage.DeleteFilter.niceToString()}
              className="sf-line-button sf-remove"
              onClick={this.props.onRemove}>
              <FontAwesomeIcon icon="times" />
            </a>}
        </td>
        <td>
          {this.props.onRenderItem ? this.props.onRenderItem(ctx) : DynamicComponent.getAppropiateComponent(ctx)}
        </td>
      </tr>
    );
  }
}



