import { MList } from '@framework/Signum.Entities'
import { IFile } from '../../Signum.Files/Signum.Files'
import { FilesClient } from '../../Signum.Files/FilesClient'
import { IAttachmentGeneratorEntity, ImageAttachmentEntity } from '../Signum.Mailing.Templates'

//The cid: protocol is only understood by mail clients, so inline images have to be re-pointed to preview them in the browser.
export function forEachCidImage(doc: Document, action: (img: HTMLImageElement, contentId: string) => void): void {
  doc.body.querySelectorAll("img").forEach(img => {
    const src = img.getAttribute("src");
    if (src != null && src.startsWith("cid:"))
      action(img, src.after("cid:"));
  });
}

export function dataUrl(file: IFile): string {
  const mimeType = FilesClient.extensionInfo[file.fileName?.tryAfterLast(".")?.toLowerCase()!]?.mimeType ?? "application/octet-stream";

  return `data:${mimeType};base64,${file.binaryFile}`;
}

//For EmailTemplate / EmailMasterTemplate previews, where the bytes of the ImageAttachments already travel with the entity.
export function replaceCidImages(doc: Document, attachments: MList<IAttachmentGeneratorEntity>): void {
  const images = attachments.map(a => a.element).filter(a => ImageAttachmentEntity.isInstance(a)) as ImageAttachmentEntity[];

  forEachCidImage(doc, (img, contentId) => {
    const image = images.firstOrNull(a => a.contentId == contentId);
    if (image?.file?.binaryFile != null)
      img.src = dataUrl(image.file);
  });
}
