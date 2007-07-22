Imports System.Drawing
Imports System.Windows.Forms
Imports Framework.DatosNegocio.Arboles
Imports Framework.DatosNegocio


Public Interface IGestorPresentacionArboles

    ''' <summary>
    ''' Carga las imágenes en el ImageList que se le pasa por refrencia
    ''' Debe invocarse antes el método CargarImagenes().
    ''' </summary>
    Sub ExponerImagenes(ByVal pImageList As ImageList)

    ''' <summary>
    ''' Este método carga las imágenes que vayan a ser utilizadas para generar
    ''' los elementos. Sólo es necesario utilizarlo en el caso de que se vaya 
    ''' a usar en un control que utilice ImageList
    ''' </summary>
    ''' <remarks></remarks>
    Sub CargarImagenes()

    Sub GenerarElementoParaImageList(ByVal pObjeto As Framework.DatosNegocio.IEntidadBaseDN, ByRef TextoSalida As String, ByRef KeyImagenSalida As String, ByRef KeyImagenSeleccionada As String)
End Interface


Public Class GestorPresentacionArbolesDefecto
    Implements IGestorPresentacionArboles


    Private mListImagenes As New List(Of System.Drawing.Image)
    Private mHashIndicesImagenes As New Dictionary(Of String, Image)


    Public Sub New()
        CargarImagenes()
    End Sub

    Public Sub ExponerImagenes(ByVal pImageList As System.Windows.Forms.ImageList) Implements IGestorPresentacionArboles.ExponerImagenes
        pImageList.Images.Clear()

        For Each mikeypair As KeyValuePair(Of String, Image) In Me.mHashIndicesImagenes
            pImageList.Images.Add(mikeypair.Key, mikeypair.Value)
        Next
    End Sub


    Public Overridable Sub CargarImagenes() Implements IGestorPresentacionArboles.CargarImagenes
        Me.mHashIndicesImagenes.Add("imagencarpeta", My.Resources.carpeta.ToBitmap)
        Me.mHashIndicesImagenes.Add("imagenhoja", My.Resources.hoja.ToBitmap)
        Me.mHashIndicesImagenes.Add("imagencarpetaseleccionada", My.Resources.carpetaseleccionada.ToBitmap)
        Me.mHashIndicesImagenes.Add("imagenhojaseleccionada", My.Resources.hojaseleccionada.ToBitmap)

    End Sub


    Public Overridable Sub GenerarElementoParaImageList(ByVal pObjeto As Framework.DatosNegocio.IEntidadBaseDN, ByRef TextoSalida As String, ByRef KeyImagenSalida As String, ByRef KeyImagenSeleccionada As String) Implements IGestorPresentacionArboles.GenerarElementoParaImageList
        If TypeOf pObjeto Is INodoDN Then
            'es un nodo (carpeta)
            KeyImagenSalida = "imagencarpeta"
            KeyImagenSeleccionada = "imagencarpetaseleccionada"
        Else
            'es una hoja
            KeyImagenSalida = "imagenhoja"
            KeyImagenSeleccionada = "imagenhojaseleccionada"
        End If
        TextoSalida = CType(pObjeto, Object).ToString
    End Sub


End Class

