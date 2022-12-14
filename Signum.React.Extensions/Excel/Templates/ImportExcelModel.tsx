import * as React from 'react'
import { ValueLine, EntityLine, OptionItem } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { FileLine } from '../../Files/FileLine'
import { ImportExcelMode, ImportExcelModel, ImportFromExcelMessage } from '../Signum.Entities.Excel'
import * as Finder from '@framework/Finder'
import { getTypeInfo, getTypeInfos, PseudoType } from '@framework/Reflection'
import { SearchControl, SearchControlLoaded } from '@framework/Search'
import * as Navigator from '@framework/Navigator'
import * as ExcelClient from '../ExcelClient'
import { Dic, softCast } from '@framework/Globals'
import { QueryRequest } from '@framework/FindOptions'
import ErrorModal from '@framework/Modals/ErrorModal'
import MessageModal from '@framework/Modals/MessageModal'
import a from 'bpmn-js/lib/features/search'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { MarkedRow } from '@framework/SearchControl/ContextualItems'
import { JavascriptMessage, liteKey } from '@framework/Signum.Entities'
import { useForceUpdate } from '@framework/Hooks'
import { selectPagination } from '../ExcelMenu'
import { RetryFilter } from '@framework/Services'

export default function ImportExcel(p: { ctx: TypeContext<ImportExcelModel>, searchControl: SearchControlLoaded, queryRequest: QueryRequest }) {
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic" });
  const forceUpdate = useForceUpdate();

  function handlePlainExcelForImport() {
    selectPagination(p.searchControl).then(req => req && ExcelClient.API.generatePlainExcel(req, undefined, true));
  }


  return (
    <div>

      <div className="row">
        <div className="col-sm-4">
          <ValueLine ctx={ctx.subCtx(f => f.operationKey)} valueLineType="DropDownList"
            optionItems={getSaveOperations(p.ctx.value.typeName, ctx.value.mode).map(a => softCast<OptionItem>({ value: a.key, label: a.niceName }))}
          />
          <ValueLine ctx={ctx.subCtx(f => f.transactional)} inlineCheckbox="block" />
        </div>
        <div className="col-sm-4">
          <ValueLine ctx={ctx.subCtx(f => f.mode)} onChange={() => {
            if (ctx.value.mode == "Insert")
              ctx.value.matchByColumn = null;
            else
              ctx.value.matchByColumn = (ctx.value.matchByColumn ?? p.queryRequest.columns.firstOrNull(a => a.token == "Id" || a.token == "Entity.Id")?.token) ?? null;

            var operations = getSaveOperations(p.ctx.value.typeName, ctx.value.mode);
            if (!operations.some(o => o.key == ctx.value.operationKey))
              ctx.value.operationKey = null!;

            if (ctx.value.mode == "Update")
              ctx.value.identityInsert = false;

            forceUpdate();
          }} />
          {(ctx.value.mode == "Insert" || ctx.value.mode == "InsertOrUpdate") && <ValueLine ctx={ctx.subCtx(f => f.identityInsert)} inlineCheckbox="block" />}
        </div>
        <div className="col-sm-4">
          {(ctx.value.mode == "Update" || ctx.value.mode == "InsertOrUpdate") &&
            <ValueLine ctx={ctx.subCtx(f => f.matchByColumn)} valueLineType="DropDownList" mandatory
              optionItems={p.queryRequest.columns.map(c => softCast<OptionItem>({ value: c.token, label: c.displayName }))}
            />
          }
        </div>
      </div>

      <br/>

      <FileLine ctx={ctx.subCtx(f => f.excelFile)} />

      <button className="btn btn-xs btn-info" onClick={handlePlainExcelForImport}>
        <FontAwesomeIcon icon="download" /> {ImportFromExcelMessage.DownloadTemplate.niceToString()}
      </button>
    </div>
  );
}

function getSaveOperations(type: PseudoType, mode: ImportExcelMode | null) {

  var ops = Dic.getValues(getTypeInfo(type).operations ?? {});

  return ops.filter(a => a.operationType == "Execute" && a.canBeModified && (mode == "Update" || a.canBeNew));
}

export async function onImportFromExcel(sc: SearchControlLoaded) {

  var qr = sc.getQueryRequest();
  qr.pagination = { mode: "All" };

  await ExcelClient.API.validateForImport(qr);

  var qd = await Finder.getQueryDescription(qr.queryKey);

  var ti = getTypeInfos(qd.columns["Entity"].type).single();

  var model = ImportExcelModel.New({
    typeName: ti.name,
    mode: null!,
    operationKey: getSaveOperations(ti, null).onlyOrNull()?.key,
  });


  model = (await Navigator.view(model, {
    extraProps: { searchControl: sc, queryRequest: qr },
    title: ImportFromExcelMessage.Import0FromExcel.niceToString(ti.nicePluralName)
  }))!;

  if (model == null)
    return;

  var resport = await ExcelClient.API.importFromExcel(qr, model, ti);

  if (model.transactional) {

    var errors = resport.results.filter(a => a.error != null);

    if (errors.length) {
      await MessageModal.showError(
        <ul>
          {errors.map((e, i) => <li key={i}><strong>{e.rowIndex}</strong> {e.error}</li>)}
        </ul>,
        ImportFromExcelMessage.ErrorsIn0Rows_N.niceToString().forGenderAndNumber(errors.length).formatWith(errors.length));

      return;
    }

  } else {
    var errors = resport.results.filter(a => a.error != null && a.entity == null);

    if (errors.length)
      await MessageModal.show({
        buttons: "ok",
        icon: "error",
        style: "error",
        size: "xl",
        title: ImportFromExcelMessage.ErrorsIn0Rows_N.niceToString().forGenderAndNumber(errors.length).formatWith(errors.length),
        message: <ul>
          {errors.map((e, i) => <li key={i}><strong>Row {e.rowIndex}:</strong> {e.error}</li>)}
        </ul>
      });
  }

  var state = resport.results.filter(a => a.entity != null).toObject(a => liteKey(a.entity!), a => {

    if (a.error) {
      return softCast<MarkedRow>({ message: `Error in Row ${a.rowIndex}: ${a.error}`, className: "text-danger" });
    }

    if (a.action == "Updated") {
      return softCast<MarkedRow>({ message: `Updated from Row ${a.rowIndex}`, className: "text-warning" });
    }

    if (a.action == "Inserted") {
      return softCast<MarkedRow>({ message: `Inserted from Row ${a.rowIndex}`, className: "text-success" });
    }

    if (a.action == "NoChanges") {
      return softCast<MarkedRow>({ message: `No changes in row Row ${a.rowIndex}`, className: "text-muted" });
    }

    throw new Error("Unexpected value " + a.action);
  });

  sc.markRows(state);

  return;

}
