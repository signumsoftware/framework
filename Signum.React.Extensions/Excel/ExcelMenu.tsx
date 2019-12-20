import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Finder from '@framework/Finder'
import { Lite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { ExcelReportEntity, ExcelMessage, ExcelReportOperation } from './Signum.Entities.Excel'
import * as ExcelClient from './ExcelClient'
import { Dropdown, DropdownButton } from 'react-bootstrap';
import * as Operations from '@framework/Operations';

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
    ExcelClient.API.generatePlainExcel(p.searchControl.getQueryRequest());
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
    Finder.explore({ queryName: ExcelReportEntity, parentToken: ExcelReportEntity.token(a => a.query!.key), parentValue: p.searchControl.props.findOptions.queryKey })
      .then(() => reloadList())
      .done();
  }

  const label = <span><FontAwesomeIcon icon={["far", "file-excel"]} />&nbsp;{p.searchControl.props.largeToolbarButtons == true ? " " + ExcelMessage.ExcelReport.niceToString() : undefined}</span>;

  if (p.plainExcel && !p.excelReport)
    return <button className={"sf-query-button sf-search btn btn-light"} onClick={handlePlainExcel}>{label} </button>;

  return (
    <Dropdown show={isOpen} onToggle={handleSelectedToggle}>
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
        {Operations.isOperationAllowed(ExcelReportOperation.Save, ExcelReportEntity) && <Dropdown.Item onClick={handleAdmnister}><FontAwesomeIcon icon={["fas", "search"]} className="mr-2" />{ExcelMessage.Administer.niceToString()}</Dropdown.Item>}
        {Operations.isOperationAllowed(ExcelReportOperation.Save, ExcelReportEntity) && <Dropdown.Item onClick={handleCreate}><FontAwesomeIcon icon={["fas", "plus"]} className="mr-2" />{ExcelMessage.CreateNew.niceToString()}</Dropdown.Item>}
      </Dropdown.Menu>
    </Dropdown>
  );
}



