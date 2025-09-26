import * as React from 'react'
import { Finder } from '../Finder'
import { classes } from '../Globals'
import { ResultTable, Pagination, PaginationMode, PaginateMath } from '../FindOptions'
import { SearchMessage } from '../Signum.Entities'
import { toNumberFormat } from '../Reflection'
import "./PaginationSelector.css"

interface PaginationSelectorProps {
  resultTable?: ResultTable;
  pagination: Pagination;
  onPagination: (pag: Pagination) => void;
}

export function PaginationSelector(p: PaginationSelectorProps): React.ReactElement | null {
  if (!p.pagination)
    return null;

  return (
    <div className="sf-search-footer">
      <div className="sf-pagination-left">{renderLeft()}</div>
      {renderCenter()}
      <div className="sf-pagination-right">{renderRight()}</div>
    </div>
  );

  function renderLeft(): React.ReactNode {

    const resultTable = p.resultTable;
    if (!resultTable)
      return "\u00a0";

    const pagination = p.pagination;

    var numberFormat = toNumberFormat("0") 

    switch (pagination.mode) {

      case "All":
        return (
          <span>{SearchMessage._0Results_N.niceToString().forGenderAndNumber(resultTable.totalElements).formatHtml(
            <span className="sf-pagination-strong" key={1}>{resultTable.totalElements && numberFormat.format(resultTable.totalElements)}</span>)
          }</span>
        );

      case "Firsts":
        return (
          <span>{SearchMessage.First0Results_N.niceToString().forGenderAndNumber(resultTable.rows.length).formatHtml(
            <span className={"sf-pagination-strong" + (resultTable.rows.length == resultTable.pagination.elementsPerPage ? " sf-pagination-overflow" : "")} key={1}>{numberFormat.format(resultTable.rows.length)}</span>)
          }</span>
        );

      case "Paginate":
        if (resultTable.rows.length == 0) {
          if (resultTable.totalElements == 0)
            return <span>{SearchMessage._0Results_N.niceToString().forGenderAndNumber(resultTable.totalElements).formatHtml(
              <span className="sf-pagination-strong" key={1}>{resultTable.totalElements && numberFormat.format(resultTable.totalElements)}</span>)
            }</span>;
          else
            return <span>{SearchMessage.NoResultsInThisPage.niceToString()}</span>;
        }

        return (
          <span>{SearchMessage._01of2Results_N.niceToString().forGenderAndNumber(resultTable.totalElements).formatHtml(
            <span className={"sf-pagination-strong"} key={1}>{numberFormat.format(PaginateMath.startElementIndex(pagination))}</span>,
            <span className={"sf-pagination-strong"} key={2}>{numberFormat.format(PaginateMath.endElementIndex(pagination, resultTable.rows.length))}</span>,
            <span className={"sf-pagination-strong"} key={3}>{resultTable.totalElements && numberFormat.format(resultTable.totalElements)}</span>)
          }</span>
        );
      default:
        throw new Error("Unexpected pagination mode");
    }
  }

  function handleMode(e: React.ChangeEvent<HTMLSelectElement>){

    const mode = e.currentTarget.value as any as PaginationMode

    const pag: Pagination = {
      mode: mode,
      elementsPerPage: mode != "All" ? Finder.Options.defaultPagination.elementsPerPage : undefined,
      currentPage: mode == "Paginate" ? 1 : undefined
    };

    p.onPagination(pag);
  }

  function handleElementsPerPage(e: React.ChangeEvent<HTMLSelectElement>) {
    const mode = p.pagination.mode;
    const pag: Pagination = {
      mode: mode,
      elementsPerPage: parseInt(e.currentTarget.value),
      currentPage: mode == "Paginate" ? 1 : undefined,
    };
    p.onPagination(pag);
  }

  function handlePageClick(page: number) {

    const pag: Pagination = {
      ...p.pagination,
      currentPage: page
    };
    p.onPagination(pag);
  }

  function renderCenter() {

    return (
      <div className="sf-pagination-center">
          {(p.pagination.mode == "All" ? SearchMessage.PaginationAll_0Elements :
            p.pagination.mode == "Firsts" ? SearchMessage.PaginationFirst_01Elements :
              SearchMessage.PaginationPages_0Of01lements)
            .niceToString().formatHtml(

              <select
                value={p.pagination.mode}
                onChange={handleMode}
                className="form-select form-select-xs w-auto sf-pagination-mode mx-1"
                aria-label={SearchMessage.PaginationMode.niceToString()}
                aria-required="false"
                aria-readonly="false"
                aria-disabled="false">
                {PaginationSelector.onGetPaginationModes().map(mode =>
                    <option key={mode} value={mode.toString()}>{PaginationMode.niceToString(mode)}</option>)}
                </select>,

                p.pagination.mode != "All" &&
                <select
                    value={p.pagination.elementsPerPage!.toString()}
                    onChange={handleElementsPerPage}
                    className="form-select form-select-xs w-auto sf-elements-per-page mx-1"
                    aria-label={SearchMessage.NumberOfElementsForPagination.niceToString()}
                    aria-required="false"
                    aria-readonly="false"
                    aria-disabled="false">
                  {PaginationSelector.onGetPaginationSizes().map(elem =>
                    <option key={elem} value={elem.toString()}>{elem}</option>)}
                </select>
              )
          }
      </div>
    );
  }

  function renderRight(): React.ReactNode {
    const resultTable = p.resultTable;
    if (!resultTable || resultTable.pagination.mode != "Paginate")
      return "\u00a0";

    const totalPages = PaginateMath.totalPages(resultTable.pagination, resultTable.totalElements!);

    return (
      <PaginationComponent
        currentPage={resultTable.pagination.currentPage!}
        totalPages={totalPages}
        maxButtons={7}
        onSelect={num => handlePageClick(num)} />
    );
  }
}

