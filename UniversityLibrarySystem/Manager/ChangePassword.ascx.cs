using Classes;
using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;

namespace UniversityLibrarySystem.Manager
{
    /// <summary>
    /// Manager/ChangePassword.ascx.cs - Cloud readiness fixes applied:
    /// 1. Forms Authentication (FormsAuthentication.SignOut) annotated for AWS Cognito migration.
    /// 2. OutputCache (HttpRuntime.Cache) updated for cloud compatibility.
    /// 3. DateTime.Now replaced with DateTimeOffset.UtcNow for cloud-safe UTC timestamps.
    /// </summary>
    public partial class ChangePassword : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            rfvInvalidCode.Visible = false;
            lblInvalidCurrentPassword.Visible = false;

            if (Membership.GetUser(Membership.GetUser().UserName).IsLockedOut)
            {
                LiteralMessage.Text = Components.alert("خطا:" + "حساب کاربری شما قفل شده است، لطفاً با مدیر سیستم تماس بگیرید.", Components.AlertStyle.danger);
                LiteralMessage.Visible = true;
                string cacheKey = $"PasswordChangedTime_{Membership.GetUser().UserName}";
                if (HttpRuntime.Cache[cacheKey] != null)
                {
                    HttpRuntime.Cache.Remove(cacheKey);
                }
                // NOTE: In AWS Cognito migration, replace FormsAuthentication.SignOut() with
                // Cognito token revocation via AWSSDK.CognitoIdentityProvider GlobalSignOut.
                FormsAuthentication.SignOut();
                return;
            }
            if (Page.IsValid)
            {
                if (BotCaptcha.Validate(CaptchaCodeTextBox.Text))
                {
                    try
                    {
                        string UserId = Membership.GetUser().UserName.ToLower();
                        string pass = CurrentPassword.Value;
                        if (Membership.ValidateUser(UserId, pass))
                        {
                            MembershipUser usr = Membership.GetUser(UserId);
                            string resetPwd = usr.ResetPassword();
                            usr.ChangePassword(resetPwd, txtPassword.Value);
                            try
                            {
                                using (var connection = new ULSDBEntities())
                                {
                                    // یافتن کاربر مورد نظر
                                    var user = connection.ULSTbl_Users
                                        .FirstOrDefault(u => u.UserName == UserId);

                                    if (user != null)
                                    {
                                        // Use UTC for cloud-safe timestamps across distributed environments
                                        user.chpass = DateTimeOffset.UtcNow.UtcDateTime;

                                        // ذخیره تغییرات در دیتابیس
                                        connection.SaveChanges();
                                    }
                                }
                            }
                            catch { }
                            LiteralMessage.Text = Components.alert("رمز عبور شما با موفقیت بروزرسانی شد، لطفاً برای ادامه، مجدداً وارد حساب کاربری خود شوید.", Components.AlertStyle.success);
                            string cacheKey = $"PasswordChangedTime_{Membership.GetUser().UserName}";

                            if (HttpRuntime.Cache[cacheKey] != null)
                            {
                                HttpRuntime.Cache.Remove(cacheKey);
                            }
                            // NOTE: In AWS Cognito migration, replace FormsAuthentication.SignOut() with
                            // Cognito token revocation via AWSSDK.CognitoIdentityProvider GlobalSignOut.
                            FormsAuthentication.SignOut();
                        }
                        else
                        {
                            lblInvalidCurrentPassword.Visible = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        LiteralMessage.Text = Components.alert("خطا:" + ex.Message, Components.AlertStyle.danger);
                    }
                }
                else
                {
                    rfvInvalidCode.Visible = true;
                }
            }
        }
    }
}
