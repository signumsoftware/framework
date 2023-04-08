declare module "draftjs-to-html" {
  import { ContentState, RawDraftEntity, RawDraftContentState } from "draft-js"
  export default function draftToHtml(
    rawContentState: RawDraftContentState,
    hashConfig?: { trigger: "#", separator: " " },
    directional?: boolean,
    customEntityTransform?: (entity: RawDraftEntity, text: string) => string | undefined
  ): string;
}
