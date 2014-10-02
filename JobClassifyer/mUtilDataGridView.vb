Module mUtilDataGridView

    Public Function isCorrectTimeString(ByVal strValue As String) As Boolean

        If strValue.IndexOf(":") = -1 Then
            showMessage("Info", "時間は、'09:00'の形式で入力してください。")
            isCorrectTimeString = False
            Exit Function
        End If

        If strValue.Length <> 5 Then
            showMessage("Info", "時間は、'09:00'の形式で入力してください。")
            isCorrectTimeString = False
            Exit Function
        End If

        If getNumberFromString(strValue.Substring(3)) Mod 15 <> 0 Then
            showMessage("Info", "時間は、15分単位で入力してください。")
            isCorrectTimeString = False
            Exit Function
        End If

        If getNumberFromString(strValue.Substring(0, 2)) < 0 Then
            showMessage("Info", "時間が、間違っています。")
            isCorrectTimeString = False
            Exit Function
        End If

        If getNumberFromString(strValue.Substring(3)) < 0 _
        Or getNumberFromString(strValue.Substring(3)) > 45 Then
            showMessage("Info", "時間が、間違っています。")
            isCorrectTimeString = False
            Exit Function
        End If

        isCorrectTimeString = True

    End Function

End Module
