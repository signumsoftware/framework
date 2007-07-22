Public Class RecuperadorMapeadoXFicheroXMLAD
    Implements IRecuperadorInstanciaMap
    Protected mRutaDirectorioMapeados As String
    Public Sub New()

    End Sub
    Public Sub New(ByVal pRutaDirectorioMapeados As String)
        mRutaDirectorioMapeados = pRutaDirectorioMapeados
    End Sub

    Public Property RutaDirectorioMapeados() As String
        Get
            Return Me.mRutaDirectorioMapeados
        End Get
        Set(ByVal value As String)
            Me.mRutaDirectorioMapeados = value
        End Set
    End Property
    Public Function RecuperarInstanciaMap(ByVal pNombreMapInstancia As String) As InstanciaMapDN Implements IRecuperadorInstanciaMap.RecuperarInstanciaMap

        Dim carpeta As IO.DirectoryInfo

        'Se comprueba si existe el directorio, como ruta completa, o como ruta relativa
        If IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() & mRutaDirectorioMapeados) Then
            mRutaDirectorioMapeados = IO.Directory.GetCurrentDirectory() & mRutaDirectorioMapeados
        Else
            carpeta = IO.Directory.CreateDirectory(mRutaDirectorioMapeados)
        End If

        Dim imap As InstanciaMapDN

        If IO.File.Exists(mRutaDirectorioMapeados & "\" & pNombreMapInstancia & ".xml") Then
            imap = New InstanciaMapDN
            Dim tr As New IO.StreamReader(mRutaDirectorioMapeados & "\" & pNombreMapInstancia & ".xml")
            imap.FromXML(tr)
            tr.Close()
            tr.Dispose()
        End If
        Return imap
    End Function

    Public Function RecuperarInstanciaMap(ByVal pTipo As System.Type) As InstanciaMapDN Implements IRecuperadorInstanciaMap.RecuperarInstanciaMap

        'If pTipo.IsInterface Then
        '    Return Nothing
        'End If

        Dim imap As InstanciaMapDN
        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsColeccion(pTipo) Then
            Dim nuevotipo As System.Type = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, Nothing)
            Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(nuevotipo)
            imap = RecuperarInstanciaMap(vc.NombreClase & "-BASICA-COL")

            'Return imap

            ' Return RecuperarInstanciaMap("IList-BASICA")

            'ElseIf pTipo.IsInterface Then

            '    Beep()



        Else
            Dim nuevotipo As System.Type = pTipo
            Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(nuevotipo)
            imap = RecuperarInstanciaMap(vc.NombreClase & "-BASICA-ENT")

            If imap Is Nothing Then

                Do While nuevotipo IsNot GetType(Object) AndAlso imap Is Nothing

                    nuevotipo = nuevotipo.BaseType
                    If nuevotipo Is GetType(Object) OrElse nuevotipo Is Nothing Then
                        'Beep()
                        Return Nothing
                    End If


                    vc = New Framework.TiposYReflexion.DN.VinculoClaseDN(nuevotipo)
                    imap = RecuperarInstanciaMap(vc.NombreClase & "-BASICA-ENT")
                Loop

            End If
        End If

        If imap Is Nothing Then
            'Beep()
        End If
        Return imap
    End Function
End Class
