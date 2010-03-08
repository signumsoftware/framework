<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<script type="text/javascript">
    var defaultString = "";
    $(function() {
    $(".shortcut").click(function(){$.copy($(this).html());});
    if(typeof window.location.hash != 'undefined' && window.location.hash != ''){
    window.location.hash += "-editor";
        // duration in ms
      //  var duration=500;

       // easing values: swing | linear
     //  var easing='swing';
   // get / set parameters
  // var newHash=window.location.hash;

   var $dd = $(window.location.hash).parents("dd").first();
  /* var target = $dd[0].offsetTop;
 
      // animate to target and set the hash to the window.location after the animation
      $('html:not(:animated),body:not(:animated)').animate({ scrollTop: target }, duration, easing, function() {
        $dd.css('background-color','#ccffaa');
        $dd.prev().css('background-color','#ccffaa');
         // add new hash to the browser location
        // window.location.href=newLocation;
      });*/

        $dd.css('background-color','#ccffaa');
        $dd.prev().css('background-color','#ccffaa');
      // cancel default click action
 

    }
    
    /*   $(".editable").live("click",function(){if ($(this).val() == defaultString) $(this).val("");});
        $("dd").live("mouseover",function(){$(".live-edit").remove();
            $(this).append("<a class=\"live-edit\" onclick=\"javascript:LiveEdit(this);\">editar</span>");});
           //TODO: Añadirlo sólo a los editables
        */
      /*
        $('.editable').inlineEdit({
            buttonText: 'Guardar',
            save: function(e, data) {
              return confirm('Change name to '+ data.value +'?');
            }
          });*/

        $("#syntax-action").click(function(){
	      $("#syntax-list").slideToggle("slow");
	      $(this).toggleClass("active");
	      return null;
	    });
    });

String.prototype.replaceAll=function(s1, s2) {return this.split(s1).join(s2)}

    function empty(myString) {
        return (myString == undefined || myString == "");
    }
    function Edit() 
    {
        $(".editable").each(function(){
            var self = $(this);
             self.bind('click', function(event) {
                $(this).addClass("modified");
             });
        /*if (empty($(this).val())) {
            $(this).addClass("default").val(defaultString);                   
        }
        else {
            $(this).addClass("default");
        }
        $("#" + this.id + "-editor").hide();*/
        $("dd").addClass("editing");
        $("#entityName").addClass("editing");
      });
      $(".shortcut").css("display", "block");
      $("#edit-action").hide();
      $("#syntax-action").show();
      $("#save-action").show();
      
    }
    
    function LiveEdit(node)
    {
    $(node).siblings(".editable").each(function(){
        if (empty($(this).val())) {
            $(this).addClass("default").val(defaultString);                   
        }
        else {
            $(this).addClass("default");
        }
        $("#" + this.id + "-editor").hide();        
      });      
      $(".action").toggle();      
    }
    
    function Save() {
        /*$tempPost = $("form#form-save").clone();
        $tempPost.children(".editable").each(function(){
                if (empty($(this).val()) || $(this).val() == defaultString) {
                    $(this).val("")
                }});*/
        $("#save-action").html("Guardando...");
         $.ajax({
            type: "POST",
            url: document.getElementById("form-save").action,
            async: false,
            data: $("form#form-save .modified").serialize(),
            success: function(msg) {
                location.reload(true);
                $("#saving-error").hide();
            },
            error: function(XMLHttpRequest, textStatus, errorThrown) {
                var msg;
                if (XMLHttpRequest.responseText != null && XMLHttpRequest.responseText != undefined) {
                    var startError = XMLHttpRequest.responseText.indexOf("<title>");
                    var endError = XMLHttpRequest.responseText.indexOf("</title>");
                    if ((startError != -1) && (endError != -1))
                        msg = XMLHttpRequest.responseText.substring(startError + 7, endError);
                    else
                        msg = XMLHttpRequest.responseText;
                }         
                $("#saving-error .text").html(msg);
                $("#saving-error").show();
            }
         });
    }


    function ModeView() {
        $(".editable").each(function(){
            $editor=$("#" + this.id + "-editor");
            if (this.tagName.toLowerCase() == "textarea") 
            {
                $editor.html($(this).val().replaceAll("\n", "<p>"));
            }
            else
                $editor.html($(this).val());
           // $(this).removeClass("default");
            //if ($(this).val() == defaultString) $(this).val(""); 
          //  $editor.show();
         });
         $("dd").removeClass("editing");
        $("#entityName").removeClass("editing");
           
        $("#save-action").html("Guardar");              
      $("#edit-action").show();
      $("#syntax-action").hide();
      $("#save-action").hide();
      $(".shortcut").hide();
      $("#saving-error").hide();
      
    }    
