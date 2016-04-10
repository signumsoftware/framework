import * as d3 from 'd3'
import { ClientColorProvider, TableInfo  } from './SchemaMap'
import { colorScale, colorScaleSqr  } from './Utils'
import { EntityData, EntityKind } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { MapMessage } from '../Signum.Entities.Map'

export default function getDefaultProviders() : ClientColorProvider[]{

    return [
        {
            name: "namespace",
            getColors: (nodes: TableInfo[]) => {
                var color = d3.scale.category20();

                return {
                    getFill: t => color(t.namespace),
                    getTooltip: t => t.namespace
                };
            }
        } as ClientColorProvider,

        {
            name: "entityKind",
            getColors: (nodes: TableInfo[]) => {
                var f: { [ek: number]: string } = {};

                f[EntityKind.SystemString] = "#8c564b";
                f[EntityKind.System] = "#7f7f7f";
                f[EntityKind.Relational] = "#17becf";
                f[EntityKind.String] = "#e377c2";
                f[EntityKind.Shared] = "#2ca02c";
                f[EntityKind.Main] = "#d62728";
                f[EntityKind.Part] = "#ff7f0e";
                f[EntityKind.SharedPart] = "#bcbd22";

                return {
                    getFill: t => f[t.entityKind],
                    getTooltip: t => EntityKind[t.entityKind]
                };
            }
        } as ClientColorProvider,

        {
            name: "entityData",
            getColors: (nodes: TableInfo[]) => {
                return {
                    getFill: t =>
                        t.entityData == EntityData.Master ? "#2ca02c" :
                            t.entityData == EntityData.Transactional ? "#d62728" : "black",
                    getTooltip: t => EntityData[t.entityData]
                };
            }
        } as ClientColorProvider,

        {
            name: "rows",
            getColors: (nodes: TableInfo[]) => {
                var color = colorScaleSqr(nodes.map(a => a.rows).max());

                return {
                    getFill: t => <any>color(t.rows),
                    getTooltip: t => t.rows + " " + MapMessage.Rows.niceToString()
                };
            }
        } as ClientColorProvider,

        {
            name: "columns",
            getColors: (nodes: TableInfo[]) => {
                var color = colorScaleSqr(nodes.map(a => a.columns).max());

                return {
                    getFill: t => <any>color(t.columns),
                    getTooltip: t => t.columns + " " + MapMessage.Columns.niceToString()
                };
            }
        } as ClientColorProvider,

        {
            name: "tableSize",
            getColors: (nodes: TableInfo[]) => {
                var color = colorScaleSqr(nodes.map(a => a.total_size_kb).max());

                return {
                    getFill: t => <any>color(t.total_size_kb),
                    getTooltip: t => t.total_size_kb + " KB"
                };
            }
        } as ClientColorProvider,

        {
            name: "namespace",
            getColors: (nodes: TableInfo[]) => {

            }
        } as ClientColorProvider,

        {
            name: "namespace",
            getColors: (nodes: TableInfo[]) => {

            }
        } as ClientColorProvider,
    ];
}

