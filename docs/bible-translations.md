# Bible Translations: API and Licensing Guide

Oct 7, 2026 · @Tom

## Summary

Only the KJV and the open-licensed interlinear data can be stored in full in your database; every copyrighted translation caps local storage at roughly 500 to 1,000 verses. These are the publishers' published terms, not legal advice, and items marked "unverified" could not be confirmed from a primary source.

| Translation | 1) Free API? | 2) Caching / storage limit | 3) Rate limits | 4) Host whole text in your DB? |
| --- | --- | --- | --- | --- |
| ESV | Yes, [api.esv.org](https://api.esv.org/), key required, non-commercial only | 500 verses or half a book, whichever is less | 5,000/day, 1,000/hour, 60/minute; 500 verses per query | No |
| NASB | No publisher API found. Possible via [API.Bible](https://api.bible/) if the NASB is on your plan (unverified) | Max 1,000 verses stored in an electronic retrieval system | API.Bible Starter: 5,000 calls/month (sources also say 5,000/day) | No |
| NRSV / NRSVue | No API found | Unverified; no published limits found | None found | No, needs a license from the rights holder |
| NET | Yes, [labs.bible.org](https://labs.bible.org/api_web_service) web service, free non-commercial | Unverified; storing the full text is not addressed | Unverified; page was not readable | Unverified; ask Biblical Studies Press |
| NIV | No public API. Not available for commercial use on API.Bible | 500 verses quotation allowance; no app or storage terms published | None | No |
| NLT | Yes, [api.nlt.to](https://api.nlt.to/), non-commercial | Not stated; the 500-verse cap matches the NLT copyright statement | Anonymous: 500 requests/day, 50 verses each. With key: 5,000/day, 500 verses each | No |
| KJV | Yes, several, e.g. Bolls and API.Bible open access | None; public domain in the US | Bolls asks you to use the bulk download, not the per-chapter endpoints | Yes |
| Hebrew interlinear | Data sets, not an API (OSHB, STEPBible) | None; CC BY 4.0, WLC text is public domain | n/a | Yes, with attribution |
| Greek interlinear | Data sets (STEPBible, MorphGNT) | None for STEPBible (CC BY 4.0); MorphGNT is CC BY-SA | n/a | Yes, with attribution and license conditions |

## ESV

The ESV API is the clearest of the copyrighted options, and it forbids exactly what you were considering: keeping a full copy.

- **API:** free, key required, for non-commercial sites and apps only. Per its [documentation](https://api.esv.org/), a site that charges for access, shows ads or takes sponsorships counts as commercial.
- **Caching:** you may not store locally more than 500 verses or one-half of any book, whichever is less. You may not display more than that on any one page.
- **Rate limits:** 5,000 queries per day, 1,000 per hour and 60 per minute. A single query can request up to 500 verses or half a book.
- **Host the whole text:** no. Crossway's [permissions guide](https://www.crossway.org/permissions/) allows quoting up to 500 verses, but not more than half of any book or 25% of your work. Anything beyond that needs a written license from Crossway.

## NASB, NRSV, NET, NIV and NLT

**NASB.** The Lockman Foundation's [permission page](https://lockman.org/?p=3) allows quoting up to 1,000 verses, no complete book, and no more than 50% of your work. It also caps storage at 1,000 verses in an electronic retrieval system. Bulk downloading and standalone datasets are excluded. I found no publisher API, and no app terms are published, so you would need to ask Lockman, (714) 879-3055.

**NRSV / NRSVue.** The National Council of Churches owns the rights, and licensing runs through Riggins Rights Management (NRSVcopyright@rigginsrights.com). I found no published quotation limits and no API. API.Bible says it does not provide the NRSV-CE. Treat any NRSV use as needing a written license.

**NET.** The [NET copyright page](https://netbible.com/copyright/) allows free quoting in non-commercial use with "(NET)" after each quotation and a link to netbible.org in apps. It does not address storing the full text. A free non-commercial web service exists at labs.bible.org, but I could not read its page, so its rate limits and key rules are unverified. Commercial use goes through HarperCollins Christian Publishing.

**NIV.** [Biblica](https://www.biblica.com/permissions/) allows quoting up to 500 verses, no complete book, and under 25% of your work. Commentaries and reference works need written permission. I found no public NIV API, and [API.Bible](https://docs.api.bible/your-account/plans-pricing) states the NIV is not currently available for commercial use. For an app, contact Biblica or Zondervan.

**NLT.** The [NLT API](https://api.nlt.to/) is free for non-commercial use. Anonymous callers get 50 verses per request and 500 requests per day. With a key you get 500 verses per request and 5,000 requests per day. Its page does not mention caching. Tyndale's [permission policy](https://www.tyndale.com/permissions) allows 500 verses and under 25% of your work, so I would assume the same 500-verse storage cap.

## KJV and the interlinears

These three are the ones you can legally store in full, each with attribution conditions.

- **KJV:** public domain in the US, so you can host it whole. Bolls offers it, but [its API docs](https://bolls.life/api/) you to use the bulk JSON download rather than the per-chapter endpoints, which risk overloading its single server. In the UK the KJV is under a Crown letters patent, so check that if you distribute there.
- **Hebrew interlinear:** the [Open Scriptures Hebrew Bible](https://github.com/openscriptures/morphhb) lemma and morphology data is CC BY 4.0, and the underlying Westminster Leningrad Codex text is public domain. You can host all of it, crediting the Open Scriptures Hebrew Bible Project.
- **Greek interlinear:** [STEPBible-Data](https://github.com/STEPBible/STEPBible-Data) (tagged Hebrew OT, tagged Greek NT, lexicons) is CC BY 4.0. Credit "STEP Bible" with a link to www.STEPBible.org. [MorphGNT](https://github.com/morphgnt/sblgnt) is an alternative, but its parsing is CC BY-SA (share-alike) and the SBLGNT text itself has a separate EULA.
- **Berean Interlinear Bible:** its [downloads page](https://berean.bible/downloads.htm) lists only the New Testament and states no license, so I could not confirm its terms.
- **API:** none of these needs one. They are downloadable data sets, so you import them once, which also avoids rate limits.

## Recommendations and open questions

To get all eight in your app, treat the ESV, NLT and NET as live API lookups and the KJV and interlinears as stored data.

1. **Store in your DB:** KJV, OSHB (Hebrew), STEPBible (Hebrew and Greek tagged text).
2. **Fetch live, cache at most 500 verses:** ESV and NLT. Both are non-commercial only, so no ads, subscriptions or paid tiers.
3. **Needs a license or a decision:** NIV, NASB and NRSV have no free API I could verify. Contact Biblica/Zondervan, the Lockman Foundation and Riggins respectively, or check [API.Bible](https://api.bible/bibles) for current availability.
4. **Confirm for NET:** ask Biblical Studies Press about rate limits and caching.

Open questions:

- Will the app ever charge or show ads? That decides whether the free ESV, NLT and NET APIs remain allowed.
- Is NRSV or NRSVue the one you want? It changes who to contact.

Your list ended at an empty item 5), so I did not cover it. Tell me what it was and I will add it.
