/*
 * [bud].[jsSyncAttachmentsToHana] — LIVE definition, supplied 2026-09-24.
 * Created 2025-04-02 12:38:46.310 · Modified 2025-11-25 18:28:42.720
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim.
 *
 * Run by SQL Agent job "Sync attchments" (enabled) every 2 minutes.
 * Despite the name it copies FROM HANA (Oil's DRAFT_APPROVAL_ATC1) INTO
 * bud.SAPAttachments; it writes nothing to HANA.
 */
  CREATE PROCEDURE [bud].[jsSyncAttachmentsToHana]  AS  BEGIN      DELETE FROM bud.SAPAttachments;        -- Create a temporary table to hold the procedure results      CREATE TABLE #TempAttachments (          Branch VARCHAR(50),          DocEntry INT,          ObjectName VARCHAR(100),          ObjType INT,          trgtPath VARCHAR(255),          FileName VARCHAR(100),          FileExt VARCHAR(10),          AtcEntry INT      );        -- Insert the results of the procedure into the temp table      INSERT INTO #TempAttachments      EXEC ('call "JIVO_OIL_HANADB"."DRAFT_APPROVAL_ATC1"') AT HANADB112;        -- Insert from temp table to the target table without replacing special characters      INSERT INTO bud.SAPAttachments (Branch, DocEntry, ObjectName, ObjType, trgtPath, FileName, FileExt, AtcEntry)      SELECT Branch, DocEntry, ObjectName, ObjType, trgtPath, FileName, FileExt, AtcEntry      FROM #TempAttachments;        -- Drop the temporary table      DROP TABLE #TempAttachments;  END
