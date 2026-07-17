using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Signum.Upgrade.Upgrades;

/// <summary>
/// Rewrites old-style FindOptions object literals into the typed builder <c>X.findOptions(token => ({ ... }))</c>.
/// It matches any balanced object literal that starts with a <c>queryName:</c> property and whose every direct
/// property is a <see cref="FindOptions"/> property — wherever it appears (JSX <c>findOptions={{...}}</c>, a variable,
/// a function argument, etc.). Objects with a non-FindOptions property are left untouched.
///
/// The receiver is the entity Type: if <c>queryName</c> is itself an entity it is used and left implicit; otherwise
/// (e.g. a registered query like <c>UserQuery.Xxx</c>) the first <c>SomeEntity.token(...)</c> in the body is used as
/// the receiver and an explicit <c>queryName</c> is kept. If no entity is referenced, the literal is left untouched.
/// When the object is the first argument of <c>fetchLites/fetchEntities/useFetchLites/useFetchEntities</c> it becomes
/// <c>X.fetchOptions(...)</c> (validated against FetchOptions keys) instead of <c>X.findOptions(...)</c>.
///
/// Uses <see cref="Regex.Replace(string, MatchEvaluator)"/> with .NET balancing capturing groups to match balanced
/// {} , [] and () (the JS grammar the regex needs). Root filter groups (a <c>groupOperation</c> with no anchor
/// <c>token</c>) become the standalone <c>filterGroup(op, {..}, [..])</c> helper (auto-imported); anchored groups
/// (with a <c>token</c>) become <c>anchor.filterGroup(op, {..}, t =&gt; [..])</c> with the inner tokens re-scoped to
/// the anchor via <c>t</c>. Elements whose token is a bare string literal, and anchored groups whose inner tokens
/// can't be cleanly re-scoped, are left as valid object literals. A filter value of the form <c>SomeEnum.value("X")</c>
/// is unwrapped to the plain string <c>"X"</c>. Run eslint/prettier afterwards to tidy indentation,
/// then <c>tsgo --build</c> to catch the residual manual cases.
/// </summary>
class Upgrade_20260716_FindOptionsBuilder : CodeUpgradeBase
{
    public override string Description => "Migrate FindOptions object literals to the Type.findOptions(token => ...) builder";

