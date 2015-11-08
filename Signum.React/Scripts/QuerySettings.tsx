import {IEntity, Lite} from 'Framework/Signum.React/Scripts/Signum.Entities';
import {Pagination, Column, FilterType} from 'Framework/Signum.React/Scripts/FindOptions';
import {typeInfo, getEnumInfo} from 'Framework/Signum.React/Scripts/Reflection';
import {navigateRoute} from 'Framework/Signum.React/Scripts/Navigator';
import * as React from 'react';
import { Link  } from 'react-router';

export class QuerySettings {
    queryName: any;
    pagination: Pagination;
    isFindable: boolean = true;
    defaultOrderColumn: string = "Id";
    
    console(queryName: any) {
        this.queryName = queryName;
    }
}

export interface FormatRule {
    name: string;
    formatter: (column: Column) => CellFormatter;
    isApplicable: (column: Column) => boolean;
}


export var GlobalFormatRules: FormatRule[] = [
    {
        name: "Object",
        isApplicable: col=> true,
        formatter: col=> new CellFormatter(cel => cel)
    },
    {
        name: "Enum",
        isApplicable: col=> col.token.filterType == FilterType.Enum,
        formatter: col=>new CellFormatter(cel => getEnumInfo(col.token.typeName, cel).niceName)
    },
    {
        name: "Lite",
        isApplicable: col=> col.token.filterType == FilterType.Lite,
        formatter: col=> new CellFormatter((cel: Lite<IEntity>) => cel && <Link to={navigateRoute(cel) }>{cel.toString}</Link>)
    },
    {
        name: "Guid",
        isApplicable: col=> col.token.filterType == FilterType.Guid,
        formatter: col=> new CellFormatter((cel: string) => cel && (cel.substr(0, 5) + "…" + cel.substring(cel.length - 5)))
    },
    {
        name: "DateTime",
        isApplicable: col=> col.token.filterType == FilterType.DateTime,
        formatter: col=> new CellFormatter((cel: string) => cel && (cel.substr(0, 5) + "…" + cel.substring(cel.length - 5)))
    },
];

export class CellFormatter {
    constructor(
        public formatter: (cell: any) => any,
        public textAllign = "left") {
    }
}

