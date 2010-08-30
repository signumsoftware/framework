<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Test" %>

    <%=new WebMenuItem
    {
        Children =
        {
            new WebMenuItem { 
                Text="Music Items", 
                Children = 
            {
                    new WebMenuItem { Text = "Albums", Link = new FindOptions(typeof(AlbumDN)) },
                    new WebMenuItem { Text = "Artists", Link = new FindOptions(typeof(ArtistDN)) },
                    new WebMenuItem { Text = "Bands", Link = new FindOptions(typeof(BandDN)) },
                    new WebMenuItem { Text = "Awards", Link = new FindOptions(typeof(AwardDN)) },
                    new WebMenuItem { Text = "Grammy Awards", Link = new FindOptions(typeof(GrammyAwardDN)) },
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
            }
        }
    }.ToString((string)ViewData["current"],"")
     %> 