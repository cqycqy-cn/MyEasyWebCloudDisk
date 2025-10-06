using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Download : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 默认显示个人云盘模式
            ShowPersonalMode();
        }
    }

    // 模式切换方法
    protected void btnPersonalMode_Click(object sender, EventArgs e)
    {
        ShowPersonalMode();
    }

    protected void btnGroupMode_Click(object sender, EventArgs e)
    {
        ShowGroupMode();
    }

    private void ShowPersonalMode()
    {
        pnlPersonal.Visible = true;
        pnlGroup.Visible = false;
        btnPersonalMode.CssClass = "btn btn-primary mode-btn";
        btnGroupMode.CssClass = "btn btn-outline-success mode-btn";
        ClearAllMessages();
    }

    private void ShowGroupMode()
    {
        pnlPersonal.Visible = false;
        pnlGroup.Visible = true;
        btnPersonalMode.CssClass = "btn btn-outline-primary mode-btn";
        btnGroupMode.CssClass = "btn btn-success mode-btn";
        ClearAllMessages();
    }

    // 个人云盘相关方法
    protected void btnAccess_Click(object sender, EventArgs e)
    {
        string folderName = txtFolderName.Text.Trim();
        string password = txtPassword.Text;
        
        var result = FileManager.GetFileList(folderName, password);
        
        ShowMessage(result.Message, result.Success);
        
        if (result.Success)
        {
            pnlPersonalFolderActions.Visible = true;
            pnlPersonalFileList.Visible = true;
            rptPersonalFiles.DataSource = result.Files;
            rptPersonalFiles.DataBind();
        }
        else
        {
            pnlPersonalFolderActions.Visible = false;
            pnlPersonalFileList.Visible = false;
        }
    }

    protected void btnDeletePersonalFolder_Click(object sender, EventArgs e)
    {
        string folderName = txtFolderName.Text.Trim();
        string password = txtPassword.Text;
        
        var result = FileManager.DeleteFolder(folderName, password);
        
        ShowMessage(result.Message, result.Success);
        
        if (result.Success)
        {
            pnlPersonalFolderActions.Visible = false;
            pnlPersonalFileList.Visible = false;
            txtFolderName.Text = "";
            txtPassword.Text = "";
        }
    }

    protected void rptPersonalFiles_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        string folderName = txtFolderName.Text.Trim();
        string password = txtPassword.Text;
        string[] fileInfo = e.CommandArgument.ToString().Split('|');
        string encryptedFileName = fileInfo[0];
        string originalFileName = fileInfo[1];

        if (e.CommandName == "Download")
        {
            var result = FileManager.PrepareFileDownload(folderName, password, encryptedFileName, originalFileName);
            
            if (result.Success)
            {
                Response.ContentType = "application/octet-stream";
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(originalFileName));
                Response.TransmitFile(result.TempFilePath);
                Response.End();
            }
            else
            {
                ShowMessage("下载失败: " + result.Message, false);
            }
        }
        else if (e.CommandName == "Delete")
        {
            var result = FileManager.DeleteFile(folderName, password, encryptedFileName, originalFileName);
            
            ShowMessage(result.Message, result.Success);
            
            if (result.Success)
            {
                // 刷新文件列表
                var fileListResult = FileManager.GetFileList(folderName, password);
                if (fileListResult.Success)
                {
                    rptPersonalFiles.DataSource = fileListResult.Files;
                    rptPersonalFiles.DataBind();
                    
                    if (fileListResult.Files.Count == 0)
                    {
                        pnlPersonalFileList.Visible = false;
                    }
                }
            }
        }
    }

    // 群云盘相关方法
    protected void btnAccessGroup_Click(object sender, EventArgs e)
    {
        string groupName = txtGroupName.Text.Trim();
        string adminPassword = txtGroupAdminPassword.Text;
        
        var result = GroupFileManager.GetGroupFileList(groupName, adminPassword);
        
        ShowMessage(result.Message, result.Success);
        
        if (result.Success)
        {
            pnlGroupFolderActions.Visible = true;
            pnlGroupFileList.Visible = true;
            rptGroupFiles.DataSource = result.Files;
            rptGroupFiles.DataBind();
            
            // 显示文件数量信息
            if (result.Files != null && result.Files.Count > 0)
            {
                ShowMessage(string.Format("成功访问群组，共 {0} 个文件", result.Files.Count), true);
            }
        }
        else
        {
            pnlGroupFolderActions.Visible = false;
            pnlGroupFileList.Visible = false;
        }
    }

    protected void btnDownloadAllGroup_Click(object sender, EventArgs e)
    {
        string groupName = txtGroupName.Text.Trim();
        string adminPassword = txtGroupAdminPassword.Text;
        
        // 显示处理中提示
        ShowMessage("正在准备打包下载，请稍候...", true);
        
        // 执行打包下载
        var result = GroupFileManager.PrepareGroupAllFilesDownload(groupName, adminPassword);
        
        if (result.Success)
        {
            try
            {
                string zipFileName = string.Format("{0}_{1:yyyyMMdd_HHmmss}.zip", groupName, DateTime.Now);
                Response.ContentType = "application/zip";
                Response.AppendHeader("Content-Disposition", string.Format("attachment; filename={0}", HttpUtility.UrlEncode(zipFileName)));
                Response.TransmitFile(result.TempFilePath);
                Response.End();
            }
            catch (Exception ex)
            {
                ShowMessage("下载过程出错: " + ex.Message, false);
            }
        }
        else
        {
            ShowMessage("打包下载失败: " + result.Message, false);
        }
    }

    protected void btnDeleteGroup_Click(object sender, EventArgs e)
    {
        string groupName = txtGroupName.Text.Trim();
        string adminPassword = txtGroupAdminPassword.Text;
        
        var result = GroupFileManager.DeleteGroup(groupName, adminPassword);
        
        ShowMessage(result.Message, result.Success);
        
        if (result.Success)
        {
            pnlGroupFolderActions.Visible = false;
            pnlGroupFileList.Visible = false;
            txtGroupName.Text = "";
            txtGroupAdminPassword.Text = "";
        }
    }

    protected void rptGroupFiles_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        string groupName = txtGroupName.Text.Trim();
        string adminPassword = txtGroupAdminPassword.Text;
        string[] fileInfo = e.CommandArgument.ToString().Split('|');
        string encryptedFileName = fileInfo[0];
        string originalFileName = fileInfo[1];

        if (e.CommandName == "Download")
        {
            var result = GroupFileManager.PrepareGroupFileDownload(groupName, adminPassword, encryptedFileName, originalFileName);
            
            if (result.Success)
            {
                Response.ContentType = "application/octet-stream";
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(originalFileName));
                Response.TransmitFile(result.TempFilePath);
                Response.End();
            }
            else
            {
                ShowMessage("下载失败: " + result.Message, false);
            }
        }
        else if (e.CommandName == "Delete")
        {
            var result = GroupFileManager.DeleteGroupFile(groupName, adminPassword, encryptedFileName, originalFileName);
            
            ShowMessage(result.Message, result.Success);
            
            if (result.Success)
            {
                // 刷新文件列表
                var fileListResult = GroupFileManager.GetGroupFileList(groupName, adminPassword);
                if (fileListResult.Success)
                {
                    rptGroupFiles.DataSource = fileListResult.Files;
                    rptGroupFiles.DataBind();
                    
                    if (fileListResult.Files.Count == 0)
                    {
                        pnlGroupFileList.Visible = false;
                        pnlGroupFolderActions.Visible = false;
                    }
                    else
                    {
                        ShowMessage(string.Format("文件删除成功，剩余 {0} 个文件", fileListResult.Files.Count), true);
                    }
                }
            }
        }
    }

    // 辅助方法
    private void ShowMessage(string message, bool isSuccess)
    {
        lblMessage.Visible = true;
        lblMessage.Text = message;
        lblMessage.CssClass = isSuccess ? "alert alert-success" : "alert alert-danger";
    }

    private void ClearAllMessages()
    {
        lblMessage.Visible = false;
        lblMessage.Text = "";
    }
}