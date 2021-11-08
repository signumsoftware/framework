using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Signum.React.Filters;
using System.ComponentModel.DataAnnotations;
using static Signum.React.Authorization.AuthController;
using Signum.Entities.Alerts;

namespace Signum.React.Authorization;

[ValidateModelFilter]
public class AlertController : ControllerBase
{

    [HttpGet("api/alerts/myAlerts")]
    public List<AlertEntity> MyAlerts()
    {
        return Database.Query<AlertEntity>().Where(a => a.Recipient.Is(UserEntity.Current) && a.Alerted).ToList();
    }

    [HttpGet("api/alerts/myAlertsCount")]
    public MyAlertCountResult MyAlertsCount()
    {
        var result =  Database.Query<AlertEntity>()
            .Where(a => a.Recipient.Is(UserEntity.Current) && a.Alerted)
            .GroupBy(a => a.Recipient) //To make two aggregates in one query
            .Select(gr => new MyAlertCountResult
            {
                LastAlert = gr.Max(a => (DateTime?)a.CreationDate),
                NumAlerts = gr.Count()
            }).SingleOrDefault();

        return result ?? new MyAlertCountResult { NumAlerts = 0 };
    }


    public class MyAlertCountResult 
    {
        public int NumAlerts;
        public DateTime? LastAlert;
    }
}
