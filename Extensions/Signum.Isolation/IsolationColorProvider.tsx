import { ClientColorProvider, SchemaMapInfo } from '../Signum.Map/Schema/ClientColorProvider';


export default function getDefaultProviders(info: SchemaMapInfo): ClientColorProvider[] {
  return [
    {
      name: "isolation",
      getFill: t => t.extra["isolation"] == undefined ? "var(--bs-body-bg)" :
        t.extra["isolation"] == "Isolated" ? "var(--bs-pink)" :
          t.extra["isolation"] == "Optional" ? "var(--bs-indigo)" :
            t.extra["isolation"] == "None" ? "var(--bs-cyan)" : "var(--bs-body-color)",
      getTooltip: t => t.extra["isolation"] == undefined ? undefined : t.extra["isolation"]
    }
  ];
}


