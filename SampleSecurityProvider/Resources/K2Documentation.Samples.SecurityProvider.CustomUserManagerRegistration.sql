-- DECLARATIONS - Of variables needed. Must be updated with your Custom User Manager details
DECLARE @SecurityLabelName NVARCHAR(20) = 'K2CUSTOM'; -- the label value that will be prepended to users and groups for the user manager
DECLARE @SecurityLabelID UNIQUEIDENTIFIER = NEWID(); -- GUID of SecurityLabel for user manager
DECLARE @AuthSecurityProviderID UNIQUEIDENTIFIER = NEWID(); -- GUID of SecurityProvider for Authentication Services(IAuthenticationProvider)
DECLARE @AuthInit XML = '' -- XML initialization data for the Authentication Provider
DECLARE @RoleSecurityProviderID UNIQUEIDENTIFIER = NEWID(); -- GUID of the SecurityProvider for User and Group Listing services (IRoleProvider)
DECLARE @RoleInit XML = '' -- XML initialization data for the Role Provider
DECLARE @DefaultLabel BIT = NULL; --1 = true, NULL and 0 = false
DECLARE @ProviderClassName NVARCHAR(200) = ''; -- the full .NET name of the Security Provider class

IF NOT EXISTS(SELECT 1 FROM [HostServer].[SecurityProvider] WHERE ProviderClassName = @ProviderClassName)
BEGIN
	INSERT INTO [HostServer].[SecurityProvider]
	VALUES
	(
		@AuthSecurityProviderID,
		@ProviderClassName
	)
END
ELSE 
BEGIN
	SELECT @AuthSecurityProviderID = SecurityProviderId FROM [HostServer].[SecurityProvider] WHERE ProviderClassName = @ProviderClassName
END

IF NOT EXISTS(SELECT 1 FROM  [HostServer].[SecurityLabel] WHERE SecurityLabelName = @SecurityLabelName)
BEGIN
	INSERT INTO [HostServer].[SecurityLabel]
	VALUES
	(
		@SecurityLabelID, 
		@SecurityLabelName, 
		@AuthSecurityProviderID, 
		@AuthInit, 
		@RoleSecurityProviderID, 
		@RoleInit, 
		@DefaultLabel
	)
END
GO