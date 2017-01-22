﻿using System;
using System.Security.Claims;

public interface IRouteContext
{
	IServiceProvider RequestServices { get; }
	ClaimsPrincipal User { get; }
}