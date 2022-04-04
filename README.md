Ref:
https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html
https://balta.io/blog/executando-processamentos-em-segundo-plano-no-dotnet-com-o-hangfire


-- Cria projeto
dotnet new console

-- Instalar pacotes necessários:
dotnet add package Newtonsoft.Json
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
// dotnet add package Hangfire.Storage.SQLite -- 
dotnet add package Hangfire.SqlServer

-- ERASE DATABASE
TRUNCATE TABLE [HangFire].[AggregatedCounter]
TRUNCATE TABLE [HangFire].[Counter]
TRUNCATE TABLE [HangFire].[JobParameter]
TRUNCATE TABLE [HangFire].[JobQueue]
TRUNCATE TABLE [HangFire].[List]
TRUNCATE TABLE [HangFire].[State]
DELETE FROM [HangFire].[Job]
DBCC CHECKIDENT ('[HangFire].[Job]', reseed, 0)
UPDATE [HangFire].[Hash] SET Value = 1 WHERE Field = 'LastJobId'

OR

ALTER TABLE [HangFire].[State] DROP CONSTRAINT [FK_HangFire_State_Job];
ALTER TABLE [HangFire].[JobParameter] DROP CONSTRAINT [FK_HangFire_JobParameter_Job];
DROP TABLE [HangFire].[Schema];
DROP TABLE [HangFire].[Job];
DROP TABLE [HangFire].[State];
DROP TABLE [HangFire].[JobParameter];
DROP TABLE [HangFire].[JobQueue];
DROP TABLE [HangFire].[Server];
DROP TABLE [HangFire].[List];
DROP TABLE [HangFire].[Set];
DROP TABLE [HangFire].[Counter];
DROP TABLE [HangFire].[Hash];
DROP TABLE [HangFire].[AggregatedCounter];
DROP SCHEMA [HangFire];