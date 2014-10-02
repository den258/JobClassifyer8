﻿
Imports System.Text.RegularExpressions
Imports System.Text.RegularExpressions.RegexOptions
Imports System.IO
Imports System.Text
Imports Microsoft.Office.Interop

Public Class fClassifyer

#Region "Private Variables"

    Private objNodeFrom As TreeNode
    Private objNodeTo As TreeNode
    Private blnDrag As Boolean
    Private blnUpdated As Boolean
    Private objVo As New cValueObjects.VoUserInfo

#End Region

#Region "Private Value Object"

    Private Class VoMonthlyWork

        Public strDate As String
        Public strStartOfDate As String
        Public strEndOfDate As String
        Public dblTotalDateSpendTime As Double

    End Class

    Private Class VoDataGrid

        Public strDate As String
        Public strTimeStart As String
        Public strTimeEnd As String
        Public strJob As String
        Public strWorkTimeDetail As String
        Public strWorkTimeCode As String

    End Class

    Private Function doCompare(ByVal objX As VoDataGrid, ByVal objY As VoDataGrid) As Integer

        Dim intCompare As Integer = objX.strDate.CompareTo(objY.strDate)

        If intCompare = 0 Then
            doCompare = objX.strTimeStart.CompareTo(objY.strTimeStart)
        Else
            doCompare = intCompare
        End If

    End Function

#End Region

