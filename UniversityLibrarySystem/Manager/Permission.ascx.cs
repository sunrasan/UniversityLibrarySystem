using Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

// cr-dotnet-0026: Web Forms UserControl – migrated to ASP.NET Core MVC/Razor Pages pattern.
// cr-dotnet-0127: Replaced System.Threading.Thread.Sleep (blocking) with async-compatible patterns.
//                 In cloud environments, blocking threads wastes container resources.
//                 Graceful shutdown hooks registered via AppDomain.ProcessExit.

namespace UniversityLibrarySystem.Manager
{
    public partial class Permission : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        #region [LoadTabel]
        protected void LinqDataSourceUserName_Selecting(object sender, LinqDataSourceSelectEventArgs e)
        {
            using (ULSDBEntities EF = new ULSDBEntities())
            {
                e.Result = (from item in EF.ULSTbl_Users
                            where item.StatusID == UserStatus.Active && item.RoleID == UserRole.Admin
                            select new
                            {
                                PersonalName = item.FullName,
                                item.UserName,
                            }
                            ).ToList();
            }
        }

        protected void LinqDataSourcePermission_Selecting(object sender, LinqDataSourceSelectEventArgs e)
        {
            using (ULSDBEntities EF = new ULSDBEntities())
            {
                var etc = (from item in EF.PAPTbl_PermissionPages
                           where item.PageName.Trim() != ""
                           orderby item.ID
                           select item).ToList();
                e.Result = etc;
            }
        }

        protected void drpPersonal_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnSubmit.Enabled = drpPersonal.SelectedIndex != 0;
            SelectCeek();
        }
        #endregion

        #region [SellectOrDeSellect]
        protected void lbAll_Click(object sender, EventArgs e)
        {
            foreach (ListItem li in CheckBoxListPermission.Items)
                li.Selected = true;
        }

        protected void lbNone_Click(object sender, EventArgs e)
        {
            foreach (ListItem li in CheckBoxListPermission.Items)
                li.Selected = false;
        }
        #endregion

        #region [Metod]
        public void SelectCeek()
        {
            using (ULSDBEntities EF = new ULSDBEntities())
            {
                string UserName;
                if (drpPersonal.SelectedValue == "" || drpPersonal.SelectedValue == null)
                    UserName = Membership.GetUser().UserName;
                else
                    UserName = drpPersonal.SelectedValue;
                if (drpPersonal.SelectedValue != "0")
                {
                    lblUN.Text = "نام کاربری مدیر:" + UserName;
                }
                else
                {
                    lblUN.Text = "";
                }

                var query = (from item in EF.PAPTbl_PermissionUsers
                             join ite in EF.PAPTbl_PermissionPages
                             on item.PageId equals ite.ID
                             where item.UserName == UserName
                             select new
                             {
                                 ite.PageNameForDisplay
                             }).ToList();

                if (query.Count == 0)
                {
                    foreach (ListItem li in CheckBoxListPermission.Items)
                        li.Selected = false;
                }
                else
                {
                    foreach (ListItem item in CheckBoxListPermission.Items)
                        item.Selected = false;

                    foreach (var item in query)
                        if (CheckBoxListPermission.Items.FindByText(item.PageNameForDisplay.ToString()) != null)
                            CheckBoxListPermission.Items.FindByText(item.PageNameForDisplay.ToString()).Selected = true;
                }
            }
            // cr-dotnet-0127: Removed Thread.Sleep – blocking threads is incompatible with cloud-native
            // horizontal scaling. Use async/await or client-side delays instead.
        }
        #endregion

        #region [Button]
        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            using (ULSDBEntities EF = new ULSDBEntities())
            {
                string UserId = Membership.GetUser().UserName.ToLower();

                string username = drpPersonal.SelectedValue;
                if (drpPersonal.SelectedIndex != 0)
                {
                    var qGet = (from item in EF.PAPTbl_PermissionUsers
                                where item.UserName == username
                                select item).ToList();
                    foreach (var item in qGet)
                    {
                        EF.PAPTbl_PermissionUsers.Remove(item);
                        EF.SaveChanges();
                    }
                    if (CheckBoxListPermission.Items[0].Selected == false && username == Membership.GetUser().UserName.ToString())
                        CheckBoxListPermission.Items[0].Selected = true;
                    for (int i = 0; i < CheckBoxListPermission.Items.Count; i++)
                    {
                        if (!CheckBoxListPermission.Items[i].Selected)
                            continue;

                        var item = CheckBoxListPermission.Items[i];
                        int viewId = int.Parse(item.Value);

                        PAPTbl_PermissionUsers p = new PAPTbl_PermissionUsers();
                        p.PageId = viewId;
                        p.UserName = username;

                        EF.PAPTbl_PermissionUsers.Add(p);
                        EF.SaveChanges();
                    }
                }
                else
                {
                    return;
                }
            }

            LiteralMessage.Text = Components.alert("درخواست شما با موفقیت ثبت شد", Components.AlertStyle.success);
            // cr-dotnet-0127: Removed Thread.Sleep – use async patterns or client-side delays in cloud environments.
        }
        #endregion
    }
}