    // --- balanced-delimiter building blocks (string literals inside are skipped so their brackets don't unbalance) ---
    const string Str = @"""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|`(?:\\.|[^`\\])*`";
    const string Brace = @"\{(?:" + Str + @"|[^{}]|(?<bc>\{)|(?<-bc>\}))*(?(bc)(?!))\}";
    const string Brack = @"\[(?:" + Str + @"|[^\[\]]|(?<bk>\[)|(?<-bk>\]))*(?(bk)(?!))\]";
    const string Paren = @"\((?:" + Str + @"|[^()]|(?<pn>\()|(?<-pn>\)))*(?(pn)(?!))\)";
    // a property value: consumes balanced groups/strings, stops at a top-level comma
    const string Value = @"(?:" + Str + "|" + Brace + "|" + Brack + "|" + Paren + @"|[^,{}\[\]()""'`])+";

    // a balanced object literal starting with `queryName:`, optionally the first argument of a fetch* call
    static readonly Regex FindOptionsObjectRx = new(
        @"(?<fetch>\b(?:useFetchLites|useFetchEntities|fetchLites|fetchEntities)\s*\(\s*)?" +
        @"(?<obj>\{\s*queryName\s*:(?:" + Str + @"|[^{}]|(?<bc>\{)|(?<-bc>\}))*(?(bc)(?!))\})",
        RegexOptions.Compiled);
    static readonly Regex QueryNameRx = new(@"queryName\s*:\s*(?<qn>" + Value + ")", RegexOptions.Compiled);
    static readonly Regex QueryNameRemoveRx = new(@"\s*queryName\s*:\s*" + Value + @"\s*,?", RegexOptions.Compiled);
    static readonly Regex FieldRx = new(@"(?<key>\w+)\s*:\s*(?<val>" + Value + ")", RegexOptions.Compiled);
    static readonly Regex ElementRx = new(Brace, RegexOptions.Compiled);
    static readonly Regex EntityTokenRx = new(@"(?<e>\w[\w.]*Entity)\.token\(", RegexOptions.Compiled);
    // EnumType.value("X") is just the string "X" -> unwrap it in values
    static readonly Regex EnumValueRx = new(@"\b[\w.]+\.value\(\s*(?<lit>['""][^'""]*['""])\s*\)", RegexOptions.Compiled);

    // The exact set of properties an object must be a subset of to be migrated (FindOptions vs FetchOptions).
    static readonly string[] FindOptionsKeys = { "queryName", "groupResults", "includeDefaultFilters", "filterOptions", "orderOptions", "columnOptionsMode", "columnOptions", "pagination", "systemTime" };
    static readonly string[] FetchOptionsKeys = { "queryName", "filterOptions", "orderOptions", "count" };
    static readonly string[] ConditionExtras = { "frozen", "pinned", "dashboardBehaviour", "removeElementWarning" };
    static readonly string[] GroupExtras = { "frozen", "pinned", "dashboardBehaviour", "value" };
    static readonly string[] ColumnExtras = { "displayName", "summaryToken", "hiddenColumn", "combineRows" };

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            if (!file.Content.Contains("queryName"))
                return;

            var newContent = FindOptionsObjectRx.Replace(file.Content, ConvertFindOptions);
            if (newContent == file.Content)
                return;

            file.Content = newContent;

            // root groups become the standalone filterGroup(...) helper: make sure it is imported
            if (Regex.IsMatch(file.Content, @"(?<![.\w])filterGroup\(") &&
                !Regex.IsMatch(file.Content, @"import\s*\{[^}]*\bfilterGroup\b[^}]*\}\s*from\s*['""]@framework/Reflection['""]"))
            {
                file.InsertBeforeFirstLine(l => l.StartsWith("import "), "import { filterGroup } from '@framework/Reflection'");
            }
        });
    }

    static string ConvertFindOptions(Match m)
    {
        var obj = m.Groups["obj"].Value; // the `{ queryName: ..., ... }` object literal
        var fetch = m.Groups["fetch"].Value; // "" unless the object is a fetch*(...) argument

        // A fetch*(...) argument is a FetchOptions and becomes .fetchOptions(...); otherwise a FindOptions -> .findOptions(...).
        var isFetch = m.Groups["fetch"].Success;
        var allowedKeys = isFetch ? FetchOptionsKeys : FindOptionsKeys;
        var method = isFetch ? "fetchOptions" : "findOptions";

        // only migrate when every direct property is allowed (else it's some other object)
        if (ParseFields(obj).Any(f => !allowedKeys.Contains(f.key)))
            return m.Value;

        var qn = QueryNameRx.Match(obj);
        if (!qn.Success)
            return m.Value;

        var queryName = qn.Groups["qn"].Value.Trim();

        // Must be rooted on an entity Type. If queryName is itself an entity, use it and let it default;
        // otherwise (e.g. a registered query like UserQuery.Xxx) find a SomeEntity.token(...) in the body to root on
        // and keep an explicit queryName. If no entity is referenced, we can't type the tokens: leave untouched.
        string entity;
        bool keepQueryName;
        if (queryName.EndsWith("Entity"))
        {
            entity = queryName;
            keepQueryName = false;
        }
        else
        {
            var et = EntityTokenRx.Match(obj);
            if (!et.Success)
                return m.Value;

            entity = et.Groups["e"].Value;
            keepQueryName = true;
        }

        var body = keepQueryName ? obj : QueryNameRemoveRx.Replace(obj, "", 1);
        body = new Regex(@"\b" + Regex.Escape(entity) + @"\.token\(").Replace(body, "token(");
        body = ConvertArray(body, "filterOptions", ConvertFilter);
        body = ConvertArray(body, "columnOptions", ConvertColumn);
        body = ConvertArray(body, "orderOptions", ConvertOrder);

        var trimmed = body.Trim();
        if (trimmed.Substring(1, trimmed.Length - 2).Trim().Length == 0) // nothing left but `{ }`
            return fetch + entity + "." + method + "()";

        return fetch + entity + "." + method + "(token => (" + body + "))";
    }

    static string ConvertArray(string body, string name, Func<Match, string?> convertElement)
    {
        var arrayRx = new Regex(@"(?<pre>\b" + name + @"\s*:\s*)(?<arr>" + Brack + ")");
        return arrayRx.Replace(body, am => am.Groups["pre"].Value + ConvertElementArray(am.Groups["arr"].Value, convertElement));
    }

    static string ConvertElementArray(string arrayLiteral, Func<Match, string?> convertElement)
    {
        var inner = arrayLiteral.Substring(1, arrayLiteral.Length - 2); // strip [ ]
        var newInner = ElementRx.Replace(inner, em => convertElement(em) ?? em.Value);
        return "[" + newInner + "]";
    }

    static string? ConvertFilter(Match element)
    {
        var fields = ParseFields(element.Value);

        if (Get(fields, "groupOperation") != null)
            return ConvertFilterGroup(fields);

        var token = Get(fields, "token");
        if (token == null || IsRawString(token))
            return null; // raw string token has no .filter(): keep the object literal

        var op = Get(fields, "operation") ?? @"""EqualTo""";
        var rawValue = Get(fields, "value");
        var value = rawValue == null ? "undefined" : EnumValueRx.Replace(rawValue, "${lit}"); // SomeEnum.value("X") -> "X"
        var extras = BuildOptions(fields, ConditionExtras);

        return $"{token}.filter({op}, {value}{(extras == null ? "" : ", " + extras)})";
    }

    static string? ConvertFilterGroup(List<(string key, string value)> fields)
    {
        var filters = Get(fields, "filters");
        if (filters == null || !filters.TrimStart().StartsWith("["))
            return null;

        var groupOperation = Get(fields, "groupOperation")!;
        var options = BuildOptions(fields, GroupExtras) ?? "{}";
        var anchor = Get(fields, "token");

        if (anchor == null) // root group -> standalone filterGroup(...)
            return $"filterGroup({groupOperation}, {options}, {ConvertElementArray(filters.Trim(), ConvertFilter)})";

        // anchored group -> <anchor>.filterGroup(op, {}, t => [...]) with inner tokens re-scoped to the anchor
        var scoped = ScopeAnchoredFilters(filters.Trim(), anchor);
        if (scoped == null)
            return null; // couldn't re-scope safely: leave as a valid FilterGroupOption object literal

        return $"{anchor}.filterGroup({groupOperation}, {options}, t => {scoped})";
    }

    // Rewrites each inner condition token from `<anchor>...` to `t...` (t = selector scoped to the anchor).
    // Returns null (bail) on anything it can't re-scope: nested groups, raw strings, or tokens not under the anchor.
    static string? ScopeAnchoredFilters(string arrayLiteral, string anchor)
    {
        var inner = arrayLiteral.Substring(1, arrayLiteral.Length - 2); // strip [ ]
        var ok = true;
        var replaced = ElementRx.Replace(inner, em =>
        {
            var scoped = ScopeAnchoredFilter(em.Value, anchor);
            if (scoped == null) { ok = false; return em.Value; }
            return scoped;
        });
        return ok ? "[" + replaced + "]" : null;
    }

    static string? ScopeAnchoredFilter(string element, string anchor)
    {
        var fields = ParseFields(element);
        if (Get(fields, "groupOperation") != null)
            return null; // nested group inside an anchored group: too complex to re-scope

        var token = Get(fields, "token");
        if (token == null || IsRawString(token) || !token.StartsWith(anchor))
            return null;

        var rest = token.Substring(anchor.Length);
        var scopedToken =
            rest.Length == 0 ? "t()" :
            rest.StartsWith(".append(") ? "t(" + rest.Substring(".append(".Length) : // fold first .append(x) into t(x)
            "t()" + rest;

        var op = Get(fields, "operation") ?? @"""EqualTo""";
        var value = Get(fields, "value") ?? "undefined";
        var extras = BuildOptions(fields, ConditionExtras);

        return $"{scopedToken}.filter({op}, {value}{(extras == null ? "" : ", " + extras)})";
    }

    static string? ConvertColumn(Match element)
    {
        var fields = ParseFields(element.Value);
        var token = Get(fields, "token");
        if (token == null || IsRawString(token))
            return null; // a bare string isn't a QueryTokenString: keep the { token: "..." } literal

        var extras = BuildOptions(fields, ColumnExtras);
        return extras == null ? token : $"{token}.column({extras})";
    }

    static string? ConvertOrder(Match element)
    {
        var fields = ParseFields(element.Value);
        var token = Get(fields, "token");
        if (token == null || IsRawString(token))
            return null; // raw string token has no .order(): keep the object literal

        var orderType = Get(fields, "orderType") ?? @"""Ascending""";
        return $"{token}.order({orderType})";
    }

    // A token written as a bare string literal ("Entity.X") has no fluent builder methods: leave such elements alone.
    static bool IsRawString(string token) => token.Length > 0 && (token[0] == '"' || token[0] == '\'' || token[0] == '`');

    static List<(string key, string value)> ParseFields(string braced)
    {
        var trimmed = braced.Trim();
        var inner = trimmed.Substring(1, trimmed.Length - 2); // strip { }
        return FieldRx.Matches(inner)
            .Select(f => (f.Groups["key"].Value, f.Groups["val"].Value.Trim()))
            .ToList();
    }

    static string? Get(List<(string key, string value)> fields, string key) =>
        fields.Where(f => f.key == key).Select(f => f.value).FirstOrDefault();

    static string? BuildOptions(List<(string key, string value)> fields, string[] allowed)
    {
        var picked = fields.Where(f => allowed.Contains(f.key)).ToList();
        if (picked.Count == 0)
            return null;

        return "{ " + string.Join(", ", picked.Select(f => f.key + ": " + f.value)) + " }";
    }
}
