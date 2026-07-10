-- ============================================
-- 快速修復：user 表格 Unicode 支援
-- ============================================
-- 執行前請確認：
-- 1. 已備份資料庫
-- 2. 了解已有的 ???? 資料無法恢復
-- 3. 修改後需要重新輸入正確資料
-- ============================================

USE [YourDatabaseName]; -- 請修改為實際的資料庫名稱
GO

-- 修改 name 欄位支援 Unicode
ALTER TABLE [user] 
ALTER COLUMN [name] NVARCHAR(255) NULL;
GO

-- 修改 email 欄位支援 Unicode
ALTER TABLE [user] 
ALTER COLUMN [email] NVARCHAR(255) NOT NULL;
GO

-- 修改 image 欄位支援 Unicode（如果此欄位儲存 URL 路徑）
ALTER TABLE [user] 
ALTER COLUMN [image] NVARCHAR(500) NULL;
GO

-- 驗證修改
SELECT 
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    CASE 
        WHEN ty.name LIKE 'n%' THEN 'Unicode Supported ✓'
        ELSE 'Not Unicode ✗'
    END AS Status
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID('user')
AND c.name IN ('name', 'email', 'image');
GO

-- 測試插入
-- UPDATE [user] 
-- SET [name] = N'Chun Chu Hsu(Corp. Application)許鈞竹'
-- WHERE [email] = N'your-email@example.com';
-- GO

PRINT 'Unicode support enabled successfully!';
PRINT 'Please re-enter user names with correct characters.';
GO