#Region "Private Method"

    ''' <summary>
    ''' DataGridView の DataRow の妥当性検査を行う
    ''' </summary>
    ''' <param name="intRowIndex"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function IsValidatedDataRow(ByVal intRowIndex As Integer) As Boolean

        Dim strStart As String = objDataGridView.Rows(intRowIndex).Cells("objColumnStart").Value
        Dim strEnd As String = objDataGridView.Rows(intRowIndex).Cells("objColumnEnd").Value

        If isNothingOrEmptyString(strStart) = True Then
            showMessage("Info", "開始時間を入力してください。")
            IsValidatedDataRow = False
            Exit Function
        End If

        If isNothingOrEmptyString(strEnd) = True Then
            showMessage("Info", "終了時間を入力してください。")
            IsValidatedDataRow = False
            Exit Function
        End If

        If getDoubleFromTimeString(strStart) >= getDoubleFromTimeString(strEnd) Then
            showMessage("Info", "開始時間と終了時間の大小関係が間違っています。")
            IsValidatedDataRow = False
            Exit Function
        End If

        IsValidatedDataRow = True

    End Function

    ''' <summary>
    ''' TreeNodes から strCode に該当する Node を取得する
    ''' </summary>
    ''' <param name="objTreeNode"></param>
    ''' <param name="strCode"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getSpecifiedNode(ByVal objTreeNode As TreeNode, ByVal strCode As String) As TreeNode

        getSpecifiedNode = Nothing

        If getNodeCodeFromTag(objTreeNode) = strCode Then

            getSpecifiedNode = objTreeNode

        Else

            For Each objNode As TreeNode In objTreeNode.Nodes
                getSpecifiedNode = getSpecifiedNode(objNode, strCode)
                If isNothing(getSpecifiedNode) = False Then
                    Exit For
                End If
            Next

        End If

    End Function

    ''' <summary>
    ''' TreeView の Top Level の Node を取得する。
    ''' TopNode が Top Level の Node を返さないために実装した。
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getTopNode() As TreeNode

        Dim objNode As TreeNode = objTreeView.TopNode

        Do While Not isNothing(objNode.Parent)
            objNode = objNode.Parent
        Loop

        getTopNode = objNode

    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getSelectedTreeNode() As TreeNode

        showMessage("Info", objTreeView.SelectedNode.Text + " から配下のノードを保存します。")

        Dim objNode As TreeNode = objTreeView.SelectedNode.Clone
        Dim objParentNode As TreeNode = objTreeView.SelectedNode

        Do While Not isNothing(objParentNode)
            Dim objTargetNode As TreeNode = objNode
            If isNothing(objParentNode.Parent) Then
                Exit Do
            End If
            objParentNode = objParentNode.Parent
            objNode = objParentNode.Clone
            objNode.Nodes.Clear()
            objNode.Nodes.Add(objTargetNode)
        Loop

        getSelectedTreeNode = objNode

    End Function

    ''' <summary>
    ''' 現在選択されている日付の年月の一致する作業データを取得する
    ''' </summary>
    ''' <remarks></remarks>
    Private Function getMonthlyWorks(ByVal objDate As Date, ByVal objNode As TreeNode) As List(Of VoDataGrid)

        Dim objVoList As New List(Of VoDataGrid)

        For Each objChildNode As TreeNode In objNode.Nodes

            If objChildNode.Nodes.Count > 0 Then

                objVoList.AddRange(getMonthlyWorks(objDate, objChildNode))

            Else

                If isNodeType(eNodeType.vWorkTime, objChildNode) = True Then

                    Dim strNodeText As String = objChildNode.Text
                    Dim strDate As String = strNodeText.Substring(strNodeText.IndexOf(":") + 1, 8)

                    ' 年月で一致するものを抽出する
                    If strDate.Substring(0, 6) = objDate.ToString("yyyyMM") Then

                        Dim objVo As New VoDataGrid

                        Dim strWorkDetail As String = getWorkTimeDetailFromTag(objChildNode)

                        objVo.strDate = strDate
                        objVo.strTimeStart = getTimeStartFromWorkDetail(strWorkDetail)
                        objVo.strTimeEnd = getTimeEndFromWorkDetail(strWorkDetail)
                        objVo.strJob = objChildNode.Parent.Text
                        objVo.strWorkTimeDetail = strWorkDetail
                        objVo.strWorkTimeCode = objChildNode.Text.Substring(1, 7)

                        objVoList.Add(objVo)

                    End If

                End If

            End If

        Next

        getMonthlyWorks = objVoList

    End Function

    ''' <summary>
    ''' 現在選択されている日付の週内の作業データを取得する
    ''' </summary>
    ''' <remarks></remarks>
    Private Function getWeeklyWorks(ByVal objDate As Date, ByVal objNode As TreeNode) As List(Of VoDataGrid)

        Dim objVoList As New List(Of VoDataGrid)

        Dim objWeekStart As Date = getStartOfWeek(objDate)
        Dim objWeekEnd As Date = getEndOfWeek(objDate)

        For Each objChildNode As TreeNode In objNode.Nodes

            If objChildNode.Nodes.Count > 0 Then

                objVoList.AddRange(getWeeklyWorks(objDate, objChildNode))

            Else

                If isNodeType(eNodeType.vWorkTime, objChildNode) = True Then

                    Dim strNodeText As String = objChildNode.Text
                    Dim strDate As String = strNodeText.Substring(strNodeText.IndexOf(":") + 1, 8)

                    ' 週が一致するものを抽出する
                    If getNumberFromString(strDate) >= getNumberFromString(objWeekStart.ToString("yyyyMMdd")) And _
                       getNumberFromString(strDate) <= getNumberFromString(objWeekEnd.ToString("yyyyMMdd")) Then

                        Dim objVo As New VoDataGrid

                        Dim strWorkDetail As String = getWorkTimeDetailFromTag(objChildNode)

                        objVo.strDate = strDate
                        objVo.strTimeStart = getTimeStartFromWorkDetail(strWorkDetail)
                        objVo.strTimeEnd = getTimeEndFromWorkDetail(strWorkDetail)
                        objVo.strJob = objChildNode.Parent.Text
                        objVo.strWorkTimeDetail = strWorkDetail
                        objVo.strWorkTimeCode = objChildNode.Text.Substring(1, 7)

                        objVoList.Add(objVo)

                    End If

                End If

            End If

        Next

        getWeeklyWorks = objVoList

    End Function

    ''' <summary>
    ''' 現在選択されている日付の作業データを取得する
    ''' </summary>
    ''' <remarks></remarks>
    Private Function getDailyWorks(ByVal objDate As Date, ByVal objNode As TreeNode) As List(Of VoDataGrid)

        Dim objVoList As New List(Of VoDataGrid)

        For Each objChildNode As TreeNode In objNode.Nodes

            If objChildNode.Nodes.Count > 0 Then

                objVoList.AddRange(getDailyWorks(objDate, objChildNode))

            Else

                If isNodeType(eNodeType.vWorkTime, objChildNode) = True Then

                    Dim strNodeText As String = objChildNode.Text
                    Dim strDate As String = strNodeText.Substring(strNodeText.IndexOf(":") + 1, 8)

                    ' 年月日で一致するものを抽出する
                    If strDate = objDate.ToString("yyyyMMdd") Then

                        Dim objVo As New VoDataGrid

                        Dim strWorkDetail As String = getWorkTimeDetailFromTag(objChildNode)

                        objVo.strDate = strDate
                        objVo.strTimeStart = getTimeStartFromWorkDetail(strWorkDetail)
                        objVo.strTimeEnd = getTimeEndFromWorkDetail(strWorkDetail)
                        objVo.strJob = objChildNode.Parent.Text
                        objVo.strWorkTimeDetail = strWorkDetail
                        objVo.strWorkTimeCode = objChildNode.Text.Substring(1, 7)

                        objVoList.Add(objVo)

                    End If

                End If

            End If

        Next

        getDailyWorks = objVoList

    End Function

    ''' <summary>
    ''' 指定された日付の作業データを DataGridView に表示する
    ''' </summary>
    ''' <param name="objDate"></param>
    ''' <remarks></remarks>
    Private Sub DisplayDataGridView(ByVal objDate As Date)

        ' DateTimePicker の日付の作業データを抽出する
        Dim objVoList As List(Of VoDataGrid) = getDailyWorks(objDate, getTopNode())

        Call objVoList.Sort(AddressOf doCompare)

        Dim intRowIndex As Integer = 0
        Dim dblTotalSpendTime As Double = 0.0

        For Each objVo As VoDataGrid In objVoList

            objDataGridView.Rows.Add()

            objDataGridView.Rows(intRowIndex).Cells("objColumnId").Value = intRowIndex + 1
            objDataGridView.Rows(intRowIndex).Cells("objColumnStart").Value = objVo.strTimeStart
            objDataGridView.Rows(intRowIndex).Cells("objColumnEnd").Value = objVo.strTimeEnd
            objDataGridView.Rows(intRowIndex).Cells("objColumnJob").Value = objVo.strJob
            objDataGridView.Rows(intRowIndex).Cells("objColumnSpendTime").Value = _
                getWorkedTime(objVo.strTimeStart, objVo.strTimeEnd).ToString("00.00")
            objDataGridView.Rows(intRowIndex).Cells("objColumnWorkTimeDetail").Value = objVo.strWorkTimeDetail
            objDataGridView.Rows(intRowIndex).Cells("objColumnWorkTimeCode").Value = objVo.strWorkTimeCode

            dblTotalSpendTime = dblTotalSpendTime _
                + getWorkedTime(objVo.strTimeStart, objVo.strTimeEnd)

            intRowIndex = intRowIndex + 1

        Next

        objLabelTotalSpendTime.Text = dblTotalSpendTime.ToString("0.00")

    End Sub

    ''' <summary>
    ''' 指定された WorkTime Node に関連付けられた Rows を削除する
    ''' </summary>
    ''' <param name="objArgNode"></param>
    ''' <remarks></remarks>
    Private Sub doDeleteWorkTimeAtGridView(ByVal objArgNode As TreeNode)

        If isNodeType(eNodeType.vWorkTime, objArgNode) = False Then

            For Each objNode As TreeNode In objArgNode.Nodes
                doDeleteWorkTimeAtGridView(objNode)
            Next

        Else

            Dim strWorkTimeDetail As String = getWorkTimeDetailFromTag(objArgNode)

            Dim strStart As String = getTimeStartFromWorkDetail(strWorkTimeDetail)
            Dim strEnd As String = getTimeEndFromWorkDetail(strWorkTimeDetail)
            Dim strDate As String = getDateFromWorkDetail(strWorkTimeDetail)

            If objDateTimePicker.Value.ToString("yyyyMMdd") = strDate Then

                For Each objRow As DataGridViewRow In objDataGridView.Rows

                    If objRow.Cells("objColumnStart").Value = strStart And _
                       objRow.Cells("objColumnEnd").Value = strEnd Then

                        objRow.Cells("objColumnJob").Value = ""

                        Exit For

                    End If

                Next

            End If

        End If

    End Sub

    ''' <summary>
    ''' 選択された Node 配下の作業時間の合計を取得する
    ''' </summary>
    ''' <param name="objArgNode"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getChildNodeWorkTimeSum(ByVal objArgNode As TreeNode) As Double

        If getTagNameFromTag(objArgNode.Tag) = "WorkTime" Then
            getChildNodeWorkTimeSum = Double.Parse(getTimeFromNodeText(objArgNode.Text))
        Else
            Dim dblSum As Double = 0.0
            For Each objNode As TreeNode In objArgNode.Nodes
                dblSum = dblSum + getChildNodeWorkTimeSum(objNode)
            Next
            getChildNodeWorkTimeSum = dblSum
        End If

    End Function

    ''' <summary>
    ''' 指定された日付の作業時間の合計を取得する
    ''' </summary>
    ''' <param name="objArgNode"></param>
    ''' <param name="objArgDate"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getChildNodeWorkTimeSumByDate(ByVal objArgNode As TreeNode, ByVal objArgDate As Date) As Double

        If getTagNameFromTag(objArgNode.Tag) = "WorkTime" Then
            If getIntDateFromDate(objArgDate) = Integer.Parse(getDateFromNodeText(objArgNode.Text)) Then
                getChildNodeWorkTimeSumByDate = Double.Parse(getTimeFromNodeText(objArgNode.Text))
            Else
                getChildNodeWorkTimeSumByDate = 0
            End If
        Else
            Dim dblSum As Double = 0.0
            For Each objNode As TreeNode In objArgNode.Nodes
                dblSum = dblSum + getChildNodeWorkTimeSumByDate(objNode, objArgDate)
            Next
            getChildNodeWorkTimeSumByDate = dblSum
        End If

    End Function

    ''' <summary>
    ''' 渡された TreeNode 配下に strCode をラベルに含むノードがあれば、それを返す
    ''' </summary>
    ''' <param name="strCode"></param>
    ''' <param name="objTreeNode"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getTreeNodeByCode(ByVal strCode As String, ByVal objTreeNode As TreeNode) As TreeNode

        Dim objReturnNode As New TreeNode

        For Each objChildNode As TreeNode In objTreeNode.Nodes
            objReturnNode = getTreeNodeByCode(strCode, objChildNode)
            If isNothingOrEmptyString(objReturnNode.Text) = False Then
                Exit For
            End If
        Next

        If objTreeNode.Text.IndexOf(strCode) <> -1 Then
            objReturnNode = objTreeNode
        End If

        Return objReturnNode

    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="objForm"></param>
    ''' <param name="objNode"></param>
    ''' <param name="blnDeleteEMptyNode"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getTreeNodeAndDatePair(ByVal objForm As fExport, ByVal objNode As TreeNode, ByVal blnDeleteEmptyNode As Boolean) As cValueObjects.VoTreeNodeAndDatePair

        Dim objReturnVo As New cValueObjects.VoTreeNodeAndDatePair

        If objForm.objCheckBoxAllDate.Checked = True Then

            objReturnVo.objNode = objNode

            Dim objVo As cValueObjects.VoDatePair = _
                getDatePairFromTreeNode( _
                    getDateListFromTreeNode( _
                        objNode))

            objReturnVo.objDatePair.objStart = objVo.objStart
            objReturnVo.objDatePair.objEnd = objVo.objEnd

        Else

            objReturnVo.objNode = _
                getTreeNodeFromNodeList( _
                    getTreeNodeCopy( _
                        objNode, _
                        objForm.objDateTimePickerStart.Value, _
                        objForm.objDateTimePickerEnd.Value, _
                        blnDeleteEmptyNode), objNode)

            objReturnVo.objDatePair.objStart = objForm.objDateTimePickerStart.Value
            objReturnVo.objDatePair.objEnd = objForm.objDateTimePickerEnd.Value

        End If

        getTreeNodeAndDatePair = objReturnVo

    End Function

