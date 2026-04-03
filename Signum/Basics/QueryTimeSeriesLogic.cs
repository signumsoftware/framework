using Microsoft.SqlServer.Server;
using Signum.Engine.Linq;
using Signum.Engine.Maps;

namespace Signum.Basics;


public class DateValue : IView
{
    [ViewPrimaryKey, DbType(DateTimeKind = DateTimeKind.Utc)]
    public DateTime Date;
}

public class QueryTimeSeriesLogic
{

    [SqlMethod(Name = "GetDatesInRange"), AvoidEagerEvaluation]
    public static IQueryable<DateValue> GetDatesInRange(DateTime startDate, DateTime endDate, string incrementType, int step)
    {
        var mi = (MethodInfo)MethodInfo.GetCurrentMethod()!;
        return new Query<DateValue>(DbQueryProvider.Single, Expression.Call(mi,
           Expression.Constant(startDate, typeof(DateTime)),
           Expression.Constant(endDate, typeof(DateTime)),
           Expression.Constant(incrementType, typeof(string)),

           Expression.Constant(step, typeof(int))
       ));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        if (!sb.Schema.Settings.IsPostgres)
        {
            Schema.Current.Assets.IncludeUserDefinedFunction("GetDatesInRange", """
            (
                @startDate DATETIME2,
                @endDate DATETIME2,
                @incrementType NVARCHAR(20),
                @step INT
            )
            RETURNS @DateRange TABLE
            (
                Date DATETIME2 PRIMARY KEY
            )
            AS
            BEGIN
                DECLARE @currentDate DATETIME2
                SET @currentDate = @startDate

                 IF @incrementType = 'millisecond'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(MILLISECOND, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'second'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(SECOND, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'minute'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(MINUTE, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'hour'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(HOUR, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'day'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(DAY, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'week'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(WEEK, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'month'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(MONTH, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'quarter'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(QUARTER, @step, @currentDate)
                    END
                END
                ELSE IF @incrementType = 'year'
                BEGIN
                    WHILE @currentDate <= @endDate
                    BEGIN
                        INSERT INTO @DateRange (Date)
                        VALUES (@currentDate)
                        SET @currentDate = DATEADD(YEAR, @step, @currentDate)
                    END
                END
                ELSE
                BEGIN
                -- Throw exception for invalid incrementType
                    DECLARE @error INT = CAST('Invalid @incrementType provided.' as int);
                END

                RETURN
            END
            """);
        }
        else
        {
            Schema.Current.Assets.IncludeUserDefinedFunction("GetDatesInRange", """                
            (start_date timestamp with time zone, end_date timestamp with time zone, increment_type character varying, step integer)
             RETURNS TABLE(date timestamp with time zone)
             LANGUAGE plpgsql
            AS $function$
            BEGIN
                RETURN QUERY
                SELECT generate_series(start_date, end_date, (step || ' ' || increment_type)::interval);
            END;
            $function$
            """);
        }
    }

}
