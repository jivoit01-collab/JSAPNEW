# BP SAP CardCode Persistence Audit

Project: `JSAPNEW`

Audit date: 2026-05-23

## Executive Summary

`BP.jsSAPData.sapCardCode` is saved when `apiStatusTag = 'N'` because the BP SAP posting failure path passes the locally generated CardCode into `BP.jsUpdateBpApiStatus`.

The value is not proof that SAP created the Business Partner. It is only the candidate CardCode generated before the SAP `BusinessPartners` POST call.

Current failing example:

```text
apiStatusTag = N
apiMessage = Property 'U_Industry' of 'BusinessPartner' is invalid
sapCardCode = CUSTA001106
```

Expected behavior:

```text
apiStatusTag = N
apiMessage = Property 'U_Industry' of 'BusinessPartner' is invalid
sapCardCode = NULL
```

Root cause:

1. `BPMasterSapService` generates `cardCode` before calling SAP.
2. `BPMasterSapService` returns that generated `cardCode` even when SAP returns a non-success response.
3. `BPmasterService` persists `sapResult.CardCode` with tag `N`.
4. `BP.jsUpdateBpApiStatus` writes `sapCardCode` for any tag, including `N`.

## Files and Objects Audited

| Area | File/Object | Result |
|---|---|---|
| SAP posting orchestration | `Services/Implementation/BPMasterSapService.cs` | Generates CardCode, builds payload, posts to SAP, returns CardCode on success and failure |
| BP approval and retry flow | `Services/Implementation/BPmasterService.cs` | Writes status `P`, `N`, `Y`; passes failed CardCode into SQL |
| API entry points | `Controllers/BPmasterController.cs` | Delegates approve/retry/update calls; does not directly write CardCode |
| SAP posting DTOs | `Models/BPSapModels.cs` | `BpSapPostResult.CardCode` has no distinction between requested and SAP-created CardCode |
| BP API response DTOs | `Models/BPmasterModels.cs` | `ApproveOrRejectBpResponse.SapCardCode` echoes failed candidate CardCode |
| Service interface | `Services/Interfaces/IBPMasterSapService.cs` | Only exposes `PostBusinessPartnerAsync` |
| SAP Service Layer service | `Services/Implementation/SapServiceLayerService.cs` | File does not exist in this project |
| SQL status writer | Live DB object `BP.jsUpdateBpApiStatus` | Persists `sapCardCode` regardless of `@tag` |
| SQL final approval guard | Live DB object `BP.jsApproveBP` | Correctly blocks final approval unless `apiStatusTag = 'Y'` |
| SAP setup update | `BP.jsUpdateSAPData`, `BPmasterService.UpdateSapDataAsync` | Updates setup fields only; does not write `sapCardCode` |

Database-wide SQL module search found only these BP objects referencing `BP.jsSAPData`, `sapCardCode`, or `apiStatusTag`:

- `BP.jsApproveBP`
- `BP.jsGetPendingBP`
- `BP.jsGetSPAData`
- `BP.jsUpdateBpApiStatus`
- `BP.jsUpdateSAPData`

## Exact CardCode Generation Point

CardCode is generated in `BPMasterSapService.PostBusinessPartnerAsync`.

Source reference:

- `Services/Implementation/BPMasterSapService.cs:35` resolves the prefix.
- `Services/Implementation/BPMasterSapService.cs:36` calls `GetNextCardCodeAsync`.
- `Services/Implementation/BPMasterSapService.cs:91` defines `GetNextCardCodeAsync`.
- `Services/Implementation/BPMasterSapService.cs:94-95` queries SAP `BusinessPartners` for the highest existing CardCode by prefix and CardType.
- `Services/Implementation/BPMasterSapService.cs:103-114` increments the numeric suffix locally.

Current code path:

```csharp
var prefix = ResolveCardCodePrefix(request, cardType);
var cardCode = await GetNextCardCodeAsync(prefix, cardType, session, cancellationToken);
```

Important finding:

This CardCode is locally predicted from SAP's latest existing CardCode. It is not returned by SAP as a created Business Partner yet.

