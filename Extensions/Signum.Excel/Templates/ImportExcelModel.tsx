import * as React from 'react'
import { AutoLine, CheckboxLine, EnumLine, OptionItem } from '@framework/Lines'
import { mlistItemContext, TypeContext } from '@framework/TypeContext'
import { FileLine } from '../../Signum.Files/Components/FileLine'
import { CollectionElementEmbedded, ImportExcelMode, ImportExcelModel, ImportFromExcelMessage } from '../Signum.Excel'
import { Finder } from '@framework/Finder'
import { getTypeInfo, getTypeInfos, PseudoType } from '@framework/Reflection'
import { SearchControlLoaded } from '@framework/Search'
import { Navigator } from '@framework/Navigator'
import { ExcelClient } from '../ExcelClient'
import { Dic, softCast } from '@framework/Globals'
import { FindOptionsParsed, getTokenParents, hasElement, QueryToken } from '@framework/FindOptions'
import ErrorModal from '@framework/Modals/ErrorModal'
import MessageModal from '@framework/Modals/MessageModal'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { MarkedRow } from '@framework/SearchControl/ContextualItems'
import { liteKey, newMListElement } from '@framework/Signum.Entities'
import { useForceUpdate } from '@framework/Hooks'
import { selectPagination } from '../ExcelMenu'

export default function ImportExcel(p: { ctx: TypeContext<ImportExcelModel>, searchControl: SearchControlLoaded, fop: FindOptionsParsed, topElementToken: QueryToken | null }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic" });
  const forceUpdate = useForceUpdate();

  var parentTokens = getTokenParents(p.topElementToken).toObject(a => a.fullKey);

  function handlePlainExcelForImport() {
    selectPagination(p.searchControl).then(req => req && ExcelClient.API.generatePlainExcel(req, undefined, true));
  }

  function potentialKeys(elementToken: string) {
    return p.fop.columnOptions.filter(a => a.token && a.token.fullKey.startsWith(elementToken) && !a.token.fullKey.after(elementToken).split(".").contains("Element"));
  }

  return (
    <div>

      <div className="row">
        <div className="col-sm-4">
          <EnumLine ctx={ctx.subCtx(f => f.operationKey)} 
            optionItems={getSaveOperations(p.ctx.value.typeName, ctx.value.mode).map(a => softCast<OptionItem>({ value: a.key, label: a.niceName }))}
          />
          <CheckboxLine ctx={ctx.subCtx(f => f.transactional)} inlineCheckbox="block" />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(f => f.mode)} onChange={() => {
            if (ctx.value.mode == "Insert" && ctx.value.collections.length == 0)
              ctx.value.matchByColumn = null;
            else
              ctx.value.matchByColumn = (ctx.value.matchByColumn ?? p.fop.columnOptions.firstOrNull(a => a.token?.fullKey == "Id" || a.token?.fullKey == "Entity.Id")?.token?.fullKey) ?? null;

            var operations = getSaveOperations(p.ctx.value.typeName, ctx.value.mode);
            if (!operations.some(o => o.key == ctx.value.operationKey))
              ctx.value.operationKey = null!;

            if (ctx.value.mode == "Update")
              ctx.value.identityInsert = false;

            forceUpdate();
          }} />
          {(ctx.value.mode == "Insert" || ctx.value.mode == "InsertOrUpdate") && <CheckboxLine ctx={ctx.subCtx(f => f.identityInsert)} inlineCheckbox="block" />}
        </div>
        <div className="col-sm-4">
          {(ctx.value.mode == "Update" || ctx.value.mode == "InsertOrUpdate" || ctx.value.collections.length > 0) &&
            <EnumLine ctx={ctx.subCtx(f => f.matchByColumn)} mandatory
              optionItems={p.fop.columnOptions.filter(a => a.token && !hasElement(a.token)).map(c => softCast<OptionItem>({ value: c.token!.fullKey, label: c.displayName ?? c.token!.niceName! }))}
            />
          }
          {
            mlistItemContext(ctx.subCtx(a => a.collections))
              .filter((ctxe, i, arr) => ctx.value.mode == "Update" || ctx.value.mode == "InsertOrUpdate" || ctx.value.mode == "Insert" && i < arr.length - 1)
              .map(ctxe => <EnumLine ctx={ctxe.subCtx(a => a.matchByColumn)} 
                label={ctxe.niceName(a => a.matchByColumn) + ": " + parentTokens[ctxe.value.collectionElement].niceName}
                optionItems={potentialKeys(ctxe.value.collectionElement).map(c => softCast<OptionItem>({ value: c.token!.fullKey, label: c.displayName ?? c.token!.niceName! }))}
            />)
          }
        </div>
      </div>

      <br/>

      <FileLine ctx={ctx.subCtx(f => f.excelFile)} />

      <button className="btn btn-xs btn-info" onClick={handlePlainExcelForImport}>
        <FontAwesomeIcon aria-hidden={true} icon="download" /> {ImportFromExcelMessage.DownloadTemplate.niceToString()}
      </button>
    </div>
  );
}

