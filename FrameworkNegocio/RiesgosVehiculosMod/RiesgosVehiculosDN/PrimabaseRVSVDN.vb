Imports Framework.DatosNegocio


<Serializable()> _
Public Class PrimabaseRVSVDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN

    Protected mIRecSumiValorLN As Framework.Operaciones.OperacionesDN.IRecSumiValorLN ' este no debe guardarse en base de datos
    Protected mColPrimasBase As ColPrimaBaseRVDN

    Protected mCobertura As FN.Seguros.Polizas.DN.CoberturaDN
    Protected mValorCacheado As PrimaBaseRVDN ' este valor no debe guardarse en base de datos



    Public Overrides Function ToString() As String
        Return Me.mNombre & "(PrimabaseRVSVDN " & Me.mCobertura.Nombre & ")"
    End Function

    <RelacionPropCampoAtribute("mCobertura")> _
     Public Property Cobertura() As FN.Seguros.Polizas.DN.CoberturaDN

        Get
            Return mCobertura
        End Get

        Set(ByVal value As FN.Seguros.Polizas.DN.CoberturaDN)
            CambiarValorRef(Of FN.Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)

        End Set
    End Property

    <RelacionPropCampoAtribute("mColPrimasBase")> _
    Public Property ColPrimasBase() As ColPrimaBaseRVDN

        Get
            Return mColPrimasBase
        End Get

        Set(ByVal value As ColPrimaBaseRVDN)
            CambiarValorRef(Of ColPrimaBaseRVDN)(value, mColPrimasBase)

        End Set
    End Property
    Public Function RecuperarModelodatos() As ModeloDatosDN
        ' si un modelo cambia de categoria y la nueva categoria perjudica a un cliente en el momento de su renovacion
        ' el cleinte tine que conservar la categoria que tubiera antes de la renovacion


        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is ModeloDatosDN Then
                Return o
            End If

        Next
        Return Nothing

    End Function

    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor



        Dim mitarif As FN.Seguros.Polizas.DN.TarifaDN = RecupearTarifa()
        If mitarif Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("tarifa no puede ser nothing para PrimabaseRVSVDN")
        End If

        If mitarif.RecuperarCobertura(Me.mCobertura.GUID) Is Nothing Then
            ' dado que los productos no continen esta cobertura este modulador no debe contar y devuelve 0 como valor neutro de coeficiente
            Return 0
        End If


        ' verificar si el riesgo de la tarifa es una moto, 
        ' no --> excepción porque debiera de serlo para que este grafo sea aplicable
        ' si --> buscar la categoria en base a los datos del riesgo (modelo y matriculado)

        Dim miRiesgoMotor As FN.RiesgosVehiculos.DN.RiesgoMotorDN
        If TypeOf mitarif.Riesgo Is FN.RiesgosVehiculos.DN.RiesgoMotorDN Then
            miRiesgoMotor = mitarif.Riesgo
        End If

        If miRiesgoMotor Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el riesgo debiera ser un riesgomotor")
        End If


        Dim miModeloDatos As ModeloDatosDN = Me.RecuperarModelodatos
        If miModeloDatos Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el ModeloDatos debiera ser un riesgomotor")
        End If



        ' debe de buscar en su coleccion de primas base aquella que coincide con la categoria 
        '   Dim ColPrimaBaseRV As ColPrimaBaseRVDN = mColPrimasBase.Recuperar(miRiesgoMotor.Modelo, miRiesgoMotor.Matriculado, mitarif.FEfecto)
        Dim ColPrimaBaseRV As ColPrimaBaseRVDN = mColPrimasBase.Recuperar(miModeloDatos, mitarif.FEfecto)

        Dim pb As PrimaBaseRVDN
        Select Case ColPrimaBaseRV.Count

            Case Is = 0
                Throw New ApplicationException("al menos se deberia haber recuperado una primabase activa")

            Case Is = 1
                pb = ColPrimaBaseRV.Item(0)
            Case Is > 1
                Throw New ApplicationException("solo se deberia haber recuperado una primabase activa")
        End Select


        If pb Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debiera existir una prima base para la categoria buscada")
        End If

        If pb.Cobertura.GUID <> Me.mCobertura.GUID Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la cobertura de la primabase y del recuperador de valor debieran ser iguales")
        End If

        Dim valor As Double = pb.Importe

        'Debug.WriteLine(pb.Nombre & " -- " & valor)

        mValorCacheado = pb

        Return valor

    End Function



    Private Function RecupearTarifa() As FN.Seguros.Polizas.DN.TarifaDN
        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is FN.Seguros.Polizas.DN.TarifaDN Then
                Return o
            End If

        Next
        Return Nothing
    End Function

    Public Property IRecSumiValorLN() As Framework.Operaciones.OperacionesDN.IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get
            Return mIRecSumiValorLN
        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)
            mIRecSumiValorLN = value
        End Set
    End Property

    Public ReadOnly Property ValorCacheado() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.ValorCacheado
        Get
            Return mValorCacheado
        End Get
    End Property

    Public Function RecuperarOrden() As Integer Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

End Class




<Serializable()> _
Public Class ColPrimabaseRVSVDN
    Inherits ArrayListValidable(Of PrimabaseRVSVDN)


    Public Function RecuperarxNombreCobertura(ByVal pNombreCobertura As String) As PrimabaseRVSVDN

        For Each pbrv As PrimabaseRVSVDN In Me

            If pbrv.Cobertura.Nombre = pNombreCobertura Then
                Return pbrv
            End If


        Next

        Return Nothing
    End Function





    Public Function RecuperarColPrimaBaseRVDN() As ColPrimaBaseRVDN

        Dim col As New ColPrimaBaseRVDN
        For Each pbrv As PrimabaseRVSVDN In Me

            col.AddRangeObjectUnico(pbrv.ColPrimasBase)
        Next

        Return col

    End Function

    Public Function RecuperarColCategoriasDN() As ColCategoriaModDatosDN

        Dim col As New ColCategoriaModDatosDN

        For Each pbrv As PrimabaseRVSVDN In Me
            col.AddRangeObjectUnico(pbrv.ColPrimasBase.RecuperarColCategoriasDN())
        Next

        Return col

    End Function


    Public Function RecuperarColCoberturaDN() As FN.Seguros.Polizas.DN.ColCoberturaDN

        Dim col As New FN.Seguros.Polizas.DN.ColCoberturaDN
        For Each pbrv As PrimabaseRVSVDN In Me

            col.AddRangeObjectUnico(pbrv.ColPrimasBase.RecuperarColCoberturaDN)

        Next

        Return col

    End Function


End Class




