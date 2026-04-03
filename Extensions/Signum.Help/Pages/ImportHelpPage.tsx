import * as React from 'react'
import { FontAwesomeIcon, FontAwesomeIconProps } from '@fortawesome/react-fontawesome'
import { icon, IconProp} from '@fortawesome/fontawesome-svg-core';
import { Navigator } from '@framework/Navigator'
import { mlistItemContext, TypeContext } from '@framework/TypeContext'
import { getTypeInfo, getTypeInfos, IsByAll, isTypeEntity, tryGetTypeInfo, tryGetTypeInfos } from '@framework/Reflection'
import { useForceUpdate } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { EntityLink } from '@framework/Search'
import { AccessibleTable } from '@framework/Basics/AccessibleTable'
import { ErrorBoundary } from '@framework/Components'
import { classes } from '@framework/Globals'
import { HelpImportPreviewModel, HelpImportReportModel, HelpMessage, ImportAction, ImportStatus } from '../Signum.Help'
import { HelpClient } from '../HelpClient'
import '../Help.css'
import { JavascriptMessage } from '@framework/Signum.Entities'

export default function ImportHelpPage(): React.JSX.Element {

  const [file, setFile] = React.useState<HelpClient.API.FileUpload | undefined>(undefined);
  const [model, setModel] = React.useState<HelpImportPreviewModel | undefined>(undefined);
  const [reportModel, setReportModel] = React.useState<HelpImportReportModel | undefined>(undefined);
  const [lastImported, setLastImported] = React.useState<string | undefined>(undefined);
  const [imported, setImported] = React.useState<boolean | undefined>(undefined);
  const [fileVer, setFileVer] = React.useState<number>(0);

  const forceUpdate = useForceUpdate();

  useTitle("Import Help Contents");

  function renderFileInput() {

    function handleInputChange(e: React.FormEvent<any>) {
      setImported(undefined);
      let f = (e.currentTarget as HTMLInputElement).files![0];
      let fileReader = new FileReader();
      fileReader.onerror = e => { window.setTimeout(() => { throw (e as any).error; }, 0); };
      fileReader.onload = e => {
        let content = ((e.target as any).result as string).after("base64,");
        let fileName = f.name;

        var file: HelpClient.API.FileUpload = { content, fileName };
        setFile(file);
        setFileVer(fileVer + 1);

        HelpClient.API.importPreview(file!).then(model => { setModel(model); setImported(false); });
      };
      fileReader.readAsDataURL(f);
    }

    return (
      <div className="mb-3">
        <div className="btn-toolbar">
          <input
            key={fileVer}
            type="file"
            id="fileUpload"
            onChange={handleInputChange}
            className="d-none"
          />
          <label htmlFor="fileUpload" className="btn btn-info">
            <FontAwesomeIcon icon="folder-open" className="me-2" />
            {HelpMessage.ChooseZIPFile.niceToString()}
          </label>
        </div>
        <small className="text-muted">
          {HelpMessage.SelectTheZIPFileWithTheHelpContentsThatYouWantToImport.niceToString()}
        </small>
      </div>
    );
  }

  function renderPreview() {

    function handleImport() {
      setLastImported(undefined);

      HelpClient.API.applyImport(file!, model!)
        .then(report => {
          setReportModel(report);
          setImported(true);
          setModel(undefined);
          setLastImported(file?.fileName);
          setFile(undefined);
        }, reason => {
          throw reason;
        });
    }

    const actionIcon: Record<ImportAction, FontAwesomeIconProps> = {
      Create: { icon: "plus-square", color: "green" },
      Override: { icon: "pen-square", color: "orange" },
      NoChange: { icon: "equals", color: "gray" }
    }

    const tc = TypeContext.root(model!, { formSize: "xs" });
    const fileSizeMB = file && (new Blob([file.content]).size / (1024 * 1024)).toFixed(2);

    function applyHeaderClick(): void {
      model?.lines.forEach(l => { l.element.apply = l.element.action != 'NoChange' });
      forceUpdate();
    }

    return (
      <div>
        <AccessibleTable
          aria-label={HelpMessage.HelpZipContents.niceToString()}
          className="table import-preview"
          multiselectable={false}>
          <thead>
            <tr>
              <th> {HelpImportPreviewModel.nicePropertyName(a => a.lines![0].element.type)} </th>
              <th> {HelpImportPreviewModel.nicePropertyName(a => a.lines![0].element.culture)} </th>
              <th> {`${HelpImportPreviewModel.nicePropertyName(a => a.lines![0].element.exitingEntity)}/${HelpMessage.NewKey.niceToString()}`} </th>
              <th> {HelpImportPreviewModel.nicePropertyName(a => a.lines![0].element.action)} </th>
              <th onClick={applyHeaderClick}> {HelpImportPreviewModel.nicePropertyName(a => a.lines![0].element.apply)} </th>
            </tr>
          </thead>

          <tbody>
            {
              mlistItemContext(tc.subCtx(a => a.lines))!.flatMap((mlec, i) => {
                const ea = mlec.value;

                return (
                  <tr key={`${ea.type!.cleanName}-${i}`} className={classes(ea.apply && "row-selected", ea.action == 'NoChange' && "no-change")}>
                    <td>{getTypeInfo(ea.type!.cleanName).niceName}</td>
                    <td>{`${ea.culture.nativeName} (${ea.culture.name})`}</td>
                    <td>{ea.exitingEntity ? <EntityLink lite={ea.exitingEntity} ></EntityLink> : ea.key}</td>
                    <td><FontAwesomeIcon {...actionIcon[ea.action]} className="me-2" size="lg" /> {ImportAction.niceToString(ea.action!)}</td>
                    <td>
                      {ea.applyVisible && (
                        <input
                          type="checkbox"
                          aria-label={ImportAction.niceToString(ea.action!)}
                          className="form-check-input"
                          checked={Boolean(ea.apply)}
                          onChange={e => {
                            ea.apply = e.currentTarget.checked;
                            ea.modified = true;
                            forceUpdate();
                          }}/>
                      )}
                    </td>
                  </tr>
                );
              })
            }
          </tbody>
        </AccessibleTable>
        <div className="alert alert-secondary d-flex align-items-center" role="alert">
          <FontAwesomeIcon icon="file" className="me-2" />
          <div className="text-muted">
            <strong>{HelpMessage.SelectedFile.niceToString()}:</strong> {file?.fileName} ({fileSizeMB} MB)
          </div>
        </div>
        <button onClick={handleImport} className="btn btn-primary"><FontAwesomeIcon aria-hidden="true" icon="cloud-arrow-up" className="me-2"/>{HelpMessage.Import.niceToString()}</button>
      </div>
    );
  }

  function renderReport() {

    const statusIcon: Record<ImportStatus, FontAwesomeIconProps> = {
      Applied: { icon: "check-square", color: "green" },
      Failed: { icon: "warning", color: "darkorange" },
      Skipped: { icon: "ban", color: "gray" },
      NoChange: { icon: "equals", color: "gray" }  
    }

    const hasErrors = reportModel!.lines.some(l => l.element.status == "Failed");
    const message = (hasErrors ? HelpMessage.ImportCompletedWithErrors : HelpMessage.ImportCompletedSuccessfully).niceToString();

    const tc = TypeContext.root(reportModel!, { formSize: "xs" });
    return (
      <div>
        <div className={classes("alert d-flex align-items-center", hasErrors ? "alert-warning" : "alert-success")} role="alert">
          <FontAwesomeIcon icon="circle-check" className="me-2" />
          <div><strong>{lastImported}</strong> {message}</div>
        </div>
        <h3 className="h3">{HelpMessage.ImportReport.niceToString()}</h3>
        <div>
          <AccessibleTable
            aria-label={HelpMessage.HelpZipContents.niceToString()}
            className="table"
            multiselectable={false}>
            <thead>
              <tr>
                <th> {HelpImportReportModel.nicePropertyName(a => a.lines![0].element.type)} </th>
                <th> {HelpImportReportModel.nicePropertyName(a => a.lines![0].element.culture)} </th>
                <th> {`${HelpImportReportModel.nicePropertyName(a => a.lines![0].element.exitingEntity)}/${HelpMessage.NewKey.niceToString()}`} </th>
                <th> {HelpMessage.ActionStatus.niceToString()} </th>
                <th> {JavascriptMessage.error.niceToString()} </th>
              </tr>
            </thead>

            <tbody>
              {
                mlistItemContext(tc.subCtx(a => a.lines))!.flatMap((mlec, i) => {
                  const ea = mlec.value;
                  return (
                    <tr key={`${ea.type!.cleanName}-${i}`} className={classes(ea.status == 'Failed' && "")}>
                      <td>{getTypeInfo(ea.type!.cleanName).niceName}</td>
                      <td>{`${ea.culture.nativeName} (${ea.culture.name})`}</td>
                      <td>{ea.exitingEntity ? <EntityLink lite={ea.exitingEntity} ></EntityLink> : ea.key}</td>
                      <td><FontAwesomeIcon {...statusIcon[ea.status]} className="me-2" size="lg" title={ImportStatus.niceToString(ea.status!)} />{ImportAction.niceToString(ea.action!)}</td>
                      <td>{ea.actionError!}</td>
                   </tr>
                  );
                })
              }
            </tbody>
          </AccessibleTable >
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className="h2">{HelpMessage.ImportHelpContentsFromZipFile.niceToString()}</h1>
      <br />
      {imported && renderReport()}
      <ErrorBoundary>
        {model ? renderPreview() :
          renderFileInput()
        }
      </ErrorBoundary>
    </div>
  );
}
