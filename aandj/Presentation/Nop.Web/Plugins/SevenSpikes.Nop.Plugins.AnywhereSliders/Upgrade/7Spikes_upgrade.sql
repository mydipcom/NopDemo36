
-- Upgrade script for JCarousel
IF EXISTS (SELECT *
           FROM   sys.objects
           WHERE  object_id = OBJECT_ID(N'[dbo].[GetJCarouselItemMinWidth]'))
BEGIN
	DROP FUNCTION [dbo].[GetJCarouselItemMinWidth]
END
GO

CREATE FUNCTION dbo.GetJCarouselItemMinWidth()
RETURNS INT
AS
BEGIN
	Declare @JCarouselItemMinWidthGlobalValue int
		
	Set @JCarouselItemMinWidthGlobalValue = (Select top 1 [Value] from [dbo].[Setting] where [Setting].Name like '%jcarouselgeneralsettings.jcarouselitemminwidth%')
		
	IF( @JCarouselItemMinWidthGlobalValue is NULL ) 
	BEGIN
		Set @JCarouselItemMinWidthGlobalValue = 0
	END

	RETURN @JCarouselItemMinWidthGlobalValue
END
GO

IF(EXISTS (SELECT NULL FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SS_JC_JCarousel]') AND type in (N'U')))
BEGIN
	
	IF( NOT EXISTS (SELECT NULL FROM sys.columns WHERE object_id = object_id(N'[dbo].[SS_JC_JCarousel]') and NAME='JCarouselItemMinWidth'))
	BEGIN
		ALTER TABLE [dbo].[SS_JC_JCarousel] ADD [JCarouselItemMinWidth] INT NOT NULL DEFAULT (dbo.GetJCarouselItemMinWidth());
	END
	
	IF( NOT EXISTS (SELECT NULL FROM sys.columns WHERE object_id = object_id(N'[dbo].[SS_JC_JCarousel]') and NAME='ShowRatings'))
	BEGIN
		ALTER TABLE [dbo].[SS_JC_JCarousel] ADD [ShowRatings] BIT NOT NULL DEFAULT 0;
	END
END

-- Upgrade script for Store Locator
IF(EXISTS (SELECT NULL FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SS_SL_Shop]') AND type in (N'U')))
BEGIN
	IF( NOT EXISTS (SELECT NULL FROM sys.columns WHERE object_id = object_id(N'[dbo].[SS_SL_Shop]') and NAME='ShowOnHomePage'))
	BEGIN
		ALTER TABLE [dbo].[SS_SL_Shop] ADD [ShowOnHomePage] BIT NOT NULL DEFAULT 1;
	END
	
	IF( NOT EXISTS (SELECT NULL FROM sys.columns WHERE object_id = object_id(N'[dbo].[SS_SL_Shop]') and NAME='Tags'))
	BEGIN
		ALTER TABLE [dbo].[SS_SL_Shop] ADD [Tags] NVARCHAR(MAX) NULL;
	END
END

-- Upgrade script for Sales Campaign
IF(EXISTS (SELECT NULL FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SS_SC_SaleCampaignCustomization]') AND type in (N'U')))
BEGIN

	IF((SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS IC WHERE TABLE_NAME = 'SS_SC_SaleCampaignCustomization' and COLUMN_NAME = 'MarginSize') = 'int')
	BEGIN
		ALTER TABLE [dbo].[SS_SC_SaleCampaignCustomization] ALTER COLUMN MarginSize NVARCHAR(MAX) NULL;

		UPDATE [dbo].[SS_SC_SaleCampaignCustomization]
		SET MarginSize = MarginSize + ',' + MarginSize + ',' + MarginSize + ',' + MarginSize;
	END

	IF((SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS IC WHERE TABLE_NAME = 'SS_SC_SaleCampaignCustomization' and COLUMN_NAME = 'PaddingSize') = 'int')
	BEGIN
		ALTER TABLE [dbo].[SS_SC_SaleCampaignCustomization] ALTER COLUMN PaddingSize NVARCHAR(MAX) NULL;

		UPDATE [dbo].[SS_SC_SaleCampaignCustomization]
		SET PaddingSize = PaddingSize + ',' + PaddingSize + ',' + PaddingSize + ',' + PaddingSize;
	END

	
	IF( EXISTS (SELECT NULL FROM sys.columns WHERE object_id = object_id(N'[dbo].[SS_SC_SaleCampaignCustomization]') and NAME = 'BorderColor'))
	BEGIN
		UPDATE [dbo].[SS_SC_SaleCampaignCustomization]
		SET CustomCSS = CustomCSS + 'border: 1px solid ' + [BorderColor] + ';';

		ALTER TABLE [dbo].[SS_SC_SaleCampaignCustomization] DROP COLUMN [BorderColor]
	END

	IF( NOT EXISTS (SELECT NULL FROM sys.columns WHERE object_id = object_id(N'[dbo].[SS_SC_SaleCampaignCustomization]') and NAME='FontWeight'))
	BEGIN
		ALTER TABLE [dbo].[SS_SC_SaleCampaignCustomization] ADD [FontWeight] INT NOT NULL DEFAULT 5;
	END

	IF( NOT EXISTS (SELECT NULL FROM sys.columns WHERE object_id = object_id(N'[dbo].[SS_SC_SaleCampaignCustomization]') and NAME='FontStyle'))
	BEGIN
		ALTER TABLE [dbo].[SS_SC_SaleCampaignCustomization] ADD [FontStyle] INT NOT NULL DEFAULT 5;
	END

END