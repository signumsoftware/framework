import {IEntity, Lite, EntityControlMessage, liteKey} from 'Framework/Signum.React/Scripts/Signum.Entities';
import {Pagination, ResultColumn, FilterType, ResultTable, ResultRow, PaginationMode, ColumnOption} from 'Framework/Signum.React/Scripts/FindOptions';
import {getTypeInfo, getEnumInfo, toMomentFormat} from 'Framework/Signum.React/Scripts/Reflection';
import {navigateRoute, isNavigable} from 'Framework/Signum.React/Scripts/Navigator';
import * as React from 'react';
import { Link  } from 'react-router';
import * as moment from 'moment';


export var defaultPagination: Pagination = {
    mode: PaginationMode.Paginate,
    elementsPerPage: 20,
    currentPage: 1,
};


export const defaultOrderColumn: string = "Id";

export interface QuerySettings {
    queryName: any;
    pagination?: Pagination;
    defaultOrderColumn?: string;
    formatters?: { [columnName: string]: CellFormatter };
    rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes;
    entityFormatter?: EntityFormatter;
}

export interface FormatRule {
    name: string;
    formatter: (column: ColumnOption) => CellFormatter;
    isApplicable: (column: ColumnOption) => boolean;
}

export class CellFormatter {
    constructor(
        public formatter: (cell: any) => React.ReactNode,
        public textAllign = "left") {
    }
}


export var FormatRules: FormatRule[] = [
    {
        name: "Object",
        isApplicable: col=> true,
        formatter: col=> new CellFormatter(cell => cell ? (cell.ToString || cell.toString()) : null)
    },
    {
        name: "Enum",
        isApplicable: col=> col.token.filterType == FilterType.Enum,
        formatter: col=> new CellFormatter(cell => getEnumInfo(col.token.type.name, cell).niceName)
    },
    {
        name: "Lite",
        isApplicable: col=> col.token.filterType == FilterType.Lite,
        formatter: col=> new CellFormatter((cell: Lite<IEntity>) => cell && <Link to={navigateRoute(cell) }>{cell.toStr}</Link>)
    },
    {
        name: "Guid",
        isApplicable: col=> col.token.filterType == FilterType.Guid,
        formatter: col=> new CellFormatter((cell: string) => cell && (cell.substr(0, 5) + "…" + cell.substring(cell.length - 5)))
    },
    {
        name: "DateTime",
        isApplicable: col=> col.token.filterType == FilterType.DateTime,
        formatter: col=> {
            var momentFormat = toMomentFormat(col.token.format);
            return new CellFormatter((cell: string) => cell == null || cell == "" ? "" : moment(cell).format(momentFormat))
        }
    },
    {
        name: "Number",
        isApplicable: col=> col.token.filterType == FilterType.Integer || col.token.filterType == FilterType.Decimal,
        formatter: col=> new CellFormatter((cell: number) => cell && cell.toString())
    },
    {
        name: "Number with Unit",
        isApplicable: col=> (col.token.filterType == FilterType.Integer || col.token.filterType == FilterType.Decimal) && !!col.token.unit,
        formatter: col=> new CellFormatter((cell: number) => cell && cell.toString() + " " + col.token.unit)
    },
    {
        name: "Bool",
        isApplicable: col=> col.token.filterType == FilterType.Boolean,
        formatter: col=> new CellFormatter((cell: boolean) => cell == null ? null : <input type="checkbox" disabled={true} checked={cell}/>)
    },
];




export interface EntityFormatRule {
    name: string;
    formatter: EntityFormatter;
    isApplicable: (row: ResultRow) => boolean;
}


export type EntityFormatter = (row: ResultRow) => React.ReactNode;

export var EntityFormatRules: EntityFormatRule[] = [
    {
        name: "View",
        isApplicable: row=> true,
        formatter: row=> !isNavigable(row.entity, null, true) ? null :
            <Link to={navigateRoute(row.entity) } title={row.entity.toStr} data-entity-link={liteKey(row.entity) }>
                {EntityControlMessage.View.niceToString() }
                </Link>
    },
];


