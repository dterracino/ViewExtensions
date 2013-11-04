﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web;
using System.Text;

// ----------------------------------------------------
// To test sub domains, create domains in 
// C:\WINDOWS\system32\drivers\etc\hosts
// ----------------------------------------------------

namespace ViewExtensions
{
    public static class PageVersions
    {
        private static IEnumerable<VersionInfo> _versionInfos = null;
        private static bool _useCookies = false;
        private static bool _useSubDomain = false;

        public class VersionInfo
        {
            public string VersionUrlName { get; set; } // Used in url
            public string VersionName { get; set; } // Used in C# code
            public string Caption { get; set; } // Used in version switcher
            public bool IsDefault { get; set; }
        }

        private const string VersionUrlParam = "version";
        private const string CookieName = "version";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="versionInfos"></param>
        /// <param name="useCookies">
        /// If true, the current version choice is stored in a cookie.
        /// When no version choice can be determined, the contents of the cookie is used.
        /// 
        /// If false, no cookies are used.
        /// </param>
        /// <param name="useSubDomain">
        /// If true, loading a new version means loading a sub domain.
        /// And the current version is determined from the currrent sub domain (if there is one).
        /// 
        /// If false, a query string param is used.
        /// </param>
        public static void Load(IEnumerable<VersionInfo> versionInfos, bool useCookies, bool useSubDomain)
        {
            _versionInfos = versionInfos;
            _useCookies = useCookies;
            _useSubDomain = useSubDomain;
        }

        /// <summary>
        /// Returns the name of the currently selected version.
        /// </summary>
        /// <returns></returns>
        public static string CurrentVersion()
        {
            if (_versionInfos == null)
            {
                return null;
            }

            // First try query string parameter

            VersionInfo versionInfo = GetCurrentVersion();

            if (!String.IsNullOrEmpty(versionName))
            {
                if (_versionInfos.Any(v=>v.VersionName == versionName)) 
                {
                    if (_useCookies)
                    {
                        // Set cookie, so when other pages are opened user gets same version
                        HttpContext.Current.Response.Cookies[CookieName].Value = versionName;
                        HttpContext.Current.Request.Cookies[CookieName].Expires = DateTime.Now.AddYears(1);
                    }

                    return versionName;
                }
            }

            if (_useCookies)
            {
                // Then try cookie

                versionName = GetVersionName(); 
                if (!String.IsNullOrEmpty(versionName))
                {
                    if (_versionInfos.Any(v => v.VersionName == versionName))
                    {
                        return versionName;
                    }
                }
            }

            // If no cookie, use default

            string defaultVersionName = _versionInfos.Single(v => v.IsDefault).VersionName;
            return defaultVersionName;
        }

        public static MvcHtmlString VersionSwitcher(this HtmlHelper htmlHelper)
        {
            var versionName = CurrentVersion();
            var sb = new StringBuilder();

            foreach (var versionInfo in _versionInfos)
            {
                if (versionInfo.VersionName == versionName)
                {
                    sb.AppendFormat("<span>{0}</span>", versionInfo.Caption);
                }
                else
                {
                    sb.AppendFormat(
                        @"<a href=""{0}"">{1}</a>", UrlWithVersionName(versionInfo.VersionName), versionInfo.Caption);
                }
            }

            return new MvcHtmlString(sb.ToString());
        }

        private static string GetCurrentVersion()
        {
            string versionUrlName = null;

            if (_useSubDomain)
            {
                versionUrlName = RequestSubdomain();
            }
            else
            {
                versionUrlName = (string)HttpContext.Current.Request.Cookies[CookieName].Value;
            }

            VersionInfo versionInfo = _versionInfos.SingleOrDefault(v => v.VersionUrlName == versionUrlName);
        }

        private static string UrlWithVersionName(string versionName)
        {
            if (!_useSubDomain)
            {
                return string.Format("?{0}={1}", VersionUrlParam, versionName);
            }

            // Take current uri, and replace sub domain with version name.
            //
            // This assumes that the pattern "//subdomain." (2 x forward slash, followed by sub domain, followed by .)
            // doesn't appear anywhere else in the uri.

            // If new version is the default, do not use a sub domain
            string newSubdomainWithDot = versionName + ".";
            if (_versionInfos.Single(v => v.VersionName == versionName).IsDefault)
            {
                newSubdomainWithDot = "";
            }

            string currentUri = HttpContext.Current.Request.Url.ToString();
            string currentSubDomain = RequestSubdomain();
            string newUri = "";

            if (string.IsNullOrEmpty(currentSubDomain))
            {
                // there is currently no sub domain
                newUri = currentUri.Replace("//", "//" + newSubdomainWithDot);
            }
            else
            {
                // there is currently a sub domain
                newUri = currentUri.Replace("//" + currentSubDomain + ".", "//" + newSubdomainWithDot);
            }

            return newUri;
        }

        /// <summary>
        /// Returns the sub domain in the current request.
        /// 
        /// Returns null if there is no sub domain.
        /// 
        /// Assumes that the domain is not country specific. 
        /// For example, jsnlog.com but not jsnlog.com.au.
        /// </summary>
        /// <returns></returns>
        private static string RequestSubdomain()
        {
            Uri currentUri = HttpContext.Current.Request.Url;

            string[] uriParts = currentUri.Host.Split(new[] { '.' });

            // If there are only 2 or 1 parts in the host name, you can't have a sub domain.
            if (uriParts.Length < 3)
            {
                return null;
            }

            // Assume that the first part is the sub domain. For example,
            // js.jsnlog.com

            return uriParts[0];
        }
    }
}
