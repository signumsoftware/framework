declare module "html-to-draftjs" {
  import { ContentState, RawDraftEntity, ContentBlock, RawDraftContentBlock } from "draft-js"
  export default function htmlToDraft(
    htmlContent: string,
    customChunkRenderer?: (nodeName: string, node: HTMLElement) => RawDraftEntity | null | undefined
  ): {
    contentBlocks: Array<ContentBlock>;
    entityMap: { [key: string]: RawDraftEntity };
  };
}