#End Region

#Region "objDateTimePicker EventHandler"

    Private Sub objDateTimePicker_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles objDateTimePicker.ValueChanged

        Call objDataGridView.Rows.Clear()

        Call DisplayDataGridView(objDateTimePicker.Value)

    End Sub

#End Region

#Region "objTreeView EventHandler"

    Private Sub objTreeView_AfterLabelEdit(ByVal sender As Object, ByVal e As System.Windows.Forms.NodeLabelEditEventArgs) Handles objTreeView.AfterLabelEdit

        If isNothingOrEmptyString(e.Label) = False Then
            If (New Regex("\[.+\]\:", IgnoreCase)).Match(e.Label).Success = False Then
                e.CancelEdit = True
            End If
        Else
            e.CancelEdit = True
        End If

        If e.CancelEdit = True Then
            showMessage("Info", "'" + e.Node.Text.Substring(0, 10) + "'の部分は変更できません。")
        End If

    End Sub

    Private Sub objTreeView_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles objTreeView.MouseDown

        If e.Button <> Windows.Forms.MouseButtons.Left Then

            Call setHideMenuAll(Me)

            Select Case Split(Me.objTreeView.SelectedNode.Tag, ",")(0)

                Case "Root"
                    setShowMenu(Me, "Customer", "Add")

                Case "Customer"
                    setShowMenu(Me, "Business", "Add")
                    setShowMenu(Me, "Customer", "Delete")

                Case "Business"
                    setShowMenu(Me, "Class", "Add")
                    setShowMenu(Me, "Business", "Delete")

                Case "Class"
                    setShowMenu(Me, "Class", "Add")
                    setShowMenu(Me, "Detail", "Add")
                    setShowMenu(Me, "Class", "Delete")

                Case "Detail"
                    setShowMenu(Me, "Detail", "Delete")
                    setShowMenu(Me, "WorkTime", "Add")

                Case "WorkTime"
                    setShowMenu(Me, "WorkTime", "Delete")

            End Select

        End If

    End Sub

    Private Sub objTreeView_ItemDrag(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ItemDragEventArgs) Handles objTreeView.ItemDrag

        If Me.objTreeView.Nodes.Count = 0 Then
            Exit Sub
        End If

        Me.objNodeFrom = Nothing
        Me.objNodeTo = Nothing

        Me.objNodeFrom = e.Item

        If Not objNodeFrom Is Nothing Then

            Me.blnDrag = True

            Me.Text = Me.objNodeFrom.Text + " を選択しています。"

        End If

    End Sub

    Private Sub objTreeView_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles objTreeView.MouseUp

        'If e.Button <> Windows.Forms.MouseButtons.Left Then
        '    Exit Sub
        'End If

        If Me.blnDrag <> True Then
            Exit Sub
        End If

        Me.blnDrag = False

        If Me.objNodeTo Is Nothing Or _
           Me.objNodeFrom Is Nothing Then
            'MsgBox("移動元、又は、移動先が確定していません。", _
            '    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
            Exit Sub
        End If

        'If Me.objNodeTo.Tag = "Leaf" Then
        '    MsgBox("葉には移動できません。", _
        '        MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
        '    Exit Sub
        'End If

        If Me.objNodeFrom Is Me.objNodeTo Then
            'MsgBox("移動元、又は、移動先が同じです。", _
            '    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
            Exit Sub
        End If

        If isParent(Me.objNodeFrom, Me.objNodeTo) Then
            MsgBox("親を子に移動しようとしています。循環参照のため移動できません。", _
                MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
            Exit Sub
        End If

        Select Case _
            Split(Me.objNodeFrom.Tag, ",")(0) + " For " + _
            Split(Me.objNodeTo.Tag, ",")(0)

            Case "Root For Root"
                Call MsgBox("ルートをルートの配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Root For Customer"
                Call MsgBox("ルートを顧客の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Root For Business"
                Call MsgBox("ルートを業務の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Root For Class"
                Call MsgBox("ルートを分類の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Root For Detail"
                Call MsgBox("ルートを詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Root For WorkTime"
                Call MsgBox("ルートを詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub

            Case "Customer For Root"
            Case "Customer For Customer"
                Call MsgBox("顧客を顧客の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Customer For Business"
                Call MsgBox("顧客を業務の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Customer For Class"
                Call MsgBox("顧客を分類の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Customer For Detail"
                Call MsgBox("顧客を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Customer For WorkTime"
                Call MsgBox("顧客を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub

            Case "Business For Root"
                Call MsgBox("業務をルートの配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Business For Customer"
            Case "Business For Business"
                Call MsgBox("業務を業務の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Business For Class"
                Call MsgBox("業務を分類の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Business For Detail"
                Call MsgBox("業務を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Business For WorkTime"
                Call MsgBox("業務を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub

            Case "Class For Root"
                Call MsgBox("分類をルートの配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Class For Customer"
                Call MsgBox("分類を顧客の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Class For Business"
            Case "Class For Class"
            Case "Class For Detail"
                Call MsgBox("分類を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Class For WorkTime"
                Call MsgBox("分類を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub

            Case "Detail For Root"
                Call MsgBox("詳細をルートの配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Detail For Customer"
                Call MsgBox("詳細を顧客の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Detail For Business"
                Call MsgBox("詳細を業務の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Detail For Class"
            Case "Detail For Detail"
                Call MsgBox("詳細を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "Detail For WorkTime"
                Call MsgBox("詳細を詳細の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub

            Case "WorkTime For Root"
                Call MsgBox("時間をルートの配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "WorkTime For Customer"
                Call MsgBox("時間を顧客の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "WorkTime For Business"
                Call MsgBox("時間を業務の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "WorkTime For Class"
                Call MsgBox("時間を分類の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub
            Case "WorkTime For Detail"
            Case "WorkTime For WorkTime"
                Call MsgBox("時間を時間の配下には移動できません。", _
                    MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                Exit Sub

        End Select

        If isParent(Me.objNodeFrom, Me.objNodeTo) Then
            MsgBox("親を子に移動しようとしています。循環参照のため移動できません。", _
                MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
            Exit Sub
        End If


        Me.objNodeFrom.Remove()
        Me.objNodeTo.Nodes.Add(Me.objNodeFrom)
        Me.objNodeTo.Expand()

        'Dim strPathTo As String = getNodePath(Me.objNodeFrom)
        'Dim strTagTo As String = Me.objNodeTo.Tag

        'Call setNewPath(Split(Me.objNodeFrom.Tag, ",")(0), strPathFrom, strPathTo)
        'Call setMoveNode(strTagFrom, strTagTo)

        Me.objNodeFrom = Nothing
        Me.objNodeTo = Nothing

        'Call setLock(str社員コード, "Editing")

        blnUpdated = True

    End Sub

    Private Sub objTreeView_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles objTreeView.MouseMove

        If Me.blnDrag <> True Then
            Exit Sub
        End If

        Me.objNodeTo = Me.objTreeView.GetNodeAt(New Point(e.X, e.Y))
        Me.objTreeView.SelectedNode = Me.objNodeTo

    End Sub

#End Region

#Region "objTreeView に Node を追加する"

    Private Sub objToolStripMenuItemAddCustomer_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemAddCustomer.Click

        Dim str顧客コード As String = getNewCustomerCode()

        Dim objNode As New TreeNode
        objNode.Text = "[" + str顧客コード + "]:" + "顧客名" + str顧客コード
        objNode.Tag = "Customer," + str顧客コード
        objNode.SelectedImageIndex = 1
        objNode.ImageIndex = 1

        Me.objTreeView.SelectedNode.Nodes.Add(objNode)
        Me.objTreeView.SelectedNode.Expand()

    End Sub

    Private Sub objToolStripMenuItemAddBusiness_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemAddBusiness.Click

        Dim str業務コード As String = getNewBusinessCode()

        Dim objNode As New TreeNode
        objNode.Text = "[" + str業務コード + "]:" + "業務名" + str業務コード
        objNode.Tag = "Business," + str業務コード
        objNode.SelectedImageIndex = 2
        objNode.ImageIndex = 2

        Me.objTreeView.SelectedNode.Nodes.Add(objNode)
        Me.objTreeView.SelectedNode.Expand()

    End Sub

    Private Sub objToolStripMenuItemAddClass_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemAddClass.Click

        Dim str分類コード As String = getNewClassCode()

        Dim objNode As New TreeNode
        objNode.Text = "[" + str分類コード + "]:" + "分類名" + str分類コード
        objNode.Tag = "Class," + str分類コード
        objNode.SelectedImageIndex = 3
        objNode.ImageIndex = 3

        Me.objTreeView.SelectedNode.Nodes.Add(objNode)
        Me.objTreeView.SelectedNode.Expand()

    End Sub

    Private Sub objToolStripMenuItemAddDetail_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemAddDetail.Click

        Dim str詳細コード As String = getNewDetailCode()

        Dim objNode As New TreeNode
        objNode.Text = "[" + str詳細コード + "]:" + "詳細名" + str詳細コード
        objNode.Tag = "Detail," + str詳細コード
        objNode.SelectedImageIndex = 4
        objNode.ImageIndex = 4

        Me.objTreeView.SelectedNode.Nodes.Add(objNode)
        Me.objTreeView.SelectedNode.Expand()

    End Sub

    ''' <summary>
    ''' TreeView に WorkTime ノードを追加する
    ''' </summary>
    ''' <param name="objArgTreeView"></param>
    ''' <param name="strDate"></param>
    ''' <param name="strTime"></param>
    ''' <param name="strWorktimeCode"></param>
    ''' <param name="strWorktimeDetail"></param>
    ''' <param name="strUserID"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function addWorktimeNode( _
        ByVal objArgTreeView As TreeView, _
        ByVal strDate As String, _
        ByVal strTime As String, _
        ByVal strWorktimeCode As String, _
        ByVal strWorktimeDetail As String, _
        ByVal strUserID As String) As TreeView

        Dim objNode As New TreeNode
        objNode.Text = "[" + strWorktimeCode + "]:" + strDate + "(" + strTime + ")@" + strUserID
        objNode.Tag = "WorkTime," + strWorktimeCode + "," + strWorktimeDetail + "," + strUserID
        objNode.SelectedImageIndex = 5
        objNode.ImageIndex = 5

        objArgTreeView.SelectedNode.Nodes.Add(objNode)

        Return objArgTreeView

    End Function

#End Region

#Region "objTreeView から Node を削除する"

    Private Sub objToolStripMenuItemDeleteCustomer_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemDeleteCustomer.Click

        If MsgBox("顧客 " + Me.objTreeView.SelectedNode.Text + " 配下の情報がすべて削除されます。" _
            + "削除すると復元はできません。よろしいですか？", _
                MsgBoxStyle.Question Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton2) _
            = MsgBoxResult.No Then
            Exit Sub
        End If

        doDeleteWorkTimeAtGridView(Me.objTreeView.SelectedNode)

        Me.objTreeView.SelectedNode.Remove()

    End Sub

    Private Sub objToolStripMenuItemDeleteBusiness_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemDeleteBusiness.Click

        If MsgBox("業務 " + Me.objTreeView.SelectedNode.Text + " 配下の情報がすべて削除されます。" _
            + "削除すると復元はできません。よろしいですか？", _
                MsgBoxStyle.Question Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton2) _
            = MsgBoxResult.No Then
            Exit Sub
        End If

        doDeleteWorkTimeAtGridView(Me.objTreeView.SelectedNode)

        Me.objTreeView.SelectedNode.Remove()

    End Sub

    Private Sub objToolStripMenuItemDeleteClass_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemDeleteClass.Click

        If MsgBox("分類 " + Me.objTreeView.SelectedNode.Text + " 配下の情報がすべて削除されます。" _
            + "削除すると復元はできません。よろしいですか？", _
                MsgBoxStyle.Question Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton2) _
            = MsgBoxResult.No Then
            Exit Sub
        End If

        doDeleteWorkTimeAtGridView(Me.objTreeView.SelectedNode)

        Me.objTreeView.SelectedNode.Remove()

    End Sub

    Private Sub objToolStripMenuItemDeleteDetail_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemDeleteDetail.Click

        If MsgBox("詳細 " + Me.objTreeView.SelectedNode.Text + " 配下の情報がすべて削除されます。" _
            + "削除すると復元はできません。よろしいですか？", _
                MsgBoxStyle.Question Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton2) _
            = MsgBoxResult.No Then
            Exit Sub
        End If

        doDeleteWorkTimeAtGridView(Me.objTreeView.SelectedNode)

        Me.objTreeView.SelectedNode.Remove()

    End Sub

    Private Sub objToolStripMenuItemDeleteWorkTime_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemDeleteWorkTime.Click

        If MsgBox("稼働時間 " + Me.objTreeView.SelectedNode.Text + " の情報が削除されます。" _
            + "削除すると復元はできません。よろしいですか？", _
                MsgBoxStyle.Question Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton2) _
            = MsgBoxResult.No Then
            Exit Sub
        End If

        doDeleteWorkTimeAtGridView(Me.objTreeView.SelectedNode)

        Me.objTreeView.SelectedNode.Remove()

    End Sub

#End Region

#Region "FormClassifyer EventHandler"

    Dim intCloseWithCancel As Integer

    Private Sub FormClassifyer_FormClosing(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing

        If intCloseWithCancel = DialogResult.Cancel Then
            Exit Sub
        End If

        Call doSaveTreeViewToXml(getTopNode(), mConsts.strDbWithPath, Me.objVo)

        Call doSaveTreeStatus(Me.objTreeView)

        Call doSaveSetting(Me.objVo.strUserID, Me.objVo.strUserName)

    End Sub

    Private Sub FormClassifyer_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim strFile As String = Application.StartupPath + "\settings.xml"

        If Not isExistsFile(strFile) Then

            Dim objForm As New fUserInfo
            objForm.ShowDialog(Me)

            If objForm.intDialogResult <> Windows.Forms.DialogResult.OK Then

                intCloseWithCancel = DialogResult.Cancel

                Me.Close()

                Exit Sub

            End If

            Me.objVo.strUserID = objForm.strUserID
            Me.objVo.strUserName = objForm.strUserName

        Else

            Me.objVo = doLoadSetting()

        End If

        Me.objLabelUserInfo.Text = "[" + objVo.strUserID + "]:" + objVo.strUserName

        Call doLoadTreeViewFromXml(getTopNode(), mConsts.strDbWithPath)

        Call doLoadTreeStatus(Me.objTreeView)

        Call DisplayDataGridView(Me.objDateTimePicker.Value)

    End Sub

#End Region

#Region "objToolStripMenuItem EventHandler"

    Private Sub objToolStripMenuItemDateTerm_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemDateTerm.Click

        Dim objVo As cValueObjects.VoDatePair = _
            getDatePairFromTreeNode( _
                getDateListFromTreeNode( _
                    objTreeView.SelectedNode))

        showMessage("Info", _
            objVo.objStart.ToString("yyyy/MM/dd") + " から " + _
            objVo.objEnd.ToString("yyyy/MM/dd") + " まで")

    End Sub

    Private Sub objToolStripMenuItemSaveToXml_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemSaveToXml.Click

        Dim objForm As New fExport

        objForm.ShowDialog(Me)
        objForm.Close()

        Dim objVo As cValueObjects.VoTreeNodeAndDatePair = _
            getTreeNodeAndDatePair(objForm, getSelectedTreeNode, False)

        objSaveFileDialog.Title = "ファイルの保存先を指定してください。"
        objSaveFileDialog.FileName _
            = objVo.objDatePair.objStart.ToString("yyyyMMdd") _
            + "-" _
            + objVo.objDatePair.objEnd.ToString("yyyyMMdd") _
            + ".xml"

        If objSaveFileDialog.ShowDialog(Me) <> Windows.Forms.DialogResult.Cancel Then

            doSaveTreeViewToXml(objVo.objNode, objSaveFileDialog.FileName, Me.objVo)

        End If

    End Sub

    Private Sub objToolStripMenuItemSaveToXmlWithEmptyNodeDelete_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemSaveToXmlWithEmptyNodeDelete.Click

        Dim objForm As New fExport

        objForm.ShowDialog(Me)
        objForm.Close()

        Dim objVo As cValueObjects.VoTreeNodeAndDatePair = _
            getTreeNodeAndDatePair(objForm, getSelectedTreeNode, True)

        objSaveFileDialog.Title = "ファイルの保存先を指定してください。"
        objSaveFileDialog.FileName _
            = objVo.objDatePair.objStart.ToString("yyyyMMdd") _
            + "-" _
            + objVo.objDatePair.objEnd.ToString("yyyyMMdd") _
            + ".xml"

        If objSaveFileDialog.ShowDialog(Me) <> Windows.Forms.DialogResult.Cancel Then

            doSaveTreeViewToXml(objVo.objNode, objSaveFileDialog.FileName, Me.objVo)

        End If

    End Sub

    Private Sub objToolStripMenuItemGetTotalTimeByNode_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemGetTotalTimeByNode.Click

        Dim objForm As New fExport

        objForm.ShowDialog(Me)
        objForm.Close()

        Dim objVo As cValueObjects.VoTreeNodeAndDatePair = _
            getTreeNodeAndDatePair(objForm, Me.objTreeView.SelectedNode, True)

        Dim dblSum As Double = _
            getChildNodeWorkTimeSum( _
                getSelectedNode( _
                    objVo.objNode, Me.objTreeView.SelectedNode))

        Dim strMessage As String = _
            objVo.objDatePair.objStart.ToString("yyyy/MM/dd") + "から" + vbCrLf + _
            objVo.objDatePair.objEnd.ToString("yyyy/MM/dd") + "の" + vbCrLf + _
            "「" + Me.objTreeView.SelectedNode.Text + "」配下の時間の合計は、" + vbCrLf + _
            " " + dblSum.ToString("0.00") + " 時間です。"

        showMessage("Info", strMessage)

    End Sub

    Private Sub objToolStripMenuItemBusinessCountSaveToCsv_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemBusinessCountSaveToCsv.Click

        Dim objForm As New fExport

        objForm.ShowDialog(Me)
        objForm.Close()

        Dim objVoTreeNodeAndDatePair As cValueObjects.VoTreeNodeAndDatePair = _
            getTreeNodeAndDatePair(objForm, getTopNode, True)

        Dim objVoList As New List(Of cValueObjects.VoCountByBusiness)

        For Each objSubNode1 As TreeNode In objVoTreeNodeAndDatePair.objNode.Nodes

            For Each objSubNode2 As TreeNode In objSubNode1.Nodes

                Dim dblSum As Double = getChildNodeWorkTimeSum(objSubNode2)

                Dim objVo As New cValueObjects.VoCountByBusiness()

                objVo.strBusinessText = getTextFromText(objSubNode2.Text)
                objVo.dblValue = dblSum

                objVoList.Add(objVo)

            Next

        Next

        objSaveFileDialog.Title = "ファイルの保存先を指定してください。"
        objSaveFileDialog.FileName _
            = objVoTreeNodeAndDatePair.objDatePair.objStart.ToString("yyyyMMdd") _
            + "-" _
            + objVoTreeNodeAndDatePair.objDatePair.objEnd.ToString("yyyyMMdd") _
            + ".csv"

        If objSaveFileDialog.ShowDialog(Me) <> Windows.Forms.DialogResult.Cancel Then

            Dim objStreamWriter As New StreamWriter( _
                objSaveFileDialog.FileName, False, _
                Encoding.GetEncoding("Shift_JIS"))

            For Each objVo As cValueObjects.VoCountByBusiness In objVoList
                objStreamWriter.WriteLine( _
                    objVo.strBusinessText + "," + _
                    objVo.dblValue.ToString("0.00"))
            Next
            objStreamWriter.Close()

            showMessage("Info", "保存しました。")

        End If

    End Sub

    Private Sub objToolStripMenuItemLoadFromXmlAdd_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemLoadFromXmlAdd.Click

        If objOpenFileDialog.ShowDialog(Me) <> Windows.Forms.DialogResult.Cancel Then

            Call doLoadTreeViewFromXml(getTopNode(), Me.objOpenFileDialog.FileName)

        End If

    End Sub

    Private Sub objToolStripMenuItemLoadFromXmlOverride_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemLoadFromXmlOverride.Click

        If objOpenFileDialog.ShowDialog(Me) <> Windows.Forms.DialogResult.Cancel Then

            Call doOverwriteTreeViewFromXml(getTopNode(), Me.objOpenFileDialog.FileName)

        End If

    End Sub

    Private Sub objToolStripMenuItemDailyReport_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemDailyReport.Click

        Dim objXL As Excel.Application
        Dim objSheet As Excel.Worksheet

        objSaveFileDialog.InitialDirectory = Application.StartupPath
        objSaveFileDialog.FileName = "dreport_" + objDateTimePicker.Value.ToString("yyyyMMdd") + ".xlsx"
        If objSaveFileDialog.ShowDialog(Me) = Windows.Forms.DialogResult.Cancel Then
            Exit Sub
        End If

        objXL = CreateObject("Excel.Application")
        objXL.Visible = False
        objXL.Workbooks.Add()
        objSheet = objXL.ActiveWorkbook.ActiveSheet

        ' DateTimePicker の日付の作業データを抽出する
        Dim objVoList As List(Of VoDataGrid) = getDailyWorks(objDateTimePicker.Value, getTopNode())

        Call objVoList.Sort(AddressOf doCompare)

        Dim intRowIndex As Integer = 0
        Dim dblTotalSpendTime As Double = 0.0

        For Each objVo As VoDataGrid In objVoList

            objSheet.Range("A" + (intRowIndex + 1).ToString()).Value = intRowIndex + 1
            objSheet.Range("B" + (intRowIndex + 1).ToString()).Value = objVo.strTimeStart
            objSheet.Range("C" + (intRowIndex + 1).ToString()).Value = objVo.strTimeEnd
            objSheet.Range("D" + (intRowIndex + 1).ToString()).Value = objVo.strJob
            objSheet.Range("E" + (intRowIndex + 1).ToString()).Value = _
                getWorkedTime(objVo.strTimeStart, objVo.strTimeEnd).ToString("00.00")

            dblTotalSpendTime = dblTotalSpendTime _
                + getWorkedTime(objVo.strTimeStart, objVo.strTimeEnd)

            intRowIndex = intRowIndex + 1

        Next

        objSheet.Range("A" + (intRowIndex + 1).ToString()).Value = dblTotalSpendTime.ToString("0.00")

        objXL.ActiveWorkbook.SaveAs(objSaveFileDialog.FileName)
        objXL.ActiveWorkbook.Close()

        showMessage("Info", "Excel ファイルに日報を出力しました。")

    End Sub

    Private Sub objToolStripMenuItemWeeklyReport_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemWeeklyReport.Click

        Dim objXL As Excel.Application
        Dim objSheet As Excel.Worksheet

        Dim objStartOfWeek As Date = getStartOfWeek(objDateTimePicker.Value)
        Dim objEndOfWeek As Date = getEndOfWeek(objDateTimePicker.Value)

        objSaveFileDialog.InitialDirectory = Application.StartupPath
        objSaveFileDialog.FileName = "wreport_" + objStartOfWeek.ToString("yyyyMMdd") + "-" + objEndOfWeek.ToString("yyyyMMdd") + ".xlsx"
        If objSaveFileDialog.ShowDialog(Me) = Windows.Forms.DialogResult.Cancel Then
            Exit Sub
        End If

        objXL = CreateObject("Excel.Application")
        objXL.Visible = False
        objXL.Workbooks.Add()
        objSheet = objXL.ActiveWorkbook.ActiveSheet

        ' DateTimePicker の日付の作業データを抽出する
        Dim objVoList As List(Of VoDataGrid) = getWeeklyWorks(objDateTimePicker.Value, getTopNode())

        Call objVoList.Sort(AddressOf doCompare)

        Dim intRowIndex As Integer = 0
        Dim dblTotalSpendTime As Double = 0.0
        Dim strSavedDate As String = ""

        For Each objVo As VoDataGrid In objVoList

            If strSavedDate <> objVo.strDate Then
                If Not isNothingOrEmptyString(strSavedDate) Then
                    intRowIndex = intRowIndex + 1
                End If
                strSavedDate = objVo.strDate
            End If

            objSheet.Range("A" + (intRowIndex + 1).ToString()).Value = intRowIndex + 1
            objSheet.Range("B" + (intRowIndex + 1).ToString()).Value = getFormattedDateString(objVo.strDate)
            objSheet.Range("C" + (intRowIndex + 1).ToString()).Value = objVo.strTimeStart
            objSheet.Range("D" + (intRowIndex + 1).ToString()).Value = objVo.strTimeEnd
            objSheet.Range("E" + (intRowIndex + 1).ToString()).Value = objVo.strJob
            objSheet.Range("F" + (intRowIndex + 1).ToString()).Value = _
                getWorkedTime(objVo.strTimeStart, objVo.strTimeEnd).ToString("00.00")

            dblTotalSpendTime = dblTotalSpendTime _
                + getWorkedTime(objVo.strTimeStart, objVo.strTimeEnd)

            intRowIndex = intRowIndex + 1

        Next

        objSheet.Range("F" + (intRowIndex + 1).ToString()).Value = dblTotalSpendTime.ToString("0.00")

        objXL.ActiveWorkbook.SaveAs(objSaveFileDialog.FileName)
        objXL.ActiveWorkbook.Close()

        showMessage("Info", "Excel ファイルに週報を出力しました。")

    End Sub

    Private Sub objToolStripMenuItemMonthlyReport_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles objToolStripMenuItemMonthlyReport.Click

        Dim objXL As Excel.Application
        Dim objSheet As Excel.Worksheet

        objSaveFileDialog.InitialDirectory = Application.StartupPath
        objSaveFileDialog.FileName = "mreport_" + objDateTimePicker.Value.ToString("yyyyMM") + ".xlsx"
        If objSaveFileDialog.ShowDialog(Me) = Windows.Forms.DialogResult.Cancel Then
            Exit Sub
        End If

        ' DateTimePicker の日付の作業データを抽出する
        Dim objVoList As List(Of VoDataGrid) = getMonthlyWorks(objDateTimePicker.Value, getTopNode())

        Call objVoList.Sort(AddressOf doCompare)

        Dim intRowIndex As Integer = 0
        Dim dblTotalDateSpendTime As Double = 0.0
        Dim dblTotalMonthSpendTime As Double = 0.0
        Dim strSavedDate As String = ""
        Dim strStartOfDate As String = ""
        Dim strEndOfDate As String = ""

        Dim objVoListOfMonthly As New List(Of VoMonthlyWork)

        ' *-------------------------
        ' * 日付ごとの構造体を作る
        ' *-------------------------

        Dim objMonthlyVo As VoMonthlyWork

        For Each objVo As VoDataGrid In objVoList

            If strSavedDate <> objVo.strDate Then

                If Not isNothingOrEmptyString(strSavedDate) Then

                    objMonthlyVo = New VoMonthlyWork

                    objMonthlyVo.strDate = strSavedDate
                    objMonthlyVo.strStartOfDate = strStartOfDate
                    objMonthlyVo.strEndOfDate = strEndOfDate
                    objMonthlyVo.dblTotalDateSpendTime = dblTotalDateSpendTime.ToString("0.00")

                    objVoListOfMonthly.Add(objMonthlyVo)

                    strStartOfDate = ""
                    strEndOfDate = ""
                    dblTotalDateSpendTime = 0

                End If

                strSavedDate = objVo.strDate

            End If

            strStartOfDate = getMoreOldDate(strStartOfDate, objVo.strTimeStart)
            strEndOfDate = getMoreNewDate(strEndOfDate, objVo.strTimeEnd)
            dblTotalDateSpendTime = dblTotalDateSpendTime _
                + getWorkedTime(objVo.strTimeStart, objVo.strTimeEnd)

        Next

        objMonthlyVo = New VoMonthlyWork

        objMonthlyVo.strDate = strSavedDate
        objMonthlyVo.strStartOfDate = strStartOfDate
        objMonthlyVo.strEndOfDate = strEndOfDate
        objMonthlyVo.dblTotalDateSpendTime = dblTotalDateSpendTime.ToString("0.00")

        objVoListOfMonthly.Add(objMonthlyVo)

        ' *-------------------------
        ' * Excel ファイルを作る
        ' *-------------------------

        objXL = CreateObject("Excel.Application")
        objXL.Visible = False
        objXL.Workbooks.Add()
        objSheet = objXL.ActiveWorkbook.ActiveSheet

        Dim objStartOfMonth As Date = getStartOfMonth(objDateTimePicker.Value)
        Dim objEndOfMonth As Date = getEndOfMonth(objDateTimePicker.Value)

        Dim objDate As Date = objStartOfMonth

        intRowIndex = 0

        Do

            Dim objReturnVo As VoMonthlyWork = Nothing

            For Each objVo As VoMonthlyWork In objVoListOfMonthly
                If objVo.strDate = objDate.ToString("yyyyMMdd") Then
                    objReturnVo = objVo
                    Exit For
                End If
            Next

            If Not isNothing(objReturnVo) Then

                objSheet.Range("A" + (intRowIndex + 1).ToString()).Value = getFormattedDateString(objReturnVo.strDate)
                objSheet.Range("B" + (intRowIndex + 1).ToString()).Value = objReturnVo.strStartOfDate
                objSheet.Range("C" + (intRowIndex + 1).ToString()).Value = objReturnVo.strEndOfDate
                objSheet.Range("D" + (intRowIndex + 1).ToString()).Value = objReturnVo.dblTotalDateSpendTime

                dblTotalMonthSpendTime = dblTotalMonthSpendTime _
                    + objReturnVo.dblTotalDateSpendTime

            End If

            intRowIndex = intRowIndex + 1

            If objDate = objEndOfMonth Then
                Exit Do
            End If

            objDate = objDate.AddDays(1)

        Loop

        objSheet.Range("D" + (intRowIndex + 1).ToString()).Value = dblTotalMonthSpendTime.ToString("0.00")

        objXL.ActiveWorkbook.SaveAs(objSaveFileDialog.FileName)
        objXL.ActiveWorkbook.Close()

        showMessage("Info", "Excel ファイルに月報を出力しました。")

    End Sub

#End Region

#Region "objDataGridView_EventHandler"

    Private Sub objDataGridView_CellContentDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles objDataGridView.CellContentDoubleClick

        Select Case objDataGridView.Columns(e.ColumnIndex).Name

            Case "objColumnJob"

                Dim strWorkTimeCode As String = _
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeCode").Value

                objTreeView.SelectedNode = _
                    getTreeNodeByCode(strWorkTimeCode, getTopNode())

        End Select

    End Sub

    Private Sub objDataGridView_CellValidated(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles objDataGridView.CellValidated

        Dim strStart As String = objDataGridView.Rows(e.RowIndex).Cells("objColumnStart").Value
        Dim strEnd As String = objDataGridView.Rows(e.RowIndex).Cells("objColumnEnd").Value

        If isNothingOrEmptyString(strStart) = True _
        Or isNothingOrEmptyString(strEnd) = True Then
            Exit Sub
        End If

        ' 稼働時間を計算して表示する
        objDataGridView.Rows(e.RowIndex).Cells("objColumnSpendTime").Value = _
            getWorkedTime(strStart, strEnd).ToString("00.00")

    End Sub

    Private Sub objDataGridView_CellValidating(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewCellValidatingEventArgs) Handles objDataGridView.CellValidating

        e.Cancel = False

        If isNothingOrEmptyString(e.FormattedValue) = True Then
            Exit Sub
        End If

        Select Case objDataGridView.Columns(e.ColumnIndex).Name

            Case "objColumnStart", "objColumnEnd"

                If isCorrectTimeString(e.FormattedValue) = False Then
                    e.Cancel = True
                    Exit Sub
                End If

                If objDataGridView.Rows(e.RowIndex).Cells(e.ColumnIndex).Value <> e.FormattedValue Then

                    Dim strWorkTimeCode As String = _
                        objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeCode").Value

                    If isNothingOrEmptyString(strWorkTimeCode) = False Then
                        getTreeNodeByCode(strWorkTimeCode, getTopNode()).Remove()
                    End If

                    ' 関連付けをクリアする
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnJob").Value = ""
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeDetail").Value = ""
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeCode").Value = ""

                End If

            Case Else

        End Select

    End Sub

    Private Sub objDataGridView_RowValidated(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles objDataGridView.RowValidated

        ' 行番号を表示する
        objDataGridView.Rows(e.RowIndex).Cells("objColumnId").Value = e.RowIndex + 1

        ' 合計稼働時間を表示する
        Dim dblTotalSpendTime As Double = 0.0
        For Each objDataRow As DataGridViewRow In objDataGridView.Rows
            dblTotalSpendTime = dblTotalSpendTime + _
                getNumberFromString(objDataRow.Cells("objColumnSpendTime").Value)
        Next
        objLabelTotalSpendTime.Text = dblTotalSpendTime.ToString("0.00")

    End Sub

    Private Sub objDataGridView_CellContentClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles objDataGridView.CellContentClick

        Select Case objDataGridView.Columns(e.ColumnIndex).Name

            Case "objColumnAddDelButton"

                If isNothingOrEmptyString(objDataGridView.Rows(e.RowIndex).Cells("objColumnJob").Value) = False Then

                    Dim objWorkTimeCode As String = _
                        objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeCode").Value

                    getSpecifiedNode(getTopNode(), objWorkTimeCode).Remove()

                    ' 関連付けをクリアする
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnJob").Value = ""
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeDetail").Value = ""
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeCode").Value = ""

                Else

                    If IsValidatedDataRow(e.RowIndex) = False Then
                        Exit Sub
                    End If

                    If isNodeType(eNodeType.vDetail, objTreeView.SelectedNode) = False Then
                        showMessage("Info", "選択された NodeType は、詳細ではありませんので関連付けできません。")
                        Exit Sub
                    End If

                    objDataGridView.Rows(e.RowIndex).Cells("objColumnJob").Value = objTreeView.SelectedNode.Text

                    Dim strDate As String = _
                        objDateTimePicker.Value.ToString("yyyyMMdd")

                    Dim strTime As String = _
                        objDataGridView.Rows(e.RowIndex).Cells("objColumnSpendTime").Value

                    Dim strWorkTimeCode As String = getNewWorkTimeCode()

                    Dim strWorkTimeDetail As String = _
                        objDataGridView.Rows(e.RowIndex).Cells("objColumnStart").Value + "-" + _
                        objDataGridView.Rows(e.RowIndex).Cells("objColumnEnd").Value + "@" + _
                        strDate

                    Me.objTreeView = _
                        addWorktimeNode( _
                            Me.objTreeView, _
                            strDate, _
                            strTime, _
                            strWorkTimeCode, _
                            strWorkTimeDetail, _
                            Me.objVo.strUserID)

                    objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeDetail").Value = strWorkTimeDetail
                    objDataGridView.Rows(e.RowIndex).Cells("objColumnWorkTimeCode").Value = strWorkTimeCode

                End If

            Case Else

        End Select

    End Sub

#End Region

End Class