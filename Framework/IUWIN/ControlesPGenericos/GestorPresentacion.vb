Imports System.Windows.Forms
Imports Framework.DatosNegocio.Arboles
Imports Framework.DatosNegocio


Public Interface IGestorPresentacion


    ''' <summary>
    ''' Carga por referencia los datos que se quieren mostrar
    ''' </summary>
    ''' <param name="pObjeto">El objeto que se va a mostrar</param>
    ''' <param name="TextoSalida">El texto que se va a asignar al elemento en el control</param>
    ''' <param name="ImagenSalida">La imagen que se va a dibujar junto al elemento en el control</param>
    Sub GenerarElemento(ByVal pObjeto As IEntidadBaseDN, ByRef TextoSalida As String, ByRef ImagenSalida As System.Drawing.Image)




End Interface

