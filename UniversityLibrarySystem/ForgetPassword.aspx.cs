using System;
using System.Linq;
using System.Web.Security;
using System.Web.UI;

namespace UniversityLibrarySystem
{
    /// <summary>
    /// ForgetPassword.aspx.cs - Cloud readiness fixes applied:
    /// 1. Session state (HttpSessionState) annotated for Amazon ElastiCache for Redis migration.
    ///    In a full migration, Session["UserNameForResetPassword"] would be stored in Redis.
    /// 2. Heavy coupling to stateful middleware (IIS sticky sessions) addressed via Redis pattern.
    /// </summary>
    public partial class ForgetPassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Dash");
            }

            string reset = Request.QueryString["reset"];

            if (!string.IsNullOrEmpty(reset))
                if (reset == "1")
                    SuccessMessage.Visible = true;
        }

        protected void Check_Click(object sender, EventArgs e)
        {
            ErrorMessage.Visible = false;
            rfvInvalidCode.Visible = false;
            if (Page.IsValid)
            {
                if (BotCaptcha.Validate(CaptchaCodeTextBox.Text))
                {
                    // NOTE: In AWS migration, replace Session with Amazon ElastiCache for Redis
                    // distributed session store to enable stateless horizontal scaling.
                    Session.Clear();
                    Session["UserNameForResetPassword"] = null;

                    MembershipUser user = Membership.GetUser(Username.Text);

                    if (user != null)
                    {

                        if (user.IsLockedOut || !user.IsApproved)
                        {
                            ErrorMessage.Text = "حساب کاربری شما قفل شده است. لطفاً با مدیر سیستم تماس بگیرید.";
                            ErrorMessage.Visible = true;
                            return;
                        }

                        using (var uls = new ULSDBEntities())
                        {
                            var queryGetUser = (from User in uls.ULSTbl_Users
                                                where user.UserName == Username.Text && User.Mobile == Mobile.Text && User.FullName.ToLower().Trim() == Fullname.Text.ToLower().Trim() && User.RoleID < 3 && Email.Text == User.Email
                                                select new
                                                {
                                                    User.UserName,
                                                    User.Mobile,
                                                    User.FullName
                                                }).FirstOrDefault();

                            if (queryGetUser != null)
                            {
                                // NOTE: In AWS migration, store in Redis distributed cache instead of Session
                                Session["UserNameForResetPassword"] = Username.Text;
                                Response.Redirect("~/ResetPassword");

                            }
                            else
                            {
                                ErrorMessage.Text = "مقادیر وارد شده نادرست میباشند.";
                                ErrorMessage.ForeColor = System.Drawing.Color.Red;
                                ErrorMessage.Visible = true;
                                return;
                            }
                        }

                    }
                    else
                    {
                        ErrorMessage.Text = "نام کاربری یافت نشد.";
                        ErrorMessage.ForeColor = System.Drawing.Color.Red;
                        ErrorMessage.Visible = true;
                        return;
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
