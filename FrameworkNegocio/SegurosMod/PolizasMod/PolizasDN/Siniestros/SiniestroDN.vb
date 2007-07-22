Imports Framework.DatosNegocio
Imports FN.Empresas.DN

<Serializable()> _
Public Class SiniestroDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mFOcurrencia As Date



#End Region

#Region "Propiedades"






    Public Property FOcurrencia() As Date

        Get
            Return mFOcurrencia
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFOcurrencia)

        End Set
    End Property







#End Region


End Class



<Serializable()> _
Public Class ColSiniestroDN
    Inherits ArrayListValidable(Of SiniestroDN)


    Public Function RecuperarDesdeFechaOcurrencia(ByVal pFechaOcurrencia As Date) As ColSiniestroDN

        RecuperarDesdeFechaOcurrencia = New ColSiniestroDN



        For Each sini As SiniestroDN In Me
            If sini.FOcurrencia >= pFechaOcurrencia Then
                RecuperarDesdeFechaOcurrencia.Add(sini)
            End If

        Next


    End Function

    Public Function RecuperarDesdeFechaOcurrencia(ByVal IntervaloFechas As Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN) As ColSiniestroDN

        RecuperarDesdeFechaOcurrencia = New ColSiniestroDN



        For Each sini As SiniestroDN In Me
            If IntervaloFechas.Contiene(sini.FOcurrencia) Then
                RecuperarDesdeFechaOcurrencia.Add(sini)
            End If

        Next


    End Function
End Class



