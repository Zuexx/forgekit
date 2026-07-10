/**
 * 檢查資料庫 Unicode 支援
 * 
 * 執行方式：
 * npx tsx docs/check-unicode-support.ts
 */

import { sql } from 'kysely'

import { db } from '@/lib/db/mssql'

interface ColumnInfo {
  TableName: string
  ColumnName: string
  DataType: string
  MaxLength: number
  Collation: string | null
}

async function checkUnicodeSupport() {
  console.log('🔍 檢查資料庫 Unicode 支援...\n')

  try {
    // 檢查 user 表格的欄位型別
    const columns = await sql<ColumnInfo>`
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
      AND c.name IN ('name', 'email', 'image')
    `.execute(db)

    console.log('📋 User 表格欄位資訊：\n')
    
    let hasIssues = false
    
    columns.rows.forEach((col) => {
      const isUnicode = col.DataType.startsWith('n')
      const status = isUnicode ? '✅' : '❌'
      const issue = isUnicode ? '' : ' ⚠️ 需要修改為 NVARCHAR'
      
      console.log(`${status} ${col.ColumnName}`)
      console.log(`   型別: ${col.DataType}(${col.MaxLength})${issue}`)
      console.log(`   Collation: ${col.Collation || 'N/A'}`)
      console.log()
      
      if (!isUnicode) {
        hasIssues = true
      }
    })

    if (hasIssues) {
      console.log('\n⚠️  發現問題：')
      console.log('某些欄位使用 VARCHAR 而非 NVARCHAR，無法正確儲存中文/韓文/日文字元。')
      console.log('\n📝 修正步驟：')
      console.log('1. 執行 docs/quick-fix-unicode.sql')
      console.log('2. 重新輸入包含非 ASCII 字元的資料')
      console.log('3. 確保未來插入資料時使用 N 前綴（Kysely 會自動處理）')
    } else {
      console.log('✅ 所有欄位都支援 Unicode！')
    }

    // 測試資料完整性
    console.log('\n\n🔍 檢查現有資料...\n')
    
    const users = await db
      .selectFrom('user')
      .select(['id', 'name', 'email'])
      .execute()

    let corruptedCount = 0
    
    users.forEach((user) => {
      if (user.name && user.name.includes('?')) {
        console.log(`❌ 損壞的資料: ${user.email}`)
        console.log(`   名字: ${user.name}`)
        corruptedCount++
      }
    })

    if (corruptedCount > 0) {
      console.log(`\n⚠️  發現 ${corruptedCount} 筆損壞的資料`)
      console.log('這些資料需要重新輸入正確的名字')
    } else {
      console.log('✅ 沒有發現損壞的資料')
    }

  } catch (error) {
    console.error('❌ 檢查失敗:', error)
  } finally {
    await db.destroy()
  }
}

// 執行檢查
checkUnicodeSupport()
