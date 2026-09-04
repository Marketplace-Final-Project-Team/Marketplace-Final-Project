DELETE FROM [__EFMigrationsHistory];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES 
(N'20260823085908_InitialCreate', N'10.0.9'),
(N'20260824081609_AddAdultStatusToBusiness', N'10.0.9'),
(N'20260825103524_AddIsAdultOnlyAndFixDecimals', N'10.0.9'),
(N'20260825141723_AddIsAdultOnlyToCategory', N'10.0.9'),
(N'20260826125351_AddPostSaleRequests', N'10.0.9'),
(N'20260826135114_AddPromoCodes', N'10.0.9'),
(N'20260826154025_AddSystemSettingsAndActivityLogs', N'10.0.9'),
(N'20260827125607_AddMarketplaceBalanceAndCommission', N'10.0.9'),
(N'20260827140000_RemoveDisputeAndAddReturnPhotos', N'10.0.9'),
(N'20260827150000_AddItemLevelReturns', N'10.0.9'),
(N'20260828201648_AddBusinessCommissionColumn', N'10.0.9'),
(N'20260829121057_AddProductModerationReason', N'10.0.9'),
(N'20260830091048_AddBusinessDiscounts', N'10.0.9');