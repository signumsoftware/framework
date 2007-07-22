Imports Framework.DatosNegocio

<Serializable()> _
Public Class ModuladorRVDN
    Inherits Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN

    'Protected mValor As Double
    Protected mCobertura As Seguros.Polizas.DN.CoberturaDN
    Protected mCategoriaModDatos As CategoriaModDatosDN
    Protected mCategoria As CategoriaDN
    Protected mCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
    Protected mModulador As ModuladorDN
    Protected mValorCacheado As Double


#Region "Propiedades"

    Public Property ValorCacheado() As Double
        Get
            Return mValorCacheado
        End Get
        Set(ByVal value As Double)
            mValorCacheado = value
        End Set
    End Property

    <RelacionPropCampoAtribute("mModulador")> _
    Public Property Modulador() As ModuladorDN

        Get
            Return mModulador
        End Get

        Set(ByVal value As ModuladorDN)
            CambiarValorRef(Of ModuladorDN)(value, mModulador)

        End Set
    End Property

    <RelacionPropCampoAtribute("mCaracteristica")> _
    Public Property Caracteristica() As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        Get
            Return mCaracteristica
        End Get

        Set(ByVal value As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)
            CambiarValorRef(Of Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)(value, mCaracteristica)
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

    <RelacionPropCampoAtribute("mCobertura")> _
    Public Property Cobertura() As Seguros.Polizas.DN.CoberturaDN
        Get
            Return mCobertura
        End Get
        Set(ByVal value As Seguros.Polizas.DN.CoberturaDN)
            Me.CambiarValorRef(Of Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)
        End Set
    End Property

#End Region


#Region "Métodos"

    Public Overrides Function TraducirValor(ByVal pvalor As Object) As Object
        mValorCacheado = MyBase.TraducirValor(pvalor)
        Return mValorCacheado
    End Function

    Public Overrides Function ToString() As String
        Return Me.mCobertura.Nombre & " " & mCategoriaModDatos.Nombre & " " & mCaracteristica.Nombre

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mCategoria Is Nothing OrElse mCategoriaModDatos Is Nothing Then
            pMensaje = "Categoría y CategoriaModDatos de la prima base no pueden ser nulos"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCategoria.GUID <> mCategoriaModDatos.Categoria.GUID Then
            pMensaje = "Categoría y CategoriaModDatos no son consistentes"
            Return EstadoIntegridadDN.Inconsistente
        End If

        ' todas los valores deben pertenercer a la misma caracteristica

        For Each valorInter As Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN In Me.mColValorIntervalNumMapDN
            If valorInter.Caracteristica.GUID <> Me.mCaracteristica.GUID Then

                pMensaje = "Todos los elementos de mColValorIntervalNumMapDN  deben estar fijados contra la misma caractristica que el modulador"
                Return EstadoIntegridadDN.Inconsistente
            End If

        Next

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class



<Serializable()> _
Public Class ColModuladorRVDN
    Inherits ArrayListValidable(Of ModuladorRVDN)

    'Public Function Recuperar(ByVal cobertura As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pModelo As ModeloDN, ByVal pMatriculado As Boolean, ByVal pCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN, ByVal pFecha As Date) As ModuladorRVDN

    '    For Each moduladorrv As ModuladorRVDN In Me
    '        If moduladorrv.Cobertura.GUID = cobertura.GUID AndAlso moduladorrv.Caracteristica.GUID = pCaracteristica.GUID AndAlso moduladorrv.CategoriaModDatos.ContieneModeloDatos(pModelo, pMatriculado, pFecha) Then
    '            'TODO: Verificar esta linea, la he comentado porque no parece hacer nada
    '            'Dim modelodatos As ModeloDatosDN = moduladorrv.Categoria.RecupearModeloDatos(pModelo, pMatriculado)
    '            Return moduladorrv
    '        End If
    '    Next

    '    Return Nothing

    'End Function


    Public Function Recuperar(ByVal cobertura As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pModeloDatos As ModeloDatosDN, ByVal pCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN) As ModuladorRVDN

        For Each moduladorrv As ModuladorRVDN In Me
            If moduladorrv.Cobertura.GUID = cobertura.GUID AndAlso moduladorrv.Caracteristica.GUID = pCaracteristica.GUID AndAlso moduladorrv.CategoriaModDatos.ColModelosDatos.Contiene(pModeloDatos, CoincidenciaBusquedaEntidadDN.Todos) Then
                Return moduladorrv
            End If
        Next

        Return Nothing

    End Function
    Public Function Recuperar(ByVal cobertura As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pModelo As ModeloDN, ByVal pMatriculado As Boolean, ByVal pFecha As Date) As ModuladorRVDN

        For Each moduladorrv As ModuladorRVDN In Me
            If moduladorrv.Cobertura.GUID = cobertura.GUID Then
                Dim modelodatos As ModeloDatosDN = moduladorrv.CategoriaModDatos.RecuperarModeloDatos(pModelo, pMatriculado, pFecha)
                If modelodatos IsNot Nothing Then
                    Return moduladorrv
                End If
            End If
        Next

        Return Nothing

    End Function

    Public Function Recuperar(ByVal cobertura As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pCategoria As FN.RiesgosVehiculos.DN.CategoriaDN, ByVal pCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN) As ModuladorRVDN

        For Each moduladorrv As ModuladorRVDN In Me
            If moduladorrv.Cobertura.GUID = cobertura.GUID AndAlso moduladorrv.CategoriaModDatos.Categoria.GUID = pCategoria.GUID AndAlso moduladorrv.Caracteristica.GUID = pCaracteristica.GUID Then
                Return moduladorrv
            End If
        Next

        Return Nothing

    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN) As ColModuladorRVDN

        Dim col As New ColModuladorRVDN

        For Each moduladorrv As ModuladorRVDN In Me

            If moduladorrv.Cobertura.GUID = pCoberturta.GUID Then
                col.Add(moduladorrv)
            End If

        Next

        Return col


    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN) As ColModuladorRVDN

        Dim col As New ColModuladorRVDN

        For Each moduladorrv As ModuladorRVDN In Me

            If moduladorrv.Cobertura.GUID = pCoberturta.GUID AndAlso moduladorrv.Caracteristica.GUID = pCaracteristica.GUID Then
                col.Add(moduladorrv)
            End If

        Next

        Return col


    End Function

    Public Function RecuperarColValorIntervalNumMapDN() As Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN

        Dim col As New Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN

        For Each moduladorrv As ModuladorRVDN In Me
            col.AddRange(moduladorrv.ColValorIntervalNumMap)

        Next

        Return col


    End Function

End Class




