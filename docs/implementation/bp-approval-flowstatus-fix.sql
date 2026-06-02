USE [jsap_test]
GO

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'BP.jsFlow', N'U') IS NULL
        THROW 51001, 'BP.jsFlow table was not found.', 1;

    IF OBJECT_ID(N'BP.jsFlowStatus', N'U') IS NULL
        THROW 51002, 'BP.jsFlowStatus table was not found.', 1;

    IF OBJECT_ID(N'dbo.jsUserStage', N'U') IS NULL
        THROW 51003, 'dbo.jsUserStage table was not found.', 1;

    ;WITH PendingApprovers AS
    (
        SELECT DISTINCT
            f.id AS flowId,
            f.status,
            f.currentStageId AS stageId,
            f.templateId,
            us.userId
        FROM BP.jsFlow AS f
        INNER JOIN BP.jsMaster AS m
            ON m.code = f.bpCode
        INNER JOIN dbo.jsUserStage AS us
            ON us.stageId = f.currentStageId
           AND ISNULL(us.status, 1) = 1
        WHERE f.status = 'P'
    )
    INSERT INTO BP.jsFlowStatus
    (
        flowId,
        status,
        stageId,
        templateId,
        userId,
        createdOn,
        description
    )
    SELECT
        pa.flowId,
        'P',
        pa.stageId,
        pa.templateId,
        pa.userId,
        GETDATE(),
        'Pending'
    FROM PendingApprovers AS pa
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM BP.jsFlowStatus AS fs
        WHERE fs.flowId = pa.flowId
          AND fs.status = 'P'
          AND fs.stageId = pa.stageId
          AND fs.templateId = pa.templateId
          AND fs.userId = pa.userId
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

SELECT
    fs.flowId,
    fs.status,
    fs.stageId,
    fs.templateId,
    fs.userId,
    fs.createdOn,
    fs.description
FROM BP.jsFlowStatus AS fs
WHERE fs.status = 'P'
ORDER BY fs.createdOn DESC, fs.flowId DESC;
GO
