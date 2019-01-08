import * as d3 from "d3";
import { ChartTable, ChartColumn, ChartRow } from "../../ChartClient";

export function stratifyTokens(
  data: ChartTable,
  keyColumn: ChartColumn<unknown>, /*Employee*/
  keyColumnParent?: ChartColumn<unknown>, /*Employee.ReportsTo*/):
  d3.HierarchyNode<ChartRow | Folder | Root> {


  const folders = data.rows
    .filter(r => keyColumnParent != null && keyColumnParent.getValue(r) != null)
    .map(r => ({ folder: keyColumnParent!.getValue(r) }) as Folder)
    .toObjectDistinct(r => keyColumnParent!.getKey(r.folder));

  const root: Root = { isRoot: true };

  const NullConst = "- Null -";


  const keyToRow = data.rows.filter(r => keyColumn.getValue(r) != null).toObjectDistinct(r => keyColumn.getValueKey(r));

  const getParent = (d: ChartRow | Folder | Root) => {
    if ((d as Root).isRoot)
      return null;

    if ((d as Folder).folder) {
      const r = keyToRow[keyColumnParent!.getKey((d as Folder).folder)];

      if (!r)
        return root;

      const parentValue = keyColumnParent!.getValue(r);
      if (parentValue == null)
        return root;  //Either null

      return folders[keyColumnParent!.getKey(parentValue)]; // Parent folder
    }

    var keyVal = keyColumn.getValue(d as ChartRow);

    if (keyVal) {
      const r = d as ChartRow;

      var fold = folders[keyColumn.getKey(keyVal)];
      if (fold)
        return fold; //My folder

      if (keyColumnParent) {

        const parentValue = keyColumnParent.getValue(r);

        const parentFolder = parentValue && folders[keyColumnParent.getKey(parentValue)];

        if (parentFolder)
          return folders[keyColumnParent.getKey(parentFolder.folder)]; //only parent
      }

      return root; //No key an no parent
    }

    return root;
  };

  var getKey = (r: ChartRow | Folder | Root) => {

    if ((r as Root).isRoot)
      return "#Root";

    if ((r as Folder).folder)
      return "F#" + keyColumnParent!.getKey((r as Folder).folder);

    const cr = (r as ChartRow);

    if (keyColumn.getValue(cr) != null)
      return keyColumn.getKey(cr);

    return NullConst;
  }

  var rootNode = d3.stratify<ChartRow | Folder | Root>()
    .id(getKey)
    .parentId(r => {
      var parent = getParent(r);
      return parent ? getKey(parent) : null
    })([root, ...Object.values(folders), ...data.rows]);

  return rootNode

}

export interface Folder {
  folder: unknown;
}

export function isFolder(obj: any): obj is Folder {
  return (obj as Folder).folder !== undefined;
}

export interface Root {
  isRoot: true;
}

export function isRoot(obj: any): obj is Root {
  return (obj as Root).isRoot;
}
