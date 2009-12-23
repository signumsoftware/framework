var menuTimeout;
$(document).ready(function() {
    $("#nav li").children("ul").hide();
    $("#nav a").click(function cancelProp(e) { e.cancelBubble = true; if (e.stopPropagation) e.stopPropagation(); });
    $("#nav li").click(function() {
        $(".clicked").not($(this).parents()).children("ul").fadeOut(500);
        $(".clicked").not($(this).parents()).removeClass("clicked");
        $(".submenu-visible").not($(this).parents()).fadeOut(500);
        $(this).addClass("clicked");
        $(this).children("ul").width($("#divMenu").width()).fadeIn(500);
        if ($(this).children("a").length == 0)
            return false;
    });
    setCurrentMenuEntry(false);
    $(".submenu").width($("#divMenu").width());
    $("#nav").hover(
        function() { clearTimeout(menuTimeout); },
        function() { menuTimeout = setTimeout("setCurrentMenuEntry(true);", 3000); }
    );
    
    $("#toggle-section-menu")
    .bind("click", function(e) {
        if ($("#mostrarMenu").hasClass('visible')){             
            $("#contenido").removeClass('grid_16').addClass('grid_13');                   
            $(this).removeClass('hidden').addClass('visible');
            $("#mostrarMenu").removeClass('visible').addClass('hidden');
            $(".menu").slideRightShow();                 
        } else {
            
            $("#mostrarMenu").removeClass('hidden').addClass('visible');
            //$(".menu").slideLeftHide();
            $(".menu").toggle();
            $(this).removeClass('visible').addClass('hidden');
            $("#content").removeClass('grid_13').addClass('grid_16');                    
        }
        e.preventDefault();
    });             
});

    
jQuery.fn.extend({
  slideRightShow: function() {
    return this.each(function() {
        $(this).show('slide', {direction: 'right'}, 400);
    });
  },
  slideLeftHide: function() {
    return this.each(function() {
      $(this).hide('slide', {direction: 'left'}, 0);
    });
  },
  slideRightHide: function() {
    return this.each(function() {
      $(this).hide('slide', {direction: 'right'}, 300);
    });
  },
  slideLeftShow: function() {
    return this.each(function() {
      $(this).show('slide', {direction: 'left'}, 300);
    });
  }
});

function endsWith(testString, endingString){
      if(endingString.length > testString.length) return false;
      return testString.indexOf(endingString)==(testString.length-endingString.length);
}

function setCurrentMenuEntry(fade) {
    var indexPath = "SegurosMVC";
    var indexControllerPath = "Home/Index";
    $(".clicked").removeClass("clicked").children("ul").each((fade==true) ? function() { $(this).fadeOut(1000); } : function() { $(this).hide(); })
    var path = location.pathname.substring(1);
    $("#nav a").each(
    function() {
        if (endsWith(this.href,path) || endsWith(this.href,indexControllerPath) && path.replace('/',"") == indexPath) {
            $(this).addClass("menuOn");
            $(this).parents("li").addClass("menuOn");
            $(this).parents("ul").addClass("submenu-visible").width($("#divMenu").width()).each((fade == true) ? function() { $(this).fadeIn(1000); } : function() { $(this).show(); })
            $("#divMenu").next().height(($(".submenu-visible").length - 1) * $("#divMenu").height());
        }
    });
}