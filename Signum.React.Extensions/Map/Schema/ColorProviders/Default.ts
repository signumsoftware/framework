import * as d3 from 'd3'
import { ClientColorProvider, SchemaMapInfo  } from '../SchemaMap'
import { colorScale, colorScaleSqr  } from '../../Utils'
import { EntityData, EntityKind } from '../../../../../Framework/Signum.React/Scripts/Reflection'
import { MapMessage } from '../../Signum.Entities.Map'

export default function getDefaultProviders(info: SchemaMapInfo): ClientColorProvider[] {

    const namespaceColor = d3.scaleOrdinal(d3.schemeCategory20);
    const namespace: ClientColorProvider = {
        name: "namespace",
        getFill: t => namespaceColor(t.namespace),
        getTooltip: t => t.namespace
    };


    const f: { [ek: string]: string } = {};

    f["SystemString"] = "#8c564b";
    f["System"] = "#7f7f7f";
    f["Relational"] = "#17becf";
    f["String"] = "#e377c2";
    f["Shared"] = "#2ca02c";
    f["Main"] = "#d62728";
    f["Part"] = "#ff7f0e";
    f["SharedPart"] = "#bcbd22";

    const entityKind: ClientColorProvider = {
        name: "entityKind",
        getFill: t => f[t.entityKind],
        getTooltip: t => t.entityKind
    };


    const entityData: ClientColorProvider = {
        name: "entityData",
        getFill: t => t.entityData == "Master" ? "#2ca02c" :
            t.entityData == "Transactional" ? "#d62728" : "black",
        getTooltip: t => t.entityData
    };

    const rowsColor = colorScaleSqr(info.tables.filter(a => a.rows != null).map(a => a.rows!).max());
    const rows: ClientColorProvider = {
        name: "rows",
        getFill: t => t.rows == null ? "blue" : <any>rowsColor(t.rows),
        getTooltip: t => t.rows + " " + MapMessage.Rows.niceToString()
    };

    const columnsColor = colorScaleSqr(info.tables.map(a => a.columns).max());
    const columns: ClientColorProvider = {
        name: "columns",
        getFill: t => <any>columnsColor(t.columns),
        getTooltip: t => t.columns + " " + MapMessage.Columns.niceToString()
    };

    const tableSizeColor = colorScaleSqr(info.tables.filter(a => a.total_size_kb != null).map(a => a.total_size_kb!).max());
    const tableSize: ClientColorProvider = {
        name: "tableSize",
        getFill: t => t.total_size_kb == null ? "blue" : <any>tableSizeColor(t.total_size_kb),
        getTooltip: t => t.total_size_kb + " KB"
    };


    return [namespace, entityKind, entityData, rows, columns, tableSize];
}

