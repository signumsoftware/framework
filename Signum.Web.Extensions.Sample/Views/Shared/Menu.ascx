<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Test" %>
<%@ Import Namespace="Signum.Entities.ControlPanel" %>

    <%=new WebMenuItem
    {
        Children =
        {
            new WebMenuItem { 
                Text="Music Items", 
                Children = 
            {
                    new WebMenuItem { Link = new FindOptions(typeof(AlbumDN)) },
                    new WebMenuItem { Link = new FindOptions(typeof(ArtistDN)) },
                    new WebMenuItem { Link = new FindOptions(typeof(BandDN)) },
                    new WebMenuItem { Link = new FindOptions(typeof(AwardDN)) },
                    new WebMenuItem { Link = new FindOptions(typeof(GrammyAwardDN)) },
                },
            },
            new WebMenuItem
            {
                Text = "Others",
                Children = 
                {
                    new WebMenuItem { Text = "Band with details", Link = "Music/BandDetail" },
                    new WebMenuItem { Text = "Band with repeater", Link = "Music/BandRepeater" },
                }
            },
            new WebMenuItem { Link = new FindOptions(typeof(ControlPanelDN)) }
        }
    }.ToString((string)ViewData["current"],"")
     %> 