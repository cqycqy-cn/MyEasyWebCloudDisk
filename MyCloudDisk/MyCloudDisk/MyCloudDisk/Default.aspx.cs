using System;
using System.Web;
using System.Web.UI;

public partial class _Default : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // 清理过期的频率限制记录
        RateLimitHelper.CleanupOldRecords();
    }

    // 个人云盘上传相关方法
    protected void btnRealUpload_Click(object sender, EventArgs e)
    {
        string folderName = txtFolderName.Text.Trim();
        string password = txtPassword.Text;
        
        if (fileUpload.HasFiles)
        {
            var result = FileManager.CreateFolderAndUpload(folderName, password, Request.Files);
            
            lblMessage.Visible = true;
            lblMessage.Text = result.Message;
            lblMessage.CssClass = result.Success ? "alert alert-success" : "alert alert-danger";
            
            if (result.Success)
            {
                txtFolderName.Text = "";
                txtPassword.Text = "";
            }
        }
        else
        {
            lblMessage.Visible = true;
            lblMessage.Text = "请选择要上传的文件";
            lblMessage.CssClass = "alert alert-warning";
        }
    }

    // 群云盘上传方法
    protected void btnUploadToGroup_Click(object sender, EventArgs e)
    {
        string groupName = txtGroupNameUpload.Text.Trim();
        string uploadPassword = txtGroupUploadPassword.Text;
        
        if (fileGroupUpload.HasFiles)
        {
            var result = GroupFileManager.UploadToGroup(groupName, uploadPassword, Request.Files);
            
            lblGroupUploadMessage.Visible = true;
            lblGroupUploadMessage.Text = result.Message;
            lblGroupUploadMessage.CssClass = result.Success ? "alert alert-success" : "alert alert-danger";
            
            if (result.Success)
            {
                txtGroupNameUpload.Text = "";
                txtGroupUploadPassword.Text = "";
            }
        }
        else
        {
            lblGroupUploadMessage.Visible = true;
            lblGroupUploadMessage.Text = "请选择要上传的文件";
            lblGroupUploadMessage.CssClass = "alert alert-warning";
        }
    }

    // 创建群组方法
    protected void btnCreateGroup_Click(object sender, EventArgs e)
    {
        string regCode = txtRegCode.Text.Trim().ToUpper();
        string groupName = txtNewGroupName.Text.Trim();
        string publicPassword = txtGroupPublicPassword.Text;
        string adminPassword = txtGroupAdminPassword.Text;

        // 基本验证
        if (string.IsNullOrEmpty(regCode))
        {
            ShowCreateGroupMessage("请输入注册码", false);
            return;
        }

        if (string.IsNullOrEmpty(groupName))
        {
            ShowCreateGroupMessage("请输入群组名称", false);
            return;
        }

        if (string.IsNullOrEmpty(publicPassword))
        {
            ShowCreateGroupMessage("请输入公共密码", false);
            return;
        }

        if (string.IsNullOrEmpty(adminPassword))
        {
            ShowCreateGroupMessage("请输入管理密码", false);
            return;
        }

        // 创建群组
        var result = GroupFileManager.CreateGroup(groupName, publicPassword, adminPassword, regCode);
        
        ShowCreateGroupMessage(result.Message, result.Success);
        
        if (result.Success)
        {
            // 清空表单
            txtRegCode.Text = "";
            txtNewGroupName.Text = "";
            txtGroupPublicPassword.Text = "";
            txtGroupAdminPassword.Text = "";
        }
    }

    private void ShowCreateGroupMessage(string message, bool isSuccess)
    {
        lblCreateGroupMessage.Visible = true;
        lblCreateGroupMessage.Text = message;
        lblCreateGroupMessage.CssClass = isSuccess ? "alert alert-success" : "alert alert-danger";
    }
}