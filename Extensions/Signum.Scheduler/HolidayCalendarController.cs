using Microsoft.AspNetCore.Mvc;

namespace Signum.Scheduler;

public class HolidayCalendarController : ControllerBase
{
    [HttpGet("api/holidaycalendar/countries")]
    public List<string> GetCountries()
    {
        return HolidayCalendarLogic.GetCountries() ?? [];
    }

    [HttpGet("api/holidaycalendar/subDivisions/{countryCode}")]
    public List<string> GetSubDivisions(string countryCode)
    {
        return HolidayCalendarLogic.GetSubDivisions(countryCode) ?? [];
    }
}
