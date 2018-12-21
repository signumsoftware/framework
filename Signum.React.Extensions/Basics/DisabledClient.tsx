import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { DisableOperation, DisabledMixin } from './Signum.Entities.Basics'
import { getAllTypes } from "@framework/Reflection";

export function start(options: { routes: JSX.Element[] }) {

  Operations.addSettings(new EntityOperationSettings(DisableOperation.Disable, {
    contextual: {
      icon: ["fas", "arrow-down"],
      iconColor: "gray",
    },
    contextualFromMany: {
      icon: ["fas", "arrow-down"],
      iconColor: "gray",
    },
  }));

  Operations.addSettings(new EntityOperationSettings(DisableOperation.Enabled, {
    contextual: {
      icon: ["fas", "arrow-up"],
      iconColor: "black",
    },
    contextualFromMany: {
      icon: ["fas", "arrow-up"],
      iconColor: "black",
    },
  }));

  var typesToOverride = getAllTypes().filter(a => a.queryDefined && a.kind == "Entity" && a.members["[DisabledMixin].IsDisabled"]);

  typesToOverride.forEach(ti => {

    {
      var querySettings = Finder.getSettings(ti.name);

      if (!querySettings) {
        querySettings = { queryName: ti.name };
        Finder.addSettings(querySettings);
      }

      querySettings.hiddenColumns = [
        { token: DisabledMixin.token().entity(e => e.isDisabled) }
      ];

      querySettings.rowAttributes = (row, columns) => {

        var index = columns.indexOf("Entity.IsDisabled");
        return row.columns[index] ? { style: { fontStyle: "italic", color: "gray" } } : undefined;
      };
    }

    {
      var entitySettings = Navigator.getSettings(ti.name);

      if (!entitySettings) {
        entitySettings = new EntitySettings(ti.name);
        Navigator.addSettings(entitySettings);
      }

      if (!entitySettings.findOptions) {
        entitySettings.findOptions = {
          queryName: ti.name,
          filterOptions: [{ token: DisabledMixin.token().entity(e => e.isDisabled), operation: "EqualTo", value: false, frozen: true }]
        };
      }
    }
  });
}
