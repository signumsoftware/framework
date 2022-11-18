import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { mlistItemContext, TypeContext } from '@framework/TypeContext'
import { getTypeInfo } from '@framework/Reflection'
import { API } from './UserAssetClient'
import { UserAssetMessage, UserAssetPreviewModel, EntityAction, LiteConflictEmbedded } from './Signum.Entities.UserAssets'
import { useForceUpdate } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { ChangeEvent, EntityLine, EntityTable, PropertyRoute, ValueLine } from '@framework/Lines'
import { EntityLink } from '@framework/Search'
import { getToString, is, liteKey, liteKeyLong, MList } from '@framework/Signum.Entities'
import SelectorModal from '../../Signum.React/Scripts/SelectorModal'
import MessageModal from '../../Signum.React/Scripts/Modals/MessageModal'

interface ImportAssetsPageProps extends RouteComponentProps<{}> {

}

export default function ImportAssetsPage(p: ImportAssetsPageProps) {

  const [file, setFile] = React.useState<API.FileUpload | undefined>(undefined);
  const [model, setModel] = React.useState<UserAssetPreviewModel | undefined>(undefined);
  const [success, setSuccess] = React.useState<boolean | undefined>(undefined);
  const [fileVer, setFileVer] = React.useState<number>(0);

  const forceUpdate = useForceUpdate();

  useTitle("Import Assets Page");

  function renderFileInput() {

    function handleInputChange(e: React.FormEvent<any>) {
      let f = (e.currentTarget as HTMLInputElement).files![0];
      let fileReader = new FileReader();
      fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
      fileReader.onload = e => {
        let content = ((e.target as any).result as string).after("base64,");
        let fileName = f.name;

        var file: API.FileUpload = { content, fileName };
        setFile(file);
        setFileVer(fileVer + 1);

        API.importPreview(file!).then(model => { setModel(model); setSuccess(false); });
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
      API.importAssets({
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
        <table className="table">
          <thead>
            <tr>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.type)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.text)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.action)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.overrideEntity)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.customResolution)} </th>
            </tr>
          </thead>

          <tbody>
            {
              mlistItemContext(tc.subCtx(a => a.lines))!.map((mlec, i) => {

                var ea = mlec.value;

                return (
                  <React.Fragment key={i}>
                    <tr key={ea.type!.cleanName}>
                      <td> {getTypeInfo(ea.type!.cleanName).niceName} </td>
                      <td> {ea.text}</td>
                      <td> {EntityAction.niceToString(ea.action!)} </td>
                      <td>
                        {ea.action == "Different" &&
                          <input type="checkbox" className="form-check-input" checked={ea.overrideEntity} onChange={e => {
                            ea.overrideEntity = (e.currentTarget as HTMLInputElement).checked;
                            ea.modified = true;
                            forceUpdate();
                          }}></input>
                        }
                      </td>
                      <td> {ea.customResolution && <a href="#" onClick={e => {
                        e.preventDefault();
                        Navigator.view(ea.customResolution!)
                          .then(cr => {
                            if (cr != null) {
                              ea.customResolution = cr;
                              ea.modified = true;
                            }
                          });
                      }}>{getToString(ea.customResolution)}</a>}</td>
                    </tr>
                    {ea.liteConflicts.length > 0 && <tr>
                      <td colSpan={1}></td>
                      <td colSpan={4}>
                        {UserAssetMessage.LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase.niceToString().formatHtml(<strong>{ea.text}</strong>)}
                        <EntityTable avoidFieldSet ctx={mlec.subCtx(a => a.liteConflicts)} create={false} remove={false} move={false}
                          columns={EntityTable.typedColumns<LiteConflictEmbedded>([
                            { property: a => a.propertyRoute, template: ctx => <code>{ctx.value.propertyRoute}</code> },
                            { property: a => a.from, template: ctx => <code>{liteKeyLong(ctx.value.from)}</code> },
                            {
                              property: a => a.to, template: ctx =>
                                <EntityLine ctx={ctx.subCtx(a => a.to)} type={PropertyRoute.parseFull(ctx.value.propertyRoute).typeReference()}
                                onChange={e =>  handleChangeConflict(ctx.value)}
                                />
                            }
                          ])}
                        />
                      </td>
                    </tr>}
                  </React.Fragment>
                );
              }
              )
            }
          </tbody>
        </table>
        <button onClick={handleImport} className="btn btn-info"><FontAwesomeIcon icon="cloud-arrow-up" /> Import</button>
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
