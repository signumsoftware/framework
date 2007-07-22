#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

<Serializable()> Public Class colRolDepartamentoDN
    Inherits ArrayListValidable(Of RolDepartamentoDN)


#Region "Metodos"

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <returns>Devuelve la colección de departamentos</returns>
    ''' <remarks></remarks>
    ''' 
    Public Function GetDepartamentos() As IList(Of DepartamentoDN)
        'devulve la coleccion de las distintas instancias de departamentos contenidos
        Dim lista As New List(Of DepartamentoDN)
        Dim e As New RolDepartamentoDN
        For Each e In Me
            If Not lista.Contains(e.DepartamentoDN) Then
                lista.Add(e.DepartamentoDN)
            End If
        Next
        Return lista
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="pDepartamento"></param>
    ''' <returns>Devuelve la colección de roles del departamento que me pasa</returns>
    ''' <remarks></remarks>
    ''' 
    Public Function GetRolesXDepartamentoEncol(ByVal pDepartamento As DepartamentoDN) As ColRolDeEmpresaDN
        ' me devulve la coleccion de las distintas intancia de departamentos contenidos
        Dim lista As New ColRolDeEmpresaDN
        Dim e As New RolDepartamentoDN
        For Each e In Me
            If e.DepartamentoDN Is pDepartamento Then
                lista.Add(e.RolDeEmpresaDN)
            End If
        Next
        Return lista
    End Function

    'Public Function GetRolesXDepartamentoEncol(ByVal pdepartamento As DepartamentoDN) As ColRolDeEmpresaDN
    '    'Del departamento obtengo la huella
    '    Dim mHuellaDepartamento As New HuellaEntidadTipadaDepartamento(pdepartamento, HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)

    '    ' me devulve la coleccion de las distintas intancia de departamentos contenidos
    '    Dim lista As New ColRolDeEmpresaDN
    '    Dim e As New RolDepartamentoDN
    '    For Each e In Me
    '        If e.HuellaEntidadTipadaDN.Iguales(mHuellaDepartamento) Then
    '            lista.Add(e.RolDeEmpresaDN)
    '        End If
    '    Next
    '    Return lista
    'End Function

    'Public Function GetRolesXDepartamentoEncol(ByVal mHuellaEntidadTipadaDN As HuellaEntidadTipadaDepartamento) As ColRolDeEmpresaDN
    '    ' me devulve la coleccion de las distintas intancia de departamentos contenidos
    '    Dim mHuellaDepartamento As New HuellaEntidadTipadaDepartamento
    '    Dim lista As New ColRolDeEmpresaDN
    '    Dim e As New RolDepartamentoDN
    '    For Each e In Me
    '        If e.HuellaEntidadTipadaDN.Iguales(mHuellaDepartamento) Then
    '            lista.Add(e.RolDeEmpresaDN)
    '        End If
    '    Next
    '    Return lista
    'End Function

    ''' <summary>
    ''' Comprueba que un departamento esté contenido en la colección colRolDepartamentoDN
    ''' </summary>
    ''' <param name="pDepartamento"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function DepartamentoEnCol(ByVal pDepartamento As DepartamentoDN) As Boolean
        Dim e As New RolDepartamentoDN
        For Each e In Me
            If e.DepartamentoDN Is pDepartamento Then
                Return True
            End If
        Next
        Return False
    End Function


    ''' <summary>
    ''' Comprueba si existe un departamento de la colección colRolDepartamentoDN que pertenezca a una empresa
    ''' </summary>
    ''' <param name="pEmpresa"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function DepartamentoColEnEmpresa(ByVal pEmpresa As EmpresaDN) As Boolean
        Dim e As New RolDepartamentoDN
        For Each e In Me
            If e.DepartamentoDN.Empresa Is pEmpresa Then
                Return True
            End If
        Next
        Return False
    End Function

#End Region

End Class