</script>
<div class="grid_16" id="syntax-help">
    <div id="syntax-list">
        Utiliza la siguiente sintaxis:
        <h2>Textos</h2>
        <table>
            <tr><td><b>Texto en negrita</b></td><td>'''Texto en negrita'''</td></tr>
            <tr><td><i>Texto en cursiva</i></td><td>''Texto en cursiva''</td></tr>
            <tr><td><u>Texto subrayado</u></td><td>_Texto subrayado_</td></tr>
            <tr><td><s>Texto tachado</s></td><td>-Texto tachado-</td></tr>
        </table>
        <h2>Listas</h2>
        <table>
            <tr><td><ul><li>Elemento no numerado de lista</li><li>Otro elemento</li></ul></td><td>* Elemento no numerado de lista<br />* Otro elemento</td></tr>
            <tr><td><ol><li>Elemento numerado de lista</li><li>Otro elemento</li></ol></td><td># Elemento numerado de lista <br /># Otro elemento</td></tr>
        </table>
        <h2>Enlaces</h2>
        <table>
            <tr><td><a href="#">Enlace a entidad</a></td><td>[e:EntidadDN]</td></tr>
            <tr><td><a href="#">Enlace a propiedad</a></td><td>[p:EntidadDN.Propiedad]</td></tr>
            <tr><td><a href="#">Enlace a consulta</a></td><td>[q:EntidadQuery.ObtenerDatos]</td></tr>                        
            <tr><td><a href="#">Enlace a operacion</a></td><td>[o:EntidadOperation.Crear]</td></tr>
            <tr><td><a href="#">Enlace a espacio de nombres</a></td><td>[n:Negocio.Entities.Bancos]</td></tr>            
            <tr><td><a href="http://www.google.es">http://www.google.es</a></td><td>[h:http://www.google.es]</td></tr>
            <tr><td><a href="#">Enlace relativo a wiki</a></td><td>[w:Portada]</td></tr>
        </table>
        Los enlaces admiten un parámetro adicional con el texto que se mostrará en el enlace. Ejemplo: <a href="http://www.google.es">Web de Google</a> con [h:http://www.google.es|Web de Google]
        <h2>Imágenes</h2>
        <table>
        <tr><td>Insertar imagen</td><td>[imageright|Pie de foto|imagen.jpg] o [imageright|imagen.jpg] (Opciones imageright, imageleft, imagecentre, imageauto)</td></tr>                        
        </table>
        <h2>Títulos</h2>
        <table>
            <tr><td><h2>Título nivel 2</h2></td><td>==Título nivel 2==</td></tr>
            <tr><td><h3>Título nivel 3</h3></td><td>===Título nivel 3===</td></tr>
            <tr><td><h4>Título nivel 4</h4></td><td>====Título nivel 4====</td></tr>
            <tr><td><h5>Título nivel 5</h5></td><td>=====Título nivel 5=====</td></tr>
        </table>
        Los enlaces admiten un parámetro adicional con el texto que se mostrará en el enlace. Ejemplo: <a href="http://www.google.es">Web de Google</a> con [h:http://www.google.es|Web de Google]
    </div>
</div>
<div class="clear"></div>
<div class="grid_12" style="min-height:1px;">
    <div id="saving-error"><img src="Images/Help/icon-warning.png" /><span class="text"></span></a></div> 
</div>
<div class="grid_4">
   <!-- <a id="refresh" href="javascript:location.reload(true);">Refresca la página para wikificar correctamente el texto modificado</a> -->
    <a id="edit-action" class="action" href="javascript:Edit();">Editar</a>
    <a id="syntax-action" class="action" style="display: none">Sintaxis</a>    
    <a id="save-action" class="action" href="javascript:Save();" style="display: none">Guardar</a>    
</div>
<div class="clear"></div>