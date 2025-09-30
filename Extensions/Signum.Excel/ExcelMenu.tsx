import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Finder } from '@framework/Finder'
import { getToString, Lite, SearchMessage, SelectorMessage } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { ExcelReportEntity, ExcelMessage, ExcelReportOperation, ImportFromExcelMessage, ExcelPermission } from './Signum.Excel'
import { ExcelClient } from './ExcelClient'
import { Dropdown } from 'react-bootstrap';
import { Operations } from '@framework/Operations';
import SelectorModal from '@framework/SelectorModal'
import { PaginationMode, QueryRequest } from '@framework/FindOptions'
import { onImportFromExcel } from './Templates/ImportExcelModel'
import { isPermissionAuthorized } from '@framework/AppContext'


export interface ExcelMenuProps {
  searchControl: SearchControlLoaded;
  plainExcel: boolean;
  importFromExcel: boolean;
  excelReport: boolean;
}

export default function ExcelMenu(p: ExcelMenuProps): React.JSX.Element {

  const [isOpen, setIsOpen] = React.useState<boolean>(false);

  const [excelReports, setExcelReports] = React.useState<Lite<ExcelReportEntity>[] | undefined>(undefined);

  function handleSelectedToggle() {
    if (isOpen == false && excelReports == undefined && p.excelReport)
      reloadExcelReports();

    setIsOpen(!isOpen);
  }

  function reloadExcelReports(): Promise<void> {
    return ExcelClient.API.forQuery(p.searchControl.props.findOptions.queryKey)
      .then(list => setExcelReports(list));
  }


  function handleExcelReport(er: Lite<ExcelReportEntity>) {
    selectPagination(p.searchControl).then(req => req && ExcelClient.API.generateExcelReport(req, er));
  }


  function handlePlainExcel() {
    selectPagination(p.searchControl).then(req => req && ExcelClient.API.generatePlainExcel(req));
  }

  function handleImportFromExcel() {
    onImportFromExcel(p.searchControl);
  }

  function handleCreate() {
    Finder.API.fetchQueryEntity(p.searchControl.props.findOptions.queryKey)
      .then(qe => ExcelReportEntity.New({ query: qe }))
      .then(er => Navigator.view(er))
      .then(() => reloadExcelReports());
  }

  function handleAdmnister() {
    Finder.explore({ queryName: ExcelReportEntity, filterOptions: [{ token: ExcelReportEntity.token(a => a.query!.key), value: p.searchControl.props.findOptions.queryKey }]})
      .then(() => reloadExcelReports());
  }

  const label = <span><FontAwesomeIcon icon={"file-excel"} />{p.searchControl.props.largeToolbarButtons == true ? <span className="d-none d-sm-inline">{" " + ExcelMessage.ExcelReport.niceToString()}</span> : undefined}</span>;

  if (p.plainExcel && !p.excelReport && !p.importFromExcel)
    return <button className={"sf-query-button sf-search btn btn-tertiary"} title={ExcelMessage.ExcelReport.niceToString() } onClick={handlePlainExcel}>{label} </button>;

  return (
    <Dropdown show={isOpen} onToggle={handleSelectedToggle} title={ExcelMessage.ExcelReport.niceToString()}>
      <Dropdown.Toggle id="userQueriesDropDown" variant="tertiary">
      {label}
      </Dropdown.Toggle>
      <Dropdown.Menu>
        {...addDropdownDividers([
          p.plainExcel && <Dropdown.Item onClick={handlePlainExcel} ><span><FontAwesomeIcon icon={"file-excel"} />&nbsp; {ExcelMessage.ExcelReport.niceToString()}</span></Dropdown.Item>,
          p.importFromExcel && isPermissionAuthorized(ExcelPermission.ImportFromExcel) && <Dropdown.Item onClick={handleImportFromExcel} ><span><FontAwesomeIcon icon={"file-excel"} />&nbsp; {ImportFromExcelMessage.ImportFromExcel.niceToString()}</span></Dropdown.Item>,
          p.excelReport && addDropdownDividers([
            excelReports?.map((uq, i) =>
            <Dropdown.Item key={i}
              onClick={() => handleExcelReport(uq)}>
              {getToString(uq)}
              </Dropdown.Item>),
            ExcelReportEntity.tryOperationInfo(ExcelReportOperation.Save) &&
            [
              <Dropdown.Item onClick={handleAdmnister}><FontAwesomeIcon icon={"magnifying-glass"} className="me-2" />{ExcelMessage.Administer.niceToString()}</Dropdown.Item>,
              <Dropdown.Item onClick={handleCreate}><FontAwesomeIcon icon={"plus"} className="me-2" />{ExcelMessage.CreateNew.niceToString()}</Dropdown.Item>,
            ]
          ])
        ]) }
      </Dropdown.Menu>
    </Dropdown>
  );
}


function addDropdownDividers(elements: (React.ReactElement | React.ReactElement[] | false | null | undefined)[]): React.ReactElement[] {
  const result: React.ReactElement[] = [];

  for (let i = 0; i < elements.length; i++) {

    var elem = elements[i]; 

    if (!elem || Array.isArray(elem) && elem.length == 0) {
      continue;
    }

    if (result.length > 0) {
      result.push(<Dropdown.Divider />);
    }

    if (Array.isArray(elem))
      result.push(...elem);
    else
      result.push(elem);

   
  }

  return result;
};


export async function selectPagination(sc: SearchControlLoaded): Promise<QueryRequest | undefined> {
  var request = sc.getQueryRequest(true);

  const rt = sc.state.resultTable;

  if (request.pagination.mode == "Firsts" || request.pagination.mode == "Paginate" && (rt == null || rt!.totalElements! > rt!.rows.length)) {

    const pm = await SelectorModal.chooseElement<PaginationMode>([request.pagination.mode, "All"], {
      title: ExcelMessage.ExportToExcel.niceToString(),
      message: ExcelMessage.WhatDoYouWantToExport.niceToString(),
      buttonDisplay: a => <span>
        {a == "All" ? SearchMessage.AllPages.niceToString() : SearchMessage.CurrentPage.niceToString()}{" "}
        ({rt && SearchMessage._0Rows_N.niceToString().forGenderAndNumber(rt.totalElements).formatHtml(<strong>{a == "All" ? rt?.totalElements : rt?.rows.length}</strong>)})
      </span>,
      buttonName: a => a,
      size: "md",
    });

    if (pm == undefined)
      return undefined;

    if (pm == "All")
      request.pagination = { mode: "All" };

    return request;

  } else {

    return request;

  }
}
