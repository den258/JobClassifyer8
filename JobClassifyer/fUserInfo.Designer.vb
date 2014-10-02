<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class fUserInfo
    Inherits System.Windows.Forms.Form

    'フォームがコンポーネントの一覧をクリーンアップするために dispose をオーバーライドします。
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows フォーム デザイナで必要です。
    Private components As System.ComponentModel.IContainer

    'メモ: 以下のプロシージャは Windows フォーム デザイナで必要です。
    'Windows フォーム デザイナを使用して変更できます。  
    'コード エディタを使って変更しないでください。
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Label1 = New System.Windows.Forms.Label
        Me.objTextBoxUserID = New System.Windows.Forms.TextBox
        Me.Label2 = New System.Windows.Forms.Label
        Me.objTextBoxUserName = New System.Windows.Forms.TextBox
        Me.objButtonCancel = New System.Windows.Forms.Button
        Me.objButtonSave = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("MS UI Gothic", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label1.Location = New System.Drawing.Point(12, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(284, 16)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "ユーザーを識別する番号を入力してください："
        '
        'objTextBoxUserID
        '
        Me.objTextBoxUserID.Font = New System.Drawing.Font("MS UI Gothic", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.objTextBoxUserID.Location = New System.Drawing.Point(302, 6)
        Me.objTextBoxUserID.Name = "objTextBoxUserID"
        Me.objTextBoxUserID.Size = New System.Drawing.Size(185, 23)
        Me.objTextBoxUserID.TabIndex = 1
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("MS UI Gothic", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label2.Location = New System.Drawing.Point(82, 47)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(214, 16)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "ユーザー氏名を入力してください："
        '
        'objTextBoxUserName
        '
        Me.objTextBoxUserName.Font = New System.Drawing.Font("MS UI Gothic", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.objTextBoxUserName.Location = New System.Drawing.Point(302, 44)
        Me.objTextBoxUserName.Name = "objTextBoxUserName"
        Me.objTextBoxUserName.Size = New System.Drawing.Size(185, 23)
        Me.objTextBoxUserName.TabIndex = 3
        '
        'objButtonCancel
        '
        Me.objButtonCancel.Font = New System.Drawing.Font("MS UI Gothic", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.objButtonCancel.Location = New System.Drawing.Point(392, 73)
        Me.objButtonCancel.Name = "objButtonCancel"
        Me.objButtonCancel.Size = New System.Drawing.Size(119, 48)
        Me.objButtonCancel.TabIndex = 4
        Me.objButtonCancel.Text = "キャンセル"
        Me.objButtonCancel.UseVisualStyleBackColor = True
        '
        'objButtonSave
        '
        Me.objButtonSave.Font = New System.Drawing.Font("MS UI Gothic", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.objButtonSave.Location = New System.Drawing.Point(267, 73)
        Me.objButtonSave.Name = "objButtonSave"
        Me.objButtonSave.Size = New System.Drawing.Size(119, 48)
        Me.objButtonSave.TabIndex = 5
        Me.objButtonSave.Text = "保存"
        Me.objButtonSave.UseVisualStyleBackColor = True
        '
        'fUserInfo
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(523, 133)
        Me.Controls.Add(Me.objButtonSave)
        Me.Controls.Add(Me.objButtonCancel)
        Me.Controls.Add(Me.objTextBoxUserName)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.objTextBoxUserID)
        Me.Controls.Add(Me.Label1)
        Me.Name = "fUserInfo"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "fUserInfo"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents objTextBoxUserID As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents objTextBoxUserName As System.Windows.Forms.TextBox
    Friend WithEvents objButtonCancel As System.Windows.Forms.Button
    Friend WithEvents objButtonSave As System.Windows.Forms.Button
End Class
