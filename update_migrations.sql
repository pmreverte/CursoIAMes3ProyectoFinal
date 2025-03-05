-- Insert a record for the AddIdentity migration
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES ('20250305112143_AddIdentity', '9.0.0');

-- Now we can apply the AddApplicationUserFields migration using EF Core
