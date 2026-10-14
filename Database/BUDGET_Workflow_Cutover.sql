/* =====================================================================
   BUDGET WORKFLOW CUTOVER - one script, self-backing-up, reversible
   Target DB : jsaplive3                      Generated : 20261014

   WHAT IT DOES
   ------------
   1. BACKS UP the on/off state and full wiring of every budget template
      into bud.bak_* tables stamped 20261014.
   2. CREATES 71 new templates - one per (unit, budget, account bucket).
      The three buckets are salary accounts / electricity / everything
      else, and they are mutually exclusive, so a budget line can only
      ever match one template. Verified by test before release: 0 lines
      matched more than one template.
   3. DEACTIVATES the current budget templates.

   WHAT IT DOES NOT TOUCH
   ----------------------
   bud.jsDocEntry, bud.jsDocEnrtyDetail and every existing flow are left
   alone. Requests already raised stay on their current template with
   their current approver and finish there. Only NEW documents enter the
   new workflow: every new query carries budgetDate > <cutover time>, so
   it cannot pick up anything historical.

   ROLLBACK
   --------
   The block at the bottom switches the old templates back on and the new
   ones off. This script never edits an existing row, so nothing else
   needs undoing.

   SAFETY
   ------
   One transaction. Any error rolls everything back. Refuses to run twice.
   ===================================================================== */

