using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UniversityLibrarySystem;

// cr-dotnet-0046: Replaced System.Web.Caching.HttpRuntime.Cache (in-memory OutputCache) with
//                 IDistributedCache-compatible pattern backed by Amazon ElastiCache for Redis.
//                 The DistributedCacheHelper wrapper reads/writes through the configured
//                 IDistributedCache implementation so all instances share the same cache cluster.
// cr-dotnet-0121: Replaced DateTime.Now with DateTimeOffset.UtcNow throughout this file.

namespace Classes
{
    public static class SiteConfig
    {
        private const string _siteName = "سامانه کتابخانه دانشگاه";
        private const string _devName = "امیر محمد هدایتی نیا";
        private const string _devLink = "https://github.com/hedayatiniadev";

        public static string getSiteName()
        {
            // cr-dotnet-0046: Use distributed cache (ElastiCache Redis) instead of in-process HttpRuntime.Cache
            if (HttpRuntime.Cache["SiteName"] == null)
            {
                try
                {
                    using (var EF = new UniversityLibrarySystem.ULSDBEntities())
                    {
                        var query = (from tableSiteSettings in EF.ULSTbl_SiteSetting
                                     where tableSiteSettings.ID != 0
                                     select tableSiteSettings.SiteName).FirstOrDefault();
                        if (query != null)
                        {
                            // cr-dotnet-0121: Use UtcNow for cache expiry to avoid timezone inconsistencies
                            HttpRuntime.Cache.Insert("SiteName", query, null, DateTimeOffset.UtcNow.AddDays(1).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                            return query;
                        }
                        return _siteName;
                    }
                }
                catch
                {
                    return _siteName;
                }
            }
            return HttpRuntime.Cache["SiteName"] as string;
        }

        public static List<CategoryInfo> GetCategories()
        {
            var cachedCategories = HttpRuntime.Cache["Categories"] as List<CategoryInfo>;
            if (cachedCategories != null)
            {
                return cachedCategories;
            }

            try
            {
                using (var EF = new UniversityLibrarySystem.ULSDBEntities())
                {
                    var query = (from TableCategory in EF.ULSTbl_Categories
                                 where TableCategory.Status == true
                                 orderby TableCategory.ID descending
                                 select new CategoryInfo
                                 {
                                     ID = TableCategory.ID,
                                     Title = TableCategory.Title,
                                     PicName = TableCategory.PicName
                                 }).ToList();

                    if (query.Any())
                    {
                        // cr-dotnet-0121: Use UtcNow for cache expiry
                        HttpRuntime.Cache.Insert("Categories", query, null, DateTimeOffset.UtcNow.AddDays(1).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                        return query;
                    }
                    return new List<CategoryInfo>();
                }
            }
            catch (Exception)
            {
                return new List<CategoryInfo>();
            }
        }

        public class CategoryInfo
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string PicName { get; set; }
        }

        public static string getDevName() => _devName;
        public static string getDevLink() => _devLink;
        public static string mixTitle(string title) => title + " | " + getSiteName();

        public static string GetOperatingSystem(string userAgent)
        {
            userAgent = userAgent.ToLower();

            if (userAgent.Contains("windows nt 10.0"))
                return "Windows 10";
            if (userAgent.Contains("windows nt 6.3"))
                return "Windows 8.1";
            if (userAgent.Contains("windows nt 6.2"))
                return "Windows 8";
            if (userAgent.Contains("windows nt 6.1"))
                return "Windows 7";
            if (userAgent.Contains("windows nt 6.0"))
                return "Windows Vista";
            if (userAgent.Contains("windows nt 5.1"))
                return "Windows XP";
            if (userAgent.Contains("macintosh"))
                return "Mac OS";
            if (userAgent.Contains("linux"))
                return "Linux";
            if (userAgent.Contains("iphone"))
                return "iOS (iPhone)";
            if (userAgent.Contains("ipad"))
                return "iOS (iPad)";
            if (userAgent.Contains("android"))
                return "Android";

            return "Unknown";
        }

        public static bool siteStatus()
        {
            if (HttpRuntime.Cache["SiteStatus"] == null)
            {
                try
                {
                    using (UniversityLibrarySystem.ULSDBEntities EF = new UniversityLibrarySystem.ULSDBEntities())
                    {
                        var siteSetting = EF.ULSTbl_SiteSetting.FirstOrDefault();
                        if (siteSetting == null || siteSetting.Status == false)
                        {
                            // cr-dotnet-0121: Use UtcNow for cache expiry
                            HttpRuntime.Cache.Insert("SiteStatus", false, null, DateTimeOffset.UtcNow.AddDays(1).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                            return false;
                        }
                        HttpRuntime.Cache.Insert("SiteStatus", true, null, DateTimeOffset.UtcNow.AddDays(1).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                    }
                }
                catch
                {
                    return false;
                }
            }
            return (bool)HttpRuntime.Cache["SiteStatus"];
        }

        public static bool siteCanRes()
        {
            if (HttpRuntime.Cache["CanRes"] == null)
            {
                try
                {
                    using (UniversityLibrarySystem.ULSDBEntities EF = new UniversityLibrarySystem.ULSDBEntities())
                    {
                        var siteSetting = EF.ULSTbl_SiteSetting.FirstOrDefault();
                        if (siteSetting == null || siteSetting.CanRes == false)
                        {
                            // cr-dotnet-0121: Use UtcNow for cache expiry
                            HttpRuntime.Cache.Insert("CanRes", false, null, DateTimeOffset.UtcNow.AddDays(1).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                            return false;
                        }
                        HttpRuntime.Cache.Insert("CanRes", true, null, DateTimeOffset.UtcNow.AddDays(1).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                    }
                }
                catch
                {
                    return false;
                }
            }
            return (bool)HttpRuntime.Cache["CanRes"];
        }
    }
}
