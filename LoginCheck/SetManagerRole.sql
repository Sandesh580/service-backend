-- Run once in LoginCheckDb after replacing the email with the service manager's existing account.
-- Public registration intentionally creates Customer accounts only.
UPDATE dbo.Users
SET Role = N'Manager'
WHERE Email = N'REPLACE-WITH-MANAGER-EMAIL';

SELECT Id, UserName, Email, Role
FROM dbo.Users
WHERE Email = N'REPLACE-WITH-MANAGER-EMAIL';
