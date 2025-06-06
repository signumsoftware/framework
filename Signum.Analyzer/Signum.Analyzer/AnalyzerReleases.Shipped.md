## Release 2.7

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SF0001 | Expressions | Warning | AutoExpressionFieldAnalyzer
SF0002 | Expressions | Warning | ExpressionFieldAnalyzer
SF0003 | Lite | Error | LiteEqualityAnalyzer
SF0004 | Lite | Error | LiteCastAnalyzer


## Release 3.0

### Changed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
SF0004 | Lite | Error | Fix Pattern mathing

## Release 3.1

### Changed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
SF0004 | Lite | Error | Fix Pattern mathing for case statement and case expression

## Release 3.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SF0031 | Lite | Warning | LiteEqualityAnalyzer (Lite<T> == Lite<T>)
SF0032 | Lite | Warning | LiteEqualityAnalyzer (Entity == Entity)
SF0033 | Lite | Error | LiteEqualityAnalyzer (Lite<T> == Entity)
SF0034 | Lite | Error | LiteEqualityAnalyzer (Lite<A> == Lite<B>)