There is no separate `GenerateCardCode` method in the solution. There is no public `GetNextCardCode` helper. The only BP CardCode generation logic found is the private `GetNextCardCodeAsync` method in `BPMasterSapService.cs`.

## Exact CardCode Assignment Points

### Payload Assignment Before SAP Call

Source reference:

- `Services/Implementation/BPMasterSapService.cs:39` builds the payload with the generated CardCode.
- `Services/Implementation/BPMasterSapService.cs:116-124` enters `BuildBusinessPartnerPayload`.
- `Services/Implementation/BPMasterSapService.cs:130` assigns `payload["CardCode"] = cardCode`.

Current behavior:

```csharp
var payload = new JObject
{
    ["CardCode"] = cardCode,
    ["CardName"] = master.Name,
    ["CardType"] = cardType,
    ["Currency"] = ...
};
```

This means the SAP request body contains the generated CardCode before SAP validates fields such as `U_Industry`.

### Failure Result Assignment

Source reference:

- `Services/Implementation/BPMasterSapService.cs:42` posts to SAP `BusinessPartners`.
- `Services/Implementation/BPMasterSapService.cs:43-56` handles non-success HTTP responses.
- `Services/Implementation/BPMasterSapService.cs:48` returns `Success = false`.
- `Services/Implementation/BPMasterSapService.cs:50` returns `CardCode = cardCode` even on failure.

Current behavior:

```csharp
if (!response.IsSuccessStatusCode)
{
    return new BpSapPostResult
    {
        Success = false,
        Message = ExtractSapError(errorBody),
        CardCode = cardCode,
        AttachmentEntry = attachmentEntry,
        Payload = payload,
        PayloadHash = ComputeHash(payloadJson),
        CardType = cardType,
        RawResponse = errorBody
    };
}
```

This is the first incorrect behavior.

### Success Result Assignment

Source reference:

- `Services/Implementation/BPMasterSapService.cs:69` returns `Success = true`.
- `Services/Implementation/BPMasterSapService.cs:71` returns `CardCode = cardCode`.

Current behavior:

```csharp
return new BpSapPostResult
{
    Success = true,
    CardCode = cardCode,
    ...
};
```

This is acceptable only if SAP definitely created the BP with that CardCode. Safer behavior is to parse `CardCode` from the SAP success response and persist that value.

## Exact `sapCardCode` Persistence Points

### Failure Branch in C#

Source reference:

- `Services/Implementation/BPmasterService.cs:878` checks `if (!sapResult.Success)`.
- `Services/Implementation/BPmasterService.cs:880` calls `UpdateBpApiStatusAsync` with tag `N` and `sapResult.CardCode`.
- `Services/Implementation/BPmasterService.cs:889` returns `SapCardCode = sapResult.CardCode` to the API caller even though SAP failed.

Current behavior:

```csharp
if (!sapResult.Success)
{
    await UpdateBpApiStatusAsync(
        flow.BpCode,
        sapResult.Message,
        "N",
        sapResult.CardCode,
        sapResult.AttachmentEntry,
        sapResult.PayloadHash,
        request.UserId);

    return new ApproveOrRejectBpResponse
    {
        Success = false,
        SapStatus = "Failed",
        SapCardCode = sapResult.CardCode,
        ...
    };
}
```

This is the second incorrect behavior.

### Success Branch in C#

Source reference:

- `Services/Implementation/BPmasterService.cs:895` calls `UpdateBpApiStatusAsync` with tag `Y` and `sapResult.CardCode`.
- `Services/Implementation/BPmasterService.cs:902` returns the successful `SapCardCode`.

This branch is correct in intent.

### SQL Parameter Binding

Source reference:

- `Services/Implementation/BPmasterService.cs:1103-1110` defines `UpdateBpApiStatusAsync`.
- `Services/Implementation/BPmasterService.cs:1118` binds `@tag`.
- `Services/Implementation/BPmasterService.cs:1119` binds `@sapCardCode`.
- `Services/Implementation/BPmasterService.cs:1125` executes `[BP].[jsUpdateBpApiStatus]`.

