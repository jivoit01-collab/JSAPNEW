/* =====================================================================
   BUDGET WORKFLOW - ROLLBACK
   Target DB : jsaplive3
   Partner file : BUDGET_Workflow_Cutover.sql  (run that one first)

   WHEN TO RUN THIS
   ----------------
   If the new budget workflow is not behaving, run this file. It puts
   everything back the way it was before the cutover.

   WHAT IT DOES
   ------------
   1. Switches the ORIGINAL budget templates back ON, restoring exactly
      the isActive value each one had at backup time.
   2. Switches the NEW templates (created by the cutover) OFF.

   WHAT IT DOES NOT TOUCH
   ----------------------
   No flow is moved or deleted. bud.jsDocEntry and bud.jsDocEnrtyDetail
   are never written to.

   So after running this:
     - anything already raised on a NEW template stays where it is and
       can still be approved normally - it is not orphaned;
     - anything raised on an OLD template is unaffected;
     - every NEW document from this moment routes through the OLD
       templates again, exactly as before the cutover.

   It finds the most recent backup table by itself - you do not have to
   edit any name or date. Safe to run more than once.
   ===================================================================== */

USE jsaplive3;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @bak SYSNAME, @sql NVARCHAR(MAX);
DECLARE @oldRestored INT = 0, @newDisabled INT = 0;

/* newest backup table left by the cutover script */
SELECT TOP 1 @bak = t.name
FROM sys.tables t
WHERE SCHEMA_NAME(t.schema_id) = 'bud'
  AND t.name LIKE 'bak[_]BudgetCutover[_]%'
ORDER BY t.create_date DESC;

IF @bak IS NULL
BEGIN
    SELECT 'NO BACKUP TABLE FOUND - nothing to roll back to.' AS outcome,
           'Expected bud.bak_BudgetCutover_<date>, created by BUDGET_Workflow_Cutover.sql' AS detail;
    RETURN;
END

SELECT @bak AS usingBackupTable;

/* ---------------------------------------------------------------------
   PREVIEW - what is about to change. Read this before you commit.
   --------------------------------------------------------------------- */
SET @sql = N'
SELECT ''will be switched ON (original)'' AS action, COUNT(*) AS templates
  FROM jsTemplate t JOIN bud.' + QUOTENAME(@bak) + N' b ON b.templateId = t.id
 WHERE t.isActive <> b.isActive
UNION ALL
SELECT ''will be switched OFF (new)'', COUNT(*)
  FROM jsTemplate t
  JOIN jsTemplateApproval ta ON ta.templateId = t.id AND ta.approvalId = 5
 WHERE t.isActive = 1
   AND t.id NOT IN (SELECT templateId FROM bud.' + QUOTENAME(@bak) + N');';
EXEC sp_executesql @sql;

BEGIN TRY
    BEGIN TRAN;

    /* 1. turn OFF the templates the cutover created */
    SET @sql = N'
    UPDATE t SET t.isActive = 0
      FROM jsTemplate t
      JOIN jsTemplateApproval ta ON ta.templateId = t.id AND ta.approvalId = 5
     WHERE t.isActive = 1
       AND t.id NOT IN (SELECT templateId FROM bud.' + QUOTENAME(@bak) + N');
    SELECT @n = @@ROWCOUNT;';
    EXEC sp_executesql @sql, N'@n INT OUTPUT', @n = @newDisabled OUTPUT;

    /* 2. restore the original templates to the state they were backed up in */
    SET @sql = N'
    UPDATE t SET t.isActive = b.isActive
      FROM jsTemplate t
      JOIN bud.' + QUOTENAME(@bak) + N' b ON b.templateId = t.id
     WHERE t.isActive <> b.isActive;
    SELECT @n = @@ROWCOUNT;';
    EXEC sp_executesql @sql, N'@n INT OUTPUT', @n = @oldRestored OUTPUT;

    /* 3. refuse to leave the system with no active budget template */
    DECLARE @active INT;
    SELECT @active = COUNT(*)
      FROM jsTemplate t
      JOIN jsTemplateApproval ta ON ta.templateId = t.id AND ta.approvalId = 5
     WHERE t.isActive = 1;

    IF @active = 0
        THROW 71001, 'Rollback would leave zero active budget templates - aborting.', 1;

    COMMIT TRAN;

    SELECT @oldRestored AS originalTemplatesRestored,
           @newDisabled AS newTemplatesDisabled,
           @active      AS activeBudgetTemplatesNow,
           'ROLLED BACK OK' AS outcome;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SELECT ERROR_NUMBER() AS err, ERROR_LINE() AS line_, ERROR_MESSAGE() AS msg,
           'FAILED - nothing changed' AS outcome;
END CATCH
GO

/* =====================================================================
   VERIFY - expect the original counts: 52 for OIL, 44 for BEVERAGE,
   and the pending flow count unchanged.
   ===================================================================== */
SELECT t.company, COUNT(*) AS activeBudgetTemplates
  FROM jsTemplate t
  JOIN jsTemplateApproval ta ON ta.templateId = t.id AND ta.approvalId = 5
 WHERE t.isActive = 1
 GROUP BY t.company;

SELECT COUNT(*) AS pendingFlowsUntouched FROM bud.jsDocEntry WHERE status = 'P';
GO

/* =====================================================================
   TO GO FORWARD AGAIN after a rollback
   -------------------------------------
   Do NOT re-run BUDGET_Workflow_Cutover.sql - it refuses to run twice,
   and it would create a second copy of every template. Instead switch
   the existing new templates back on and the old ones off:

   UPDATE t SET t.isActive = 1
     FROM jsTemplate t
     JOIN jsTemplateApproval ta ON ta.templateId = t.id AND ta.approvalId = 5
    WHERE t.id NOT IN (SELECT templateId FROM bud.bak_BudgetCutover_<date>);

   UPDATE t SET t.isActive = 0
     FROM jsTemplate t
     JOIN bud.bak_BudgetCutover_<date> b ON b.templateId = t.id;
   ===================================================================== */
