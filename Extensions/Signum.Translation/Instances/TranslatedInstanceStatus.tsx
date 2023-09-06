import * as React from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { Dic, classes } from '@framework/Globals'
import { getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { API, TranslatedTypeSummary } from '../TranslatedInstanceClient'
import { TranslationMessage } from '../Signum.Translation'
import "../Translation.css"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { getTypeInfo, reloadTypes } from '@framework/Reflection'
import { notifySuccess } from '@framework/Operations'
import MessageModal from '@framework/Modals/MessageModal'
import * as CultureClient from '@framework/Basics/CultureClient'

export default function TranslationCodeStatus() {

  const [result, reload] = useAPIWithReload(() => API.status(), []);
  const [file, setFile] = React.useState<API.FileUpload | undefined>(undefined);
  const [fileVer, setFileVer] = React.useState<number>(0);



  function renderFileInput() {

    function handleInputChange(e: React.FormEvent<any>) {
      let f = (e.currentTarget as HTMLInputElement).files![0];
      let fileReader = new FileReader();
      fileReader.onerror = e => { window.setTimeout(() => { throw (e as any).error; }, 0); };
      fileReader.onload = e => {
        let content = ((e.target as any).result as string).after("base64,");
        let fileName = f.name;

        var file: API.FileUpload = { content, fileName };
        setFile(file);
        setFileVer(fileVer + 1);

        API.uploadFile(file!).then(model => { notifySuccess(); reload(); });
      };
      fileReader.readAsDataURL(f);
    }

    return (
      <div>
        <div className="btn-toolbar">
          <input key={fileVer} type="file" onChange={handleInputChange} style={{ display: "inline", float: "left", width: "inherit" }} />
        </div>
        <small>Select a .xlsx file with the translations</small>
      </div>
    );
  }

  return (
    <div>
      <h2>{TranslationMessage.InstanceTranslations.niceToString()}</h2>
      {result == undefined ? <p><strong>{JavascriptMessage.loading.niceToString()}</strong></p> :
        result.length == 0 ? <p>No routes marked for translation. Consider using <code>TranslatedInstanceLogic.AddRoute()</code></p> :
          <TranslationTable result={result} onRefreshView={reload} />}
      {result && result.length > 0 && renderFileInput()}
    </div>
  );
}


function TranslationTable({ result, onRefreshView }: { result: TranslatedTypeSummary[], onRefreshView?: () => void }) {
  const tree = result.groupBy(a => a.type)
    .toObject(gr => gr.key, gr => gr.elements.toObject(a => a.culture));

  const [onlyNeutral, setOnlyNeutral] = React.useState<boolean>(true);

  const types = Dic.getKeys(tree);
  let cultures = Dic.getKeys(tree[types.first()]);

  if (onlyNeutral)
    cultures = cultures.filter(a => !onlyNeutral || !a.contains("-"));

  return (
    <table className="st">
      <thead>
        <tr>
          <th><label><input type="checkbox" checked={onlyNeutral} onChange={e => setOnlyNeutral(e.currentTarget.checked)} /> Only Neutral Cultures</label></th>
          <th> {TranslationMessage.All.niceToString()} </th>
          {cultures.map(culture =>
            <th key={culture}>
              <span>{culture}</span>
              {result.some(r => !r.isDefaultCulture && r.culture == culture && r.state != "Completed") &&
                <a href="#" className={classes("auto-translate-all", culture, "ms-2")} onClick={e => handleAutoTranslateClick(e, null, culture)}>{TranslationMessage.AutoTranslate.niceToString()}</a>}
            </th>)}
        </tr>
      </thead>
      <tbody>
        {types.map(type =>
          <tr key={type}>
            <th> {getTypeInfo(type).nicePluralName}</th>
            <td>
              <Link to={`/translatedInstance/view/${type}`}>{TranslationMessage.View.niceToString()}</Link>
            </td>
            {cultures.map(culture =>
            (tree[type][culture].isDefaultCulture ?
              <td key={culture}>
                {TranslationMessage.None.niceToString()}
              </td>
              :
              <td key={culture}>
                <Link to={`/translatedInstance/view/${type}/${culture}`}>{TranslationMessage.View.niceToString()}</Link>
                <a href="#" className="ms-2" onClick={e => { e.preventDefault(); API.downloadView(type, culture); }}><FontAwesomeIcon icon="download" /></a>
                <br />
                <Link to={`/translatedInstance/sync/${type}/${culture}`} className={"status-" + tree[type][culture].state}>{TranslationMessage.Sync.niceToString()}</Link>
                <a href="#" className={classes("status-" + tree[type][culture].state, "ms-2")} onClick={e => { e.preventDefault(); API.downloadSync(type, culture); }}><FontAwesomeIcon icon="download" /></a>
                {tree[type][culture].state != "Completed" &&
                  <>
                    <br />
                    <a href="#" className={classes("auto-translate", "status-" + tree[type][culture].state)} onClick={e => handleAutoTranslateClick(e, type, culture)}>{TranslationMessage.AutoTranslate.niceToString()}</a>
                  </>}
              </td>
            )
            )}
          </tr>
        )}
      </tbody>
    </table>
  );

  function handleAutoTranslateClick(e: React.MouseEvent<any>, type: string | null, culture: string) {
    e.preventDefault();

    CultureClient.getCultures(null)
      .then(cultures =>
        MessageModal.show(
          {
            title: TranslationMessage.AutoTranslate.niceToString(),
            message: type ? TranslationMessage.AreYouSureToContinueAutoTranslation0For1WithoutRevision.niceToString().formatHtml(<strong>{getTypeInfo(type).niceName}</strong>, <strong>{getToString(cultures[culture])}</strong>) :
              TranslationMessage.AreYouSureToContinueAutoTranslationAllTypesFor0WithoutRevision.niceToString().formatHtml(<strong>{getToString(cultures[culture])}</strong>),
            buttons: "yes_no",
            style: "warning",
            icon: "warning"
          })
          .then(mr => {
            if (mr == "yes")
              (type ? API.autoTranslate(type, culture) : API.autoTranslateAll(culture))
                .then(() => onRefreshView?.());
          })
      );
  }
}


