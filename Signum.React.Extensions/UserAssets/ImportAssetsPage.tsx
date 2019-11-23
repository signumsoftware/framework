import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { TypeContext } from '@framework/TypeContext'
import { getTypeInfo } from '@framework/Reflection'
import { API } from './UserAssetClient'
import { UserAssetMessage, UserAssetPreviewModel, EntityAction } from './Signum.Entities.UserAssets'
import { useForceUpdate, useTitle } from '@framework/Hooks'

interface ImportAssetsPageProps extends RouteComponentProps<{}> {

}

interface ImportAssetsPageState {
  file?: API.FileUpload;
  model?: UserAssetPreviewModel;
  success?: boolean;
  fileVer: number;
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

        setFile({ content, fileName });
        setFileVer(fileVer + 1);

        API.importPreview(file!).then(model => { setModel(model); setSuccess(false); }).done();
      };
      fileReader.readAsDataURL(f);
    }

    return (
      <div>
        <div className="btn-toolbar">
          <input key={fileVer} type="file" className="form-control" onChange={handleInputChange} style={{ display: "inline", float: "left", width: "inherit" }} />
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
        })
        .done();
    }

    const tc = TypeContext.root(model!, undefined);

    return (
      <div>
        <table className="table">
          <thead>
            <tr>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.action)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.overrideEntity)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.type)} </th>
              <th> {UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.text)} </th>
            </tr>
          </thead>

          <tbody>
            {
              tc.value.lines!.map(mle =>
                <tr key={mle.element.type!.cleanName}>
                  <td> {EntityAction.niceToString(mle.element.action!)} </td>
                  <td>
                    {mle.element.action == "Different" &&
                      <input type="checkbox" checked={mle.element.overrideEntity} onChange={e => {
                        mle.element.overrideEntity = (e.currentTarget as HTMLInputElement).checked;
                        mle.element.modified = true;
                        forceUpdate();
                      }}></input>
                    }
                  </td>
                  <td> {getTypeInfo(mle.element.type!.cleanName).niceName} </td>
                  <td> {mle.element.text}</td>
                </tr>
              )
            }
          </tbody>
        </table>
        <button onClick={handleImport} className="btn btn-info"><FontAwesomeIcon icon="cloud-upload" /> Import</button>
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



