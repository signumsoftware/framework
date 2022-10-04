import { PredictorEntity } from "../Signum.Entities.MachineLearning";
import { Modal } from "react-bootstrap";
import * as React from "react";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Navigator from "@framework/Navigator";
import { IModalProps, openModal } from "@framework/Modals";
import { API, PredictRequest, PredictOutputTuple, PredictSubQueryTable, AlternativePrediction } from "../PredictorClient";
import { Lite, Entity, EntityControlMessage, getToString } from "@framework/Signum.Entities";
import { StyleContext, FormGroup, TypeContext, EntityLine, EntityCombo, ValueLine } from "@framework/Lines";
import { QueryToken } from "@framework/FindOptions";
import { tryGetTypeInfos, ReadonlyBinding, getTypeInfo, getTypeInfos, toNumberFormatOptions, toNumberFormat } from "@framework/Reflection";
import { IsByAll } from "@framework/Reflection";
import { Dic } from "@framework/Globals";
import { Binding } from "@framework/Reflection";
import { is } from "@framework/Signum.Entities";
import { isLite } from "@framework/Signum.Entities";
import {  } from "@framework/Components";
import { ModalHeaderButtons } from "@framework/Components/ModalHeaderButtons";
import { NumericTextBox, isNumber } from "@framework/Lines/ValueLine";
import { AbortableRequest } from "@framework/Services";

interface PredictModalProps extends IModalProps<undefined> {
  initialPredict: PredictRequest;
  isClassification: boolean;
  entity?: Lite<Entity>;
}

export function PredictModal(p: PredictModalProps) {

  const [show, setShow] = React.useState<boolean>(true);
  const [hasChanged, setHasChanged] = React.useState<boolean>(false);
  const [predict, setPredict] = React.useState<PredictRequest>(p.initialPredict);

  const abortableUpdateRequest = React.useMemo(() => new AbortableRequest((abortController, request: PredictRequest) => API.updatePredict(request)), []);

  function handleOnClose() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(undefined);
  }

  
  function hangleOnChange() {
    setHasChanged(true);
    abortableUpdateRequest.getData(predict)
      .then(predict => setPredict(predict));
  }


  var e = p.entity;

  var sctx = new StyleContext(undefined, {});

  return (
    <Modal onHide={handleOnClose} onExited={handleOnExited} show={show} className="message-modal" size="lg">
      <ModalHeaderButtons onClose={handleOnClose}>
        {getToString(predict.predictor)}<br />
        <small>{PredictorEntity.niceName()}&nbsp;{predict.predictor.id} (<a href={Navigator.navigateRoute(predict.predictor)} target="_blank">{EntityControlMessage.View.niceToString()}</a>)</small>
        {e && <span><br />{getTypeInfo(e.EntityType).niceName}: <a href={Navigator.navigateRoute(e)} target="_blank">{getToString(e)}</a></span>}
      </ModalHeaderButtons>
      <div className="modal-body">
        <div>
          {predict.columns.filter(c => c.usage == "Input").map((col, i) =>
            <PredictLine key={i} sctx={sctx} hasOriginal={predict.hasOriginal} hasChanged={hasChanged} binding={Binding.create(col, c => c.value)} usage={col.usage} token={col.token} onChange={hangleOnChange} />)}
        </div>
        {predict.subQueries.map((c, i) => <PredictTable key={i} sctx={sctx} table={c} onChange={hangleOnChange} hasChanged={hasChanged} hasOriginal={predict.hasOriginal} />)}
        <div>
          {predict.columns.filter(c => c.usage == "Output").map((col, i) =>
            <PredictLine key={i} sctx={sctx} hasOriginal={predict.hasOriginal} hasChanged={hasChanged} binding={Binding.create(col, c => c.value)} usage={col.usage} token={col.token} onChange={hangleOnChange} />)}
        </div>
        {p.isClassification && <AlternativesCheckBox binding={Binding.create(predict, p2 => p2.alternativesCount)} onChange={hangleOnChange} />}
      </div>
    </Modal>
  );
}


PredictModal.show = (predict: PredictRequest, entity: Lite<Entity> | undefined, isClassification: boolean): Promise<void> => {
  return openModal<undefined>(<PredictModal initialPredict={predict} entity={entity} isClassification={isClassification} />);
}

export function AlternativesCheckBox(p : { binding: Binding<number | null>, onChange: () => void }){

  function setValue(val: number | null) {
    p.binding.setValue(val);
    p.onChange();
  }
  var val = p.binding.getValue();
  return (
    <label>
      <input type="checkbox" className="form-check-input" checked={val != null} onChange={() => setValue(val == null ? 5 : null)} /> Show <NumericTextBox value={val} onChange={n => setValue(n)} validateKey={isNumber} format={toNumberFormat("0")} /> alternative predictions </label>
  );
}

interface PredictLineProps {
  binding: Binding<any>;
  token: QueryToken;
  usage: "Key" | "Input" | "Output"
  sctx: StyleContext;
  hasOriginal: boolean;
  hasChanged: boolean;
  onChange: () => void;
}

