/*
 * [bud].[jsGetHanaConfiguration] — LIVE definition, supplied 2026-09-24.
 * Created 2025-04-02 12:38:42.420 · Modified 2026-07-09 11:06:34.550
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 */
CREATE PROCEDURE [bud].[jsGetHanaConfiguration]      @company VARCHAR(MAX) = '1',      @serverName NVARCHAR(100) OUTPUT,      @schemaName NVARCHAR(100) OUTPUT  AS  BEGIN      SET NOCOUNT ON;        SELECT @serverName = ConfigValue      FROM dbo.jsConfiguration      WHERE ConfigKey = 'ServerName';        IF @company IN ('1', 'OIL')      BEGIN          SELECT @schemaName = ConfigValue          FROM dbo.jsConfiguration          WHERE ConfigKey = 'OilDatabase';      END      ELSE IF @company IN ('2', 'BEVERAGE', 'BEV')      BEGIN          SELECT @schemaName = ConfigValue          FROM dbo.jsConfiguration          WHERE ConfigKey = 'BeverageDatabase';      END      ELSE IF @company IN ('3', 'MART')      BEGIN          SELECT @schemaName = ConfigValue          FROM dbo.jsConfiguration          WHERE ConfigKey = 'MartDatabase';      END      ELSE      BEGIN          THROW 50001, 'Invalid company for HANA configuration. Use 1/OIL, 2/BEVERAGE, or 3/MART.', 1;      END  END;
