import { ClientColorProvider, SchemaMapInfo } from '../Signum.Map/Schema/ClientColorProvider';


export default function getDefaultProviders(info: SchemaMapInfo): ClientColorProvider[] {
  return [
    {
      name: "isolation",
      getFill: t => t.extra["isolation"] == undefined ? "white" :
        t.extra["isolation"] == "Isolated" ? "#CC0099" :
          t.extra["isolation"] == "Optional" ? "#9966FF" :
            t.extra["isolation"] == "None" ? "#00CCFF" : "black",
      getTooltip: t => t.extra["isolation"] == undefined ? undefined : t.extra["isolation"]
    }
  ];
}


