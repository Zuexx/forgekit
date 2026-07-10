-- ============================================
-- 修正 SQL Server Unicode 支援問題
-- ============================================
-- 問題：VARCHAR 欄位無法儲存中文/韓文/日文等 Unicode 字元
-- 解決：將 VARCHAR 改為 NVARCHAR
-- ============================================

-- 步驟 1: 檢查目前的欄位型別
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.collation_name AS Collation,
    CASE 
        WHEN ty.name LIKE 'n%' THEN 'Unicode (Good ✓)'
        ELSE 'Non-Unicode (Need Fix ✗)'
    END AS UnicodeSupport
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.name = 'user'
AND c.name IN ('name', 'email', 'image');
GO

-- 步驟 2: 備份資料（重要！）
-- 建議先做完整的資料庫備份
BACKUP DATABASE [YourDatabaseName] 
TO DISK = 'C:\Backup\YourDatabase_BeforeUnicodeFix.bak'
WITH FORMAT, INIT, NAME = 'Before Unicode Fix';
GO

-- 或者備份表格資料
SELECT * INTO [user_backup] FROM [user];
GO

-- 步驟 3: 修改欄位型別為 NVARCHAR
-- 注意：這會清除已經損壞的資料（????），需要重新輸入

-- 修改 name 欄位
ALTER TABLE [user] 
ALTER COLUMN [name] NVARCHAR(255) NULL;
GO

-- 修改 email 欄位（如果有需要）
ALTER TABLE [user] 
ALTER COLUMN [email] NVARCHAR(255) NOT NULL;
GO

-- 修改 image 欄位（如果有需要）
ALTER TABLE [user] 
ALTER COLUMN [image] NVARCHAR(500) NULL;
GO

-- 步驟 4: 驗證修改結果
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.collation_name AS Collation
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.name = 'user'
AND c.name IN ('name', 'email', 'image');
GO

-- 步驟 5: 測試插入 Unicode 資料
-- 重要：必須使用 N 前綴
UPDATE [user] 
SET [name] = N'Chun Chu Hsu(Corp. Application)許鈞竹'
WHERE [email] = 'your-email@example.com';
GO

-- 步驟 6: 驗證資料是否正確儲存
SELECT 
    [id],
    [name],
    [email],
    LEN([name]) AS NameLength,
    DATALENGTH([name]) AS NameBytes
FROM [user]
WHERE [email] = 'your-email@example.com';
GO

-- ============================================
-- 其他需要修改的表格
-- ============================================

-- 檢查所有表格中的 VARCHAR 欄位
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE ty.name IN ('varchar', 'char', 'text')
AND t.name IN ('user', 'session', 'account', 'verification')
ORDER BY t.name, c.name;
GO

-- 如果 better-auth 使用其他表格，也需要修改
-- 例如：account 表格
ALTER TABLE [account] 
ALTER COLUMN [providerId] NVARCHAR(255) NULL;
GO

ALTER TABLE [account] 
ALTER COLUMN [accountId] NVARCHAR(255) NOT NULL;
GO

-- session 表格
ALTER TABLE [session] 
ALTER COLUMN [token] NVARCHAR(500) NOT NULL;
GO

-- verification 表格
ALTER TABLE [verification] 
ALTER COLUMN [identifier] NVARCHAR(255) NOT NULL;
GO

ALTER TABLE [verification] 
ALTER COLUMN [value] NVARCHAR(500) NOT NULL;
GO

-- ============================================
-- 注意事項
-- ============================================
/*
1. VARCHAR vs NVARCHAR:
   - VARCHAR: 1 byte per character, ASCII only
   - NVARCHAR: 2 bytes per character, Unicode support
   
2. 長度計算:
   - VARCHAR(255) = 最多 255 個字元
   - NVARCHAR(255) = 最多 255 個字元（但佔用 510 bytes）
   
3. 效能影響:
   - NVARCHAR 佔用空間是 VARCHAR 的兩倍
   - 但對於現代硬體，影響可以忽略
   - 必須使用 NVARCHAR 來支援國際化
   
4. 已損壞的資料:
   - 如果資料已經以 ???? 儲存，無法恢復
   - 必須重新輸入正確的資料
   
5. 未來插入資料:
   - 在 SQL 中必須使用 N 前綴: N'中文'
   - Kysely/ORM 通常會自動處理
   - 但手動 SQL 必須注意

6. 索引和約束:
   - 修改欄位型別可能影響索引
   - 如果有唯一約束或外鍵，可能需要先刪除再重建
*/
