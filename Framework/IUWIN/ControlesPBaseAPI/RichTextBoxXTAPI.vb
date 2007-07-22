Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Drawing.Printing

' An extension to RichTextBox suitable for printing
Public Class RichTextBoxXTAPI
    Inherits RichTextBox

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure STRUCT_RECT
        Public left As Int32
        Public top As Int32
        Public right As Int32
        Public bottom As Int32
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure STRUCT_CHARRANGE
        Public cpMin As Int32
        Public cpMax As Int32
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure STRUCT_FORMATRANGE
        Public hdc As IntPtr
        Public hdcTarget As IntPtr
        Public rc As STRUCT_RECT
        Public rcPage As STRUCT_RECT
        Public chrg As STRUCT_CHARRANGE
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure STRUCT_CHARFORMAT
        Public cbSize As Integer
        Public dwMask As UInt32
        Public dwEffects As UInt32
        Public yHeight As Int32
        Public yOffset As Int32
        Public crTextColor As Int32
        Public bCharSet As Byte
        Public bPitchAndFamily As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=32)> _
        Public szFaceName() As Char
    End Structure

    <DllImport("user32.dll")> _
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Int32, ByVal wParam As Int32, ByVal lParam As IntPtr) As Int32
    End Function

    ' Mensajes definidos por Windows
    Private Const WM_USER As Int32 = &H400&
    Private Const EM_FORMATRANGE As Int32 = WM_USER + 57
    Private Const EM_GETCHARFORMAT As Int32 = WM_USER + 58
    Private Const EM_SETCHARFORMAT As Int32 = WM_USER + 68

    ' Definidos para EM_GETCHARFORMAT/EM_SETCHARFORMAT
    Private SCF_SELECTION As Int32 = &H1&
    Private SCF_WORD As Int32 = &H2&
    Private SCF_ALL As Int32 = &H4&

    ' Definiciones xa el miembro dwMask STRUCT_CHARFORMAT 
    ' (Long porque UInt32 no es un tipo intrínseco)
    Private Const CFM_BOLD As Long = &H1&
    Private Const CFM_ITALIC As Long = &H2&
    Private Const CFM_UNDERLINE As Long = &H4&
    Private Const CFM_STRIKEOUT As Long = &H8&
    Private Const CFM_PROTECTED As Long = &H10&
    Private Const CFM_LINK As Long = &H20&
    Private Const CFM_SIZE As Long = &H80000000&
    Private Const CFM_COLOR As Long = &H40000000&
    Private Const CFM_FACE As Long = &H20000000&
    Private Const CFM_OFFSET As Long = &H10000000&
    Private Const CFM_CHARSET As Long = &H8000000&

    ' Definiciones para el miembro dwEffects STRUCT_CHARFORMAT 
    Private Const CFE_BOLD As Long = &H1&
    Private Const CFE_ITALIC As Long = &H2&
    Private Const CFE_UNDERLINE As Long = &H4&
    Private Const CFE_STRIKEOUT As Long = &H8&
    Private Const CFE_PROTECTED As Long = &H10&
    Private Const CFE_LINK As Long = &H20&
    Private Const CFE_AUTOCOLOR As Long = &H40000000&

    ''' <summary>
    ''' Calcula el renderizado del contenido del RichTextBox para la impresión
    ''' </summary>
    ''' <param name="measureOnly">Si es True, sólo se realiza el cálculo, si no el texto se renderiza</param>
    ''' <param name="e">El objeto PrintPageeventArgs del evento PrintPage</param>
    ''' <param name="charFrom">El índice del primer carácter a imprimir</param>
    ''' <param name="charTo">El ínidice del último carácter impreso</param>
    ''' <returns>El índice del último carácter colocado en la página +1</returns>
    Public Function FormatRange(ByVal measureOnly As Boolean, ByVal e As PrintPageEventArgs, ByVal charFrom As Integer, ByVal charTo As Integer) As Integer
        ' especifica los caracteres a imprimir
        Dim cr As STRUCT_CHARRANGE
        cr.cpMin = charFrom
        cr.cpMax = charTo

        'especifica el área dentro de los márgenes de impresión
        Dim rc As STRUCT_RECT
        rc.top = HundredthInchToTwips(e.MarginBounds.Top)
        rc.bottom = HundredthInchToTwips(e.MarginBounds.Bottom)
        rc.left = HundredthInchToTwips(e.MarginBounds.Left)
        rc.right = HundredthInchToTwips(e.MarginBounds.Right)

        'especifica el área de la página
        Dim rcPage As STRUCT_RECT
        rcPage.top = HundredthInchToTwips(e.PageBounds.Top)
        rcPage.bottom = HundredthInchToTwips(e.PageBounds.Bottom)
        rcPage.left = HundredthInchToTwips(e.PageBounds.Left)
        rcPage.right = HundredthInchToTwips(e.PageBounds.Right)

        'obtiene el contexto del dispositivo de salida
        Dim hdc As IntPtr
        hdc = e.Graphics.GetHdc()

        'rellena la estructura FORMATRANGE
        Dim fr As STRUCT_FORMATRANGE
        fr.chrg = cr
        fr.hdc = hdc
        fr.hdcTarget = hdc
        fr.rc = rc
        fr.rcPage = rcPage

        'wParam: <>0 = render, 0 = medir
        Dim wParam As Int32
        If measureOnly Then
            wParam = 0
        Else
            wParam = 1
        End If

        'asigna espacio en memoria para la struct FORMATRANGE y copia
        'el contenido de nuestro struct en ese espacio de memoria
        Dim lParam As IntPtr
        lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fr))
        Marshal.StructureToPtr(fr, lParam, False)

        'manda el mensaje Win32 actual
        Dim res As Integer
        res = SendMessage(Handle, EM_FORMATRANGE, wParam, lParam)

        ' Free allocated memory
        Marshal.FreeCoTaskMem(lParam)

        'libera el contexto del dispositivo
        e.Graphics.ReleaseHdc(hdc)

        Return res
    End Function

    ''' <summary>
    ''' Convierte entre 1/100 inch (pulgadas - usadas x .Net)
    ''' y twips (1/1440 inch, usada por API de Win32)
    ''' </summary>
    ''' <param name="n">valor en pulgadas</param>
    ''' <returns>valor en twips</returns>
    Private Function HundredthInchToTwips(ByVal n As Integer) As Int32
        Return Convert.ToInt32(n * 14.4)
    End Function

    ' Free cached data from rich edit control after printing
    Public Sub FormatRangeDone()
        Dim lParam As New IntPtr(0)
        SendMessage(Handle, EM_FORMATRANGE, 0, lParam)
    End Sub

    ' Sets the font only for the selected part of the rich text box
    ' without modifying the other properties like size or style
    ' <param name="face">Name of the font to use</param>
    ' <returns>true on success, false on failure</returns>
    Public Function SetSelectionFont(ByVal face As String) As Boolean
        Dim cf As New STRUCT_CHARFORMAT()
        cf.cbSize = Marshal.SizeOf(cf)
        cf.dwMask = Convert.ToUInt32(CFM_FACE)

        ' ReDim face name to relevant size
        ReDim cf.szFaceName(32)
        face.CopyTo(0, cf.szFaceName, 0, Math.Min(31, face.Length))

        Dim lParam As IntPtr
        lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(cf))
        Marshal.StructureToPtr(cf, lParam, False)

        Dim res As Integer
        res = SendMessage(Handle, EM_SETCHARFORMAT, SCF_SELECTION, lParam)
        If (res = 0) Then
            Return True
        Else
            Return False
        End If
    End Function

    ' Sets the font size only for the selected part of the rich text box
    ' without modifying the other properties like font or style
    ' <param name="size">new point size to use</param>
    ' <returns>true on success, false on failure</returns>
    Public Function SetSelectionSize(ByVal size As Integer) As Boolean
        Dim cf As New STRUCT_CHARFORMAT()
        cf.cbSize = Marshal.SizeOf(cf)
        cf.dwMask = Convert.ToUInt32(CFM_SIZE)
        ' yHeight is in 1/20 pt
        cf.yHeight = size * 20

        Dim lParam As IntPtr
        lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(cf))
        Marshal.StructureToPtr(cf, lParam, False)

        Dim res As Integer
        res = SendMessage(Handle, EM_SETCHARFORMAT, SCF_SELECTION, lParam)
        If (res = 0) Then
            Return True
        Else
            Return False
        End If
    End Function

    ' Sets the bold style only for the selected part of the rich text box
    ' without modifying the other properties like font or size
    ' <param name="bold">make selection bold (true) or regular (false)</param>
    ' <returns>true on success, false on failure</returns>
    Public Function SetSelectionBold(ByVal bold As Boolean) As Boolean
        If (bold) Then
            Return SetSelectionStyle(CFM_BOLD, CFE_BOLD)
        Else
            Return SetSelectionStyle(CFM_BOLD, 0)
        End If
    End Function

    ' Sets the italic style only for the selected part of the rich text box
    ' without modifying the other properties like font or size
    ' <param name="italic">make selection italic (true) or regular (false)</param>
    ' <returns>true on success, false on failure</returns>
    Public Function SetSelectionItalic(ByVal italic As Boolean) As Boolean
        If (italic) Then
            Return SetSelectionStyle(CFM_ITALIC, CFE_ITALIC)
        Else
            Return SetSelectionStyle(CFM_ITALIC, 0)
        End If
    End Function

    ' Sets the underlined style only for the selected part of the rich text box
    ' without modifying the other properties like font or size
    ' <param name="underlined">make selection underlined (true) or regular (false)</param>
    ' <returns>true on success, false on failure</returns>
    Public Function SetSelectionUnderlined(ByVal underlined As Boolean) As Boolean
        If (underlined) Then
            Return SetSelectionStyle(CFM_UNDERLINE, CFE_UNDERLINE)
        Else
            Return SetSelectionStyle(CFM_UNDERLINE, 0)
        End If
    End Function

    ' Set the style only for the selected part of the rich text box
    ' with the possibility to mask out some styles that are not to be modified
    ' <param name="mask">modify which styles?</param>
    ' <param name="effect">new values for the styles</param>
    ' <returns>true on success, false on failure</returns>
    Private Function SetSelectionStyle(ByVal mask As Int32, ByVal effect As Int32) As Boolean
        Dim cf As New STRUCT_CHARFORMAT()
        cf.cbSize = Marshal.SizeOf(cf)
        cf.dwMask = Convert.ToUInt32(mask)
        cf.dwEffects = Convert.ToUInt32(effect)

        Dim lParam As IntPtr
        lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(cf))
        Marshal.StructureToPtr(cf, lParam, False)

        Dim res As Integer
        res = SendMessage(Handle, EM_SETCHARFORMAT, SCF_SELECTION, lParam)
        If (res = 0) Then
            Return True
        Else
            Return False
        End If
    End Function
End Class