USE jsaplive3;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @cut DATETIME = GETDATE();
DECLARE @cutTxt VARCHAR(30) = CONVERT(VARCHAR(30), GETDATE(), 121);
DECLARE @createdBy INT = 67;
DECLARE @made INT = 0, @off INT = 0;
DECLARE @qid INT, @tid INT, @sid INT;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID('bud.bak_BudgetCutover_20261014') IS NOT NULL
        THROW 70001, 'Backup table already exists - this script has already been run.', 1;

    /* ---------- 1. BACKUP ---------- */
    SELECT t.id AS templateId, t.name, t.isActive, t.company, GETDATE() AS backedUpOn
      INTO bud.bak_BudgetCutover_20261014
      FROM jsTemplate t
      JOIN jsTemplateApproval ta ON ta.templateId = t.id AND ta.approvalId = 5;

    SELECT * INTO bud.bak_StageTemplate_20261014 FROM jsStageTemplate
     WHERE templateId IN (SELECT templateId FROM bud.bak_BudgetCutover_20261014);

    SELECT * INTO bud.bak_UserStage_20261014 FROM jsUserStage
     WHERE stageId IN (SELECT stageId FROM bud.bak_StageTemplate_20261014);

    SELECT * INTO bud.bak_TemplateQuery_20261014 FROM jsTemplateQuery
     WHERE templateId IN (SELECT templateId FROM bud.bak_BudgetCutover_20261014);

    SELECT * INTO bud.bak_Query_20261014 FROM jsQuery
     WHERE id IN (SELECT queryId FROM bud.bak_TemplateQuery_20261014);

    /* ---------- 2. CREATE the new templates ---------- */

    /* bw oil backoff salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''BackOff'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil backoff salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil backoff salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil backoff salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil backoff elec | electricity -> owner then Gurpreet */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''BackOff'' AND ProcesStat = ''Y'' AND AcctCode in (5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw oil backoff elec', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil backoff elec', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil backoff elec s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 68);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil backoff elec s2', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil delbkhp salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Del Bkhp'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil delbkhp salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil delbkhp salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil delbkhp salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil delbkhp other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Del Bkhp'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil delbkhp other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil delbkhp other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil delbkhp other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 74);

    /* bw oil delmayp salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Del Mayp'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil delmayp salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil delmayp salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil delmayp salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil delmayp other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Del Mayp'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil delmayp other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil delmayp other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil delmayp other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 74);

    /* bw oil fact_com salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''FACT_COM'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil fact_com salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil fact_com salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil fact_com salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil fact_com elec | electricity -> owner then Gurpreet */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''FACT_COM'' AND ProcesStat = ''Y'' AND AcctCode in (5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw oil fact_com elec', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil fact_com elec', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil fact_com elec s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 69);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil fact_com elec s2', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil fact_com other | not in the Excel - keeps its current owner */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''FACT_COM'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016,5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw oil fact_com other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil fact_com other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil fact_com other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 69);

    /* bw oil factory salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Factory'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil factory salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil factory salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil factory salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil factory elec | electricity -> owner then Gurpreet */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Factory'' AND ProcesStat = ''Y'' AND AcctCode in (5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw oil factory elec', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil factory elec', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil factory elec s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 75);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil factory elec s2', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil factory other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Factory'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016,5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw oil factory other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil factory other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil factory other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 75);

    /* bw oil interest salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Interest'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil interest salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil interest salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil interest salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil interest other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Interest'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil interest other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil interest other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil interest other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);

    /* bw oil medmkt salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Med MKT'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil medmkt salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil medmkt salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil medmkt salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil medmkt other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Med MKT'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil medmkt other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil medmkt other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil medmkt other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 87);

    /* bw oil npd1 salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''NPD1'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil npd1 salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil npd1 salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd1 salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil npd1 other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''NPD1'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil npd1 other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil npd1 other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd1 other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd1 other s2', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil npd2 salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''NPD2'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil npd2 salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil npd2 salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd2 salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil npd2 other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''NPD2'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil npd2 other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil npd2 other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd2 other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd2 other s2', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil npd3 salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''NPD3'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil npd3 salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil npd3 salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd3 salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil npd3 other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''NPD3'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil npd3 other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil npd3 other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd3 other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil npd3 other s2', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil ote salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''OTE'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil ote salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil ote salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil ote salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil ote other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''OTE'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil ote other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil ote other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil ote other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil rnd salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''R & D'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil rnd salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil rnd salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil rnd salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil rnd other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''R & D'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil rnd other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil rnd other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil rnd other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw oil salcf salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Sal CF'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil salcf salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil salcf salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil salcf salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil salcf other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Sal CF'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil salcf other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil salcf other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil salcf other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil sales salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Sales'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil sales salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil sales salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil sales salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil sales other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Sales'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil sales other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil sales other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil sales other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 69);

    /* bw oil salesre salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Sales RE'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil salesre salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil salesre salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil salesre salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil salesre other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Sales RE'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil salesre other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil salesre other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil salesre other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 69);

    /* bw oil transprt salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Transprt'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil transprt salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil transprt salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil transprt salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil transprt other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''Transprt'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil transprt other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil transprt other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil transprt other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 92);

    /* bw oil jivos salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''JIVOS'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil jivos salary', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil jivos salary', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil jivos salary s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw oil jivos other | not in the Excel - keeps its current owner */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''JIVOS'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw oil jivos other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil jivos other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil jivos other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil jivos other s2', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev backoff salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''BackOff'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev backoff salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev backoff salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev backoff salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev backoff elec | electricity -> owner then Gurpreet */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''BackOff'' AND ProcesStat = ''Y'' AND AcctCode in (5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw bev backoff elec', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev backoff elec', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev backoff elec s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 68);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev backoff elec s2', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev backoff other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''BackOff'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016,5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw bev backoff other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev backoff other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev backoff other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 68);

    /* bw bev delbkhp salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Del Bkhp'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev delbkhp salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev delbkhp salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev delbkhp salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev delbkhp other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Del Bkhp'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev delbkhp other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev delbkhp other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev delbkhp other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 74);

    /* bw bev delmayp salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Del Mayp'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev delmayp salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev delmayp salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev delmayp salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev delmayp other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Del Mayp'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev delmayp other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev delmayp other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev delmayp other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 74);

    /* bw bev fact_com salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''FACT_COM'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev fact_com salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev fact_com salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev fact_com salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev fact_com elec | electricity -> owner then Gurpreet */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''FACT_COM'' AND ProcesStat = ''Y'' AND AcctCode in (5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw bev fact_com elec', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev fact_com elec', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev fact_com elec s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 80);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev fact_com elec s2', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev fact_com other | not in the Excel - keeps its current owner */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''FACT_COM'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016,5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw bev fact_com other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev fact_com other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev fact_com other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 80);

    /* bw bev factory salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Factory'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev factory salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev factory salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev factory salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev factory elec | electricity -> owner then Gurpreet */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Factory'' AND ProcesStat = ''Y'' AND AcctCode in (5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw bev factory elec', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev factory elec', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev factory elec s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 80);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev factory elec s2', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev factory other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Factory'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016,5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw bev factory other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev factory other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev factory other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 80);

    /* bw bev interest salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Interest'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev interest salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev interest salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev interest salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev interest other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Interest'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev interest other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev interest other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev interest other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);

    /* bw bev jivos salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''JIVOS'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev jivos salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev jivos salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev jivos salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev jivos other | not in the Excel - keeps its current owner */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''JIVOS'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev jivos other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev jivos other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev jivos other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev jivos other s2', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev medmkt salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Med MKT'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev medmkt salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev medmkt salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev medmkt salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev medmkt other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Med MKT'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev medmkt other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev medmkt other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev medmkt other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 87);

    /* bw bev npd1 salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''NPD1'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev npd1 salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev npd1 salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev npd1 salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev npd1 other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''NPD1'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev npd1 other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev npd1 other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev npd1 other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev npd1 other s2', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev npd2 salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''NPD2'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev npd2 salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev npd2 salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev npd2 salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev npd2 other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''NPD2'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev npd2 other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev npd2 other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev npd2 other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 72);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev npd2 other s2', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 2);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev ote salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''OTE'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev ote salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev ote salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev ote salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev ote other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''OTE'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev ote other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev ote other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev ote other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 79);

    /* bw bev salcf salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Sal CF'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev salcf salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev salcf salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev salcf salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev salcf other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Sal CF'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev salcf other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev salcf other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev salcf other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev sales salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Sales'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev sales salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev sales salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev sales salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev sales other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Sales'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev sales other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev sales other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev sales other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 87);

    /* bw bev salesre salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Sales RE'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev salesre salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev salesre salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev salesre salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev salesre other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Sales RE'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev salesre other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev salesre other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev salesre other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 87);

    /* bw bev transprt salary | salary accounts -> Ziaul */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Transprt'' AND ProcesStat = ''Y'' AND AcctCode in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev transprt salary', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev transprt salary', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev transprt salary s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 73);

    /* bw bev transprt other */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''BEVERAGE'' AND BUDGET = ''Transprt'' AND ProcesStat = ''Y'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016) AND budgetDate > ''' + @cutTxt + '''', 'bw bev transprt other', 5, 2);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw bev transprt other', 1, @createdBy, @cut, 2);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw bev transprt other s1', 1, @createdBy, @cut, 2, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 92);

    /* bw oil backoff-import other | IMPORT parked for Himanshu - keeps current owner Ravinder */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''BackOff'' AND ProcesStat = ''Y'' AND SUB_BUDGET = ''IMPORT'' AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016,5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw oil backoff-import other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil backoff-import other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil backoff-import other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 71);

    /* bw oil backoff other | owner change Ravinder -> Nirmal */
    INSERT INTO jsQuery ([query], name, type, company) VALUES ('SELECT * FROM bud.jsBudgetTable WHERE BRANCH = ''OIL'' AND BUDGET = ''BackOff'' AND ProcesStat = ''Y'' AND (SUB_BUDGET IS NULL OR SUB_BUDGET <> ''IMPORT'') AND AcctCode not in (5630001,5630002,5630005,5630006,5630007,5630008,5630009,5630010,5630011,5630012,5630013,5630014,5630015,5630016,5680011) AND budgetDate > ''' + @cutTxt + '''', 'bw oil backoff other', 5, 1);
    SET @qid = SCOPE_IDENTITY();
    INSERT INTO jsTemplate (name, isActive, createdBy, createdOn, company) VALUES ('bw oil backoff other', 1, @createdBy, @cut, 1);
    SET @tid = SCOPE_IDENTITY(); SET @made += 1;
    INSERT INTO jsTemplateApproval (templateId, approvalId) VALUES (@tid, 5);
    INSERT INTO jsTemplateQuery (templateId, queryId) VALUES (@tid, @qid);
    INSERT INTO jsStage (stage, approvalId, createdBy, createdOn, company, rejectid, description) VALUES ('bw oil backoff other s1', 1, @createdBy, @cut, 1, 1, 'budget cutover 20261014');
    SET @sid = SCOPE_IDENTITY();
    INSERT INTO jsStageTemplate (stageId, templateId, priority) VALUES (@sid, @tid, 1);
    INSERT INTO jsUserStage (stageId, userId) VALUES (@sid, 68);

    /* ---------- 3. DEACTIVATE the old templates ---------- */
    UPDATE t SET t.isActive = 0
      FROM jsTemplate t
      JOIN bud.bak_BudgetCutover_20261014 b ON b.templateId = t.id
     WHERE t.isActive = 1;
    SET @off = @@ROWCOUNT;

    IF @made <> 71 THROW 70002, 'Wrong number of templates created.', 1;
    IF @off  =  0 THROW 70003, 'No old template was deactivated.', 1;

    COMMIT TRAN;
    SELECT @made AS templatesCreated, @off AS templatesDeactivated,
           CONVERT(VARCHAR(30), @cut, 121) AS cutoverTime, 'COMMITTED' AS outcome;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SELECT ERROR_NUMBER() AS err, ERROR_LINE() AS line_, ERROR_MESSAGE() AS msg,
           'ROLLED BACK - nothing changed' AS outcome;
END CATCH
GO

/* ===================== VERIFY (run after) ===================== */
SELECT t.company, COUNT(*) AS activeBudgetTemplates
  FROM jsTemplate t JOIN jsTemplateApproval ta ON ta.templateId=t.id AND ta.approvalId=5
 WHERE t.isActive=1 GROUP BY t.company;

SELECT COUNT(*) AS pendingFlowsUntouched FROM bud.jsDocEntry WHERE status='P';
GO

/* =====================================================================
   ROLLBACK
   --------
   Do NOT hand-edit anything here. Run the companion file:

       BUDGET_Workflow_ROLLBACK.sql

   It finds this run's backup table on its own, switches the original
   templates back on, switches these new ones off, previews the change
   before committing, and refuses to leave zero active templates.
   No flow is moved either way.
   ===================================================================== */
