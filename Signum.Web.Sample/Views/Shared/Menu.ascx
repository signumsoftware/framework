<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Test" %>

    <%        
        OrderedMenu menu = new OrderedMenu { Children = new List<OrderedMenu>() };
        
        menu.Children.Add(new OrderedMenu 
        { 
            node = new Item { Text = "Music Items"},
            Children = new List<OrderedMenu> {
                new OrderedMenu { node = new Item { Text = "Albums", QueryName = typeof(AlbumDN) } },
                new OrderedMenu { node = new Item { Text = "Artists", QueryName = typeof(ArtistDN) } },
                new OrderedMenu { node = new Item { Text = "Bands", QueryName = typeof(BandDN) } },
                new OrderedMenu { node = new Item { Text = "Awards", QueryName = typeof(AwardDN) } },
                new OrderedMenu { node = new Item { Text = "Grammy Awards", QueryName = typeof(GrammyAwardDN) } },
            }
        });
     %> 
     <%=menu.ToString(ViewData["current"].ToString())%>