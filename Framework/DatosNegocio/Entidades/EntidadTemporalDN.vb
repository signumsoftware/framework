Imports Framework.DatosNegocio.Localizaciones.Temporales
Imports Framework.DatosNegocio
<Serializable()> _
Public MustInherit Class EntidadTemporalDN
    Inherits EntidadDN
    Implements IEntidadTemporalDN



    Protected mPeriodo As IntervaloFechasDN


    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of IntervaloFechasDN)(New IntervaloFechasDN(Now, Nothing), mPeriodo)
    End Sub

    Public Property Periodo() As IIntervaloTemporal Implements IEntidadTemporalDN.Periodo
        Get
            Return (Me.mPeriodo)
        End Get
        Set(ByVal value As IIntervaloTemporal)
            Me.CambiarValorRef(Of IntervaloFechasDN)(value, Me.mPeriodo)
        End Set
    End Property

    Protected Overrides Property BajaPersistente() As Boolean
        Get
            Return MyBase.BajaPersistente
        End Get
        Set(ByVal value As Boolean)

            If Me.mBaja <> True Then
                mPeriodo.FFinal = Date.Now
            End If

            MyBase.BajaPersistente = value
        End Set
    End Property



    Public Function BienFormado() As Boolean Implements IIntervaloTemporal.BienFormado
        Return mPeriodo.BienFormado
    End Function

    Public Function Contiene(ByVal pValor As Date) As Boolean Implements IIntervaloTemporal.Contiene
        Return mPeriodo.Contiene(pValor)
    End Function

    Public Function SolapadoOContenido(ByVal pIntervalo As IIntervaloTemporal) As IntSolapadosOContenido Implements IIntervaloTemporal.SolapadoOContenido
        Return mPeriodo.SolapadoOContenido(pIntervalo)
    End Function

    Public Property FF() As Date Implements IIntervaloTemporal.FF
        Get
            Return mPeriodo.FFinal
        End Get
        Set(ByVal value As Date)
            mPeriodo.FFinal = value
        End Set
    End Property

    Public Property FI() As Date Implements IIntervaloTemporal.FI
        Get
            Return mPeriodo.FInicio
        End Get
        Set(ByVal value As Date)
            mPeriodo.FInicio = value
        End Set
    End Property
End Class






<Serializable()> _
Public Class ColEntidadTemporalBaseDN(Of T As {EntidadTemporalDN})
    Inherits ArrayListValidable(Of T)

    Public Function Recuperar(ByVal pFecha As Date) As ColEntidadTemporalBaseDN(Of T)

        Dim col As New ColEntidadTemporalBaseDN(Of T)


        For Each et As EntidadTemporalDN In Me
            If et.Periodo.Contiene(pFecha) Then
                col.Add(et)
            End If
        Next
        Return col

    End Function

End Class




