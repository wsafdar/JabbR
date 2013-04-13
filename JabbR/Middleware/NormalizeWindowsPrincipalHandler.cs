﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    public class NormalizeWindowsPrincipalHandler
    {
        private readonly AppFunc _next;

        public NormalizeWindowsPrincipalHandler(AppFunc next)
        {
            _next = next;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            object value;
            if (env.TryGetValue("server.User", out value))
            {
                var windowsPrincipal = value as WindowsPrincipal;
                if (windowsPrincipal != null && windowsPrincipal.Identity.IsAuthenticated)
                {
                    // We're going no add the identifier claim
                    var nameClaim = windowsPrincipal.FindFirst(ClaimTypes.Name);

                    // This is the domain name
                    string name = nameClaim.Value;

                    // If the name is something like DOMAIN\username then
                    // grab the name part
                    var parts = name.Split(new[] { '\\' }, 2);

                    string shortName = parts.Length == 1 ? parts[0] : parts[parts.Length - 1];

                    // REVIEW: Do we want to preserve the other claims?

                    // Normalize the claims here
                    var claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, name));
                    claims.Add(new Claim(ClaimTypes.Name, shortName));
                    claims.Add(new Claim(ClaimTypes.AuthenticationMethod, "Windows"));
                    var identity = new ClaimsIdentity(claims, windowsPrincipal.Identity.AuthenticationType);
                    var claimsPrincipal = new ClaimsPrincipal(identity);

                    env["server.User"] = claimsPrincipal;
                }
            }

            return _next(env);
        }
    }
}