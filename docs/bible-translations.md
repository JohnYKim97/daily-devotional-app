# Bible Translations: API and Licensing Guide

Last updated Oct 16, 2026

## Summary

Only the KJV, the NET (non-commercial only) and the open-licensed interlinear data are stored in full in the database; every other copyrighted translation is cached for a short time, capped at roughly 500 verses. These are the publishers' published terms, not legal advice, and items marked "unverified" could not be confirmed from a primary source.

| Translation | 1) Free API? | 2) Caching / storage limit | 3) Rate limits | 4) Host whole text in your DB? |
| --- | --- | --- | --- | --- |
| ESV | Yes, [api.esv.org](https://api.esv.org/), key required, non-commercial only | 500 verses or half a book, whichever is less | 5,000/day, 1,000/hour, 60/minute; 500 verses per query | No |
| NASB | Yes, through the [YouVersion Platform](https://developers.youversion.com/) (NASB 2020 is Bible id 2692), under the Lockman Fast-track license, free and non-commercial | Cached by this app: 500 verses, 14 days | 10,000 requests an hour on a production key, 200 an hour on a free one | No |
| NRSV / NRSVue | No API found | Unverified; no published limits found | None found | No, needs a license from the rights holder |
| NET | Free EPUB download from [bible.org](https://bible.org/downloads) (text and notes); labs.bible.org web service exists but its terms are unpublished | Text quotable in any form, electronic included, non-commercial only; the page says "without the NET Bible notes" and does not address storing a full copy | Not applicable to the EPUB (imported once); labs.bible.org limits are unpublished | Stored in full in this app, non-commercial only; commercial use needs HarperCollins Christian Publishing |
| NIV | Yes, through the [YouVersion Platform](https://developers.youversion.com/) (Bible id 111), under the Biblica Fast-track license, free and non-commercial. Not available for commercial use | Cached by this app: 500 verses, 14 days. A limit applies to how much may be shown at once | 10,000 requests an hour on a production key, 200 an hour on a free one | No |
| NLT | Yes, [api.nlt.to](https://api.nlt.to/), non-commercial | Not stated; the app caches at most 500 verses for 14 days, matching the 500-verse quotation limit | Anonymous: 500 requests/day, 50 verses each. With key: 5,000/day, 500 verses each | No |
| KJV | Yes, several, e.g. Bolls and API.Bible open access | None; public domain in the US | Bolls asks you to use the bulk download, not the per-chapter endpoints | Yes |
| Hebrew interlinear | Data sets, not an API (OSHB, STEPBible) | None; CC BY 4.0, WLC text is public domain | n/a | Yes, with attribution |
| Greek interlinear | Data sets (STEPBible, MorphGNT) | None for STEPBible (CC BY 4.0); MorphGNT is CC BY-SA | n/a | Yes, with attribution and license conditions |

## APIs and data sources this app uses

This is what the code calls or imports today, per translation. The rest of this guide explains the licensing behind each choice.

| Translation | Source | Endpoint or file | How it is kept | Setting |
|---|---|---|---|---|
| ESV | ESV API (Crossway) | `https://api.esv.org/v3/passage/text/` | Cache only: 500 verses, 14 days | `ESV:ApiKey` |
| KJV | bolls.life bulk file | `https://bolls.life/static/translations/KJV.json`, imported once at startup | Stored in full | none |
| NET | NET Bible 2.1 EPUB from bible.org | A file placed by hand in `Data/Imports/` (no API; bible.org blocks automated downloads) | Stored in full, non-commercial only. Translators' notes are on (permission granted) | `Translations:NetEpubPath`, `Translations:NetNotes` |
| NLT | NLT API (Tyndale) | `https://api.nlt.to/api/passages` | Cache only: 500 verses, 14 days | `NLT:ApiKey` (optional; anonymous access works) |
| NIV | YouVersion Platform API | `https://api.youversion.com/v1/bibles/111/passages/{ref}` (called from the backend, one chapter per call) | Cache only: 500 verses, 14 days. Notes and headings are included | `YouVersion:AppKey` |
| NASB (2020 edition) | YouVersion Platform API | `https://api.youversion.com/v1/bibles/2692/passages/{ref}` | Cache only: 500 verses, 14 days. Notes and headings are included | `YouVersion:AppKey` |

Only the 2020 edition of the NASB is offered in the app, shown as "NASB", so the version picker stays short. Checked against the YouVersion Platform with this project's key: the NIV (id 111) and the NASB (ids 100 for 1995 and 2692 for 2020) are available, but the ESV, NLT, NET, NRSV and KJV are not, which is why those keep their own sources. The Biblica and Lockman license agreements are confidential and are kept out of the repository (`docs/Licenses/` is git-ignored); this guide only notes where they apply.

**YouVersion Platform (NIV and NASB).** One generic provider serves both, using the YouVersion Bible id stored with each translation. The NIV and NASB are switched on at startup only when `YouVersion:AppKey` is set; without it they stay off and keep no cached text. Passages are requested one chapter at a time as HTML (`format=html`, with headings and notes). The app limits its own calls (`RateLimiting:YouVersionApi`) to 9,000 an hour, 150 a minute and 200,000 a day, below the 10,000 an hour a production app key allows. A free key allows only 200 an hour, so lower these limits while still on one. Both versions are shown under the publisher's own license agreement, with the copyright notice and the publisher link those agreements ask for (the NIV's notice is refreshed from the API at most once a day). The NIV also has a limit on how much may be shown at once, so a reading longer than that shows a message asking the reader to pick another version. The verse counts for both were read from YouVersion's own index for each Bible and equal the ESV's. The license agreements themselves are confidential and are not kept in this repository.

Not implemented, with no source chosen yet: the NRSV (no API found), and the Hebrew and Greek interlinears (data sets, to import later).

## ESV

The ESV API is the clearest of the copyrighted options, and it forbids exactly what you were considering: keeping a full copy.

- **API:** free, key required, for non-commercial sites and apps only. Per its [documentation](https://api.esv.org/), a site that charges for access, shows ads or takes sponsorships counts as commercial.
- **Caching:** you may not store locally more than 500 verses or one-half of any book, whichever is less. You may not display more than that on any one page.
- **Rate limits:** 5,000 queries per day, 1,000 per hour and 60 per minute. A single query can request up to 500 verses or half a book.
- **Host the whole text:** no. Crossway's [permissions guide](https://www.crossway.org/permissions/) allows quoting up to 500 verses, but not more than half of any book or 25% of your work. Anything beyond that needs a written license from Crossway.

## NASB, NRSV, NET, NIV and NLT

**NASB.** The Lockman Foundation's [permission page](https://lockman.org/?p=3) allows quoting up to 1,000 verses, no complete book, and no more than 50% of your work, and caps storage at 1,000 verses in an electronic retrieval system. There is no publisher API. This app uses the 2020 edition through the YouVersion Platform under Lockman's Fast-track license agreement (see "How this app uses the NIV and NASB" below). The 1995 edition is also available there but is not offered, so the version picker stays short.

**NRSV / NRSVue.** The National Council of Churches owns the rights, and licensing runs through Riggins Rights Management (NRSVcopyright@rigginsrights.com). I found no published quotation limits and no API. API.Bible says it does not provide the NRSV-CE. Treat any NRSV use as needing a written license.

**NET.** The [NET copyright page](https://netbible.com/copyright/) and the publisher's [notice page](https://thebible.org/gt/notices/net.html) say:

- **Quoting:** the Scripture text (without the NET Bible notes) may be quoted in any form (written, visual, electronic, projection or audio) for non-commercial use, without written permission.
- **Notices:** each quotation must end with "(NET)", and the acknowledgment is "Quotations designated (NET) are from the NET Bible® copyright ©1996, 2019 by Biblical Studies Press, L.L.C. All rights reserved." Software apps with internet access should hyperlink "NET" to netbible.org.
- **Distribution:** copies may be given away free, but not bundled with anything sold or used to solicit donations. The free download is at [bible.org/downloads](https://bible.org/downloads), where the EPUB with the notes is offered.
- **Commercial use:** contact HarperCollins Christian Publishing. Anything not expressly granted needs written permission from Biblical Studies Press.
- **Not addressed:** storing a full copy in an app database, and the terms and rate limits of the labs.bible.org web service (its page returned 403 to me).

**How this app uses the NET.**

- **Source:** the free NET Bible 2.1 EPUB from bible.org (copyright 1996-2019), imported once by hand (see LOCAL_SETUP.md). The EPUB is git-ignored, not committed.
- **Stored:** the whole text in the database, like the KJV, flagged non-commercial only. It is not fetched from labs.bible.org, so there is no rate limit to manage.
- **Imported content:** verse text, section headings and Psalm titles, plus every translators' note (tn, sn and tc, about 58,000) when `Translations:NetNotes` is `true`, which it is in `appsettings.json` now that permission is granted; see "Permission status" below. The divine names appear as in the EPUB ("Lord", "God"; the EPUB prints them in small capitals, which plain text cannot).
- **Shown:** the notice above under each NET passage, with "NET" linking to netbible.org.
- **Reasoning:** storing relies on the "any form, electronic included" grant and on the free download being offered for exactly this kind of non-commercial use. The notes are the weakest part: the page's grant is for the text "without the NET Bible notes", and the notes are included because the free download ships them. Biblical Studies Press has since given permission for the notes (see "Permission status").
- **Permission status:** Biblical Studies Press has granted permission to use the translators' notes, so `Translations:NetNotes` is `true`. Before the grant, no public page covered the notes: the copyright page and FAQ cover the text "without the NET Bible notes". Keep the written permission on file. It is not known whether it also covers commercial use.
- **Before charging or showing ads:** get written permission from Biblical Studies Press (and HarperCollins Christian Publishing for commercial use), or turn the NET off.

**NIV.** [Biblica](https://www.biblica.com/permissions/) allows quoting up to 500 verses, no complete book, and under 25% of your work, and offers a royalty-free Express License for free, non-commercial apps through its partners ([API.Bible](https://docs.api.bible/your-account/plans-pricing) and the YouVersion Platform). That license excludes sales, subscriptions, advertising and any other monetization, and the NIV is not available for commercial use through API.Bible or YouVersion. For a commercial app, contact Biblica or the publisher (HarperCollins Christian Publishing for the US and Canada). This app uses the YouVersion Platform route.

**NLT.** The [NLT API](https://api.nlt.to/) is free for non-commercial use, and you must affirm that your use fits Tyndale's purpose. Anonymous callers get 50 verses per request and 500 requests per day. With a key (free sign-up at api.nlt.to/Account/Register) you get 500 verses per request and 5,000 requests per day. The API pages say nothing about caching or required attribution. Tyndale's [permission policy](https://www.tyndale.com/permissions) allows quoting up to 500 verses, not more than 25% of a work and not a complete book, and it does not address apps, software or electronic storage. Its credit line is "Scripture quotations are taken from the Holy Bible, New Living Translation, copyright ©1996, 2004, 2015 by Tyndale House Foundation. Used by permission of Tyndale House Publishers, Carol Stream, Illinois 60188. All rights reserved."

**How this app uses the NLT.**

- **Provider:** fetched live from `https://api.nlt.to/api/passages`, like the ESV, never stored in full.
- **Cache:** at most 500 verses (and half of any book), kept 14 days. This is an assumption copied from the ESV and the 500-verse quotation limit, because the API does not state caching rules.
- **Limits:** requests are split to 50 verses each without a key and 500 with one (`NLT:ApiKey`). The app also stops itself at 30 calls a minute, 250 an hour and 450 a day (`RateLimiting:NltApi`), under the anonymous allowance of 500 a day. All visitors share that allowance, because it is counted per server IP address. Raise the limits in configuration when using a key.
- **Content:** verse text, section headings, Psalm titles and the NLT's footnotes, shown in the existing footnote UI. The divine name stays "Lord", as in the NLT's plain text.
- **Shown:** the credit line above under each NLT passage. The API's `(NLT)` abbreviation appears in the version picker.
- **Non-commercial only:** like the ESV, this stops being allowed if the app charges or shows ads. Commercial use needs a commercial license from Tyndale.

**How this app uses the NIV and NASB.** The agreements themselves are confidential and are not kept in the repository (`docs/Licenses/` is git-ignored); what follows is how the code meets them.

- **Source:** the YouVersion Platform API, called from the backend. Each translation row stores its YouVersion Bible id (NIV 111, NASB 2692).
- **Accounts:** both publishers' license agreements must be accepted on the YouVersion account that owns the app key, and the key must be set (`YouVersion:AppKey`). Without the key both versions are switched off at startup, hidden from the picker, and any cached text for them is deleted. The switch follows the key on every start, so to turn them off remove the key (they cannot be left off in the database while the key is set).
- **Text and notes:** the words and every footnote are kept as supplied. Section headings and Psalm titles are shown. Italics, red letters and small capitals are not shown, as with every version in the app.
- **Cache:** 500 verses for 14 days, stored in the database like the ESV. Neither license states a figure, so this is a conservative choice. Switching a version off deletes its cache.
- **Notices:** each version shows its copyright notice with the publisher link its license asks for (Biblica for the NIV, Lockman.org for the NASB). The NIV notice is refreshed from the API at most once a day.
- **NIV display limit:** the NIV license limits how much may be shown at once (set on the NIV translation row as `MaxDisplayChapters` and `MaxDisplayVerses`). A passage over the limit is not shown: the reading says so and asks the reader to choose another version, and a full-chapter request shows the same limit as an error message.
- **Request volume:** about one call per chapter per version per day, because passages are cached. The app limits its own calls to 9,000 an hour (`RateLimiting:YouVersionApi`), under the 10,000 an hour of a production key.
- **AI commentary:** the commentary always uses the default version (the ESV), never the version the reader has selected, because the NIV license does not allow its text to be used for AI features and YouVersion forbids training on its content. The same question is open for the ESV (see the open questions below).
- **Duties outside the code:** the agreements carry reporting, notification and approval duties. They are the account owner's to keep, and they are listed in the agreements (kept outside the repository). Re-read them before adding features or changing how the app is distributed.

## KJV and the interlinears

These three are the ones you can legally store in full, each with attribution conditions.

- **KJV:** public domain in the US, so you can host it whole. Bolls offers it, but [its API docs](https://bolls.life/api/) you to use the bulk JSON download rather than the per-chapter endpoints, which risk overloading its single server. In the UK the KJV is under a Crown letters patent, so check that if you distribute there.
- **Hebrew interlinear:** the [Open Scriptures Hebrew Bible](https://github.com/openscriptures/morphhb) lemma and morphology data is CC BY 4.0, and the underlying Westminster Leningrad Codex text is public domain. You can host all of it, crediting the Open Scriptures Hebrew Bible Project.
- **Greek interlinear:** [STEPBible-Data](https://github.com/STEPBible/STEPBible-Data) (tagged Hebrew OT, tagged Greek NT, lexicons) is CC BY 4.0. Credit "STEP Bible" with a link to www.STEPBible.org. [MorphGNT](https://github.com/morphgnt/sblgnt) is an alternative, but its parsing is CC BY-SA (share-alike) and the SBLGNT text itself has a separate EULA.
- **Berean Interlinear Bible:** its [downloads page](https://berean.bible/downloads.htm) lists only the New Testament and states no license, so I could not confirm its terms.
- **API:** none of these needs one. They are downloadable data sets, so you import them once, which also avoids rate limits.

## Recommendations and open questions

The app now serves the ESV, KJV, NET, NLT, NIV and NASB. Treat the ESV, NLT, NIV and NASB as live API lookups with a short cache, and the KJV, NET and interlinears as stored data.

1. **Store in your DB:** KJV, OSHB (Hebrew), STEPBible (Hebrew and Greek tagged text), and the NET (non-commercial only, from the bible.org EPUB). The NET translators' notes are on, with permission from Biblical Studies Press.
2. **Fetch live, cache at most 500 verses:** ESV, NLT, and the NIV and NASB through the YouVersion Platform. All are non-commercial only, so no ads, subscriptions or paid tiers.
3. **Needs a license or a decision:** the NRSV has no API I could verify. Contact Riggins Rights Management, or check [API.Bible](https://api.bible/bibles) and the YouVersion Platform catalog for current availability (neither offers it to this project's key today).
4. **Confirm for NET:** the NET is already stored in full. Ask Biblical Studies Press in writing to confirm that storing the text and notes in an app is fine, and for terms if the app ever becomes commercial.

Open questions:

- Will the app ever charge or show ads? That decides whether the free ESV and NLT APIs and the free NET text remain allowed.
- Is NRSV or NRSVue the one you want? It changes who to contact.
- The AI commentary sends the day's ESV passage text to Claude. Check Crossway's terms on that, as the NIV and NASB licenses and YouVersion have rules about AI use of their content.
- Going live on the YouVersion Platform: the production key allows 10,000 requests an hour, and the publisher agreements may depend on YouVersion's review of the app, so check the agreements before going live.
