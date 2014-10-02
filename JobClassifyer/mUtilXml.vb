﻿
Imports System.Xml
Imports System.IO

Module mUtilXml

#Region "SaveTreeViewToXml"

    Public Sub doSaveTreeViewToXml( _
        ByVal objTreeNode As TreeNode, ByVal strFileNameWithPath As String, _
        ByVal objUserInfoVo As cValueObjects.VoUserInfo)

        Dim objXmlDoc As XmlDocument = New XmlDocument()

        '---------------------------
        ' XML 宣言の追加
        '---------------------------
        Call objXmlDoc.AppendChild( _
            objXmlDoc.CreateXmlDeclaration("1.0", "Shift-JIS", String.Empty))

        '---------------------------
        ' Tree から XML へのコピー
        '---------------------------
        Call objXmlDoc.AppendChild( _
            doCopyTreeNodeToXmlNode( _
                objXmlDoc, objTreeNode, objUserInfoVo))

        '---------------------------
        ' XML の保存
        '---------------------------
        Call objXmlDoc.Save(strFileNameWithPath)

        Call MsgBox("データを保存しました。", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

    End Sub

    Public Function doCopyTreeNodeToXmlNode( _
        ByVal objXmlDoc As XmlDocument, ByVal objTreeNode As TreeNode, _
        ByVal objUserInfoVo As cValueObjects.VoUserInfo) _
            As XmlNode

        Dim strTagName As String = getTagNameFromTag(objTreeNode.Tag)

        Dim objXmlNode As XmlNode = _
            objXmlDoc.CreateNode(XmlNodeType.Element, strTagName, "")

        If strTagName = "WorkTime" Then

            Dim strDate As String = getDateFromNodeText(objTreeNode.Text)
            Dim strTime As String = getTimeFromNodeText(objTreeNode.Text)
            Dim strDetail As String = getWorkTimeDetailFromTag(objTreeNode)

            Dim objAttributeDate As XmlAttribute = _
                objXmlDoc.CreateAttribute("Date")

            Dim objAttributeTime As XmlAttribute = _
                objXmlDoc.CreateAttribute("Time")

            Dim objAttributeStart As XmlAttribute = _
                objXmlDoc.CreateAttribute("Start")

            Dim objAttributeEnd As XmlAttribute = _
                objXmlDoc.CreateAttribute("End")

            Dim objAttributeUserID As XmlAttribute = _
                objXmlDoc.CreateAttribute("UserID")

            objAttributeDate.Value = strDate
            objAttributeTime.Value = strTime
            objAttributeStart.Value = getTimeStartFromWorkDetail(strDetail)
            objAttributeEnd.Value = getTimeEndFromWorkDetail(strDetail)
            objAttributeUserID.Value = getUserIDFromTag(objTreeNode)

            objXmlNode.Attributes.Append(objAttributeDate)
            objXmlNode.Attributes.Append(objAttributeTime)
            objXmlNode.Attributes.Append(objAttributeStart)
            objXmlNode.Attributes.Append(objAttributeEnd)
            objXmlNode.Attributes.Append(objAttributeUserID)

        Else

            Dim objAttribute As XmlAttribute = _
                objXmlDoc.CreateAttribute("Text")

            objAttribute.Value = _
                getTextFromText(objTreeNode.Text)

            objXmlNode.Attributes.Append(objAttribute)

            For Each objNode As TreeNode In objTreeNode.Nodes

                Call objXmlNode.AppendChild( _
                    doCopyTreeNodeToXmlNode( _
                        objXmlDoc, objNode, objUserInfoVo))

            Next

        End If


        doCopyTreeNodeToXmlNode = objXmlNode

    End Function

#End Region

#Region "LoadTreeViewFromXml"

    Public Sub doOverwriteTreeViewFromXml(ByVal objTreeNode As TreeNode, ByVal strFileNameWithPath As String)

        Dim objXmlDoc As XmlDocument = New XmlDocument()

        If Not File.Exists(strFileNameWithPath) Then
            Exit Sub
        End If

        '---------------------------
        ' XML の読み込み
        '---------------------------
        Call objXmlDoc.Load(strFileNameWithPath)

        '---------------------------
        ' XML の DB への保存
        '---------------------------
        For Each objNode As XmlNode In objXmlDoc.ChildNodes(1)
            Call doOverwriteXmlNodeToTreeView(objTreeNode, objNode, "TP00000", "TP00000")
        Next

    End Sub

    Public Sub doLoadTreeViewFromXml(ByVal objTreeNode As TreeNode, ByVal strFileNameWithPath As String)

        Dim objXmlDoc As XmlDocument = New XmlDocument()

        If Not File.Exists(strFileNameWithPath) Then
            Exit Sub
        End If

        '---------------------------
        ' XML の読み込み
        '---------------------------
        Call objXmlDoc.Load(strFileNameWithPath)

        '---------------------------
        ' XML の DB への保存
        '---------------------------
        For Each objNode As XmlNode In objXmlDoc.ChildNodes(1)
            Call doCopyXmlNodeToTreeView(objTreeNode, objNode, "TP00000", "TP00000")
        Next

    End Sub

    Public Sub doOverwriteXmlNodeToTreeView( _
        ByVal objTreeNode As TreeNode, ByVal objXmlNode As XmlNode, _
        ByVal strParentCode As String, ByVal strPath As String)

        Dim strCode As String
        Dim strName As String = ""
        Dim objSubNode As New TreeNode
        Dim blnAdded As Boolean = False

        Select Case objXmlNode.Name

            Case "Customer"

                strCode = "CS" + Format(objCustomerCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Customer," + strCode
                objSubNode.SelectedImageIndex = 1
                objSubNode.ImageIndex = 1

            Case "Business"

                strCode = "BS" + Format(objBusinessCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Business," + strCode
                objSubNode.SelectedImageIndex = 2
                objSubNode.ImageIndex = 2

            Case "Class"

                strCode = "CL" + Format(objClassCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Class," + strCode
                objSubNode.SelectedImageIndex = 3
                objSubNode.ImageIndex = 3

            Case "Detail"

                strCode = "DT" + Format(objDetailCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Detail," + strCode
                objSubNode.SelectedImageIndex = 4
                objSubNode.ImageIndex = 4

            Case "WorkTime"

                Dim strDate As String = objXmlNode.Attributes("Date").Value
                Dim strTime As String = objXmlNode.Attributes("Time").Value
                Dim strStart As String = objXmlNode.Attributes("Start").Value
                Dim strEnd As String = objXmlNode.Attributes("End").Value

                Dim strUserID As String
                If Not isNothing(objXmlNode.Attributes("UserID")) Then
                    strUserID = objXmlNode.Attributes("UserID").Value
                Else
                    strUserID = ""
                End If

                Dim strDetail As String = strStart + "-" + strEnd + "@" + strDate

                strCode = "WT" + Format(objWorkTimeCodeManager.getNewCode(), "00000")

                objSubNode.Text = "[" + strCode + "]:" + strDate + "(" + strTime + ")@" + strUserID
                objSubNode.Tag = "WorkTime," + strCode + "," + strDetail + "," + strUserID
                objSubNode.SelectedImageIndex = 5
                objSubNode.ImageIndex = 5

                For Each objNode As TreeNode In objTreeNode.Nodes

                    Dim strDate2 As String = getDateFromNodeText(objNode.Text)
                    Dim strDetail2 As String = getWorkTimeDetailFromTag(objNode)
                    Dim strStart2 As String = getTimeStartFromWorkDetail(strDetail2)
                    Dim strEnd2 As String = getTimeEndFromWorkDetail(strDetail2)
                    Dim strUserID2 As String = getUserIDFromTag(objNode)

                    If strDate = strDate2 And strStart = strStart2 And _
                       strEnd = strEnd2 And strUserID = strUserID2 Then
                        objSubNode = objNode
                        blnAdded = True
                        Exit For
                    End If
                Next

            Case Else
                Exit Sub

        End Select

        For Each objNode As TreeNode In objTreeNode.Nodes
            If objNode.Text.Substring(10) = strName Then
                objSubNode = objNode
                blnAdded = True
                Exit For
            End If
        Next

        If blnAdded = False Then
            objTreeNode.Nodes.Add(objSubNode)
        End If

        For Each objChildNode As XmlNode In objXmlNode.ChildNodes

            Call doOverwriteXmlNodeToTreeView(objSubNode, objChildNode, strCode, strPath + strCode)

        Next

    End Sub

    Public Sub doCopyXmlNodeToTreeView( _
        ByVal objTreeNode As TreeNode, ByVal objXmlNode As XmlNode, _
        ByVal strParentCode As String, ByVal strPath As String)

        Dim strCode As String
        Dim strName As String
        Dim objSubNode As New TreeNode

        Select Case objXmlNode.Name

            Case "Customer"

                strCode = "CS" + Format(objCustomerCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Customer," + strCode
                objSubNode.SelectedImageIndex = 1
                objSubNode.ImageIndex = 1

                objTreeNode.Nodes.Add(objSubNode)

            Case "Business"

                strCode = "BS" + Format(objBusinessCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Business," + strCode
                objSubNode.SelectedImageIndex = 2
                objSubNode.ImageIndex = 2

                objTreeNode.Nodes.Add(objSubNode)

            Case "Class"

                strCode = "CL" + Format(objClassCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Class," + strCode
                objSubNode.SelectedImageIndex = 3
                objSubNode.ImageIndex = 3

                objTreeNode.Nodes.Add(objSubNode)

            Case "Detail"

                strCode = "DT" + Format(objDetailCodeManager.getNewCode(), "00000")
                strName = objXmlNode.Attributes("Text").Value

                objSubNode.Text = "[" + strCode + "]:" + strName
                objSubNode.Tag = "Detail," + strCode
                objSubNode.SelectedImageIndex = 4
                objSubNode.ImageIndex = 4

                objTreeNode.Nodes.Add(objSubNode)

            Case "WorkTime"

                Dim strDate As String = objXmlNode.Attributes("Date").Value
                Dim strTime As String = objXmlNode.Attributes("Time").Value
                Dim strStart As String = objXmlNode.Attributes("Start").Value
                Dim strEnd As String = objXmlNode.Attributes("End").Value

                Dim strUserID As String
                If Not isNothing(objXmlNode.Attributes("UserID")) Then
                    strUserID = objXmlNode.Attributes("UserID").Value
                Else
                    strUserID = ""
                End If

                Dim strDetail As String = strStart + "-" + strEnd + "@" + strDate

                strCode = "WT" + Format(objWorkTimeCodeManager.getNewCode(), "00000")

                objSubNode.Text = "[" + strCode + "]:" + strDate + "(" + strTime + ")@" + strUserID
                objSubNode.Tag = "WorkTime," + strCode + "," + strDetail + "," + strUserID
                objSubNode.SelectedImageIndex = 5
                objSubNode.ImageIndex = 5

                objTreeNode.Nodes.Add(objSubNode)

            Case Else
                Exit Sub

        End Select

        'If strCode = "" Then
        '    Exit Sub
        'End If

        For Each objChildNode As XmlNode In objXmlNode.ChildNodes

            Call doCopyXmlNodeToTreeView(objSubNode, objChildNode, strCode, strPath + strCode)

        Next

    End Sub

#End Region

#Region "SaveTreeStatus"

    Public Sub doSaveTreeStatus(ByVal objTree As TreeView)

        Dim objXmlDoc As XmlDocument = New XmlDocument()

        '---------------------------
        ' XML 宣言の追加
        '---------------------------
        Call objXmlDoc.AppendChild( _
            objXmlDoc.CreateXmlDeclaration("1.0", "Shift-JIS", String.Empty))

        '---------------------------
        ' Tree から XML へのコピー
        '---------------------------
        Call objXmlDoc.AppendChild(doSaveTreeStatusToXmlNode(objXmlDoc, objTree.Nodes.Item(0)))

        '---------------------------
        ' XML の保存
        '---------------------------
        Call objXmlDoc.Save(Application.StartupPath + "\treestatus.xml")

    End Sub

    Public Function doSaveTreeStatusToXmlNode( _
        ByVal objXmlDoc As XmlDocument, ByVal objTreeNode As TreeNode) As XmlNode

        Dim strTagName = getTagNameFromTag(objTreeNode.Tag)

        Dim objXmlNode As XmlNode = _
            objXmlDoc.CreateNode(XmlNodeType.Element, strTagName, "")

        Dim objAttributeIsExpaned As XmlAttribute = _
            objXmlDoc.CreateAttribute("IsExpaned")

        objAttributeIsExpaned.Value = CStr(objTreeNode.IsExpanded)

        objXmlNode.Attributes.Append(objAttributeIsExpaned)

        Dim objAttributeIsSelected As XmlAttribute = _
            objXmlDoc.CreateAttribute("IsSelected")

        If objTreeNode.IsSelected = True Then
            objAttributeIsSelected.Value = True
        Else
            objAttributeIsSelected.Value = False
        End If

        objXmlNode.Attributes.Append(objAttributeIsSelected)

        For Each objNode As TreeNode In objTreeNode.Nodes

            Call objXmlNode.AppendChild( _
                doSaveTreeStatusToXmlNode(objXmlDoc, objNode))

        Next

        doSaveTreeStatusToXmlNode = objXmlNode

    End Function

#End Region

#Region "LoadTreeStatus"

    Public Sub doLoadTreeStatus(ByVal objTree As TreeView)

        Dim strFile As String = Application.StartupPath + "\treestatus.xml"

        If Not File.Exists(strFile) Then
            Exit Sub
        End If

        Dim objXmlDoc As XmlDocument = New XmlDocument()

        '---------------------------
        ' XML の読み込み
        '---------------------------
        Call objXmlDoc.Load(strFile)

        '---------------------------
        ' TreeStatus の読み込み
        '---------------------------
        Call doLoadTreeStatusToTreeNode(objXmlDoc.ChildNodes.Item(1), objTree.Nodes.Item(0))

    End Sub

    Public Sub doLoadTreeStatusToTreeNode( _
        ByVal objArgXmlNode As XmlNode, ByVal objArgTrteeNode As TreeNode)

        If objArgXmlNode.Attributes("IsExpaned").Value = True Then
            Call objArgTrteeNode.Expand()
        End If

        If objArgXmlNode.Attributes("IsSelected").Value = True Then
            objArgTrteeNode.TreeView.SelectedNode = objArgTrteeNode
        End If

        For intIndex As Integer = 0 To objArgTrteeNode.Nodes.Count - 1

            Dim objTreeNode As TreeNode = _
                objArgTrteeNode.Nodes.Item(intIndex)
            Dim objXmlNode As XmlNode = _
                objArgXmlNode.ChildNodes.Item(intIndex)

            If Not objXmlNode Is Nothing Then

                Call doLoadTreeStatusToTreeNode(objXmlNode, objTreeNode)

            End If

        Next

    End Sub

#End Region

#Region "SaveSetting"

    Public Sub doSaveSetting(ByVal strUserID As String, ByVal strUserName As String)

        Dim objXmlDoc As XmlDocument = New XmlDocument()

        '---------------------------
        ' XML 宣言の追加
        '---------------------------
        Call objXmlDoc.AppendChild( _
            objXmlDoc.CreateXmlDeclaration("1.0", "Shift-JIS", String.Empty))

        '---------------------------
        ' XML の作成
        '---------------------------
        Dim objXmlNode_Root As XmlNode = _
            objXmlDoc.CreateNode(XmlNodeType.Element, "Settings", "")

        Dim objXmlNode_UserID As XmlNode = _
            objXmlDoc.CreateNode(XmlNodeType.Element, "UserID", "")

        Dim objXmlNode_UserName As XmlNode = _
            objXmlDoc.CreateNode(XmlNodeType.Element, "UserName", "")

        objXmlNode_Root.AppendChild(objXmlNode_UserID)
        objXmlNode_Root.AppendChild(objXmlNode_UserName)

        objXmlNode_UserID.InnerText = strUserID
        objXmlNode_UserName.InnerText = strUserName

        objXmlDoc.AppendChild(objXmlNode_Root)

        '---------------------------
        ' XML の保存
        '---------------------------
        Call objXmlDoc.Save(Application.StartupPath + "\settings.xml")

    End Sub

#End Region

#Region "LoadSetting"

    Public Function doLoadSetting() As cValueObjects.VoUserInfo

        Dim strFile As String = Application.StartupPath + "\settings.xml"

        Dim objXmlDoc As XmlDocument = New XmlDocument()

        '---------------------------
        ' XML の読み込み
        '---------------------------
        Call objXmlDoc.Load(strFile)

        '---------------------------
        ' Settings の読み込み
        '---------------------------
        Dim objVo As New cValueObjects.VoUserInfo

        objVo.strUserID = objXmlDoc.SelectSingleNode("Settings/UserID").InnerText
        objVo.strUserName = objXmlDoc.SelectSingleNode("Settings/UserName").InnerText

        doLoadSetting = objVo

    End Function

#End Region

    Public Function IsXmlFile(ByVal strFileName As String) As Boolean

        If strFileName.Substring(strFileName.Length - 3) = "xml" Then
            IsXmlFile = True
        Else
            IsXmlFile = False
        End If

    End Function


End Module
