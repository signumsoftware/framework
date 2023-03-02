import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Finder from '@framework/Finder'
import { getToString, Lite, PaginationMessage, SearchMessage, SelectorMessage } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { ExcelReportEntity, ExcelMessage, ExcelReportOperation, ImportFromExcelMessage } from './Signum.Entities.Excel'
import * as ExcelClient from './ExcelClient'
import { Dropdown, DropdownButton } from 'react-bootstrap';
import * as Operations from '@framework/Operations';
import SelectorModal from '@framework/SelectorModal'
import { PaginationMode, QueryRequest } from '@framework/FindOptions'
import { getTypeInfos } from '@framework/Reflection'
import { onImportFromExcel } from './Templates/ImportExcelModel'


export interface ExcelMenuProps {
  searchControl: SearchControlLoaded;
  plainExcel: boolean;
  importFromExcel: boolean;
  excelReport: boolean;
}

export default function ExcelMenu(p: ExcelMenuProps) {

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

  const label = <span><FontAwesomeIcon icon={["far", "file-excel"]} />{p.searchControl.props.largeToolbarButtons == true ? <span className="d-none d-sm-inline">{" " + ExcelMessage.ExcelReport.niceToString()}</span> : undefined}</span>;

  if (p.plainExcel && !p.excelReport && !p.importFromExcel)
    return <button className={"sf-query-button sf-search btn btn-light"} title={ExcelMessage.ExcelReport.niceToString() } onClick={handlePlainExcel}>{label} </button>;

  return (
    <Dropdown show={isOpen} onToggle={handleSelectedToggle} title={ExcelMessage.ExcelReport.niceToString()}>
      <Dropdown.Toggle id="userQueriesDropDown" className="sf-userquery-dropdown" variant="light">
      {label}
      </Dropdown.Toggle>
      <Dropdown.Menu>
        {...addDropdownDividers([
          p.plainExcel && <Dropdown.Item onClick={handlePlainExcel} ><span><FontAwesomeIcon icon={["far", "file-excel"]} />&nbsp; {ExcelMessage.ExcelReport.niceToString()}</span></Dropdown.Item>,
          p.importFromExcel && <Dropdown.Item onClick={handleImportFromExcel} ><span><FontAwesomeIcon icon={["fas", "file-excel"]} />&nbsp; {ImportFromExcelMessage.ImportFromExcel.niceToString()}</span></Dropdown.Item>,
          p.excelReport && addDropdownDividers([
            excelReports?.map((uq, i) =>
            <Dropdown.Item key={i}
              onClick={() => handleExcelReport(uq)}>
              {getToString(uq)}
            </Dropdown.Item>),
            Operations.tryGetOperationInfo(ExcelReportOperation.Save, ExcelReportEntity) &&
            [
              <Dropdown.Item onClick={handleAdmnister}><FontAwesomeIcon icon={["fas", "magnifying-glass"]} className="me-2" />{ExcelMessage.Administer.niceToString()}</Dropdown.Item>,
              <Dropdown.Item onClick={handleCreate}><FontAwesomeIcon icon={["fas", "plus"]} className="me-2" />{ExcelMessage.CreateNew.niceToString()}</Dropdown.Item>,
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
      buttonDisplay: a => <span>{PaginationMode.niceToString(a)} {rt && SearchMessage._0Results_N.niceToString().forGenderAndNumber(rt.totalElements).formatHtml(
        <span className="sf-pagination-strong" key={1}>{a == "All" ? rt?.totalElements : rt?.rows.length}</span>)
      }</span>,
      buttonName: a => a,
      title: SelectorMessage._0Selector.niceToString(PaginationMode.niceTypeName()),
      message: SelectorMessage.PleaseChooseA0ToContinue.niceToString(PaginationMode.niceTypeName()),
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
