﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using ViewExtensions;

namespace TestSite
{
    public class ViewExtensionsConfig
    {
        public static void RegisterViews()
        {
            Views.Load("/Views");
        }
    }
}