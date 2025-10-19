import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Navigator } from '@framework/Navigator'
import { mlistItemContext, TypeContext } from '@framework/TypeContext'
import { getTypeInfo, getTypeInfos, IsByAll, isTypeEntity, tryGetTypeInfo, tryGetTypeInfos } from '@framework/Reflection'
import { UserAssetClient } from './UserAssetClient'
import { UserAssetMessage, UserAssetPreviewModel, EntityAction, LiteConflictEmbedded } from './Signum.UserAssets'
import { useForceUpdate } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { ChangeEvent, EntityLine, EntityTable, PropertyRoute, AutoLine } from '@framework/Lines'
import { EntityLink } from '@framework/Search'
import { getToString, is, liteKey, liteKeyLong, MList } from '@framework/Signum.Entities'
import SelectorModal from '@framework/SelectorModal'
import MessageModal from '@framework/Modals/MessageModal'
import { AccessibleTable } from '../../Signum/React/Basics/AccessibleTable'

export default function ImportAssetsPage(): React.JSX.Element {

  const [file, setFile] = React.useState<UserAssetClient.API.FileUpload | undefined>(undefined);
  const [model, setModel] = React.useState<UserAssetPreviewModel | undefined>(undefined);
  const [success, setSuccess] = React.useState<boolean | undefined>(undefined);
  const [fileVer, setFileVer] = React.useState<number>(0);

  const forceUpdate = useForceUpdate();

  useTitle("Import Assets Page");

  function renderFileInput() {

    function handleInputChange(e: React.FormEvent<any>) {
      let f = (e.currentTarget as HTMLInputElement).files![0];
      let fileReader = new FileReader();
      fileReader.onerror = e => { window.setTimeout(() => { throw (e as any).error; }, 0); };
      fileReader.onload = e => {
        let content = ((e.target as any).result as string).after("base64,");
        let fileName = f.name;

        var file: UserAssetClient.API.FileUpload = { content, fileName };
        setFile(file);
        setFileVer(fileVer + 1);

        UserAssetClient.API.importPreview(file!).then(model => { setModel(model); setSuccess(false); });
      };
      fileReader.readAsDataURL(f);
    }

    return (
      <div>
        <div className="btn-toolbar">
          <input key={fileVer} type="file" onChange={handleInputChange} style={{ display: "inline", float: "left", width: "inherit" }} />
        </div>
        <small>{UserAssetMessage.SelectTheXmlFileWithTheUserAssetsThatYouWantToImport.niceToString()}</small>
      </div>
    );
  }

  function renderModel() {

    function handleImport() {
      UserAssetClient.API.importAssets({
        file: file!,
        model: model!
      })
        .then(model => {
          setSuccess(true);
          setModel(undefined);
          setFile(undefined);
        });
    }

    function handleChangeConflict(conflict: LiteConflictEmbedded) {

      if (conflict.to != undefined) {
        var listChange = model!.lines.flatMap(l => l.element.liteConflicts).filter(c => is(c.element.from, conflict.from));
        if (listChange.length > 1) {
          return MessageModal.show({
            title: "",
            message: UserAssetMessage.SameSelectionForAllConflictsOf0.niceToString(getToString(conflict.from)),
            buttons: "yes_no",
          }).then(result => {
            if (result == "yes") {
              listChange.forEach(element =>
                element.element.to = conflict.to
              );

              forceUpdate();
            }
          });
        }
      }
    }

    const tc = TypeContext.root(model!, { formSize: "xs" });

    return (
      <div>
        <AccessibleTable
          caption={UserAssetMessage.UserAssetLines.niceToString()}
          className="table"
          multiselectable={false}>
          <thead>
            <tr>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.type)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.text)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.entityType)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.action)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.overrideEntity)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.customResolution)} </th>
            </tr>
          </thead>

          <tbody>
            {
              mlistItemContext(tc.subCtx(a => a.lines))!.flatMap((mlec, i) => {
                const ea = mlec.value;

                const mainRow = (
                  <tr key={`${ea.type!.cleanName}-${i}`}>
                    <td>{getTypeInfo(ea.type!.cleanName).niceName}</td>
                    <td>{ea.text}</td>
                    <td>{ea.entityType ? getTypeInfo(ea.entityType!.cleanName)?.niceName : ""}</td>
                    <td>{EntityAction.niceToString(ea.action!)}</td>
                    <td>
                      {ea.action == "Different" && (
                        <input
                          type="checkbox"
                          aria-label={EntityAction.niceToString(ea.action!)}
                          className="form-check-input"
                          checked={ea.overrideEntity}
                          onChange={e => {
                            ea.overrideEntity = e.currentTarget.checked;
                            ea.modified = true;
                            forceUpdate();
                          }}/>
                      )}
                    </td>
                    <td>
                      {ea.customResolution && (
                        <a
                          role="button"
                          href="#"
                          onClick={e => {
                            e.preventDefault();
                            Navigator.view(ea.customResolution!).then(cr => {
                              if (cr != null) {
                                ea.customResolution = cr;
                                ea.modified = true;
                              }
                            });
                          }}>
                          {getToString(ea.customResolution)}
                        </a>
                      )}
                    </td>
                  </tr>
                );

                const conflictRows = ea.liteConflicts.length > 0 ? [
                  <tr key={`${ea.type!.cleanName}-${i}-conflict`}>
                    <td colSpan={1}></td>
                    <td colSpan={4}>
                      {UserAssetMessage.LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase
                        .niceToString().formatHtml(<strong>{ea.text}</strong>)}
                      <EntityTable
                        avoidFieldSet
                        ctx={mlec.subCtx(a => a.liteConflicts)}
                        create={false}
                        remove={false}
                        move={false}
                        columns={[
                          { property: a => a.propertyRoute, template: ctx => <code>{ctx.value.propertyRoute}</code> },
                          { property: a => a.from, template: ctx => <code>{liteKeyLong(ctx.value.from)}</code> },
                          { property: a => a.to, template: ctx => <EntityLineSameType ctx={ctx} onChange={() => handleChangeConflict(ctx.value)} /> }
                        ]}
                      />
                    </td>
                  </tr>
                ] : [];

                return [mainRow, ...conflictRows];
              })
            }
          </tbody>
        </AccessibleTable>
        <button onClick={handleImport} className="btn btn-info"><FontAwesomeIcon aria-hidden="true" icon="cloud-arrow-up" />{UserAssetMessage.Import.niceToString()}</button>
      </div>
    );
  }

  function renderSuccess() {
    return (
      <div className="alert alert-success" role="alert">{UserAssetMessage.SucessfullyImported.niceToString()}</div>
    );
  }

  return (
    <div>
      <h2>{UserAssetMessage.ImportUserAssets.niceToString()}</h2>
      <br />
      {success && renderSuccess()}
      {model ? renderModel() :
        renderFileInput()
      }
    </div>
  );
}

function EntityLineSameType(p: { ctx: TypeContext<LiteConflictEmbedded>, onChange: () => void }) {

  const [avoidLastType, setAvoidLastType] = React.useState(false);

  var prType = PropertyRoute.parseFull(p.ctx.value.propertyRoute).typeReference();

  const validPrType = prType.name == IsByAll || tryGetTypeInfos(prType.name).some(a=> a != null);

  const type = validPrType ? prType :
    !avoidLastType ? { isLite: true, name: p.ctx.value.from.EntityType } :
      { isLite: true, name: IsByAll }

  return <EntityLine ctx={p.ctx.subCtx(a => a.to)} type={type} onChange={p.onChange}
    helpText={!validPrType && !avoidLastType && <span><a href="#" onClick={e => { e.preventDefault(); setAvoidLastType(true) }}><FontAwesomeIcon icon="xmark" /></a> {UserAssetMessage.AssumeIs.niceToString()} <code>{p.ctx.value.from.EntityType}</code></span >} />;
}
