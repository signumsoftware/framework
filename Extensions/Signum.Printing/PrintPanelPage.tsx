import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { SearchValue, SearchControl, SearchValueLine, SearchValueController } from '@framework/Search'
import { StyleContext } from '@framework/Lines'
import { PrintClient } from './PrintClient'
import { Navigator } from '@framework/Navigator'
import { PrintLineState, PrintLineEntity, PrintPackageEntity } from './Signum.Printing'
import { FileTypeSymbol } from '../Signum.Files/Signum.Files'
import { ProcessEntity } from '../Signum.Processes/Signum.Processes'
import { useAPI } from '@framework/Hooks'
import { getToString, JavascriptMessage } from '@framework/Signum.Entities'

export default function PrintPanelPage(p: {}): React.JSX.Element {

  const stats = useAPI(() => PrintClient.API.getStats(), []);

  function renderStateButton(vsc: SearchValueController, fileType: FileTypeSymbol) {
    if (vsc.value == undefined || vsc.value == 0)
      return undefined;

    return (
      <LinkButton className="sf-line-button" title="Print" onClick={e => handlePrintClick(e, fileType, vsc)}>
        <FontAwesomeIcon icon="print" />
      </LinkButton>
    );
  }

  function handlePrintClick(e: React.MouseEvent<any>, fileType: FileTypeSymbol, vsc: SearchValueController) {
    e.preventDefault();
    PrintClient.API.createPrintProcess(fileType)
      .then(p => p && Navigator.view(p))
      .then(p => vsc.refreshValue());
  }
  var ctx = new StyleContext(undefined, undefined);
  return (
    <div>
      <h2>PrintPanel</h2>
      <div>
        <fieldset>
          <legend>Ready To Print</legend>
          {stats == null ? JavascriptMessage.loading.niceToString() :
            stats.map((s, i) =>
            <SearchValueLine ctx={ctx} key={i} initialValue={s.count}
              label={getToString(s.fileType).after(".")}
              extraButtons={vsc => renderStateButton(vsc, s.fileType)}
              findOptions={PrintLineEntity.findOptions(token => ({
                filterOptions: [
                  token(e => e.state).filter("EqualTo", "ReadyToPrint" as PrintLineState),
                  token(a => a.file!.fileType).filter("EqualTo", s.fileType),
                ]
              }))} />)
          }
        </fieldset>
      </div>

      <h3>{ProcessEntity.nicePluralName()}</h3>
      <SearchControl findOptions={ProcessEntity.findOptions(token => ({
        filterOptions: [token(e => e.entity.data).cast(PrintPackageEntity).filter("DistinctTo", undefined)],
        pagination: { elementsPerPage: 10, mode: "Paginate", currentPage: 1 },
      }))}
      />
    </div>
  );
}
