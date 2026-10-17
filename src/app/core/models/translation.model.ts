export interface Translation {
  id: number;
  code: string;
  name: string;
  language: string;
  copyrightNotice: string;
  // A link the license requires beside the copyright notice, e.g. to the publisher's site.
  attributionLinkText?: string | null;
  attributionLinkUrl?: string | null;
}
