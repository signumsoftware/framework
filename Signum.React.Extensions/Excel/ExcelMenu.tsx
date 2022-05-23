import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Finder from '@framework/Finder'
import { Lite, PaginationMessage, SearchMessage, SelectorMessage } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { ExcelReportEntity, ExcelMessage, ExcelReportOperation } from './Signum.Entities.Excel'
import * as ExcelClient from './ExcelClient'
import { Dropdown, DropdownButton } from 'react-bootstrap';
import * as Operations from '@framework/Operations';
import SelectorModal from '@framework/SelectorModal'
import { PaginationMode } from '@framework/FindOptions'

export interface ExcelMenuProps {
  searchControl: SearchControlLoaded;
  plainExcel: boolean;
  excelReport: boolean;
}

export default function ExcelMenu(p: ExcelMenuProps) {

  const [isOpen, setIsOpen] = React.useState<boolean>(false);

  const [excelReports, setExcelReports] = React.useState<Lite<ExcelReportEntity>[] | undefined>(undefined);

  function handleSelectedToggle() {
    if (isOpen == false && excelReports == undefined)
      reloadList().done();

    setIsOpen(!isOpen);
  }

  function reloadList(): Promise<void> {
    return ExcelClient.API.forQuery(p.searchControl.props.findOptions.queryKey)
      .then(list => setExcelReports(list));
  }


  function handlePlainExcel() {
    var request = p.searchControl.getQueryRequest();

    const rt = p.searchControl.state.resultTable;

    if (request.pagination.mode == "Firsts" || request.pagination.mode == "Paginate" && (rt == null || rt!.totalElements! > rt!.rows.length)) {

      SelectorModal.chooseElement<PaginationMode>([request.pagination.mode, "All"], {
        buttonDisplay: a => <span>{PaginationMode.niceToString(a)} {rt && SearchMessage._0Results_N.niceToString().forGenderAndNumber(rt.totalElements).formatHtml(
          <span className="sf-pagination-strong" key={1}>{a == "All" ? rt?.totalElements : rt?.rows.length}</span>)
        }</span>,
        buttonName: a => a,
        title: SelectorMessage._0Selector.niceToString(PaginationMode.niceTypeName()),
        message: SelectorMessage.PleaseChooseA0ToContinue.niceToString(PaginationMode.niceTypeName()),
        size: "md",
      })
        .then(pm => {
          if (pm == undefined)
            return;

          if (pm == "All") {
            request.pagination = { mode: "All" };
          }

          ExcelClient.API.generatePlainExcel(request);
        })
        .done();
    } else {
      ExcelClient.API.generatePlainExcel(request);
    }
  }


  function handleClick(er: Lite<ExcelReportEntity>) {
    ExcelClient.API.generateExcelReport(p.searchControl.getQueryRequest(), er);
  }

  function handleCreate() {
    Finder.API.fetchQueryEntity(p.searchControl.props.findOptions.queryKey)
      .then(qe => ExcelReportEntity.New({ query: qe }))
      .then(er => Navigator.view(er))
      .then(() => reloadList())
      .done();
  }

  function handleAdmnister() {
    Finder.explore({ queryName: ExcelReportEntity, filterOptions: [{ token: ExcelReportEntity.token(a => a.query!.key), value: p.searchControl.props.findOptions.queryKey }]})
      .then(() => reloadList())
      .done();
  }

  const label = <span><FontAwesomeIcon icon={["far", "file-excel"]} />&nbsp;{p.searchControl.props.largeToolbarButtons == true ? " " + ExcelMessage.ExcelReport.niceToString() : undefined}</span>;

  if (p.plainExcel && !p.excelReport)
    return <button className={"sf-query-button sf-search btn btn-light"} title={ExcelMessage.ExcelReport.niceToString() } onClick={handlePlainExcel}>{label} </button>;

  return (
    <Dropdown show={isOpen} onToggle={handleSelectedToggle} title={ExcelMessage.ExcelReport.niceToString()}>
      <Dropdown.Toggle id="userQueriesDropDown" className="sf-userquery-dropdown" variant="light">
      {label}
      </Dropdown.Toggle>
      <Dropdown.Menu>
        {p.plainExcel && <Dropdown.Item onClick={handlePlainExcel} ><span><FontAwesomeIcon icon={["far", "file-excel"]} />&nbsp; {ExcelMessage.ExcelReport.niceToString()}</span></Dropdown.Item>}
        {p.plainExcel && excelReports && excelReports.length > 0 && <Dropdown.Divider />}
        {
          excelReports?.map((uq, i) =>
            <Dropdown.Item key={i}
              onClick={() => handleClick(uq)}>
              {uq.toStr}
            </Dropdown.Item>)
        }
        {(p.plainExcel || excelReports && excelReports.length > 0) && <Dropdown.Divider />}
        {Operations.tryGetOperationInfo(ExcelReportOperation.Save, ExcelReportEntity) && <Dropdown.Item onClick={handleAdmnister}><FontAwesomeIcon icon={["fas", "search"]} className="me-2" />{ExcelMessage.Administer.niceToString()}</Dropdown.Item>}
        {Operations.tryGetOperationInfo(ExcelReportOperation.Save, ExcelReportEntity) && <Dropdown.Item onClick={handleCreate}><FontAwesomeIcon icon={["fas", "plus"]} className="me-2" />{ExcelMessage.CreateNew.niceToString()}</Dropdown.Item>}
      </Dropdown.Menu>
    </Dropdown>
  );
}



