CREATE OR ALTER PROCEDURE [BP].[jsApproveBP]
    @flowId INT = 1,
    @company INT = 1,
    @userId INT = 69,
    @remarks NVARCHAR(MAX) = 'OK',
    @action NVARCHAR(20) = 'Approve'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Message NVARCHAR(4000);
    DECLARE @templateId INT;
    DECLARE @stageTemplateId INT;
    DECLARE @totalStages INT;
    DECLARE @currentStage INT;
    DECLARE @currentStageId INT;
    DECLARE @previousStage INT;
    DECLARE @previousStageId INT;
    DECLARE @approvalRequired INT;
    DECLARE @approvedCount INT;
    DECLARE @existingStatus NVARCHAR(50) = 'None';
    DECLARE @currentStageActions INT;
    DECLARE @userStage INT;
    DECLARE @userStageId INT;
    DECLARE @nextStageId INT;
    DECLARE @currentBPStatus NVARCHAR(10);
    DECLARE @bpCode INT;
    DECLARE @bpCompany INT;
    DECLARE @apiStatusTag CHAR(1);
    DECLARE @apiMessage NVARCHAR(1000);
    DECLARE @sapCardCode VARCHAR(50);
    DECLARE @sapAttachmentEntry INT;
    DECLARE @sapBlockMessage NVARCHAR(2048);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @templateId = bf.templateId,
            @totalStages = bf.totalStage,
            @currentStage = bf.currentStage,
            @currentStageId = bf.currentStageId,
            @bpCode = bf.bpCode,
            @bpCompany = m.company
        FROM BP.jsFlow AS bf
        INNER JOIN BP.jsMaster AS m ON bf.bpCode = m.code
        WHERE bf.id = @flowId;

        IF @templateId IS NULL
            THROW 50001, 'BP workflow not found with the specified parameters', 1;

        IF @bpCompany <> @company
            THROW 50003, 'Access denied: BP belongs to different company', 1;

        SELECT TOP (1) @stageTemplateId = st.templateId
        FROM dbo.jsStageTemplate AS st
        WHERE st.templateId = @templateId
        ORDER BY st.priority;

        IF @stageTemplateId IS NULL AND @currentStageId IS NOT NULL
        BEGIN
            SELECT TOP (1) @stageTemplateId = st.templateId
            FROM dbo.jsStageTemplate AS st
            INNER JOIN dbo.jsStage AS s ON s.id = st.stageId
            LEFT JOIN dbo.jsTemplateApproval AS ta
                ON ta.templateId = @templateId
               AND ta.approvalId = s.approvalId
            WHERE st.stageId = @currentStageId
              AND (ta.templateId IS NOT NULL OR s.approvalId = @templateId OR @templateId = @currentStageId)
            ORDER BY st.priority;
        END;

        IF @stageTemplateId IS NULL
        BEGIN
            SELECT TOP (1) @stageTemplateId = st.templateId
            FROM dbo.jsStageTemplate AS st
            INNER JOIN dbo.jsStage AS s ON s.id = st.stageId
            INNER JOIN dbo.jsTemplateApproval AS ta ON ta.approvalId = s.approvalId
            WHERE ta.templateId = @templateId
            ORDER BY st.priority;
        END;

        IF @stageTemplateId IS NULL
            THROW 50004, 'Current BP approval stage is not configured for this template.', 1;

        IF @currentStageId IS NULL
           OR NOT EXISTS
           (
               SELECT 1
               FROM dbo.jsStageTemplate AS st
               WHERE st.templateId = @stageTemplateId
                 AND st.stageId = @currentStageId
                 AND st.priority = @currentStage
           )
        BEGIN
            SELECT @currentStageId = st.stageId
            FROM dbo.jsStageTemplate AS st
            WHERE st.templateId = @stageTemplateId
              AND st.priority = @currentStage;
        END;

        IF @currentStageId IS NULL
            THROW 50004, 'Current BP approval stage is not configured for this template.', 1;

        SET @previousStage = @currentStage - 1;

        SELECT @previousStageId = st.stageId
        FROM dbo.jsStageTemplate AS st
        WHERE st.templateId = @stageTemplateId
          AND st.priority = @previousStage;

        SELECT TOP 1
            @userStage = jst.priority,
            @userStageId = jst.stageId
        FROM dbo.jsUserStage AS us
        INNER JOIN dbo.jsStageTemplate AS jst ON us.stageId = jst.stageId
        WHERE us.userId = @userId
          AND jst.templateId = @stageTemplateId
          AND ISNULL(us.status, 1) = 1
        ORDER BY jst.priority;

        IF @userStage IS NULL
            THROW 50002, 'User is not assigned to any active stage in this BP approval workflow', 1;

        SELECT @currentStageActions = COUNT(*)
        FROM BP.jsFlowStatus
        WHERE flowId = @flowId
          AND stageId = @currentStageId;

        IF @action = 'Approve'
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.jsStageTemplate AS st
                WHERE st.templateId = @stageTemplateId
                  AND (st.stageId = @currentStageId OR (@currentStageActions = 0 AND st.stageId = @previousStageId))
                  AND EXISTS
                  (
                      SELECT 1
                      FROM dbo.jsUserStage AS us
                      WHERE us.stageId = st.stageId
                        AND us.userId = @userId
                        AND ISNULL(us.status, 1) = 1
                  )
            )
                THROW 50006, 'User is not authorized to approve this BP.', 1;

            SELECT TOP 1 @existingStatus = status
            FROM BP.jsFlowStatus
            WHERE flowId = @flowId
              AND stageId = @userStageId
              AND userId = @userId
            ORDER BY createdOn DESC;

            IF @existingStatus = 'A' AND @userStage = @currentStage
                THROW 50020, 'User has already approved this BP.', 1;

            IF @existingStatus = 'A' AND @userStage < @currentStage AND @currentStageActions = 0
            BEGIN
                UPDATE BP.jsFlow
                SET currentStage = @userStage,
                    currentStageId = @userStageId
                WHERE id = @flowId;

                SELECT @currentBPStatus = status FROM BP.jsFlow WHERE id = @flowId;

                IF @currentBPStatus = 'R'
                    UPDATE BP.jsFlow SET status = 'P', updatedOn = GETDATE() WHERE id = @flowId;

                SET @Message = 'BP moved back to previous stage for re-approval';
            END;

            IF @existingStatus IS NULL OR @existingStatus = 'None'
            BEGIN
                INSERT INTO BP.jsFlowStatus
                    (flowId, status, stageId, templateId, userId, createdOn, description)
                VALUES
                    (@flowId, 'A', @userStageId, @templateId, @userId, GETDATE(), @remarks);
            END
            ELSE
            BEGIN
                UPDATE BP.jsFlowStatus
                SET status = 'A',
                    description = @remarks,
                    createdOn = GETDATE()
                WHERE flowId = @flowId
                  AND stageId = @userStageId
                  AND userId = @userId;

                SELECT @currentBPStatus = status FROM BP.jsFlow WHERE id = @flowId;

                IF @currentBPStatus = 'R'
                    UPDATE BP.jsFlow SET status = 'P', updatedOn = GETDATE() WHERE id = @flowId;
            END;

            SELECT
                @currentStage = currentStage,
                @currentStageId = currentStageId
            FROM BP.jsFlow
            WHERE id = @flowId;

            IF @currentStageId IS NULL
               OR NOT EXISTS
               (
                   SELECT 1
                   FROM dbo.jsStageTemplate AS st
                   WHERE st.templateId = @stageTemplateId
                     AND st.stageId = @currentStageId
                     AND st.priority = @currentStage
               )
            BEGIN
                SELECT @currentStageId = st.stageId
                FROM dbo.jsStageTemplate AS st
                WHERE st.templateId = @stageTemplateId
                  AND st.priority = @currentStage;
            END;

            SELECT @approvalRequired = ac.approval
            FROM dbo.jsStage AS s
            LEFT JOIN dbo.jsApprovalCount AS ac ON s.approvalId = ac.id
            WHERE s.id = @currentStageId;

            SET @approvalRequired = ISNULL(NULLIF(@approvalRequired, 0), 1);

            SELECT @approvedCount = COUNT(*)
            FROM BP.jsFlowStatus
            WHERE flowId = @flowId
              AND stageId = @currentStageId
              AND status = 'A';

            IF @approvedCount >= @approvalRequired
            BEGIN
                IF @currentStage = @totalStages
                BEGIN
                    SELECT TOP 1
                        @apiStatusTag = apiStatusTag,
                        @apiMessage = apiMessage,
                        @sapCardCode = sapCardCode,
                        @sapAttachmentEntry = sapAttachmentEntry
                    FROM BP.jsSAPData WITH (UPDLOCK, HOLDLOCK)
                    WHERE masterId = @bpCode
                    ORDER BY id DESC;

                    IF ISNULL(@apiStatusTag, '') <> 'Y'
                    BEGIN
                        SET @sapBlockMessage =
                            CASE
                                WHEN @apiStatusTag = 'P' THEN 'Final BP approval blocked: SAP Business Partner posting is still processing.'
                                WHEN @apiStatusTag = 'N' THEN CONCAT('Final BP approval blocked: SAP Business Partner posting failed. Last SAP error: ', ISNULL(@apiMessage, 'No SAP error message stored.'))
                                ELSE 'Final BP approval blocked: SAP Business Partner has not been created successfully.'
                            END;

                        THROW 50110, @sapBlockMessage, 1;
                    END;

                    UPDATE BP.jsFlow SET status = 'A', updatedOn = GETDATE() WHERE id = @flowId;
                    SET @Message = CONCAT('BP approved and activated successfully. SAP CardCode: ', ISNULL(@sapCardCode, ''));
                END
                ELSE
                BEGIN
                    DECLARE @nextStage INT = @currentStage + 1;

                    SELECT @nextStageId = st.stageId
                    FROM dbo.jsStageTemplate AS st
                    WHERE st.templateId = @stageTemplateId
                      AND st.priority = @nextStage;

                    IF @nextStageId IS NULL
                        THROW 50005, 'Next BP approval stage is not configured for this template.', 1;

                    UPDATE BP.jsFlow
                    SET currentStage = @nextStage,
                        currentStageId = @nextStageId,
                        updatedOn = GETDATE()
                    WHERE id = @flowId;

                    SET @Message = 'BP moved to next stage';
                END;
            END
            ELSE
            BEGIN
                SET @Message = CONCAT('Not enough approvals to move to next stage. Current: ', @approvedCount, ', Required: ', @approvalRequired);
            END;
        END
        ELSE IF @action = 'Revoke'
        BEGIN
            IF @userStage < @currentStage
            BEGIN
                IF @currentStageActions = 0
                BEGIN
                    DECLARE @newDescription NVARCHAR(MAX);

                    SELECT @newDescription = ISNULL(description, '')
                    FROM BP.jsFlowStatus
                    WHERE flowId = @flowId
                      AND stageId = @userStageId
                      AND userId = @userId;

                    SET @newDescription = CONCAT(@newDescription, ' | Revoked: ', @remarks);

                    UPDATE BP.jsFlowStatus
                    SET status = 'Revoked', description = @newDescription, createdOn = GETDATE()
                    WHERE flowId = @flowId
                      AND stageId = @userStageId
                      AND userId = @userId
                      AND status = 'A';

                    UPDATE BP.jsFlow
                    SET currentStage = @userStage,
                        currentStageId = @userStageId,
                        status = 'P',
                        updatedOn = GETDATE()
                    WHERE id = @flowId;

                    SET @Message = 'Approval revoked and BP returned to previous stage';
                END
                ELSE
                    THROW 50007, 'Cannot revoke approval because actions have already been taken in the current stage', 1;
            END
            ELSE
                THROW 50008, 'Only users from previous stages can revoke approvals', 1;
        END
        ELSE IF @action = 'Reject'
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.jsStageTemplate AS st
                WHERE st.templateId = @stageTemplateId
                  AND (st.stageId = @currentStageId OR (@currentStageActions = 0 AND st.stageId = @previousStageId))
                  AND EXISTS
                  (
                      SELECT 1
                      FROM dbo.jsUserStage AS us
                      WHERE us.stageId = st.stageId
                        AND us.userId = @userId
                        AND ISNULL(us.status, 1) = 1
                  )
            )
                THROW 50011, 'User is not authorized to reject this BP.', 1;

            INSERT INTO BP.jsFlowStatus
                (flowId, status, stageId, templateId, userId, createdOn, description)
            VALUES
                (@flowId, 'R', @userStageId, @templateId, @userId, GETDATE(), @remarks);

            UPDATE BP.jsFlow SET status = 'R', updatedOn = GETDATE() WHERE id = @flowId;
            SET @Message = 'BP rejected successfully';
        END
        ELSE
            THROW 50009, 'Invalid action specified. Use "Approve", "Reject", or "Revoke"', 1;

        COMMIT TRANSACTION;

        SELECT
            @Message AS ResultMessage,
            @bpCode AS BPCode,
            @bpCompany AS BPCompany,
            CASE
                WHEN @apiStatusTag = 'Y' THEN 'Success'
                WHEN @apiStatusTag = 'P' THEN 'Processing'
                WHEN @apiStatusTag = 'N' THEN 'Failed'
                ELSE 'Not applicable'
            END AS SapStatus,
            @sapCardCode AS SapCardCode,
            @sapAttachmentEntry AS AttachmentEntry;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE(),
                @ErrorSeverity INT = ERROR_SEVERITY(),
                @ErrorState INT = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
