

DECLARE @ProviderId UNIQUEIDENTIFIER
SET @ProviderId = 'A8296CEB-33B8-435B-BA09-1648D4939B38'

IF NOT EXISTS(SELECT 1 FROM [HostServer].[SecurityProvider] WHERE ProviderClassName = 'SourceCode.Security.Providers.XmlRoleProvider')
BEGIN
	INSERT INTO [HostServer].[SecurityProvider]
	VALUES
	(
		@ProviderId,
		'SourceCode.Security.Providers.XmlRoleProvider'
	)
END
ELSE 
BEGIN
	SELECT @ProviderId = SecurityProviderId FROM [HostServer].[SecurityProvider] WHERE ProviderClassName = 'SourceCode.Security.Providers.XmlRoleProvider'
END

IF NOT EXISTS(SELECT 1 FROM  [HostServer].[SecurityLabel] WHERE SecurityLabelName = 'XML')
BEGIN
	INSERT INTO [HostServer].[SecurityLabel]
	VALUES
	(
		'4AE76FE9-D4F5-46A5-98A2-72897F8D2CA0',
		'XML',	
		@ProviderId,
		'<authInit/>',
		@ProviderId, 
		'<roleprovider userStoreFileName="UserStore.xml" />',
		0
	)
END
GO