<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>安全云盘 - 上传文件</title>
    <link href="css/bootstrap.min.css" rel="stylesheet" />
    <style>
        .container { max-width: 800px; margin-top: 30px; }
        .card { box-shadow: 0 4px 8px rgba(0,0,0,0.1); margin-bottom: 20px; }
        .form-group { margin-bottom: 20px; }
        .terms-content { max-height: 300px; overflow-y: auto; border: 1px solid #ddd; padding: 15px; margin: 15px 0; }
        .declaration { background-color: #f8f9fa; border-left: 4px solid #007bff; padding: 20px; margin-bottom: 30px; border-radius: 5px; }
        .declaration h4 { color: #007bff; margin-bottom: 15px; }
        .declaration-content { max-height: 400px; overflow-y: auto; padding: 15px; background-color: white; border-radius: 5px; border: 1px solid #dee2e6; }
        .scroll-indicator { text-align: center; color: #6c757d; margin: 20px 0; }
        .scroll-indicator i { font-size: 24px; animation: bounce 2s infinite; }
        .group-section { border: 2px solid #28a745; border-radius: 10px; padding: 20px; margin-top: 30px; background-color: #f8fff9; }
        .group-header { color: #28a745; border-bottom: 2px solid #28a745; padding-bottom: 10px; margin-bottom: 20px; }
        .password-info { font-size: 0.9em; color: #6c757d; margin-top: 5px; }
        .file-selected { background-color: #e8f5e8; border-color: #28a745; }
        @keyframes bounce {
            0%, 20%, 50%, 80%, 100% { transform: translateY(0); }
            40% { transform: translateY(-10px); }
            60% { transform: translateY(-5px); }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server" enctype="multipart/form-data">
        <div class="container">
            <!-- 使用声明区域 -->
            <div class="declaration">
                <h4>📋 安全云盘使用声明</h4>
                <div class="declaration-content">
                    <h5>系统运行原理说明：</h5>
                    <p><strong>为了保障您的文件安全，本系统采用以下安全机制：</strong></p>
                    
                    <div class="mb-3">
                        <h6>🔒 端到端加密</h6>
                        <p>所有文件在上传时使用 <strong>AES-256 军用级加密算法</strong> 进行加密，密钥由您设置的密码生成。服务器仅存储加密后的文件，无法查看文件内容。</p>
                    </div>
                    
                    <div class="mb-3">
                        <h6>🔑 密码保护</h6>
                        <p>每个文件夹都有独立的访问密码。系统使用 <strong>PBKDF2 密钥派生函数</strong> 将您的密码转换为加密密钥，确保密码安全性。</p>
                    </div>
                    
                    <div class="mb-3">
                        <h6>💾 安全存储</h6>
                        <p>文件在服务器上的存储形式：</p>
                        <ul>
                            <li>原始文件名被加密存储在索引文件中</li>
                            <li>文件内容被加密后存储为随机文件名</li>
                            <li>服务器不保存您的明文密码</li>
                        </ul>
                    </div>
                    
                    <div class="mb-3">
                        <h6>⚡ 临时解密</h6>
                        <p>下载文件时，系统会在内存中临时解密文件供您下载，下载完成后立即清除临时数据。</p>
                    </div>
                    
                    <div class="alert alert-warning mt-3">
                        <h6>⚠️ 重要提醒</h6>
                        <ul>
                            <li>请妥善保管您的文件夹名称和访问密码</li>
                            <li>如果您忘记密码，我们将无法为您恢复文件访问权限</li>
                            <li>系统会自动清理30分钟前的临时文件</li>
                            <li>禁止上传违法、侵权或恶意软件等内容</li>
                        </ul>
                    </div>
                    
                    <div class="alert alert-info">
                        <h6>🔧 技术特性</h6>
                        <ul>
                            <li>加密算法：AES-256-CBC</li>
                            <li>密钥派生：PBKDF2 with 15,000 iterations</li>
                            <li>安全传输：HTTPS/TLS 1.2+</li>
                            <li>文件索引：加密存储，防止文件名泄露</li>
                        </ul>
                    </div>
                </div>
            </div>

            <!-- 滚动提示 -->
            <div class="scroll-indicator">
                <p>下滑开始使用云盘功能</p>
                <i>⬇️</i>
            </div>

            <!-- 个人云盘上传区域 -->
            <div class="card">
                <div class="card-header bg-primary text-white">
                    <h3 class="card-title">个人云盘 - 上传文件</h3>
                </div>
                <div class="card-body">
                    <div class="form-group">
                        <label for="txtFolderName">文件夹名称：</label>
                        <asp:TextBox ID="txtFolderName" runat="server" CssClass="form-control" 
                            placeholder="输入1-15个字符，不含非法字符" MaxLength="15"></asp:TextBox>
                        <small class="form-text text-muted">如果文件夹不存在会自动创建</small>
                    </div>
                    
                    <div class="form-group">
                        <label for="txtPassword">访问密码：</label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" 
                            CssClass="form-control" placeholder="至少8位，包含特殊字符"></asp:TextBox>
                    </div>
                    
                    <div class="form-group">
                        <label for="fileUpload">选择文件：</label>
                        <asp:FileUpload ID="fileUpload" runat="server" CssClass="form-control" 
                            AllowMultiple="true" onchange="highlightFileUpload(this)" />
                        <small class="form-text text-muted">可同时选择多个文件</small>
                        <div id="fileUploadStatus" class="mt-1"></div>
                    </div>
                    
                    <div class="form-group">
                        <asp:Button ID="btnShowTerms" runat="server" Text="上传文件" 
                            CssClass="btn btn-success btn-block" OnClientClick="showTermsModal(); return false;" />
                    </div>
                    
                    <div class="form-group">
                        <asp:Label ID="lblMessage" runat="server" CssClass="alert" 
                            Visible="false"></asp:Label>
                    </div>

                    <!-- 隐藏的实际上传按钮 -->
                    <asp:Button ID="btnRealUpload" runat="server" Text="实际上传" 
                        style="display: none;" OnClick="btnRealUpload_Click" />
                </div>
            </div>

            <!-- 群云盘上传区域 -->
            <div class="group-section">
                <div class="group-header">
                    <h3><i class="fas fa-users"></i> 群云盘 - 上传文件</h3>
                    <small class="text-muted">团队成员使用公共密码上传，管理员使用管理密码查看</small>
                </div>
                
                <div class="form-group">
                    <label for="txtGroupNameUpload">群组名称：</label>
                    <asp:TextBox ID="txtGroupNameUpload" runat="server" CssClass="form-control" 
                        placeholder="输入要上传的群组名称" MaxLength="15"></asp:TextBox>
                </div>
                
                <div class="form-group">
                    <label for="txtGroupUploadPassword">公共密码：</label>
                    <asp:TextBox ID="txtGroupUploadPassword" runat="server" TextMode="Password" 
                        CssClass="form-control" placeholder="输入群组的公共密码"></asp:TextBox>
                    <small class="password-info">⚠️ 公共密码只能上传文件，无法查看或下载文件</small>
                </div>
                
                <div class="form-group">
                    <label for="fileGroupUpload">选择文件：</label>
                    <asp:FileUpload ID="fileGroupUpload" runat="server" CssClass="form-control" 
                        AllowMultiple="true" onchange="highlightFileUpload(this)" />
                    <small class="form-text text-muted">可同时选择多个文件上传到群组</small>
                    <div id="fileGroupUploadStatus" class="mt-1"></div>
                </div>
                
                <div class="form-group">
                    <asp:Button ID="btnUploadToGroup" runat="server" Text="上传到群组" 
                        CssClass="btn btn-outline-success btn-block" OnClick="btnUploadToGroup_Click" />
                </div>
                
                <div class="form-group">
                    <asp:Label ID="lblGroupUploadMessage" runat="server" CssClass="alert" 
                        Visible="false"></asp:Label>
                </div>
            </div>

            <!-- 群云盘创建区域 -->
            <div class="group-section" style="border-color: #dc3545; background-color: #fff5f5;">
                <div class="group-header" style="color: #dc3545; border-color: #dc3545;">
                    <h3><i class="fas fa-plus-circle"></i> 创建新群组</h3>
                    <small class="text-muted">需要有效的注册码才能创建群组</small>
                </div>
                
                <div class="form-group">
                    <label for="txtRegCode">注册码：</label>
                    <asp:TextBox ID="txtRegCode" runat="server" CssClass="form-control" 
                        placeholder="输入3位大写字母+6位数字注册码（如：ABC123456）" MaxLength="9"></asp:TextBox>
                    <small class="form-text text-muted">注册码格式：3位大写字母 + 6位数字</small>
                </div>
                
                <div class="form-group">
                    <label for="txtNewGroupName">新群组名称：</label>
                    <asp:TextBox ID="txtNewGroupName" runat="server" CssClass="form-control" 
                        placeholder="输入1-15个字符，不含非法字符" MaxLength="15"></asp:TextBox>
                    <small class="form-text text-muted">群组名称必须唯一，不能与现有群组重复</small>
                </div>
                
                <div class="form-group">
                    <label for="txtGroupPublicPassword">公共密码：</label>
                    <asp:TextBox ID="txtGroupPublicPassword" runat="server" TextMode="Password" 
                        CssClass="form-control" placeholder="设置公共密码（至少8位，含特殊字符）"></asp:TextBox>
                    <small class="password-info">📤 团队成员使用此密码上传文件</small>
                </div>
                
                <div class="form-group">
                    <label for="txtGroupAdminPassword">管理密码：</label>
                    <asp:TextBox ID="txtGroupAdminPassword" runat="server" TextMode="Password" 
                        CssClass="form-control" placeholder="设置管理密码（至少8位，含特殊字符）"></asp:TextBox>
                    <small class="password-info">📥 仅管理员使用此密码查看和下载文件</small>
                </div>
                
                <div class="form-group">
                    <asp:Button ID="btnCreateGroup" runat="server" Text="创建群组" 
                        CssClass="btn btn-danger btn-block" OnClick="btnCreateGroup_Click" />
                </div>
                
                <div class="form-group">
                    <asp:Label ID="lblCreateGroupMessage" runat="server" CssClass="alert" 
                        Visible="false"></asp:Label>
                </div>
            </div>
            
            <div class="text-center mt-3">
                <a href="Download.aspx" class="btn btn-outline-primary">前往下载页面</a>
            </div>
        </div>
    </form>

    <!-- 用户须知模态框 -->
    <div class="modal fade" id="termsModal" tabindex="-1" aria-labelledby="termsModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header bg-warning">
                    <h5 class="modal-title" id="termsModalLabel">用户须知 - 请仔细阅读</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="关闭"></button>
                </div>
                <div class="modal-body">
                    <div class="terms-content">
                        <p><strong>继续创建视做同意以下条款：</strong></p>
                        <ol>
                            <li>本系统采用端到端加密技术，确保您的文件安全。</li>
                            <li><strong class="text-danger">服务器不保存用户密码，如果您遗忘密码，我们将无法为您恢复文件访问权限。</strong></li>
                            <li>请妥善保管您的文件夹名称和访问密码。</li>
                            <li>禁止上传违法、侵权或恶意软件等内容。</li>
                            <li>系统会自动清理临时文件，请及时下载您需要的文件。</li>
                        </ol>
                        
                        <div class="alert alert-info mt-3">
                            <h6>技术支持联系方式：</h6>
                            <p class="mb-1">📞 电话/微信：<strong>13660894324</strong></p>
                            <p class="mb-0">💬 QQ：<strong>1439662316</strong></p>
                        </div>
                        
                        <div class="form-check mt-3">
                            <input class="form-check-input" type="checkbox" id="agreeTerms">
                            <label class="form-check-label" for="agreeTerms">
                                <strong>我已阅读并同意以上用户须知</strong>
                            </label>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
                    <button type="button" class="btn btn-primary" id="btnConfirmUpload" onclick="confirmUpload()">确认创建</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Bootstrap JS -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js"></script>
    <!-- Font Awesome -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/js/all.min.js"></script>
    
    <script type="text/javascript">
        function showTermsModal() {
            var folderName = document.getElementById('<%= txtFolderName.ClientID %>').value;
            var password = document.getElementById('<%= txtPassword.ClientID %>').value;
            var fileUpload = document.getElementById('<%= fileUpload.ClientID %>');
            
            if (!folderName) {
                alert('请输入文件夹名称');
                return false;
            }
            
            if (!password) {
                alert('请输入访问密码');
                return false;
            }
            
            if (!fileUpload.files || fileUpload.files.length === 0) {
                alert('请选择要上传的文件');
                return false;
            }
            
            document.getElementById('agreeTerms').checked = false;
            
            var termsModal = new bootstrap.Modal(document.getElementById('termsModal'));
            termsModal.show();
            
            return false;
        }
        
        function confirmUpload() {
            var agreeTerms = document.getElementById('agreeTerms').checked;
            
            if (!agreeTerms) {
                alert('请先阅读并同意用户须知');
                return;
            }
            
            var termsModal = bootstrap.Modal.getInstance(document.getElementById('termsModal'));
            termsModal.hide();
            
            document.getElementById('<%= btnRealUpload.ClientID %>').click();
        }

        // 高亮显示已选择的文件
        function highlightFileUpload(fileUpload) {
            var statusDiv = fileUpload.id === '<%= fileUpload.ClientID %>' 
                ? document.getElementById('fileUploadStatus')
                : document.getElementById('fileGroupUploadStatus');
                
            if (fileUpload.files && fileUpload.files.length > 0) {
                fileUpload.classList.add('file-selected');
                var fileNames = [];
                for (var i = 0; i < fileUpload.files.length; i++) {
                    fileNames.push(fileUpload.files[i].name);
                }
                statusDiv.innerHTML = '<small class="text-success">✅ 已选择: ' + fileNames.join(', ') + '</small>';
            } else {
                fileUpload.classList.remove('file-selected');
                statusDiv.innerHTML = '';
            }
        }

        // 页面加载后恢复文件选择状态
        document.addEventListener('DOMContentLoaded', function() {
            const scrollIndicator = document.querySelector('.scroll-indicator');
            if (scrollIndicator) {
                setTimeout(() => {
                    scrollIndicator.style.opacity = '0.7';
                }, 2000);
            }
            
            // 检查是否有消息显示，如果有则自动滚动到消息位置
            var messageLabel = document.getElementById('<%= lblMessage.ClientID %>');
            if (messageLabel && messageLabel.style.display !== 'none') {
                messageLabel.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        });

        // 在页面回发后保持滚动位置
        var scrollPosition = 0;
        function saveScrollPosition() {
            scrollPosition = window.pageYOffset || document.documentElement.scrollTop;
        }
        function restoreScrollPosition() {
            window.scrollTo(0, scrollPosition);
        }
        
        // 在回发前保存位置，在加载后恢复位置
        if (typeof(Sys) !== 'undefined') {
            Sys.WebForms.PageRequestManager.getInstance().add_beginRequest(saveScrollPosition);
            Sys.WebForms.PageRequestManager.getInstance().add_pageLoaded(restoreScrollPosition);
        }
    </script>
</body>
</html>