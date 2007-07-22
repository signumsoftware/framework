Public Class LNC

    ''' <summary>
    ''' Rellena el contenido de la carta del Talón a partir del contenido de la PlantillaCarta.
    ''' Establece ByRef la carta (establece la Huella y el ContenedorRTF) y además hace los reemplazos
    ''' necesarios para la carta.
    ''' </summary>
    ''' <param name="Talon">El Talón cuya carta va a ser generada.</param>
    ''' <remarks></remarks>
    Public Overloads Shared Sub GenerarTextoCartaConPlantilla(ByRef Talon As FN.GestionPagos.DN.TalonDN)
        GenerarTextoCartaConPlantilla(Nothing, Talon)
    End Sub


    ''' <summary>
    ''' Rellena el contenido de la carta del Talón a partir del contenido de la PlantillaCarta.
    ''' Establece ByRef la carta (establece la Huella y el ContenedorRTF) y además hace los reemplazos
    ''' necesarios para la carta.
    ''' </summary>
    ''' <param name="pPlantilla">La plantilla que se va a usar para el reemplazo.
    ''' Si está vacío, usará la plantilla que tenga el Talón.</param>
    ''' <param name="Talon">El Talón cuya carta va a ser generada.</param>
    ''' <remarks></remarks>
    Public Overloads Shared Sub GenerarTextoCartaConPlantilla(ByVal pPlantilla As FN.GestionPagos.DN.PlantillaCartaDN, ByRef Talon As FN.GestionPagos.DN.TalonDN)
        If pPlantilla Is Nothing Then
            pPlantilla = Talon.PlantillaCarta
        End If

        'cargamos la huella de la plantilla para asegurarnos de que tiene el texto
        LNC.CargarHuella(pPlantilla.HuellaRTF)

        'creamos una nueva huellaRTF con su ContenedorRTF a partir
        'del contenedorRTF de la plantilla
        Talon.HuellaRTF = New FN.GestionPagos.DN.HuellaContenedorRTFDN(New FN.GestionPagos.DN.ContenedorRTFDN(CType(pPlantilla.HuellaRTF.EntidadReferida, FN.GestionPagos.DN.ContenedorRTFDN).RTF))

        'ahora pasamos por los reemplazos
        LNC.PasarTextoPorReemplazos(Talon)
    End Sub

    ''' <summary>
    ''' Carga en la Huella que nos pasan la EntidadReferida
    ''' </summary>
    ''' <param name="pHuella">la huella en la que se quiere cargar la Entidad Referida</param>
    ''' <remarks></remarks>
    Public Shared Sub CargarHuella(ByRef pHuella As Framework.DatosNegocio.HEDN)
        If (Not pHuella Is Nothing) AndAlso (Not pHuella.TipoEntidadReferida Is Nothing) AndAlso _
        (Not String.IsNullOrEmpty(pHuella.IdEntidadReferida)) AndAlso _
        (pHuella.EntidadReferida Is Nothing) Then

            Dim mias As New Framework.AS.DatosBasicosAS

            Dim miobj As Object = mias.RecuperarGenerico(pHuella)

            pHuella.EntidadReferida = miobj
        End If
    End Sub

    Public Shared Function RecuperarConfiguracionImpresion(ByVal pId As String) As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
        Dim mias As New Framework.AS.DatosBasicosAS

        Return mias.RecuperarGenerico(pId, GetType(FN.GestionPagos.DN.ConfiguracionImpresionTalonDN))

    End Function

    Public Shared Function RecuperarPlantillaCarta(ByVal pId As String) As FN.GestionPagos.DN.PlantillaCartaDN
        Dim mias As New Framework.AS.DatosBasicosAS

        Return mias.RecuperarGenerico(pId, GetType(FN.GestionPagos.DN.PlantillaCartaDN))
    End Function

    Public Shared Function RecuperarTodasConfiguracionesImpresion() As List(Of FN.GestionPagos.DN.ConfiguracionImpresionTalonDN)
        Dim mias As New Framework.AS.DatosBasicosAS

        Dim milista As New List(Of FN.GestionPagos.DN.ConfiguracionImpresionTalonDN)

        Dim arr As ArrayList = mias.RecuperarListaTipos(GetType(FN.GestionPagos.DN.ConfiguracionImpresionTalonDN))

        If Not arr Is Nothing Then
            For Each tmp As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN In arr
                milista.Add(tmp)
            Next
        End If


        Return milista
    End Function

    Public Shared Function RecuperarTodosReemplazos() As List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN)
        Dim mias As New Framework.AS.DatosBasicosAS

        Dim milista As New List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN)

        Dim tmp As ArrayList = mias.RecuperarListaTipos(GetType(FN.GestionPagos.DN.ReemplazosTextoCartasDN))

        If Not tmp Is Nothing Then
            For Each objeto As FN.GestionPagos.DN.ReemplazosTextoCartasDN In tmp
                milista.Add(objeto)
            Next
        End If

        Return milista
    End Function

    ''' <summary>
    ''' Reemplaza el texto de la carta del talon con todos los patrones de reemplazo
    ''' quehaya guardados en la bd
    ''' </summary>
    ''' <param name="pTalonDoc">El talon doc cuyo texto se quiere reemplazar</param>
    Public Shared Sub PasarTextoPorReemplazos(ByRef pTalonDoc As FN.GestionPagos.DN.TalonDocumentoDN)
        Dim milista As List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN) = RecuperarTodosReemplazos()

        For Each reempl As FN.GestionPagos.DN.ReemplazosTextoCartasDN In milista
            reempl.ReemplazarTexto(pTalonDoc)
        Next
    End Sub

    ''' <summary>
    ''' Reemplaza el texto de la carta del talon con todos los patrones de reemplazo
    ''' quehaya guardados en la bd
    ''' </summary>
    ''' <param name="pTalonDN"> talon doc cuyo texto se quiere reemplazar</param>
    Public Shared Sub PasarTextoPorReemplazos(ByRef pTalonDN As FN.GestionPagos.DN.TalonDN)
        Dim milista As List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN) = RecuperarTodosReemplazos()

        For Each reempl As FN.GestionPagos.DN.ReemplazosTextoCartasDN In milista
            reempl.ReemplazarTexto(pTalonDN)
        Next
    End Sub

    Public Shared Function GuardarTalonDoc(ByVal pTalonDoc As FN.GestionPagos.DN.TalonDocumentoDN) As FN.GestionPagos.DN.TalonDocumentoDN
        Dim mias As New GestionPagos.AS.PagosAS

        Return mias.GuardarTalonDoc(pTalonDoc)
    End Function

    Public Shared Function GuardarTalonDN(ByVal pTalonDN As FN.GestionPagos.DN.TalonDN) As FN.GestionPagos.DN.TalonDN
        Dim mias As New GestionPagos.AS.PagosAS

        Return mias.GuardarTalonDN(pTalonDN)
    End Function

    Public Shared Function GuardarConfiguracionImpresionTalon(ByVal pConfiguracionImpresion As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
        Dim mias As New GestionPagos.AS.PagosAS

        Return mias.GuardarConfiguracionImpresionTalon(pConfiguracionImpresion)
    End Function
End Class
