#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Esta clase representa la división de las empresas por zonas.
''' Atributos: 
'''     - La colección de empresas que constituye la agrupación
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class AgrupacionDeEmpresasDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mColEmpresasDN As ColEmpresasDN
    Protected mColAgrupacionDeEmpresasDN As ColAgrupacionDeEmpresasDN
#End Region

#Region "Contructores"
    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of ColEmpresasDN)(New ColEmpresasDN, mColEmpresasDN)
    End Sub

#End Region

#Region "Propiedades"







    Protected mTipoAgrupacionEmpresa As TipoAgrupacionEmpresa

    <RelacionPropCampoAtribute("mTipoAgrupacionEmpresa")> _
    Public Property TipoAgrupacionEmpresa() As TipoAgrupacionEmpresa

        Get
            Return mTipoAgrupacionEmpresa
        End Get

        Set(ByVal value As TipoAgrupacionEmpresa)
            CambiarValorRef(Of TipoAgrupacionEmpresa)(value, mTipoAgrupacionEmpresa)

        End Set
    End Property







    <RelacionPropCampoAtribute("mColAgrupacionDeEmpresasDN")> _
    Public Property ColAgrupacionDeEmpresasDN() As ColAgrupacionDeEmpresasDN

        Get
            Return mColAgrupacionDeEmpresasDN
        End Get

        Set(ByVal value As ColAgrupacionDeEmpresasDN)
            CambiarValorRef(Of ColAgrupacionDeEmpresasDN)(value, mColAgrupacionDeEmpresasDN)

        End Set
    End Property





    Public Property ColEmpresasDN() As ColEmpresasDN
        Get
            Return mColEmpresasDN
        End Get
        Set(ByVal value As ColEmpresasDN)
            Me.CambiarValorRef(Of ColEmpresasDN)(value, mColEmpresasDN)
        End Set
    End Property

#End Region

End Class

<Serializable()> Public Class ColAgrupacionDeEmpresasDN
    Inherits ArrayListValidable(Of AgrupacionDeEmpresasDN)

    ' metodos de coleccion

    Public Function RecupearXId(ByVal IdAgrupacion As String) As AgrupacionDeEmpresasDN
        Dim age As AgrupacionDeEmpresasDN

        For Each age In Me
            If age.ID = IdAgrupacion Then
                Return age
            End If
        Next

        Return Nothing
    End Function

    Public Function Unificar() As AgrupacionDeEmpresasDN

        Dim age As AgrupacionDeEmpresasDN
        Unificar = New AgrupacionDeEmpresasDN

        For Each age In Me
            Dim empresa As FN.Empresas.DN.EmpresaDN
            For Each empresa In age.ColEmpresasDN
                Unificar.ColEmpresasDN.AddUnico(empresa)
            Next
        Next

    End Function

End Class


<Serializable()> _
Public Class GrupoAgrupacionDeEmpresasDN
    Inherits Framework.DatosNegocio.EntidadDN
    Private mAgrupaciones As ColAgrupacionDeEmpresasDN
End Class


Public Class TipoAgrupacionEmpresa
    Inherits Framework.DatosNegocio.EntidadDN
End Class