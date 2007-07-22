<Serializable()> Public Class LocalidadDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"
    Protected mProvincia As ProvinciaDN
    Protected mColCodigoPostal As ColCodigoPostalDN
#End Region

#Region "Constructores"

    Public Sub New()
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pProvincia As ProvinciaDN)
        Me.mNombre = pNombre
        Me.CambiarValorRef(Of ProvinciaDN)(pProvincia, Me.mProvincia)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property ToCadena() As String
        Get
            Return Me.mNombre & " " & mProvincia.ToCadena
        End Get
    End Property

    Public Property Provincia() As ProvinciaDN
        Get
            Return Me.mProvincia
        End Get
        Set(ByVal value As ProvinciaDN)
            Me.CambiarValorRef(Of ProvinciaDN)(value, Me.mProvincia)
        End Set
    End Property

    Public Property ColCodigoPostal() As ColCodigoPostalDN
        Get
            Return mColCodigoPostal
        End Get
        Set(ByVal value As ColCodigoPostalDN)
            CambiarValorCol(Of ColCodigoPostalDN)(value, mColCodigoPostal)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarProvincia(ByRef mensaje As String, ByVal provincia As ProvinciaDN) As Boolean
        If provincia Is Nothing Then
            mensaje = "La provincia no puede ser nula"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarColCodigoPostal(ByRef mensaje As String, ByVal colCodPostal As ColCodigoPostalDN) As Boolean
        If colCodPostal Is Nothing OrElse colCodPostal.Count = 0 Then
            mensaje = ""
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarProvincia(pMensaje, mProvincia) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarColCodigoPostal(pMensaje, mColCodigoPostal) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

<Serializable()> _
Public Class ColLocalidadDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of LocalidadDN)


    Public Function Recuperar(ByVal pais As String, ByVal Provincia As String, ByVal localidad As String) As LocalidadDN

        For Each loc As LocalidadDN In Me

            If loc.Nombre.ToLower = localidad.ToLower AndAlso loc.Provincia.Nombre.ToLower = Provincia.ToLower AndAlso loc.Provincia.Pais.Nombre.ToLower = pais.ToLower Then
                Return loc


            End If

        Next
        Return Nothing

    End Function

    Public Function RecuperarImpreciso(ByVal pais As String, ByVal Provincia As String, ByVal localidad As String) As Framework.DatosNegocio.EntidadDN


        Dim miProvincia As FN.Localizaciones.DN.ProvinciaDN = Nothing
        Dim miPais As PaisDN = Nothing


        For Each loc As LocalidadDN In Me

            If loc.Nombre.ToLower = localidad.ToLower AndAlso loc.Provincia.Nombre.ToLower = Provincia.ToLower AndAlso loc.Provincia.Pais.Nombre.ToLower = pais.ToLower Then
                Return loc
            End If

            If loc.Provincia.Nombre.ToLower = Provincia.ToLower AndAlso loc.Provincia.Pais.Nombre.ToLower = pais.ToLower Then
                miProvincia = loc.Provincia

            End If

            If loc.Provincia.Pais.Nombre.ToLower = pais.ToLower Then
                miPais = loc.Provincia.Pais
            End If

        Next

        If Provincia IsNot Nothing Then
            Return miProvincia
        End If


        If pais IsNot Nothing Then
            Return miPais
        End If

        Return Nothing

    End Function
End Class