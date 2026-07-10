# Commit Message Convention

本專案採用 **Conventional Commits** 規範。

## 格式

```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

## Type

- **feat**: 新功能
- **fix**: Bug 修復
- **docs**: 文檔更新
- **style**: 格式調整（不影響程式碼運行）
- **refactor**: 重構（既不是新功能也不是 bug 修復）
- **perf**: 效能優化
- **test**: 測試相關
- **chore**: 建構過程或輔助工具的變動

## Scope

根據變更的功能模組選擇：

- **unit-of-work**: Unit of Work 模式相關
- **exceptions**: 異常處理
- **result-pattern**: Result 模式
- **audit**: 審計上下文
- **soft-delete**: 軟刪除
- **validation**: 驗證錯誤處理
- **adr**: 架構決策記錄
- **logging**: 結構化日誌
- **api-docs**: API 文檔
- **tests**: 測試相關
- **core**: 核心功能或跨模組變更

## Subject

- 使用現在式、祈使語氣："add" 而不是 "added" 或 "adds"
- 首字母小寫
- 結尾不加句號
- 簡潔明確，建議不超過 50 字元

## 範例

### 好的範例 ✅

```
feat(unit-of-work): add transaction support for batch operations
fix(logging): correct correlation ID middleware registration
docs(adr): update ADR-003 with performance benchmarks
refactor(exceptions): simplify exception handling middleware
test(validation): add integration tests for error responses
```

### 不好的範例 ❌

```
feat: Added some stuff                    ❌ 沒有 scope，描述不清楚
Fix: Update Test                          ❌ Type 首字母大寫
feat(openspec): Create proposal           ❌ scope 太通用
docs: Phase 8 - Validation Report         ❌ 沒有 scope
```

## Body（可選）

詳細說明變更的原因、做了什麼、為什麼這樣做：

```
feat(audit): add user context tracking

Store user information from JWT in audit context for all database
operations. This enables tracking who created, updated, or deleted
records at the application level.

- Add IAuditContext interface
- Implement AuditContextService with HTTP context access
- Register as scoped service for DI
```

## Footer（可選）

用於引用 issue 或標註 breaking changes：

```
feat(api): change authentication flow

BREAKING CHANGE: API endpoints now require OAuth2 instead of API keys.
Migration guide: https://...

Closes #123
```

## Git Hooks（建議）

使用 commitlint 自動驗證 commit messages：

```bash
npm install --save-dev @commitlint/{cli,config-conventional}
echo "module.exports = {extends: ['@commitlint/config-conventional']}" > commitlint.config.js
```

## 更多資訊

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Angular Commit Guidelines](https://github.com/angular/angular/blob/main/CONTRIBUTING.md#commit)