interface PaginationComponentProps {
  currentPage: number;
  totalPages: number;
  maxButtons: number;
  onSelect: (num: number) => void;
}

export function PaginationComponent(p: PaginationComponentProps): React.ReactElement {

  function handlePageClicked(e: React.MouseEvent<any>, num: number) {
    e.preventDefault();
    p.onSelect(num);
  }

  const { currentPage, totalPages, maxButtons, onSelect } = p;

  var prevCount = Math.floor((maxButtons - 1) / 2);
  var nextCount = maxButtons - 1 - prevCount;

  const { first, last } = getFirstLast();

  return (
    <ul className="pagination">
      {addPageLink("First", 1, "«", "First", currentPage == 1 ? "disabled" : undefined)}
      {first != 1 && <li className="page-item disabled"><span className="page-link">…</span></li>}
      {Array.range(first, last + 1).map(page => addPageLink(page.toString(), page, page.toString(), page.toString(), page == currentPage ? "active" : undefined))}
      {last != totalPages && <li className="page-item disabled"><span className="page-link">…</span></li>}
      {addPageLink("Last", totalPages, "»", "Last", currentPage == totalPages ? "disabled" : undefined)}
    </ul>
  );


  function getFirstLast(): { first: number; last: number; } {
    const { currentPage, totalPages, maxButtons, onSelect } = p;

    if (totalPages <= maxButtons)
      return { first: 1, last: totalPages };

    const prevCount = Math.floor((maxButtons - 1) / 2);
    const nextCount = maxButtons - 1 - prevCount;

    if (currentPage - prevCount <= 1)
      return { first: 1, last: maxButtons };

    if (currentPage + nextCount > totalPages)
      return { first: totalPages - maxButtons + 1, last: totalPages };

    return {
      first: currentPage - prevCount,
      last: currentPage + nextCount
    };
  }

  function addPageLink(key: string, page: number, text: string, ariaLabel: string, mode?: "active" | "disabled") {
    return (
      <li className={classes("page-item", mode)} key={key} >
        {
          mode != undefined ?
            <span className="page-link">{text}</span> :
            <a href="#" className="page-link" onClick={e => handlePageClicked(e, page)}>
              {text}
            </a>
        }
      </li>
    );
  }
}

export namespace PaginationSelector {
  export let onGetPaginationModes = (): PaginationMode[] => {
    return ["Paginate", "Firsts", "All"];
  }

  export let onGetPaginationSizes = (): number[] => {
    return [5, 10, 20, 50, 100, 200];
  }
}

export default PaginationSelector;
