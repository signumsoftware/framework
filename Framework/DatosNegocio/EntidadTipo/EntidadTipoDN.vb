
<Serializable()> _
Public MustInherit Class EntidadTipoDN(Of T)

    Inherits Framework.DatosNegocio.EntidadBaseDN


    Public Sub New()

    End Sub

    Public Sub New(ByVal pId As T)
        Dim o As Object
        o = pId

        Dim valor As Integer
        valor = (CType(o, Integer))
        mID = valor
        mNombre = pId.ToString
        mGUID = GetType(T).Name & "id:" & valor


    End Sub

    Public Function RecuperarTiposTodos() As ArrayListValidable


        Dim col As New ArrayListValidable


        Dim valor As T

        For Each valor In [Enum].GetValues(GetType(T))
            'col.Add(New EntidadTipoDN(Of T)(valor))
            col.Add(CrearInstancia(valor))
        Next

        Return col

    End Function



    Public ReadOnly Property Valor() As T
        Get
            Return [Enum].Parse(GetType(T), CType(Me.mID, Integer))
        End Get
    End Property

    Protected MustOverride Function CrearInstancia(ByVal valor As T) As Object





End Class

<Serializable()> _
Public Class ColEntidadTipoDN(Of T)
    Inherits Framework.DatosNegocio.ArrayListValidable(Of EntidadTipoDN(Of T))
End Class
