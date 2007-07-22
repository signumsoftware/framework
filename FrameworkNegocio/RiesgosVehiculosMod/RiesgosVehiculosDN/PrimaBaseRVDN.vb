

Imports Framework.DatosNegocio
<Serializable()> _
Public Class PrimaBaseRVDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN
    Implements FN.Seguros.Polizas.DN.IPrimaBaseDN

    Protected mCobertura As Seguros.Polizas.DN.CoberturaDN
    Protected mImporte As Double
    Protected mCategoriaModDatos As CategoriaModDatosDN
    Protected mCategoria As CategoriaDN



#Region "Propiedades"

    <RelacionPropCampoAtribute("mCategoriaModDatos")> _
    Public Property CategoriaModDatos() As CategoriaModDatosDN
        Get
            Return mCategoriaModDatos
        End Get
        Set(ByVal value As CategoriaModDatosDN)
            CambiarValorRef(Of CategoriaModDatosDN)(value, mCategoriaModDatos)
        End Set
    End Property

    <RelacionPropCampoAtribute("mCobertura")> _
    Public Property Cobertura() As Seguros.Polizas.DN.CoberturaDN Implements Seguros.Polizas.DN.IPrimaBaseDN.Cobertura
        Get
            Return mCobertura
        End Get
        Set(ByVal value As Seguros.Polizas.DN.CoberturaDN)
            Me.CambiarValorRef(Of Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)
        End Set
    End Property

    Public Property Importe() As Double Implements Seguros.Polizas.DN.IPrimaBaseDN.Importe
        Get
            Return mImporte
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, mImporte)
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

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Return Me.mNombre & "(" & Me.mCategoria.Nombre & " a " & Me.mCobertura.Nombre & " en " & Me.mPeriodo.ToString & "  )"
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

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


#End Region


End Class





<Serializable()> _
Public Class ColPrimaBaseRVDN
    Inherits ArrayListValidableEntTemp(Of PrimaBaseRVDN)

    'Public Function Recuperar(ByVal pModelo As ModeloDN, ByVal pMatriculado As Boolean, ByVal pFechaEfecto As Date) As ColPrimaBaseRVDN
    '    Dim col As New ColPrimaBaseRVDN

    '    For Each pb As PrimaBaseRVDN In Me
    '        If pb.Periodo.Contiene(pFechaEfecto) AndAlso pb.CategoriaModDatos.ColModelosDatos.RecupearModeloDatos(pModelo, pMatriculado, pFechaEfecto) IsNot Nothing Then
    '            col.Add(pb)
    '        End If

    '    Next
    '    Return col

    'End Function

    Public Function Recuperar(ByVal pModeloDatos As ModeloDatosDN, ByVal pFechaEfecto As Date) As ColPrimaBaseRVDN
        Dim col As New ColPrimaBaseRVDN

        For Each pb As PrimaBaseRVDN In Me
            If pb.Periodo.Contiene(pFechaEfecto) AndAlso pb.CategoriaModDatos.ColModelosDatos.Contiene(pModeloDatos, CoincidenciaBusquedaEntidadDN.Todos) Then
                col.Add(pb)
            End If

        Next
        Return col

    End Function

    Public Function RecuperarxGUIDCategoria(ByVal pGUID As String) As PrimaBaseRVDN

        For Each pb As PrimaBaseRVDN In Me
            If pb.Categoria.GUID = pGUID Then
                Return pb
            End If

        Next
        Return Nothing

    End Function



    Public Function RecuperarColCoberturaDN() As FN.Seguros.Polizas.DN.ColCoberturaDN


        'hay que comprobar que sula correctamente
        'Throw New ApplicationException

        Dim micol As New FN.Seguros.Polizas.DN.ColCoberturaDN

        For Each pb As PrimaBaseRVDN In Me

            micol.AddUnico(pb.Cobertura)


        Next


        Return micol

    End Function

    Public Function RecuperarColCategoriasDN() As FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN
        Dim micol As New FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN

        For Each pb As PrimaBaseRVDN In Me
            micol.AddUnico(pb.CategoriaModDatos)
        Next

        Return micol

    End Function



    Public Function SeleccionarPCDeCobertura(ByVal pguidCobertura As String) As ColPrimaBaseRVDN

        Dim micol As New ColPrimaBaseRVDN

        For Each pb As PrimaBaseRVDN In Me

            If pb.Cobertura.GUID = pguidCobertura Then
                micol.Add(pb)
            End If
        Next

        Return micol

    End Function




    Public Function SeleccionarX(ByVal cate As CategoriaModDatosDN, ByVal cob As FN.Seguros.Polizas.DN.CoberturaDN) As ColPrimaBaseRVDN

        Dim micol As New ColPrimaBaseRVDN

        For Each pb As PrimaBaseRVDN In Me

            If pb.Cobertura.GUID = cob.GUID AndAlso pb.Categoria.GUID = cate.GUID Then
                micol.Add(pb)
            End If
        Next

        Return micol

    End Function


    Public Function VerificarIntegridadColCompleta() As ColPrimaBaseRVDN
        ' no pueden haber dos primas base  activos solapados  para el mismo categoria y cobertura


        Dim colcate As ColCategoriaModDatosDN = Me.RecuperarColCategoriasDN
        Dim colCobertura As FN.Seguros.Polizas.DN.ColCoberturaDN = Me.RecuperarColCoberturaDN


        For Each cob As FN.Seguros.Polizas.DN.CoberturaDN In colCobertura
            For Each cate As CategoriaModDatosDN In colcate

                Dim ColPrimaBaseRV As ColPrimaBaseRVDN = Me.SeleccionarX(cate, cob)
                Dim ColIntervaloFechas As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN
                ColIntervaloFechas.AddRangeObject(RecuperarColPeridosFechas())

                Dim par As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos
                par = ColIntervaloFechas.PrimeroNoCumple(IntSolapadosOContenido.Libres)
                If Not par Is Nothing Then
                    Dim col As New ColPrimaBaseRVDN
                    col.AddRange(ColPrimaBaseRV.RecuperarXPar(par))
                    Return col
                End If

            Next
        Next

        Return New ColPrimaBaseRVDN





    End Function


    'Public Function RecuperarXPar(ByVal pPar As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos) As ColImpuestoRVDN
    '    Dim col As New 




    '    For Each pb As Framework.DatosNegocio.EntidadTemporalDN In Me

    '        If pb.Periodo Is pPar.Int1 OrElse pb.Periodo Is pPar.Int2 Then
    '            col.Add(pb)
    '        End If
    '    Next

    '    Return col
    'End Function
    'Public Function RecuperarColPeridosFechas() As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN
    '    Dim col As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN

    '    For Each pb As Framework.DatosNegocio.EntidadTemporalDN In Me

    '        col.Add(pb.Periodo)

    '    Next

    '    Return col
    'End Function


End Class


