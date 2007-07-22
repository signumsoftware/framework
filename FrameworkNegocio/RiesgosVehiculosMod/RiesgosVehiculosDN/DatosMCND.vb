Imports Framework

<Serializable()> _
Public Class DatosMCND
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mApellido1 As String
    Protected mApellido2 As String
    Protected mFechaNacimiento As DateTime
    Protected mNIF As FN.Localizaciones.DN.NifDN
    Protected mParentesco As ParentescoConductorAdicional

    Public Property Apellido1() As String
        Get
            Return Me.mApellido1
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mApellido1)
        End Set
    End Property

    Public Property Apellido2() As String
        Get
            Return Me.mApellido2
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mApellido2)
        End Set
    End Property

    Public Property FechaNacimiento() As DateTime
        Get
            Return Me.mFechaNacimiento
        End Get
        Set(ByVal value As DateTime)
            Me.CambiarValorVal(Of DateTime)(value, Me.mFechaNacimiento)
        End Set
    End Property

    Public Property Parentesco() As ParentescoConductorAdicional
        Get
            Return Me.mParentesco
        End Get
        Set(ByVal value As ParentescoConductorAdicional)
            Me.CambiarValorVal(Of ParentescoConductorAdicional)(value, Me.mParentesco)
        End Set
    End Property

    Public ReadOnly Property Edad() As Integer
        Get
            If Me.mFechaNacimiento <> DateTime.MinValue Then
                Return CInt(DateTime.Now.Subtract(Me.mFechaNacimiento).TotalDays / 365)
            Else
                Return 0
            End If
        End Get
    End Property

    Public Property NIF() As FN.Localizaciones.DN.NifDN
        Get
            Return mNIF
        End Get
        Set(ByVal value As FN.Localizaciones.DN.NifDN)
            CambiarValorRef(Of FN.Localizaciones.DN.NifDN)(value, mNIF)
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        If String.IsNullOrEmpty(Me.mNombre) Then
            pMensaje = "El nombre del conductor Adicional no puede ser nulo"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If String.IsNullOrEmpty(Me.mApellido1) Then
            pMensaje = "El conductor Adicional debe tener relleno al menso el primer apellido"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Me.mParentesco = ParentescoConductorAdicional.Desconocido Then
            pMensaje = "Es necesario asignar un parentesco válido al Conductor Adicional"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Me.mFechaNacimiento = DateTime.MinValue Then
            pMensaje = "Hay que asignarle una fecha válida al conductor adicional"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class

Public Enum ParentescoConductorAdicional
    Conyuge = 1
    Hermano = 2
    Padre = 3
    Hijo = 4
    Desconocido = 0
End Enum

<Serializable()> _
Public Class ColDatosMCND
    Inherits Framework.DatosNegocio.ArrayListValidable(Of DatosMCND)

    Public ReadOnly Property FechaNacimientoMenor() As DateTime
        Get
            If Me.Count <> 0 Then
                Dim fechamenor As DateTime = DateTime.Now
                For Each ca As DatosMCND In Me
                    If ca.FechaNacimiento < fechamenor Then
                        fechamenor = ca.FechaNacimiento
                    End If
                Next
                Return fechamenor
            End If
            Return DateTime.MinValue
        End Get
    End Property
End Class