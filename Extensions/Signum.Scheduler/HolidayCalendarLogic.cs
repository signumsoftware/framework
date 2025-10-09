using System.Collections.Frozen;
using System.Net.Http;
using System.Text.Json;

namespace Signum.Scheduler;

public static class HolidayCalendarLogic
{
    public static ResetLazy<HolidayCalendarEntity?> DefaultHolidayCalendar = null!;
    public static ResetLazy<FrozenDictionary<Lite<HolidayCalendarEntity>, HolidayCalendarEntity>> HolidayCalendarsByLite = null!;

    public static HolidayCalendarEntity RetrieveFromCache(this Lite<HolidayCalendarEntity> lite)
    {
        return HolidayCalendarsByLite.Value.GetOrThrow(lite);
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<HolidayCalendarEntity>()
            .WithUniqueIndex(hc => hc.IsDefault, hc => hc.IsDefault)
            .WithQuery(() => st => new
            {
                Entity = st,
                st.Id,
                st.Name,
                st.IsDefault,
                Holidays = st.Holidays.Count,
            });

        HolidayCalendarsByLite = sb.GlobalLazy(() => Database.Query<HolidayCalendarEntity>().ToFrozenDictionary(hc => hc.ToLite()), 
            new InvalidateWith(typeof(HolidayCalendarEntity)));

        DefaultHolidayCalendar = sb.GlobalLazy(() => HolidayCalendarsByLite.Value.Values.SingleOrDefault(hc => hc.IsDefault), 
            new InvalidateWith(typeof(HolidayCalendarEntity)));

        new Graph<HolidayCalendarEntity>.Execute(HolidayCalendarOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (c, _) => { },
        }.Register();

        new Graph<HolidayCalendarEntity>.Execute(HolidayCalendarOperation.ImportPublicHolidays)
        {
            CanExecute = (e) => e.FromYear.HasValue && e.ToYear.HasValue && e.CountryCode.HasText() ? null :
                HolidayCalendarMessage.ForImport01and2ShouldBeSet.NiceToString(
                    Entity.NicePropertyName(()=> e.FromYear),
                    Entity.NicePropertyName(()=> e.ToYear),
                    Entity.NicePropertyName(()=> e.CountryCode)
                ),
            Execute = (e, _) =>
            {
                for (int i = e.FromYear!.Value; i <= e.ToYear!.Value; i++)
                {
                    string url = $"https://date.nager.at/api/v3/PublicHolidays/{i}/{e.CountryCode}";

                    using var client = new HttpClient();
                    var json = client.GetStringAsync(url).Result;

                    var holidays = JsonSerializer.Deserialize<List<NagerHoliday>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })!;

                    foreach (var h in holidays.Where(h => h.Global || h.Counties.Contains(e.SubDivisionCode)))
                    {
                        var existing = e.Holidays.FirstOrDefault(a => a.Date == h.Date);

                        if (existing == null)
                        {
                            e.Holidays.Add(new HolidayEmbedded
                            {
                                Date = h.Date,
                                Name = h.LocalName,
                            });
                        }
                    }
                }
                e.Save();
            },
        }.Register();

        new Graph<HolidayCalendarEntity>.Delete(HolidayCalendarOperation.Delete)
        {
            Delete = (c, _) => { c.Delete(); },
        }.Register();
    }

    public static List<string>? GetCountries()
    {
        var countriesJson = new HttpClient().GetStringAsync("https://date.nager.at/api/v3/AvailableCountries").Result;
        var countries = JsonSerializer.Deserialize<List<Country>>(countriesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return countries?.Select(c => c.CountryCode).ToList();
    }

    public static List<string>? GetSubDivisions(string countryCode)
    {
        var year = Clock.Now.Year;
        var countriesJson = new HttpClient().GetStringAsync($"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}").Result;
        var holidays = JsonSerializer.Deserialize<List<NagerHoliday>>(countriesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var allRegions = holidays?
            .Where(h => h.Counties != null)
            .SelectMany(h => h.Counties!)
            .Distinct()
            .ToList();
        return allRegions;
    }

    private class NagerHoliday
    {
        public DateOnly Date { get; set; }
        public string LocalName { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public string[] Counties { get; set; }
        public bool Global { get; set; }
        public string[] Types { get; set; }
    }

    private class Country
    {
        public string CountryCode { get; set; }
        public string Name { get; set; }
    }
}
