Public Class fUserInfo

    Public strUserID As String
    Public strUserName As String
    Public intDialogResult As Integer

    Private Sub fUserInfo_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        If Me.intDialogResult = DialogResult.Cancel Then
            e.Cancel = False
            Exit Sub
        End If

        If isNothingOrEmptyString(objTextBoxUserID.Text) = True Then
            showMessage("Info", "ユーザー識別番号を入力してください。")
            e.Cancel = True
            Exit Sub
        End If

        If isNothingOrEmptyString(objTextBoxUserName.Text) = True Then
            showMessage("Info", "ユーザー氏名を入力してください。")
            e.Cancel = True
            Exit Sub
        End If

        Me.strUserID = objTextBoxUserID.Text
        Me.strUserName = objTextBoxUserName.Text

    End Sub

    Private Sub objButtonCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objButtonCancel.Click

        Me.intDialogResult = DialogResult.Cancel

        Me.Close()

    End Sub

    Private Sub objButtonSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objButtonSave.Click

        Me.intDialogResult = DialogResult.OK

        Me.Close()

    End Sub

    Private Sub fUserInfo_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Me.objTextBoxUserID.Text = Format(Now, "yyyyMMddhhmmssfff")

    End Sub

End Class