import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { SearchValue, SearchControl, SearchValueLine, SearchValueController } from '@framework/Search'
import { StyleContext } from '@framework/Lines'
import { API, PrintStat } from './PrintClient'
import * as Navigator from '@framework/Navigator'
import { PrintLineState, PrintLineEntity, PrintPackageEntity } from './Signum.Entities.Printing'
import { FileTypeSymbol } from '../Files/Signum.Entities.Files'
import { ProcessEntity } from '../Processes/Signum.Entities.Processes'
import { useAPI } from '@framework/Hooks'
import { getToString, JavascriptMessage } from '@framework/Signum.Entities'

export default function PrintPanelPage(p: {}) {

  const stats = useAPI(() => API.getStats(), []);

  function renderStateButton(vsc: SearchValueController, fileType: FileTypeSymbol) {
    if (vsc.value == undefined || vsc.value == 0)
      return undefined;

    return (
      <a href="#" className="sf-line-button" title="Print" onClick={e => handlePrintClick(e, fileType, vsc)}>
        <FontAwesomeIcon icon="print" />
      </a>
    );
  }

  function handlePrintClick(e: React.MouseEvent<any>, fileType: FileTypeSymbol, vsc: SearchValueController) {
    e.preventDefault();
    API.createPrintProcess(fileType)
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
              findOptions={{
                queryName: PrintLineEntity,
                filterOptions: [
                  { token: PrintLineEntity.token(e => e.state), value: "ReadyToPrint" as PrintLineState },
                  { token: PrintLineEntity.token(a => a.file!.fileType), value: s.fileType },
                ]
              }} />)
          }
        </fieldset>
      </div>

      <h3>{ProcessEntity.nicePluralName()}</h3>
      <SearchControl findOptions={{
        queryName: ProcessEntity,
        filterOptions: [{ token: ProcessEntity.token(e => e.entity.data).cast(PrintPackageEntity), operation: "DistinctTo", value: undefined }],
        pagination: { elementsPerPage: 10, mode: "Paginate", currentPage: 1 },
      }}
      />
    </div>
  );
}
