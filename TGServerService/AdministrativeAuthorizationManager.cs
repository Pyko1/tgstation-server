﻿using System;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceModel;
using TGServiceInterface.Components;

namespace TGServerService
{
	/// <summary>
	/// A <see cref="ServiceAuthorizationManager"/> used to determine only if the caller is an admin
	/// </summary>
	sealed class AdministrativeAuthorizationManager : ServiceAuthorizationManager
	{
		string LastSeenUser;
		protected override bool CheckAccessCore(OperationContext operationContext)
		{
			var contract = operationContext.EndpointDispatcher.ContractName;

			if (contract == typeof(ITGConnectivity).Name)   //always allow connectivity checks
				return true;

			var windowsIdent = operationContext.ServiceSecurityContext.WindowsIdentity;

			var wp = new WindowsPrincipal(windowsIdent);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);

			var user = windowsIdent.Name;
			if (LastSeenUser != user)
			{
				LastSeenUser = user;
				Service.WriteEntry(String.Format("Root access from: {0}", user), EventID.Authentication, authSuccess ? EventLogEntryType.SuccessAudit : EventLogEntryType.FailureAudit, Service.LoggingID);
			}
			return authSuccess;
		}
	}
}