Current behavior:

```csharp
parameters.Add("@tag", tag);
parameters.Add("@sapCardCode", sapCardCode);
...
connection.QueryFirstOrDefaultAsync<string>(
    "[BP].[jsUpdateBpApiStatus]",
    parameters,
    commandType: CommandType.StoredProcedure);
```

### SQL Write Point

Live database procedure: `BP.jsUpdateBpApiStatus`

Queried definition line references:

- Line 6 accepts `@sapCardCode`.
- Line 40 sets `apiStatusTag = @tag`.
- Line 42 updates `sapCardCode = COALESCE(NULLIF(@sapCardCode, ''), sapCardCode)`.
- Line 52-57 inserts `sapCardCode` using `NULLIF(@sapCardCode, '')`.

Current update block:

```sql
UPDATE BP.jsSAPData
SET apiStatusTag = @tag,
    apiMessage = LEFT(ISNULL(@apiMessage, ''), 1000),
    sapCardCode = COALESCE(NULLIF(@sapCardCode, ''), sapCardCode),
    sapAttachmentEntry = COALESCE(@attachmentEntry, sapAttachmentEntry),
    payloadHash = COALESCE(NULLIF(@payloadHash, ''), payloadHash),
    lastAttemptOn = SYSUTCDATETIME(),
    lastAttemptBy = @userId,
    retryCount = CASE WHEN @tag = 'P' THEN retryCount + 1 ELSE retryCount END
WHERE masterId = @bpCode;
```

Current insert block:

```sql
INSERT INTO BP.jsSAPData
    (masterId, apiStatusTag, apiMessage, sapCardCode, sapAttachmentEntry,
     payloadHash, lastAttemptOn, lastAttemptBy, retryCount)
VALUES
    (@bpCode, @tag, LEFT(ISNULL(@apiMessage, ''), 1000), NULLIF(@sapCardCode, ''),
     @attachmentEntry, NULLIF(@payloadHash, ''), SYSUTCDATETIME(), @userId,
     CASE WHEN @tag = 'P' THEN 1 ELSE 0 END);
```

This is the final database write that allows `sapCardCode` to be populated while `apiStatusTag = 'N'`.

## Exact `apiStatusTag` Update Points

| Status | Location | Behavior |
|---|---|---|
| `P` | `BPmasterService.cs:842` | Before SAP post, calls `UpdateBpApiStatusAsync(..., "P", null, null, null, userId)` |
| `N` | `BPmasterService.cs:880` | After SAP non-success result, calls `UpdateBpApiStatusAsync(..., "N", sapResult.CardCode, ...)` |
| `Y` | `BPmasterService.cs:895` | After SAP success result, calls `UpdateBpApiStatusAsync(..., "Y", sapResult.CardCode, ...)` |
| Any tag | `BP.jsUpdateBpApiStatus` line 40 | Persists `apiStatusTag = @tag` |

The status update is not inside a C# `catch` block. It is part of the normal non-success SAP response branch.

## Catch and Finally Audit

### `BPMasterSapService`

`PostBusinessPartnerAsync` does not catch SAP post failures that throw exceptions. It only handles HTTP non-success responses returned by `HttpClient`.

Result:

- SAP 400/500 response with body: returns `Success = false` and includes `CardCode`.
- Network exception / thrown exception: bubbles up; no `N` status update occurs in this method.

### `BPmasterService`

`ApproveBPAsync` has `finally` blocks that only release semaphores:

- SAP post lock release
- Approval lock release

No `sapCardCode` is written in a `finally` block.

There is no catch block around `_bpMasterSapService.PostBusinessPartnerAsync` that writes `sapCardCode`. If the SAP call throws, the controller catches the exception and returns HTTP 500, leaving the prior `P` status unless some lower layer updated it.

### `BPmasterController`

Source reference:

- `Controllers/BPmasterController.cs:346-361` approve endpoint.
- `Controllers/BPmasterController.cs:390-405` retry endpoint.
- `Controllers/BPmasterController.cs:763-772` update SAP setup endpoint.

