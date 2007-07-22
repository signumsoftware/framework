'Public Class ProveedorImagenes


'    Protected Shared mHTBitmap As New Hashtable


'    Public Shared Function ObtenerImagen(ByVal pNombreImagen As String) As Bitmap

'        'Dim rutaCarpetaImagenes, rutaAbsoluta As String
'        'If Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey("RutaCargaIconos") Then
'        '    rutaCarpetaImagenes = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("RutaCarpetaIconos")
'        'Else
'        '    rutaCarpetaImagenes = ""
'        'End If


'        'rutaAbsoluta = rutaCarpetaImagenes & "\" & pNombreImagen


'        If String.IsNullOrEmpty(pNombreImagen) Then

'        Else
'            If mHTBitmap.ContainsKey(pNombreImagen) Then
'                Return mHTBitmap.Item(pNombreImagen)
'            Else
'                Dim miBitmap As Bitmap = CargarImagenDisco(pNombreImagen)
'                mHTBitmap.Add(pNombreImagen, miBitmap)
'                Return miBitmap
'            End If

'        End If




'    End Function



'    Private Shared Function CargarImagenDisco(ByVal pNombreImagen As String) As Bitmap


'        Dim directorioDeImagenes As String = Application.StartupPath & "\Icos"
'        If Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey("RutaCargaIconos") Then
'            directorioDeImagenes = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("RutaCargaIconos")
'        End If
'        Dim imagenesACargar As String = pNombreImagen & "*"
'        Dim carpeta As New IO.DirectoryInfo(directorioDeImagenes)
'        Dim imagenes As IO.FileInfo()
'        Try
'            imagenes = carpeta.GetFiles(imagenesACargar)
'        Catch ex As Exception
'            Debug.WriteLine(ex)
'        End Try

'        If imagenes IsNot Nothing AndAlso imagenes.Length > 0 Then
'            Return New Bitmap(imagenes(0).FullName)
'        Else
'            Return Nothing
'        End If





'    End Function

'End Class
