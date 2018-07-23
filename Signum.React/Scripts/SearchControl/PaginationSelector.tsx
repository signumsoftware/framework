import * as React from 'react'
import * as numbro from 'numbro'
import * as Finder from '../Finder'
import { classes, Dic } from '../Globals'
import { ResultTable, Pagination, PaginationMode, PaginateMath } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey } from '../Signum.Entities'
import { getEnumInfo } from '../Reflection'
import * as Navigator from '../Navigator'


import "./PaginationSelector.css"

interface PaginationSelectorProps {
    resultTable?: ResultTable;
    pagination: Pagination;
    onPagination: (pag: Pagination) => void;
}


export default class PaginationSelector extends React.Component<PaginationSelectorProps> {

    render() {

        if (!this.props.pagination)
            return null;

        return (
            <div className="sf-search-footer">
                <div className="sf-pagination-left">{this.renderLeft()}</div>
                {this.renderCenter()}
                <div className="sf-pagination-right">{this.renderRight()}</div>
            </div>
        );
    }

    renderLeft(): React.ReactNode {

        const resultTable = this.props.resultTable;
        if (!resultTable)
            return "\u00a0";

        const pagination = this.props.pagination;

        function format(num: number): string {
            return numbro(num).format("0,0");
        }

        switch (pagination.mode) {

            case "All":
                return (
                    <span>{SearchMessage._0Results_N.niceToString().forGenderAndNumber(resultTable.totalElements).formatHtml(
                        <span className="sf-pagination-strong" key={1}>{resultTable.totalElements && format(resultTable.totalElements)}</span>)
                    }</span>
                );

            case "Firsts":
                return (
                    <span>{SearchMessage.First0Results_N.niceToString().forGenderAndNumber(resultTable.rows.length).formatHtml(
                        <span className={"sf-pagination-strong" + (resultTable.rows.length == resultTable.pagination.elementsPerPage ? " sf-pagination-overflow" : "")} key={1}>{format(resultTable.rows.length)}</span>)
                    }</span>
                );

            case "Paginate":
                return (
                    <span>{SearchMessage._01of2Results_N.niceToString().forGenderAndNumber(resultTable.totalElements).formatHtml(
                        <span className={"sf-pagination-strong"} key={1}>{format(PaginateMath.startElementIndex(pagination))}</span>,
                        <span className={"sf-pagination-strong"} key={2}>{format(PaginateMath.endElementIndex(pagination, resultTable.rows.length))}</span>,
                        <span className={"sf-pagination-strong"} key={3}>{resultTable.totalElements && format(resultTable.totalElements)}</span>)
                    }</span>
                );
            default:
                throw new Error("Unexpected pagination mode");
        }

    }

    handleMode = (e: React.ChangeEvent<HTMLSelectElement>) => {

        const mode = e.currentTarget.value as any as PaginationMode

        const p: Pagination = {
            mode: mode,
            elementsPerPage: mode != "All" ? Finder.defaultPagination.elementsPerPage : undefined,
            currentPage: mode == "Paginate" ? 1 : undefined
        };

        this.props.onPagination(p);
    }

    handleElementsPerPage = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const mode = this.props.pagination.mode;
        const p: Pagination = {
            mode: mode,
            elementsPerPage: parseInt(e.currentTarget.value),
            currentPage: mode == "Paginate" ? 1 : undefined,
        };
        this.props.onPagination(p);
    }

    handlePageClick = (page: number) => {

        const p: Pagination = {
            ...this.props.pagination,
            currentPage: page
        };
        this.props.onPagination(p);
    }

    renderCenter() {
        return (
            <div className="sf-pagination-center">
                <div className="form-inline">
                    <select value={this.props.pagination.mode} onChange={this.handleMode} ref="mode" className="form-control form-control-xs sf-pagination-mode">
                        {["Paginate" as PaginationMode,
                        "Firsts" as PaginationMode,
                        "All" as PaginationMode].map(mode =>
                            <option key={mode} value={mode.toString()}>{PaginationMode.niceToString(mode)}</option>)}
                    </select>
                    {this.props.pagination.mode != "All" &&
                        <select value={this.props.pagination.elementsPerPage!.toString()} onChange={this.handleElementsPerPage} ref="elementsPerPage" className="form-control form-control-xs sf-elements-per-page">
                            {[5, 10, 20, 50, 100, 200].map(elem =>
                                <option key={elem} value={elem.toString()}>{elem}</option>)}
                        </select>
                    }
                </div>
            </div>
        );
    }

    renderRight(): React.ReactNode {
        const resultTable = this.props.resultTable;
        if (!resultTable || resultTable.pagination.mode != "Paginate")
            return "\u00a0";

        const totalPages = PaginateMath.totalPages(resultTable.pagination, resultTable.totalElements);

        return (
            <PaginationComponent
                currentPage={resultTable.pagination.currentPage!}
                totalPages={totalPages}
                maxButtons={7}
                onSelect={num => this.handlePageClick(num)} />
        );
    }
}


interface PaginationComponentProps {
    currentPage: number;
    totalPages: number;
    maxButtons: number;
    onSelect: (num: number) => void;
}

export class PaginationComponent extends React.Component<PaginationComponentProps> {

    handlePageClicked = (e: React.MouseEvent<any>, num: number) => {
        e.preventDefault();
        this.props.onSelect(num);
    }

    render() {
        const { currentPage, totalPages, maxButtons, onSelect } = this.props;

        var prevCount = Math.floor((maxButtons - 1) / 2);
        var nextCount = maxButtons - 1 - prevCount;

        const { first, last } = this.getFirstLast();

        return (
            <ul className="pagination">
                {this.addPageLink("First", 1, "«", "First", currentPage == 1 ? "disabled" : undefined)}
                {first != 1 && <li className="page-item disabled"><span className="page-link">…</span></li>}
                {Array.range(first, last + 1).map(page => this.addPageLink(page.toString(), page, page.toString(), page.toString(), page == currentPage ? "active" : undefined))}
                {last != totalPages && <li className="page-item disabled"><span className="page-link">…</span></li>}
                {this.addPageLink("Last", totalPages, "»", "Last", currentPage == totalPages ? "disabled" : undefined)}
            </ul>
        );
    }


    getFirstLast(): { first: number; last: number; } {
        const { currentPage, totalPages, maxButtons, onSelect } = this.props;

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

    addPageLink(key: string, page: number, text: string, ariaLabel: string, mode?: "active" | "disabled") {
        return (
            <li className={classes("page-item", mode)} key={key} >
                {
                    mode != undefined ?
                        <span className="page-link">{text}</span> :
                        <a href="#" className="page-link" onClick={e => this.handlePageClicked(e, page)}>
                            {text}
                        </a>
                }
            </li>
        );
    }
}
