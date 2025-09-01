import * as d3 from 'd3'
import { colorScaleLog } from '../Utils'
import { MapMessage } from '../Signum.Map'
import { bytesToSize } from '@framework/Globals'
import { toNumberFormat } from '@framework/Reflection'
import { ClientColorProvider, SchemaMapInfo } from './ClientColorProvider'

export default function getDefaultProviders(info: SchemaMapInfo): ClientColorProvider[] {
  const namespaceColor = d3.scaleOrdinal(d3.schemePaired);
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
      t.entityData == "Transactional" ? "#d62728" : "var(--bs-body-color)",
    getTooltip: t => t.entityData
  };

  const columnsColor = colorScaleLog(info.tables.map(a => a.columns).max()!);
  const columns: ClientColorProvider = {
    name: "columns",
    getFill: t => <any>columnsColor(t.columns),
    getTooltip: t => t.columns + " " + MapMessage.Columns.niceToString()
  };

  const rowsColor = colorScaleLog(info.tables.filter(a => a.rows != null).map(a => a.rows!).max()!);
  const rows: ClientColorProvider = {
    name: "rows",
    getFill: t => t.rows == null ? "gray" : <any>rowsColor(t.rows),
    getTooltip: t => (t.rows == null ? "" : roundValue(t.rows, scientificUnits)) + " " + MapMessage.Rows.niceToString()
  };

  const tableSizeColor = colorScaleLog(info.tables.filter(a => a.total_size_kb != null).map(a => a.total_size_kb!).max()!);
  const tableSize: ClientColorProvider = {
    name: "tableSize",
    getFill: t => t.total_size_kb == null ? "gray" : <any>tableSizeColor(t.total_size_kb),
    getTooltip: t => bytesToSize((t.total_size_kb || 0) * 1024)
  };

  var result = [namespace, entityKind, entityData, columns, rows, tableSize];

  if (info.providers.some(a => a.name == "rows_history")) {
    const rowsColorHistory = colorScaleLog(info.tables.filter(a => a.rows_history != null).map(a => a.rows_history!).max()!);
    const rowsHistory: ClientColorProvider = {
      name: "rows_history",
      getFill: t => t.rows_history == null ? "gray" : <any>rowsColorHistory(t.rows_history),
      getTooltip: t => t.rows_history == null ? "No history table" : roundValue(t.rows_history, scientificUnits) + " " + MapMessage.Rows.niceToString()
    };

    result.push(rowsHistory);
  }

  if (info.providers.some(a => a.name == "tableSize_history")) {
    const tableSizeColorHistory = colorScaleLog(info.tables.filter(a => a.total_size_kb_history != null).map(a => a.total_size_kb_history!).max()!);
    const tableSizeHistory: ClientColorProvider = {
      name: "tableSize_history",
      getFill: t => t.total_size_kb_history == null ? "gray" : <any>tableSizeColorHistory(t.total_size_kb_history),
      getTooltip: t => t.rows_history == null ? "No history table" : bytesToSize((t.total_size_kb_history || 0) * 1024)
    };

    result.push(tableSizeHistory);
  }

  return result;
}

const scientificUnits = [ "", "k", "m", "b", "t" ];
const computerUnitsAbrev = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" ];
const computerUnits = ["Bytes", "KBytes", "MBytes", "GBytes", "TBytes", "PBytes", "EBytes", "ZBytes", "YBytes" ];

export function roundValue(value: number, units: string[]): string {

  let valor = value;
  let i;

  var base = units == scientificUnits ? 1000 : 1024;

  for (i = 0; i < units.length && valor >= base; i++)
    valor /= base;

  var numberFormat = toNumberFormat("N2");

  return numberFormat.format(valor) + " " + units[i];
}