function getSaveOperations(type: PseudoType, mode: ImportExcelMode | null) {

  var ops = Dic.getValues(getTypeInfo(type).operations ?? {});

  return ops.filter(a => a.operationType == "Execute" && a.canBeModified && (mode == "Update" || a.canBeNew));
}


export async function onImportFromExcel(sc: SearchControlLoaded): Promise<void> {

  var qr = sc.getQueryRequest(true);
  qr.pagination = { mode: "All" };

  var topToken = await ExcelClient.API.validateForImport(qr);

  var qd = await Finder.getQueryDescription(qr.queryKey);

  var ti = getTypeInfos(qd.columns["Entity"].type).single();

  var model = ImportExcelModel.New({
    typeName: ti.name,
    mode: null!,
    operationKey: getSaveOperations(ti, null).onlyOrNull()?.key,
    collections: getTokenParents(topToken)
      .filter(t => t.queryTokenType == "Element")
      .map(m => newMListElement(CollectionElementEmbedded.New({ collectionElement: m.fullKey }))), 
  });

  await onImportFromExcelRetry();

  async function onImportFromExcelRetry() {

    model = (await Navigator.view(model, {
      extraProps: { searchControl: sc, fop: sc.state.resultFindOptions, topElementToken: topToken },
      title: ImportFromExcelMessage.Import0FromExcel.niceToString(ti.nicePluralName)
    }))!;

    if (model == null)
      return;

    var r = await ExcelClient.API.importFromExcel(qr, model, ti);

    if (r.error) {

      await ErrorModal.showErrorModal(r.error);
      await onImportFromExcelRetry();

    } else {

      if (model.transactional) {

        var errors = r.results.filter(a => a.error != null);

        if (errors.length) {
          await MessageModal.showError(
            <ul>
              {errors.map((e, i) => <li key={i}><strong>{e.rowIndex}</strong> {e.error}</li>)}
            </ul>,
            ImportFromExcelMessage.ErrorsIn0Rows_N.niceToString().forGenderAndNumber(errors.length).formatWith(errors.length));

          await onImportFromExcelRetry();

          return;
        }

      } else {

        var errors = r.results.filter(a => a.error != null && a.entity == null);

        if (errors.length) {
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

          if (errors.length == r.results.length) {
            await onImportFromExcelRetry();

            return;
          }
        }
      }

      var state = r.results.filter(a => a.entity != null).toObject(a => liteKey(a.entity!), a => {

        if (a.error)
          return softCast<MarkedRow>({ message: `Error in Row ${a.rowIndex}: ${a.error}`, status: "Error" });

        if (a.action == "Updated")
          return softCast<MarkedRow>({ message: `Updated from Row ${a.rowIndex}`, status: "Warning" });

        if (a.action == "Inserted")
          return softCast<MarkedRow>({ message: `Inserted from Row ${a.rowIndex}`, status: "Success" });

        if (a.action == "NoChanges")
          return softCast<MarkedRow>({ message: `No changes in row Row ${a.rowIndex}`, status: "Muted" });

        throw new Error("Unexpected value " + a.action);
      });

      sc.markRows(state);

      return;
    }
  }
}
