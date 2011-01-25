$(function(){initTooltips();});

function RefreshNotes(urlController, typeName){
    var info = EntityInfoFor('');
    var params =
        {
            sfRuntimeType: typeName,
            sfIdRelated: info.id(),
            sfRuntimeTypeRelated: info.runtimeType() 
        };
        
    $.post(urlController, params,
        function(data){
            $("a#Notas").parent().html(data);
            initTooltips("a#Notas");
    });
}

function RefreshAlerts(urlController, typeName){
    var info = EntityInfoFor('');
    var params =
        {
            sfRuntimeType: typeName,
            sfIdRelated: info.id(),
            sfRuntimeTypeRelated: info.runtimeType() 
        };
        
    $.post(urlController, params,
        function(data){
            $("a#Alertas").parent().html(data);
            initTooltips("a#Alertas");
    });
}

function RefreshDocumentos(urlController, typeName){
    var info = EntityInfoFor('');
    var params =
        {
            sfRuntimeType: typeName,
            sfIdRelated: info.id(),
            sfRuntimeTypeRelated: info.runtimeType() 
        };
        
    $.post(urlController, params,
        function(data){
            $("a#Documentos").parent().html(data);
            initTooltips("a#Documentos");
    });
}
   
function initTooltips(node){	      
    $((node ? node : "") + '.tooltipped').each(function(){
      
        var id=$(this).attr("for");
        if (!id) id = $(this).attr("id");
        $(this).qtip({
            textAlign: 'center',
            content: $("#tt"+id).html(),
            position: {
                corner: {
                    target: 'bottomMiddle',
                    tooltip: 'topMiddle'
                }
            },    
            style: { 
                name: 'light', // Inherit from preset style
                tip: 'topMiddle',
                 border: {
                     width: 1,
                     radius: 3
                  }
            },
            hide: {
                fixed:true
            }
        });
    });
}