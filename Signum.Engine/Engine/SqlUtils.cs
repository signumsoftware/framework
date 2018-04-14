using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Text.RegularExpressions;
using Signum.Engine.SchemaInfoTables;
using Signum.Engine.Maps;

namespace Signum.Engine
{
    public static class SqlUtils
    {
        static HashSet<string> Keywords =
@"ADD
ALL
ALTER
AND
ANY
AS
ASC
AUTHORIZATION
AVG
BACKUP
BEGIN
BETWEEN
BREAK
BROWSE
BULK
BY
CASCADE
CASE
CHECK
CHECKPOINT
CLOSE
CLUSTERED
COALESCE
COLUMN
COMMIT
COMMITTED
COMPUTE
CONFIRM
CONSTRAINT
CONTAINS
CONTAINSTABLE
CONTINUE
CONTROLROW
CONVERT
COUNT
CREATE
CROSS
CURRENT
CURRENT_DATE
CURRENT_TIME
CURRENT_TIMESTAMP
CURRENT_USER
CURSOR
DATABASE
DBCC
DEALLOCATE
DECLARE
DEFAULT
DELETE
DENY
DESC
DISK
DISTINCT
DISTRIBUTED
DOUBLE
DROP
DUMMY
DUMP
ELSE
END
ERRLVL
ERROREXIT
ESCAPE
EXCEPT
EXEC
EXECUTE
EXISTS
EXIT
FETCH
FILE
FILLFACTOR
FLOPPY
FOR
FOREIGN
FREETEXT
FREETEXTTABLE
FROM
FULL
GOTO
GRANT
GROUP
HAVING
HOLDLOCK
IDENTITY
IDENTITY_INSERT
IDENTITYCOL
IF
IN
INDEX
INNER
INSERT
INTERSECT
INTO
IS
ISOLATION
JOIN
KEY
KILL
LEFT
LEVEL
LIKE
LINENO
LOAD
MAX
MIN
MIRROREXIT
NATIONAL
NOCHECK
NONCLUSTERED
NOT
NULL
NULLIF
OF
OFF
OFFSETS
ON
ONCE
ONLY
OPEN
OPENDATASOURCE
OPENQUERY
OPENROWSET
OPTION
OR
ORDER
OUTER
OVER
PERCENT
PERM
PERMANENT
PIPE
PLAN
PRECISION
PREPARE
PRIMARY
PRINT
PRIVILEGES
PROC
PROCEDURE
PROCESSEXIT
PUBLIC
RAISERROR
READ
READTEXT
RECONFIGURE
REFERENCES
REPEATABLE
REPLICATION
RESTORE
RESTRICT
RETURN
REVOKE
RIGHT
ROLLBACK
ROWCOUNT
ROWGUIDCOL
RULE
SAVE
SCHEMA
SELECT
SERIALIZABLE
SESSION_USER
SET
SETUSER
SHUTDOWN
SOME
STATISTICS
SUM
SYSTEM_USER
TABLE
TAPE
TEMP
TEMPORARY
TEXTSIZE
THEN
TO
TOP
TRAN
TRANSACTION
TRIGGER
TRUNCATE
TSEQUAL
UNCOMMITTED
UNION
UNIQUE
UPDATE
UPDATETEXT
USE
USER
VALUES
VARYING
VIEW
WAITFOR
WHEN
WHERE
WHILE
WITH
WORK
WRITETEXT".Lines().Select(a => a.Trim().ToUpperInvariant()).ToHashSet();

        public static string SqlEscape(this string ident)
        {
            if (Keywords.Contains(ident.ToUpperInvariant()) || Regex.IsMatch(ident, @"-\d|[áéíóúàèìòùÁÉÍÓÚÀÈÌÒÙ/\\. ]"))
                return "[" + ident + "]";

            return ident;
        }

        public static SqlPreCommand RemoveDuplicatedIndices()
        {
            var plainData = (from s in Database.View<SysSchemas>()
                             from t in s.Tables()
                             from ix in t.Indices()
                             from ic in ix.IndexColumns()
                             from c in t.Columns()
                             where ic.column_id == c.column_id
                             select new
                             {
                                 table = new ObjectName(new SchemaName(null, s.name), t.name),
                                 index = ix.name,
                                 ix.is_unique,
                                 column = c.name,
                                 ic.is_descending_key,
                                 ic.is_included_column,
                                 ic.index_column_id
                             }).ToList();

            var tables = plainData.AgGroupToDictionary(a => a.table,
                gr => gr.AgGroupToDictionary(a => new { a.index, a.is_unique },
                    gr2 => gr2.OrderBy(a => a.index_column_id)
                        .Select(a => a.column + (a.is_included_column ? "(K)" : "(I)") + (a.is_descending_key ? "(D)" : "(A)"))
                        .ToString("|")));

            var result = tables.SelectMany(t =>
                t.Value.GroupBy(a => a.Value, a => a.Key)
                .Where(gr => gr.Count() > 1)
                .Select(gr =>
                {
                    var best = gr.OrderByDescending(a => a.is_unique).ThenByDescending(a => a.index.StartsWith("IX")).ThenByDescending(a => a.index).First();

                    return gr.Where(g => g != best)
                        .Select(g => SqlBuilder.DropIndex(t.Key, g.index))
                        .PreAnd(new SqlPreCommandSimple("-- DUPLICATIONS OF {0}".FormatWith(best.index))).Combine(Spacing.Simple);
                })
            ).Combine(Spacing.Double);

            if (result == null)
                return null;

            return SqlPreCommand.Combine(Spacing.Double,
                 new SqlPreCommandSimple("use {0}".FormatWith(Connector.Current.DatabaseName())),
                 result);
        }
    }
}
