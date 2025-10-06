<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Download.aspx.cs" Inherits="Download" %>
<%@ Register Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>安全云盘 - 下载文件</title>
    <link href="css/bootstrap.min.css" rel="stylesheet" />
    <style>
        .container { max-width: 800px; margin-top: 50px; }
        .file-list { margin-top: 20px; }
        .file-item { padding: 10px; border: 1px solid #ddd; margin-bottom: 10px; border-radius: 5px; }
        .btn-group { display: flex; gap: 5px; }
        .folder-actions { margin-bottom: 20px; padding: 15px; background-color: #f8f9fa; border-radius: 5px; border: 1px solid #dee2e6; }
        .mode-selector { margin-bottom: 20px; }
        .mode-btn { margin: 0 5px; }
        .group-section { border: 2px solid #28a745; border-radius: 10px; padding: 20px; margin-top: 20px; background-color: #f8fff9; }
        .personal-section { border: 2px solid #007bff; border-radius: 10px; padding: 20px; margin-top: 20px; background-color: #f0f8ff; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="container">
            <div class="card">
                <div class="card-header bg-info text-white">
                    <h3 class="card-title">安全云盘 - 下载文件</h3>
                </div>
                <div class="card-body">
                    <!-- 模式选择 -->
                    <div class="mode-selector text-center">
                        <asp:Button ID="btnPersonalMode" runat="server" Text="个人云盘" 
                            CssClass="btn btn-primary mode-btn" OnClick="btnPersonalMode_Click" />
                        <asp:Button ID="btnGroupMode" runat="server" Text="群云盘" 
                            CssClass="btn btn-success mode-btn" OnClick="btnGroupMode_Click" />
                    </div>

                    <!-- 个人云盘区域 -->
                    <asp:UpdatePanel ID="UpdatePanelPersonal" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <asp:Panel ID="pnlPersonal" runat="server" Visible="true" CssClass="personal-section">
                                <h4 class="text-primary">个人云盘</h4>
                                <div class="form-group">
                                    <label for="txtFolderName">文件夹名称：</label>
                                    <asp:TextBox ID="txtFolderName" runat="server" CssClass="form-control" 
                                        placeholder="输入要访问的文件夹名称"></asp:TextBox>
                                </div>
                                
                                <div class="form-group">
                                    <label for="txtPassword">访问密码：</label>
                                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" 
                                        CssClass="form-control" placeholder="输入文件夹密码"></asp:TextBox>
                                </div>
                                
                                <div class="form-group">
                                    <asp:Button ID="btnAccess" runat="server" Text="访问文件夹" 
                                        CssClass="btn btn-primary btn-block" OnClick="btnAccess_Click" />
                                </div>
                                
                                <!-- 个人文件夹操作区域 -->
                                <asp:Panel ID="pnlPersonalFolderActions" runat="server" Visible="false" CssClass="folder-actions">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <h5 class="mb-0">文件夹管理</h5>
                                        <asp:Button ID="btnDeletePersonalFolder" runat="server" Text="删除文件夹" 
                                            CssClass="btn btn-danger" OnClick="btnDeletePersonalFolder_Click"
                                            OnClientClick="return confirm('⚠️ 警告：这将永久删除整个文件夹及其所有文件！\n\n此操作不可恢复！\n\n确定要删除整个文件夹吗？');" />
                                    </div>
                                    <small class="text-muted">删除整个文件夹及其所有文件，操作不可恢复</small>
                                </asp:Panel>
                                
                                <!-- 个人文件列表区域 -->
                                <asp:Panel ID="pnlPersonalFileList" runat="server" Visible="false" CssClass="file-list">
                                    <h5>文件夹中的文件：</h5>
                                    <asp:Repeater ID="rptPersonalFiles" runat="server" OnItemCommand="rptPersonalFiles_ItemCommand">
                                        <ItemTemplate>
                                            <div class="file-item d-flex justify-content-between align-items-center">
                                                <span><%# Eval("Value") %></span>
                                                <div class="btn-group">
                                                    <asp:LinkButton ID="btnDownload" runat="server" 
                                                        CommandName="Download" 
                                                        CommandArgument='<%# Eval("Key") + "|" + Eval("Value") %>'
                                                        CssClass="btn btn-sm btn-outline-success"
                                                        OnClientClick="return confirm('确定要下载这个文件吗？');">
                                                        下载
                                                    </asp:LinkButton>
                                                    <asp:LinkButton ID="btnDelete" runat="server" 
                                                        CommandName="Delete" 
                                                        CommandArgument='<%# Eval("Key") + "|" + Eval("Value") %>'
                                                        CssClass="btn btn-sm btn-outline-danger"
                                                        OnClientClick="return confirm('确定要删除这个文件吗？此操作不可恢复！');">
                                                        删除
                                                    </asp:LinkButton>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </asp:Panel>
                            </asp:Panel>
                        </ContentTemplate>
                    </asp:UpdatePanel>

                    <!-- 群云盘区域 -->
                    <asp:UpdatePanel ID="UpdatePanelGroup" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <asp:Panel ID="pnlGroup" runat="server" Visible="false" CssClass="group-section">
                                <h4 class="text-success">群云盘</h4>
                                <div class="form-group">
                                    <label for="txtGroupName">群组名称：</label>
                                    <asp:TextBox ID="txtGroupName" runat="server" CssClass="form-control" 
                                        placeholder="输入要访问的群组名称"></asp:TextBox>
                                </div>
                                
                                <div class="form-group">
                                    <label for="txtGroupAdminPassword">管理密码：</label>
                                    <asp:TextBox ID="txtGroupAdminPassword" runat="server" TextMode="Password" 
                                        CssClass="form-control" placeholder="输入群组管理密码"></asp:TextBox>
                                    <small class="form-text text-muted">⚠️ 只有管理密码可以查看和下载群组文件</small>
                                </div>
                                
                                <div class="form-group">
                                    <asp:Button ID="btnAccessGroup" runat="server" Text="访问群组" 
                                        CssClass="btn btn-success btn-block" OnClick="btnAccessGroup_Click" />
                                </div>
                                
                                <!-- 群组操作区域 -->
                                <asp:Panel ID="pnlGroupFolderActions" runat="server" Visible="false" CssClass="folder-actions">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <h5 class="mb-0">群组管理</h5>
                                        <div class="btn-group">
                                            <asp:Button ID="btnDownloadAllGroup" runat="server" Text="打包下载全部" 
                                                CssClass="btn btn-warning" OnClick="btnDownloadAllGroup_Click" />
                                            <asp:Button ID="btnDeleteGroup" runat="server" Text="删除群组" 
                                                CssClass="btn btn-danger" OnClick="btnDeleteGroup_Click"
                                                OnClientClick="return confirm('⚠️ 警告：这将永久删除整个群组及其所有文件！\n\n此操作不可恢复！\n\n确定要删除整个群组吗？');" />
                                        </div>
                                    </div>
                                    <small class="text-muted">打包下载所有文件或删除整个群组</small>
                                </asp:Panel>
                                
                                <!-- 群组文件列表区域 -->
                                <asp:Panel ID="pnlGroupFileList" runat="server" Visible="false" CssClass="file-list">
                                    <h5>群组中的文件：</h5>
                                    <asp:Repeater ID="rptGroupFiles" runat="server" OnItemCommand="rptGroupFiles_ItemCommand">
                                        <ItemTemplate>
                                            <div class="file-item d-flex justify-content-between align-items-center">
                                                <span><%# Eval("Value") %></span>
                                                <div class="btn-group">
                                                    <asp:LinkButton ID="btnDownloadGroup" runat="server" 
                                                        CommandName="Download" 
                                                        CommandArgument='<%# Eval("Key") + "|" + Eval("Value") %>'
                                                        CssClass="btn btn-sm btn-outline-success"
                                                        OnClientClick="return confirm('确定要下载这个文件吗？');">
                                                        下载
                                                    </asp:LinkButton>
                                                    <asp:LinkButton ID="btnDeleteGroupFile" runat="server" 
                                                        CommandName="Delete" 
                                                        CommandArgument='<%# Eval("Key") + "|" + Eval("Value") %>'
                                                        CssClass="btn btn-sm btn-outline-danger"
                                                        OnClientClick="return confirm('确定要删除这个文件吗？此操作不可恢复！');">
                                                        删除
                                                    </asp:LinkButton>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </asp:Panel>
                            </asp:Panel>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    
                    <!-- 消息显示区域 -->
                    <asp:UpdatePanel ID="UpdatePanelMessage" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <div class="form-group">
                                <asp:Label ID="lblMessage" runat="server" CssClass="alert" 
                                    Visible="false"></asp:Label>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
            </div>
            
            <hr />
            <div class="text-center">
                <a href="Default.aspx" class="btn btn-outline-primary">返回上传页面</a>
            </div>
        </div>
    </form>
</body>
</html>