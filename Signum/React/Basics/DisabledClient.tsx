import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '../Navigator'
import * as Finder from '../Finder'
import { EntityOperationSettings } from '../Operations'
import * as Operations from '../Operations'
import { getAllTypes } from "../Reflection";
import { DisableOperation, DisabledMixin } from "../Signum.Basics";

export function start(options: { routes: RouteObject[] }) {

  Operations.addSettings(new EntityOperationSettings(DisableOperation.Disable, {
    contextual: {
      icon: "arrow-down",
      iconColor: "gray",
    },
    contextualFromMany: {
      icon: "arrow-down",
      iconColor: "gray",
    },
  }));

  Operations.addSettings(new EntityOperationSettings(DisableOperation.Enabled, {
    contextual: {
      icon: "arrow-up",
      iconColor: "black",
    },
    contextualFromMany: {
      icon: "arrow-up",
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
        ...(querySettings.hiddenColumns ?? []),
        { token: DisabledMixin.token(e => e.entity.isDisabled) }
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

      if (!entitySettings.defaultFindOptions) {
        entitySettings.defaultFindOptions = {
          queryName: ti.name,
          filterOptions: [{ token: DisabledMixin.token(e => e.entity.isDisabled), operation: "EqualTo", value: false, frozen: true }]
        };
      }
    }
  });
}
