/*
    BP SAP CardCode persistence fix
    Generated: 2026-05-23

    Purpose:
    - Prevent locally generated candidate CardCodes from being persisted when SAP BP posting fails.
    - Treat BP.jsSAPData.sapCardCode as a confirmed SAP-created CardCode only.
    - Preserve existing workflow, retry, payloadHash, retryCount, and attachment status behavior.

    Expected states after this procedure is deployed:
    - SAP failed:     apiStatusTag = 'N', sapCardCode = NULL
    - Retry running:  apiStatusTag = 'P', sapCardCode = NULL
    - SAP succeeded:  apiStatusTag = 'Y', sapCardCode = SAP-confirmed CardCode

    Notes:
    - This script only changes BP.jsUpdateBpApiStatus.
    - It does not delete workflow, approval, attachment, audit, or SAP history rows.
    - It also clears already-stale sapCardCode values on failed/processing rows.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    EXEC(N'
CREATE OR ALTER PROCEDURE [BP].[jsUpdateBpApiStatus]
    @bpCode INT,
    @apiMessage NVARCHAR(1000),
    @tag CHAR(1),
    @sapCardCode VARCHAR(50) = NULL,
    @attachmentEntry INT = NULL,
    @payloadHash VARCHAR(128) = NULL,
    @userId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @tag NOT IN (''P'', ''Y'', ''N'')
        THROW 50101, ''Invalid BP SAP API status tag. Use P, Y, or N.'', 1;

    IF NOT EXISTS (SELECT 1 FROM BP.jsMaster WHERE code = @bpCode)
        THROW 50102, ''BP master record not found for API status update.'', 1;

    DECLARE @previousTag CHAR(1);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT TOP 1 @previousTag = apiStatusTag
        FROM BP.jsSAPData WITH (UPDLOCK, HOLDLOCK)
        WHERE masterId = @bpCode
        ORDER BY id DESC;

        IF @previousTag = ''Y'' AND @tag <> ''Y''
        BEGIN
            COMMIT TRANSACTION;
            SELECT @previousTag AS PreviousTag;
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM BP.jsSAPData WHERE masterId = @bpCode)
        BEGIN
            UPDATE BP.jsSAPData
            SET apiStatusTag = @tag,
                apiMessage = LEFT(ISNULL(@apiMessage, ''''), 1000),
                sapCardCode =
                    CASE
                        WHEN @tag = ''Y''
                            THEN COALESCE(NULLIF(@sapCardCode, ''''), sapCardCode)
                        WHEN @tag IN (''P'', ''N'')
                            THEN NULL
                        ELSE sapCardCode
                    END,
                sapAttachmentEntry = COALESCE(@attachmentEntry, sapAttachmentEntry),
                payloadHash = COALESCE(NULLIF(@payloadHash, ''''), payloadHash),
                lastAttemptOn = SYSUTCDATETIME(),
                lastAttemptBy = @userId,
                retryCount = CASE WHEN @tag = ''P'' THEN retryCount + 1 ELSE retryCount END
            WHERE masterId = @bpCode;
        END
        ELSE
        BEGIN
            INSERT INTO BP.jsSAPData
                (masterId, apiStatusTag, apiMessage, sapCardCode, sapAttachmentEntry,
                 payloadHash, lastAttemptOn, lastAttemptBy, retryCount)
            VALUES
                (@bpCode,
                 @tag,
                 LEFT(ISNULL(@apiMessage, ''''), 1000),
                 CASE WHEN @tag = ''Y'' THEN NULLIF(@sapCardCode, '''') ELSE NULL END,
                 @attachmentEntry,
                 NULLIF(@payloadHash, ''''),
                 SYSUTCDATETIME(),
                 @userId,
                 CASE WHEN @tag = ''P'' THEN 1 ELSE 0 END);
        END;

        COMMIT TRANSACTION;
        SELECT @previousTag AS PreviousTag;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE(),
                @ErrSev INT = ERROR_SEVERITY(),
                @ErrState INT = ERROR_STATE();
        RAISERROR (@ErrMsg, @ErrSev, @ErrState);
    END CATCH;
END
');

    UPDATE BP.jsSAPData
    SET sapCardCode = NULL
    WHERE apiStatusTag IN ('P', 'N')
      AND sapCardCode IS NOT NULL;

    COMMIT TRANSACTION;

    PRINT 'BP.jsUpdateBpApiStatus updated. sapCardCode now persists only for apiStatusTag = Y.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE(),
            @ErrorSeverity INT = ERROR_SEVERITY(),
            @ErrorState INT = ERROR_STATE();

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

/*
Post-deployment verification:

SELECT masterId, apiStatusTag, sapCardCode, apiMessage, retryCount, lastAttemptOn
FROM BP.jsSAPData
WHERE apiStatusTag IN ('P', 'N')
  AND sapCardCode IS NOT NULL;

Expected result: zero rows.
*/