The controller does not write `sapCardCode`. It delegates to `BPmasterService`.

## Full Execution Sequence

### Final Stage Approve Path

1. `POST /api/BPmaster/ApproveBP` enters `BPmasterController.ApproveBP`.
2. Controller calls `BPmasterService.ApproveBPAsync` at `Controllers/BPmasterController.cs:351`.
3. `ApproveBPAsync` loads flow runtime.
4. If not final stage, it calls `BP.jsApproveBP` and does not post SAP.
5. If final stage, it verifies the user is assigned to the current stage.
6. It acquires an in-process SAP post lock for the BP code.
7. It sets SAP status to processing at `BPmasterService.cs:842`:

```text
apiStatusTag = P
sapCardCode = NULL parameter, but existing DB value is preserved by SQL COALESCE
```

8. It loads BP detail via `BP.jsGetSingleBPData`.
9. It loads SAP setup via `BP.jsGetSPAData`.
10. It calls `BPMasterSapService.PostBusinessPartnerAsync` at `BPmasterService.cs:867`.
11. `BPMasterSapService` resolves CardType and prefix.
12. `BPMasterSapService` generates a local candidate CardCode at `BPMasterSapService.cs:36`.
13. Attachments are uploaded to SAP `Attachments2` before BP posting at `BPMasterSapService.cs:38`.
14. SAP payload is built with `CardCode` at `BPMasterSapService.cs:39` and `:130`.
15. SAP `BusinessPartners` POST executes at `BPMasterSapService.cs:42`.
16. If SAP returns failure, `BPMasterSapService` returns `Success = false` with the local candidate CardCode at `BPMasterSapService.cs:48-56`.
17. `BPmasterService` persists tag `N` and that candidate CardCode at `BPmasterService.cs:880`.
18. `BP.jsUpdateBpApiStatus` writes `apiStatusTag = 'N'` and `sapCardCode = @sapCardCode`.
19. The workflow remains pending at final stage.
20. Retry is enabled.

### Successful Final Approval Path

1. Steps 1-15 above run.
2. SAP returns success.
3. `BPMasterSapService` returns `Success = true` and CardCode.
4. `BPmasterService` persists tag `Y` and `sapCardCode` at `BPmasterService.cs:895`.
5. `BPmasterService` calls `BP.jsApproveBP`.
6. `BP.jsApproveBP` checks `BP.jsSAPData.apiStatusTag`.
7. If the latest tag is `Y`, it sets `BP.jsFlow.status = 'A'`.

Live `BP.jsApproveBP` queried definition:

- Lines 176-183 read `apiStatusTag`, `apiMessage`, `sapCardCode`, and `sapAttachmentEntry`.
- Lines 185-195 block final approval unless `apiStatusTag = 'Y'`.
- Lines 197-198 approve the flow and include the SAP CardCode in the message.

This final approval guard is correct. The bug is earlier: failed attempts already put a local candidate into `sapCardCode`.

### Retry Path

1. `POST /api/BPmaster/RetrySapPost` enters `BPmasterController.RetrySapPost`.
2. Controller forces `request.Action = "Approve"` at `Controllers/BPmasterController.cs:395`.
3. Controller calls `BPmasterService.RetrySapPostAsync` at `Controllers/BPmasterController.cs:396`.
4. `RetrySapPostAsync` loads the flow.
5. It requires the flow to be pending and final stage.
6. It reads latest `apiStatusTag` via `GetBpApiStatusTagAsync`.
7. It blocks `P`.
8. It blocks empty status.
9. It allows `N` and `Y`.
10. It calls `ApproveBPAsync` again at `BPmasterService.cs:1016`.

Retry issue:

- If previous failed attempt saved `sapCardCode`, the next `P` update passes `null`, but the SQL procedure preserves the previous value through `COALESCE(NULLIF(@sapCardCode, ''), sapCardCode)`.
- If retry fails again, the new local candidate CardCode is persisted with tag `N`.
- If retry succeeds, the success branch overwrites `sapCardCode` with the latest success value.

