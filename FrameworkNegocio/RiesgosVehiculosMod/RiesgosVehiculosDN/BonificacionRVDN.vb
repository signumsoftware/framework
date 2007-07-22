Imports Framework.DatosNegocio

<Serializable()> _
Public Class BonificacionRVDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "Atributos"

    Protected mCategoriaModDatos As CategoriaModDatosDN
    Protected mCategoria As CategoriaDN
    Protected mBonificacion As BonificacionDN
    Protected mValor As Double
    Protected mIntervaloNumerico As IntvaloNumericoDN

#End Region

#Region "Propiedades"

    Public Property Valor() As Double
        Get
            Return mValor
        End Get
        Set(ByVal value As Double)
            mValor = value
        End Set
    End Property

    <RelacionPropCampoAtribute("mBonificacion")> _
    Public Property Bonificacion() As BonificacionDN

        Get
            Return mBonificacion
        End Get

        Set(ByVal value As BonificacionDN)
            CambiarValorRef(Of BonificacionDN)(value, mBonificacion)

        End Set
    End Property

    <RelacionPropCampoAtribute("mCategoriaModDatos")> _
    Public Property CategoriaModDatos() As CategoriaModDatosDN
        Get
            Return mCategoriaModDatos
        End Get

        Set(ByVal value As CategoriaModDatosDN)
            CambiarValorRef(Of CategoriaModDatosDN)(value, mCategoriaModDatos)
        End Set
    End Property

    <RelacionPropCampoAtribute("mCategoria")> _
    Public Property Categoria() As CategoriaDN
        Get
            Return mCategoria
        End Get

        Set(ByVal value As CategoriaDN)
            CambiarValorRef(Of CategoriaDN)(value, mCategoria)
        End Set
    End Property

    <RelacionPropCampoAtribute("mIntervaloNumerico")> _
    Public Property IntervaloNumerico() As IntvaloNumericoDN
        Get
            Return mIntervaloNumerico
        End Get
        Set(ByVal value As IntvaloNumericoDN)
            CambiarValorRef(Of IntvaloNumericoDN)(value, mIntervaloNumerico)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Return mNombre & " " & mCategoriaModDatos.Nombre

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mBonificacion Is Nothing Then
            pMensaje = "El objeto BonificacionDN no puede ser nulo para la BonificacionRVDN"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCategoria Is Nothing OrElse mCategoriaModDatos Is Nothing Then
            pMensaje = "Categoría y CategoriaModDatos de la prima base no pueden ser nulos"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCategoria.GUID <> mCategoriaModDatos.Categoria.GUID Then
            pMensaje = "Categoría y CategoriaModDatos no son consistentes"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class ColBonificacionRVDN
    Inherits ColEntidadTemporalBaseDN(Of BonificacionRVDN)

#Region "Métodos"


    Public Function RecuperarXPar(ByVal pPar As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos) As ColBonificacionRVDN
        Dim col As New ColBonificacionRVDN

        For Each bonRV As BonificacionRVDN In Me

            If bonRV.Periodo Is pPar.Int1 OrElse bonRV.Periodo Is pPar.Int2 Then
                col.Add(bonRV)
            End If
        Next

        Return col
    End Function

    Public Function RecuperarColPeriodosFechas() As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN
        Dim col As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN

        For Each bonRV As BonificacionRVDN In Me
            col.Add(bonRV.Periodo)
        Next

        Return col
    End Function

    Public Function RecuperarBonificaciones() As ColBonificacionDN
        Dim col As New ColBonificacionDN

        For Each bonRV As BonificacionRVDN In Me
            col.AddUnico(bonRV.Bonificacion)
        Next

        Return col
    End Function

    'Public Function RecuperarBonificaciones(ByVal bonificacion As FN.RiesgosVehiculos.DN.BonificacionDN, ByVal valorBonificacion As Double, ByVal modelo As ModeloDN, ByVal matriculado As Boolean, ByVal fecha As Date) As ColBonificacionRVDN
    '    Dim col As New ColBonificacionRVDN()

    '    If bonificacion Is Nothing OrElse modelo Is Nothing Then
    '        Return Nothing
    '    End If

    '    For Each bonRV As BonificacionRVDN In Me
    '        If bonRV.Bonificacion.GUID = bonificacion.GUID AndAlso bonRV.CategoriaModDatos.ContieneModeloDatos(modelo, matriculado, fecha) AndAlso bonRV.IntervaloNumerico.Contiene(valorBonificacion) AndAlso bonRV.Periodo.Contiene(fecha) Then
    '            col.Add(bonRV)
    '        End If
    '    Next

    '    Return col
    'End Function

    Public Function RecuperarBonificaciones(ByVal bonificacion As FN.RiesgosVehiculos.DN.BonificacionDN, ByVal valorBonificacion As Double, ByVal pModelodatos As ModeloDatosDN, ByVal fecha As Date) As ColBonificacionRVDN
        Dim col As New ColBonificacionRVDN()

        If bonificacion Is Nothing OrElse pModelodatos Is Nothing Then
            Return Nothing
        End If

        For Each bonRV As BonificacionRVDN In Me
            If bonRV.Bonificacion.GUID = bonificacion.GUID AndAlso bonRV.CategoriaModDatos.ColModelosDatos.Contiene(pModelodatos, CoincidenciaBusquedaEntidadDN.Todos) AndAlso bonRV.IntervaloNumerico.Contiene(valorBonificacion) AndAlso bonRV.Periodo.Contiene(fecha) Then
                col.Add(bonRV)
            End If
        Next



        Return col
    End Function

    Public Function RecuperarBonificaciones(ByVal bonificacion As FN.RiesgosVehiculos.DN.BonificacionDN, ByVal categoria As CategoriaDN) As ColBonificacionRVDN
        Dim col As New ColBonificacionRVDN()

        If bonificacion Is Nothing OrElse categoria Is Nothing Then
            Return Nothing
        End If

        For Each bonRV As BonificacionRVDN In Me
            If bonRV.Bonificacion.GUID = bonificacion.GUID AndAlso bonRV.Categoria.GUID = categoria.GUID Then
                col.Add(bonRV)
            End If
        Next

        Return col
    End Function

    Public Function RecuperarXValorBonificacion(ByVal valorBonificacion As Double) As ColBonificacionRVDN
        Dim colBonifRV As New ColBonificacionRVDN()

        For Each bonifRV As BonificacionRVDN In Me
            If bonifRV.IntervaloNumerico.Contiene(valorBonificacion) Then
                colBonifRV.Add(bonifRV)
            End If
        Next

        Return colBonifRV
    End Function


    Public Function SeleccionarX(ByVal bonificacion As FN.RiesgosVehiculos.DN.BonificacionDN) As ColBonificacionRVDN
        Dim col As New ColBonificacionRVDN

        For Each bonRV As BonificacionRVDN In Me
            If bonRV.Bonificacion.GUID = bonificacion.GUID Then
                col.Add(bonRV)
            End If
        Next

        Return col
    End Function

#End Region

End Class