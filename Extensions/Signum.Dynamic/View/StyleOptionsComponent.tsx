import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic } from '@framework/Globals'
import { Binding } from '@framework/Reflection'
import { EntityControlMessage } from '@framework/Signum.Entities'
import { ExpressionOrValueComponent, DesignerModal } from './Designer'
import { DesignerNode, isExpression } from './NodeUtils'
import { BaseNode } from './Nodes'
import { StyleOptionsExpression, formGroupStyle, formSize } from './StyleOptionsExpression'
import { useForceUpdate } from '@framework/Hooks'
import { LinkButton } from '@framework/Basics/LinkButton'

interface StyleOptionsLineProps {
  binding: Binding<StyleOptionsExpression | undefined>;
  dn: DesignerNode<BaseNode>;
}

export function StyleOptionsLine(p : StyleOptionsLineProps): React.JSX.Element {
  function renderMember(expr: StyleOptionsExpression | undefined): React.ReactNode {
    return (<span
      className={expr === undefined ? "design-default" : "design-changed"}>
      {p.binding.member}
    </span>);
  }

  function handleRemove(e: React.MouseEvent<any>) {
    p.binding.deleteValue();
    p.dn.context.refreshView();
  }

  function handleCreate(e: React.MouseEvent<any>) {
    modifyExpression({} as StyleOptionsExpression);
  }

  function handleView(e: React.MouseEvent<any>) {
    var hae = JSON.parse(JSON.stringify(p.binding.getValue())) as StyleOptionsExpression;
    modifyExpression(hae);
  }

  function modifyExpression(soe: StyleOptionsExpression) {
    DesignerModal.show("StyleOptions", () => <StyleOptionsComponent dn={p.dn} styleOptions={soe} />).then(result => {
      if (result) {

        if (Dic.getKeys(soe).length == 0)
          p.binding.deleteValue();
        else
          p.binding.setValue(soe);
      }

      p.dn.context.refreshView();
    });
  }


  function getDescription(soe: StyleOptionsExpression) {
    var keys = Dic.map(soe as any, (key, value) => key + ": " + (isExpression(value) ? value.__code__ : value));
    return keys.join("\n");
  }
  const val = p.binding.getValue();

  return (
    <div className="form-group form-group-xs">
      <label className="control-label label-xs">
        {renderMember(val)}

        {val && " "}
        {val && <LinkButton className={classes("sf-line-button", "sf-remove")}
          onClick={handleRemove}
          title={EntityControlMessage.Remove.niceToString()}>
          <FontAwesomeIcon icon="xmark" />
        </LinkButton>}
      </label>
      <div>
        {val ?
          <LinkButton title={undefined} onClick={handleView}><pre style={{ padding: "0px", border: "none", color: "blue" }}>
            {getDescription(val)}</pre>
          </LinkButton>
          :
          <LinkButton title={EntityControlMessage.Create.niceToString()}
            className="sf-line-button sf-create"
            onClick={handleCreate}>
            <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{EntityControlMessage.Create.niceToString()}
          </LinkButton>}
      </div>
    </div>
  );
}

export interface StyleOptionsComponentProps {
  dn: DesignerNode<BaseNode>;
  styleOptions: StyleOptionsExpression
}

export function StyleOptionsComponent(p : StyleOptionsComponentProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const so = p.styleOptions;
  const dn = p.dn;

  return (
    <div className="form-sm code-container">
      <ExpressionOrValueComponent dn={dn} refreshView={() => forceUpdate()} binding={Binding.create(so, s => s.formGroupStyle)} type="string" options={formGroupStyle} defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} refreshView={() => forceUpdate()} binding={Binding.create(so, s => s.formSize)} type="string" options={formSize} defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} refreshView={() => forceUpdate()} binding={Binding.create(so, s => s.placeholderLabels)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} refreshView={() => forceUpdate()} binding={Binding.create(so, s => s.readonlyAsPlainText)} type="string" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} refreshView={() => forceUpdate()} binding={Binding.create(so, s => s.labelColumns)} type="number" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} refreshView={() => forceUpdate()} binding={Binding.create(so, s => s.valueColumns)} type="number" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} refreshView={() => forceUpdate()} binding={Binding.create(so, s => s.readOnly)} type="boolean" defaultValue={null} />
    </div>
  );
}