## Direct Answers to Audit Questions

| Question | Answer |
|---|---|
| Where is CardCode generated? | `BPMasterSapService.PostBusinessPartnerAsync`, line 36, via private `GetNextCardCodeAsync` at line 91 |
| Where is CardCode assigned to payload? | `BPMasterSapService.BuildBusinessPartnerPayload`, line 130 |
| Where is CardCode assigned to result on failure? | `BPMasterSapService.cs:50` |
| Where is CardCode assigned to result on success? | `BPMasterSapService.cs:71` |
| Where is `sapCardCode` written from C# on failure? | `BPmasterService.cs:880` |
| Where is `sapCardCode` written from C# on success? | `BPmasterService.cs:895` |
| Where is SQL parameter bound? | `BPmasterService.cs:1119` |
| Where does SQL persist it? | `BP.jsUpdateBpApiStatus`, update line 42 and insert line 56 |
| Is it stored before SAP API call? | Not newly stored before the SAP call. The `P` update passes null, but SQL preserves any old value |
| Is it stored after SAP API success? | Yes, correctly at `BPmasterService.cs:895` |
| Is it stored after SAP API failure? | Yes, incorrectly at `BPmasterService.cs:880` |
| Is it stored inside catch? | No |
| Is it stored inside finally? | No |
| Is local generated code treated as SAP-created code? | Yes, on failure |
| Does failed SAP response still update `sapCardCode`? | Yes |
| Does retry reuse stale CardCode? | The payload generation reads SAP again, but DB stale `sapCardCode` is preserved during `P` and can remain visible until overwritten |

## Bug Root Cause

The system uses one property, `BpSapPostResult.CardCode`, for two different meanings:

1. Candidate/requested CardCode generated before SAP posting.
2. Confirmed SAP-created CardCode after successful SAP posting.

Because the failure path also fills `CardCode`, the caller cannot distinguish between a requested code and a created code. The caller then persists it as `BP.jsSAPData.sapCardCode`.

## Current Incorrect Behavior

When SAP rejects the payload, for example because `U_Industry` is invalid:

1. `CUSTA001106` is generated locally.
2. Payload is sent with `CardCode = CUSTA001106`.
3. SAP rejects the payload.
4. `BPMasterSapService` returns `Success = false` and `CardCode = CUSTA001106`.
5. `BPmasterService` updates:

```text
apiStatusTag = N
apiMessage = Property 'U_Industry' of 'BusinessPartner' is invalid
sapCardCode = CUSTA001106
```

This makes a failed SAP post look like it has a real SAP CardCode.

## Recommended Fix

### Minimum Safe Fix

Change the failure path so failed SAP posting never persists a CardCode.

Recommended C# changes:

1. In `BPMasterSapService.PostBusinessPartnerAsync`, when `!response.IsSuccessStatusCode`, return no confirmed CardCode:

```csharp
CardCode = string.Empty
```

2. In `BPmasterService.ApproveBPAsync`, failure branch should call:

```csharp
await UpdateBpApiStatusAsync(
    flow.BpCode,
    sapResult.Message,
    "N",
    null,
    sapResult.AttachmentEntry,
    sapResult.PayloadHash,
    request.UserId);
```

3. Do not set `ApproveOrRejectBpResponse.SapCardCode` on failure.

4. Update `BP.jsUpdateBpApiStatus` so `sapCardCode` is written only when `@tag = 'Y'`.

Recommended SQL behavior:

```sql
sapCardCode =
    CASE
        WHEN @tag = 'Y' THEN COALESCE(NULLIF(@sapCardCode, ''), sapCardCode)
        WHEN @tag IN ('P', 'N') THEN NULL
        ELSE sapCardCode
    END
```

This clears stale failed CardCodes on processing and failure while preserving the guard that prevents downgrading a completed `Y` record.

### Better DTO Fix

Separate requested and confirmed CardCode semantics.

Recommended model shape:

