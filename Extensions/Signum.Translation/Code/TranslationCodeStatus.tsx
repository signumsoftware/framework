import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Link, useLocation, useParams } from 'react-router-dom'
import { Dic, classes } from '@framework/Globals'
import { JavascriptMessage, getToString } from '@framework/Signum.Entities'
import { TranslationClient } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Translation'
import "../Translation.css"
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { saveFile } from '@framework/Services'
import { CultureClient } from '@framework/Basics/CultureClient'
import MessageModal from '@framework/Modals/MessageModal'
import { AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'
import { LinkButton } from '@framework/Basics/LinkButton'

export default function TranslationCodeStatus(): React.JSX.Element {

  const [result, reloadResult] = useAPIWithReload(() => TranslationClient.API.status(), []);

  return (
    <div>
      <h2>{TranslationMessage.CodeTranslations.niceToString()}</h2>
      {result == undefined ? <strong>{JavascriptMessage.loading.niceToString()}</strong> :
        <TranslationTable result={result} onRefreshView={reloadResult} />}
    </div>
  );
}

function TranslationTable({ result, onRefreshView }: { result: TranslationClient.TranslationFileStatus[], onRefreshView: () => void }) {
  const tree = result.groupBy(a => a.assembly)
    .toObject(gr => gr.key, gr => gr.elements.toObject(a => a.culture));

  const [onlyNeutral, setOnlyNeutral] = React.useState<boolean>(true);

  const assemblies = Dic.getKeys(tree);
  let cultures = Dic.getKeys(tree[assemblies.first()]);

  if (onlyNeutral)
    cultures = cultures.filter(a => !onlyNeutral || !a.contains("-"));

  return (
    <AccessibleTable
      caption={TranslationMessage.TranslationStatus.niceToString()}
      className="st table">
      <thead>
        <tr>
          <th><label><input type="checkbox" checked={onlyNeutral} onChange={e => setOnlyNeutral(e.currentTarget.checked)} />{TranslationMessage.OnlyNeutralCultures.niceToString()}</label></th>
          <th> {TranslationMessage.All.niceToString()} </th>
          {cultures.map(culture =>
            <th key={culture}>
              {culture}
              {result.some(r => !r.isDefault && r.culture == culture && r.status != "Completed") &&
                <LinkButton title={undefined} className={classes("auto-translate-all", culture, "ms-2")} onClick={e => handleAutoTranslateClick(e, null, culture)}>{TranslationMessage.AutoSync.niceToString()}</LinkButton>}
            </th>)
          }
        </tr>
      </thead>
      <tbody>
        {assemblies.map(assembly =>
          <tr key={assembly}>
            <th> {assembly}</th>
            <td>
              <Link to={`/translation/view/${encodeDots(assembly)}`}>{TranslationMessage.View.niceToString()}</Link>
            </td>
            {cultures.map(culture => {
              const fileStatus = tree[assembly][culture];
              return (
                <td key={culture}>
                  <Link role="button" to={`/translation/view/${encodeDots(assembly)}/${culture}`}>{TranslationMessage.View.niceToString()}</Link>
                  {fileStatus.status != "None" && <LinkButton className="ms-2" onClick={e => { TranslationClient.API.download(assembly, culture).then(r => saveFile(r)); }} title={TranslationMessage.Download.niceToString()}>{<FontAwesomeIcon aria-hidden="true" icon="download" />}</LinkButton>}
                  <br />
                  {
                    !fileStatus.isDefault &&
                    <Link to={`/translation/syncNamespaces/${encodeDots(assembly)}/${culture}`} className={"status-" + fileStatus.status}>
                      {TranslationMessage.Sync.niceToString()}
                    </Link>
                  }
                  {
                    fileStatus.status != "Completed" && !fileStatus.isDefault &&
                    <>
                      <br />
                      <LinkButton title={undefined} className={classes("auto-translate", "status-" + fileStatus.status)} onClick={e => handleAutoTranslateClick(e, assembly, culture)}>{TranslationMessage.AutoSync.niceToString()}</LinkButton>
                    </>
                  }
                </td>
              );
            }
            )}
          </tr>
        )}
      </tbody>
    </AccessibleTable>
  );

  function handleAutoTranslateClick(e: React.MouseEvent<any>, assembly: string | null, culture: string) {
    e.preventDefault();

    CultureClient.getCultures(null)
      .then(cultures =>
        MessageModal.show(
          {
            title: TranslationMessage.AutoSync.niceToString(),
            message: assembly ? TranslationMessage.AreYouSureToContinueAutoTranslation0For1WithoutRevision.niceToString().formatHtml(<strong>{assembly}</strong>, <strong>{getToString(cultures[culture])}</strong>) :
              TranslationMessage.AreYouSureToContinueAutoTranslationAllAssembliesFor0WithoutRevision.niceToString().formatHtml(<strong>{getToString(cultures[culture])}</strong>),
            buttons: "yes_no",
            style: "warning",
            icon: "warning"
          })
          .then(mr => {
            if (mr == "yes")
              (assembly ? TranslationClient.API.autoTranslate(assembly, culture) : TranslationClient.API.autoTranslateAll(culture))
                .then(() => onRefreshView());
          })
      );
  }
}

export function encodeDots(value: string): string {
  return value.replaceAll(".", "-");
}

export function decodeDots(value: string): string {
  return value.replaceAll("-", ".");
}


