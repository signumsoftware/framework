import { Lite, Entity } from "@framework/Signum.Entities";
import { PanelPartContentProps } from "../Signum.Dashboard/DashboardClient";
import { BigValuePartEntity } from "./Signum.UserQueries";
import { Type } from "@framework/Reflection";

interface CustomMessageContext<T extends Entity> {
  content: BigValuePartEntity;
  entity?: Lite<T>;
}

export namespace BigValueClient {
  export const customMessageNames: Record<string, Record<string, (c: CustomMessageContext<Entity>) => React.ReactNode>> = {};

  export function registerCustomMessage<T extends Entity>(entityType: Type<T> | undefined, messageName: string, renderer: (c: CustomMessageContext<T>) => React.ReactNode): void {
    (customMessageNames[entityType?.typeName ?? "global"] ??= {})[messageName] = renderer as (c: CustomMessageContext<Entity>) => React.ReactNode;
  }

  export function getKeys(entityType: string | undefined): string[] {
    return Object.keys(customMessageNames[entityType?.typeName ?? "global"] ?? {});
  }

  export function renderCustomMessage(messageName: string, ctx: CustomMessageContext<Entity>): React.ReactNode {
    var cm = customMessageNames[ctx.entity?.EntityType ?? "global"][messageName];
    if (cm == null)
      return <span className="text-danger">No CustomMessage {messageName} registered for {ctx.entity?.EntityType ?? "global"}</span>;

    return cm(ctx);
  }
}