```csharp
public class BpSapPostResult
{
    public bool Success { get; set; }
    public string ConfirmedCardCode { get; set; } = string.Empty;
    public string RequestedCardCode { get; set; } = string.Empty;
}
```

Rules:

- `RequestedCardCode` may be populated before SAP success for diagnostics only.
- `ConfirmedCardCode` must be populated only after SAP returns successful BP creation.
- Only `ConfirmedCardCode` can be persisted to `BP.jsSAPData.sapCardCode`.

### Parse SAP Success Response

On success, parse the SAP response body and persist the CardCode returned by SAP:

```csharp
var json = JObject.Parse(responseBody);
var confirmedCardCode = json["CardCode"]?.ToString() ?? cardCode;
```

Persist `confirmedCardCode`, not the pre-post candidate, wherever possible.

## Retry-Safe Flow Recommendation

Retry should work like this:

1. Failed SAP post sets:

```text
apiStatusTag = N
apiMessage = SAP error message
sapCardCode = NULL
payloadHash = failed payload hash
retryCount = unchanged or attempt-counted consistently
```

2. Retry sets:

```text
apiStatusTag = P
apiMessage = Processing SAP BP creation
sapCardCode = NULL
```

3. Retry success sets:

```text
apiStatusTag = Y
sapCardCode = confirmed SAP CardCode
```

4. Retry failure sets:

```text
apiStatusTag = N
sapCardCode = NULL
```

If a previous attempt actually succeeded in SAP but the final SQL approval failed after status `Y`, the existing `previousTag == 'Y'` branch in `ApproveBPAsync` is useful: it completes SQL workflow without posting to SAP again.

## Concurrency-Safe CardCode Generation Recommendation

Current generation is not fully concurrency-safe.

Current logic:

1. Query latest SAP CardCode by prefix.
2. Increment locally.
3. Send POST with that code.

Risk:

- Two app instances can read the same latest SAP code and generate the same next code.
- One request may succeed and the other may fail with duplicate CardCode.
- A failed request may store a ghost local code if the current bug remains.

Safest options, in order:

1. Prefer SAP Numbering Series allocation. Send `Series` and omit `CardCode` if SAP configuration supports automatic BP numbering. Persist only SAP's returned `CardCode`.
2. If manual CardCode is mandatory, use a database-backed sequence/reservation table per company, BP type, and prefix with a unique constraint and `sp_getapplock` or serializable transaction.
3. Store attempted/reserved codes separately from `sapCardCode`, for example in a posting attempt/audit table. Do not expose them as created SAP CardCodes.
4. Add an idempotency key per BP final approval attempt so the same BP cannot be posted twice across multiple application instances.

## Avoiding Ghost or Unused CardCodes

To avoid ghost CardCodes after SAP failure:

- Never persist candidate CardCode to `BP.jsSAPData.sapCardCode`.
- Persist only CardCode returned by successful SAP `BusinessPartners` creation.
- Keep failed payload hash and SAP error for troubleshooting.
- If diagnostics require seeing the attempted CardCode, store it in an audit-only field/table named clearly, such as `attemptedCardCode`, not `sapCardCode`.
- Clear `sapCardCode` whenever status moves to `P` or `N`, unless the current status is already `Y`.

## Final Recommendation

`sapCardCode` must be treated as a confirmed SAP-created identifier only.

The safest implementation approach is:

1. Change `BPMasterSapService` so failed SAP responses do not return `CardCode` as a confirmed value.
2. Change `BPmasterService` so the `N` branch passes `null` for `sapCardCode`.
3. Change `BP.jsUpdateBpApiStatus` so it only writes `sapCardCode` when `@tag = 'Y'`, and clears it for `P`/`N`.
4. Parse the successful SAP response and persist the returned `CardCode`.
5. Consider SAP Numbering Series or a locked DB sequence for concurrency-safe CardCode allocation.

This will produce the expected behavior:

```text
SAP failed  -> apiStatusTag = N, sapCardCode = NULL
SAP success -> apiStatusTag = Y, sapCardCode = actual SAP CardCode
```
