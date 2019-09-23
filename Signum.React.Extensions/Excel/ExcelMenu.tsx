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

export default class ExcelMenu extends React.Component<ExcelMenuProps, { excelReport?: Lite<ExcelReportEntity>[], isOpen: boolean }> {
  constructor(props: ExcelMenuProps) {
    super(props);
    this.state = { isOpen: false };
  }

  handleSelectedToggle = () => {

    if (this.state.isOpen == false && this.state.excelReport == undefined)
      this.reloadList().done();

    this.setState({ isOpen: !this.state.isOpen })
  }

  reloadList(): Promise<void> {
    return ExcelClient.API.forQuery(this.props.searchControl.props.findOptions.queryKey)
      .then(list => this.setState({ excelReport: list }));
  }


  handlePlainExcel = () => {
    ExcelClient.API.generatePlanExcel(this.props.searchControl.getQueryRequest());
  }


  handleClick = (er: Lite<ExcelReportEntity>) => {
    ExcelClient.API.generateExcelReport(this.props.searchControl.getQueryRequest(), er);
  }

  handleCreate = () => {
    Finder.API.fetchQueryEntity(this.props.searchControl.props.findOptions.queryKey)
      .then(qe => ExcelReportEntity.New({ query: qe }))
      .then(er => Navigator.view(er))
      .then(() => this.reloadList())
      .done();
  }

  handleAdmnister = () => {
    Finder.explore({ queryName: ExcelReportEntity, parentToken: ExcelReportEntity.token(a => a.query!.key), parentValue: this.props.searchControl.props.findOptions.queryKey })
      .then(() => this.reloadList())
      .done();
  }

  render() {
    const label = <span><FontAwesomeIcon icon={["far", "file-excel"]} />&nbsp;{this.props.searchControl.props.largeToolbarButtons == true ? " " + ExcelMessage.ExcelReport.niceToString() : undefined}</span>;

    if (this.props.plainExcel && !this.props.excelReport)
      return <button className={"sf-query-button sf-search btn btn-light"} onClick={this.handlePlainExcel}>{label} </button>;

    const excelReports = this.state.excelReport;
    return (
      <Dropdown show={this.state.isOpen} onToggle={this.handleSelectedToggle}>
        <Dropdown.Toggle id="userQueriesDropDown" className="sf-userquery-dropdown" variant="light">
        {label}
        </Dropdown.Toggle>
        <Dropdown.Menu>
          {this.props.plainExcel && <Dropdown.Item onClick={this.handlePlainExcel} ><span><FontAwesomeIcon icon={["far", "file-excel"]} />&nbsp; {ExcelMessage.ExcelReport.niceToString()}</span></Dropdown.Item>}
          {this.props.plainExcel && excelReports && excelReports.length > 0 && <Dropdown.Divider />}
          {
            excelReports && excelReports.map((uq, i) =>
              <Dropdown.Item key={i}
                onClick={() => this.handleClick(uq)}>
                {uq.toStr}
              </Dropdown.Item>)
          }
          {(this.props.plainExcel || excelReports && excelReports.length > 0) && <Dropdown.Divider />}
          {Operations.isOperationAllowed(ExcelReportOperation.Save, ExcelReportEntity) && <Dropdown.Item onClick={this.handleAdmnister}><FontAwesomeIcon icon={["fas", "search"]} className="mr-2" />{ExcelMessage.Administer.niceToString()}</Dropdown.Item>}
          {Operations.isOperationAllowed(ExcelReportOperation.Save, ExcelReportEntity) && <Dropdown.Item onClick={this.handleCreate}><FontAwesomeIcon icon={["fas", "plus"]} className="mr-2" />{ExcelMessage.CreateNew.niceToString()}</Dropdown.Item>}
        </Dropdown.Menu>
      </Dropdown>
    );
  }
}