export default function PredictLine(p : PredictLineProps){

  function renderValue() {
    if (p.usage == "Output") {
      if (p.hasOriginal) {
        var tuple = p.binding.getValue() as PredictOutputTuple;

        const octx = new TypeContext<any>(p.sctx, { readOnly: true }, undefined as any, Binding.create(tuple, a => a.original));
        const pctx = new TypeContext<any>(p.sctx, { readOnly: true }, undefined as any, Binding.create(tuple, a => a.predicted));

        return (
          <div>
            <div style={{ opacity: p.hasChanged ? 0.5 : 1 }}>
              <PredictValue token={p.token} ctx={octx} label={<FontAwesomeIcon icon="bullseye" />} />
            </div>
            {renderValueOrMultivalue(pctx, octx.value)}
          </div>
        );
      }
      else {
        const ctx = new TypeContext<any>(p.sctx, { readOnly: true }, undefined as any, p.binding);
        return renderValueOrMultivalue(ctx, null);

      }
    } else if (p.usage == "Input") {
      const ctx = new TypeContext<any>(p.sctx, undefined, undefined as any, p.binding);
      return (<PredictValue token={p.token} ctx={ctx} onChange={p.onChange} />);
    } else throw new Error("unexpected Usage");
  }

  function renderValueOrMultivalue(pctx: TypeContext<any>, originalValue: any) {
    if (!Array.isArray(pctx.value)) {
      return <PredictValue token={p.token} ctx={pctx} label={<FontAwesomeIcon icon={["far", "lightbulb"]} color={getColor(pctx.value, originalValue)} />} />
    } else {
      const predictions = pctx.value as AlternativePrediction[];
      const numberFormat = toNumberFormat("P2");
      return (
        <div>
          {predictions.map((a, i) => <PredictValue key={i} token={p.token}
            ctx={new TypeContext<any>(p.sctx, { readOnly: true }, undefined as any, new ReadonlyBinding(a.value, p.sctx + "_" + i))}
            label={<i style={{ color: getColor(a.value, originalValue) }}>{numberFormat.format(a.probability)}</i>}
            labelHtmlAttributes={{ style: { textAlign: "right", whiteSpace: "nowrap" } }}
          />)}
        </div>
      );
    }
  }

  function getColor(predicted: any, original: any) {
    return !p.hasOriginal ? undefined :
      predicted == original || isLite(predicted) && isLite(original) && is(predicted, original) ? "green" : "red";
  }
  return (
    <FormGroup ctx={p.sctx} label={p.token.niceName} labelHtmlAttributes={{ title: fullNiceName(p.token) }}>
      {renderValue()}
    </FormGroup>
  );
}


interface PredictTableProps {
  sctx: StyleContext;
  table: PredictSubQueryTable;
  hasChanged: boolean;
  hasOriginal: boolean;
  onChange: () => void;
}

export function PredictTable(p : PredictTableProps){
  var p = p;
  var { subQuery, columnHeaders, rows } = p.table;
  var sctx = new StyleContext(p.sctx, { formGroupStyle: "SrOnly" });
  return (
    <div>
      <h4>{getToString(subQuery)}</h4>
      <div style={{ maxHeight: "500px", overflowY: "scroll", marginBottom: "10px" }}>
        <table className="table table-sm">
          <thead>
            <tr >
              {
                columnHeaders.map((he, i) => <th key={i} className={"header-" + he.headerType.toLowerCase()} title={fullNiceName(he.token)}>
                  {he.headerType == "Key" && <FontAwesomeIcon icon="key" style={{ marginRight: "10px" }} />}
                  {he.token.niceName}
                </th>)
              }
            </tr>
          </thead>
          <tbody>
            {
              rows.map((row, j) => <tr key={j}>
                {
                  row.map((v, i) => {
                    var ch = columnHeaders[i];
                    return (
                      <td>
                        <PredictLine sctx={sctx} token={ch.token} binding={new Binding(row, i)}
                          usage={ch.headerType} hasChanged={p.hasChanged} hasOriginal={p.hasOriginal} onChange={p.onChange} />
                      </td>
                    );
                  })
                }
              </tr>)
            }
          </tbody>
        </table>
      </div>
    </div>
  );
}


function fullNiceName(token: QueryToken): string {

  var rec = (token.parent ? fullNiceName(token.parent) + "." : "");

  return rec + `[${token.niceName}]`;
}

interface PredictValueProps {
  token: QueryToken;
  ctx: TypeContext<any>;
  onChange?: () => void;
  label?: React.ReactElement<any>;
  labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
}

export function PredictValue(p : PredictValueProps){
  function handleValueChange() {
    if (p.onChange)
      p.onChange();
  }

  const ctx = p.ctx.subCtx({ labelColumns: 1 });
  const token = p.token;
  const label = p.label;
  const lha = p.labelHtmlAttributes;

  switch (token.filterType) {
    case "Lite":
      if (token.type.name == IsByAll || getTypeInfos(token.type).some(ti => !ti.isLowPopulation))
        return <EntityLine ctx={ctx} type={token.type} create={false} label={label} labelHtmlAttributes={lha} onChange={handleValueChange} />;
      else
        return <EntityCombo ctx={ctx} type={token.type} create={false} label={label} labelHtmlAttributes={lha} onChange={handleValueChange} />
    case "Enum":
      const ti = tryGetTypeInfos(token.type).single();
      if (!ti)
        throw new Error(`EnumType ${token.type.name} not found`);
      const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
      return <ValueLine ctx={ctx} type={token.type} format={token.format} unit={token.unit} labelHtmlAttributes={lha} label={label} onChange={handleValueChange} optionItems={members} />;
    default:
      return <ValueLine ctx={ctx} type={token.type} format={token.format} unit={token.unit} labelHtmlAttributes={lha} label={label} onChange={handleValueChange} />;
  }
}
