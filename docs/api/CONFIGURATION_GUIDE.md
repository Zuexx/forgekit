# Configuration Guide

A comprehensive guide to configuring the ForgeKit API through `appsettings.json` and environment variables.

## Table of Contents

1. [Overview](#overview)
2. [Configuration Files](#configuration-files)
3. [Core Settings](#core-settings)
4. [Database Configuration](#database-configuration)
5. [Authentication & Security](#authentication--security)
6. [Logging Configuration](#logging-configuration)
7. [Email Configuration](#email-configuration)
8. [Environment-Specific Settings](#environment-specific-settings)
9. [Advanced Configuration](#advanced-configuration)
10. [Troubleshooting](#troubleshooting)

---

## Overview

The ForgeKit API uses **hierarchical configuration** with the following precedence (highest to lowest):

1. **Environment Variables** - Override all other sources
2. **appsettings.{Environment}.json** - Environment-specific settings (Development, Staging, Production)
3. **appsettings.json** - Base settings for all environments
4. **User Secrets** - For local development only (not committed to source control)

### Quick Reference

| Setting | Required | Default | Environment |
|---------|----------|---------|-------------|
| Database:Provider | ❌ No | Sqlite | All |
| ConnectionStrings:Sqlite | ✅ Yes | Data Source=./data/forgekit.db | Sqlite |
| ConnectionStrings:Postgres | ⚠️ Conditional | - | Postgres |
| ConnectionStrings:SqlServer | ⚠️ Conditional | - | SqlServer |
| JwtData:Issuer | ✅ Yes | - | All |
| JwtData:Audience | ✅ Yes | - | All |
| JwksCallBackUrl:Jwks | ✅ Yes | - | All |
| Cors:AllowedOrigins | ⚠️ Conditional | localhost:* (Dev) | Production |
| Serilog:MinimumLevel:Default | ❌ No | Information | All |
| SMTP:SmtpHost | ⚠️ Conditional | - | If email features used |

---

## Configuration Files

### File Structure

```
ForgeKit.Api/
├── appsettings.json              # Base configuration (committed)
├── appsettings.Development.json  # Development overrides (committed)
├── appsettings.Staging.json      # Staging overrides (committed, if exists)
├── appsettings.Production.json   # Production overrides (DO NOT commit secrets!)
└── secrets.json                  # User secrets (NEVER commit!)
```

### Environment Detection

The application automatically detects the environment from:

1. `ASPNETCORE_ENVIRONMENT` environment variable
2. `--environment` command-line argument
3. Defaults to `Production` if not set

**Example:**
```bash
# Windows
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run

# Linux/macOS
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

---

## Core Settings

### AllowedHosts

Controls which host headers are allowed to prevent host header injection attacks.

**Location:** `appsettings.json`
```json
{
  "AllowedHosts": "*"
}
```

**Options:**
- `"*"` - Allow all hosts (use only in Development)
- `"localhost;example.com"` - Semicolon-separated list
- `"*.example.com"` - Wildcard for subdomains

**Recommendation:**
```json
// Development
"AllowedHosts": "*"

// Production
"AllowedHosts": "api.yourdomain.com;*.yourdomain.com"
```

---

## Database Configuration

### Provider Selection

The starter kit defaults to SQLite so a fresh fork can run without PostgreSQL, SQL Server, Docker, or cloud database setup. `AppDbContext` and `BetterAuthDbContext` use the same provider setting.

```json
{
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "Sqlite": "Data Source=./data/forgekit.db",
    "Postgres": "Host=localhost;Database=forgekit_db",
    "SqlServer": "Server=localhost;Database=forgekit_db;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Supported values:
- `Sqlite`
- `Postgres` (also accepts `PostgreSQL` or `Npgsql`)
- `SqlServer` (also accepts `MSSQL`)

### Migration Compatibility

Database providers do not share identical DDL. SQLite, PostgreSQL, and SQL Server migrations MUST NOT be mixed against the same migration history.

ForgeKit ships one migration project per provider. Each project contains independent migration chains for `AppDbContext` and `BetterAuthDbContext`:

- `ForgeKit.Api.Migrations.Sqlite`
- `ForgeKit.Api.Migrations.Postgres`
- `ForgeKit.Api.Migrations.SqlServer`

```bash
cd api

# Default local SQLite database
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context BetterAuthDbContext

# PostgreSQL: the factory reads ConnectionStrings__Postgres
export ConnectionStrings__Postgres='<read-from-secret-store>'
dotnet ef database update --project ForgeKit.Api.Migrations.Postgres --startup-project ForgeKit.Api.Migrations.Postgres --context AppDbContext
dotnet ef database update --project ForgeKit.Api.Migrations.Postgres --startup-project ForgeKit.Api.Migrations.Postgres --context BetterAuthDbContext

# SQL Server: the factory reads ConnectionStrings__SqlServer
export ConnectionStrings__SqlServer='<read-from-secret-store>'
dotnet ef database update --project ForgeKit.Api.Migrations.SqlServer --startup-project ForgeKit.Api.Migrations.SqlServer --context AppDbContext
dotnet ef database update --project ForgeKit.Api.Migrations.SqlServer --startup-project ForgeKit.Api.Migrations.SqlServer --context BetterAuthDbContext
```

Use `--connection` on `database update` to override the factory connection string for a single command. When the EF model changes, add a migration to every supported provider project and use `--output-dir Migrations/App` or `Migrations/Auth` for the matching context. Keep the shared EF model provider-neutral unless the fork has committed to one database.

Better Auth requires durable storage for local sessions and accounts. Runtime authentication SHOULD use SQLite/PostgreSQL/SQL Server, not EF Core InMemory. EF Core InMemory is reserved for narrow unit tests only.

### Connection Strings

**Required:** ✅ Yes for the selected provider
**Location:** `appsettings.json`, `appsettings.{Environment}.json`, environment variables, or `appsettings.Local.json` for ignored local overrides.

#### SQLite (Default)

```json
{
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "Sqlite": "Data Source=./data/forgekit.db"
  }
}
```

Relative SQLite paths are resolved under the API content root. The default creates `api/ForgeKit.Api/data/forgekit.db`, and SQLite files under `api/ForgeKit.Api/data/` are ignored by git.

#### PostgreSQL

```json
{
  "Database": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=ForgeKitDb;Timeout=30;"
  }
}
```

#### Hosted PostgreSQL

```json
{
  "Database": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "Postgres": "Host=your-postgres-host.example.com;Port=5432;Database=ForgeKitDb;SSL Mode=Require;Trust Server Certificate=false;Timeout=30;"
  }
}
```

#### SQL Server

```json
{
  "Database": {
    "Provider": "SqlServer"
  },
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=ForgeKitDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

#### Using Environment-Specific Secrets

```json
{
  "ConnectionStrings": {
    "Postgres": "<set from secrets manager or environment variable>",
    "SqlServer": "<set from secrets manager or environment variable>"
  }
}
```

### Connection String Best Practices

✅ **DO:**
- Store production connection strings in **environment variables** or **Azure Key Vault**
- Use **separate databases** for each environment
- Use **least privilege** SQL users (not `sa`)
- Enable **SSL/TLS** in production (`Encrypt=True`, `TrustServerCertificate=False`)

❌ **DON'T:**
- Commit connection strings with passwords to source control
- Use `sa` account in production
- Share databases between environments
- Disable encryption in production

---

## Authentication & Security

### JWT Configuration

**Required:** ✅ Yes
**Location:** `appsettings.{Environment}.json`

```json
{
  "JwtData": {
    "Issuer": "http://localhost:3000",
    "Audience": "http://localhost:3000"
  },
  "JwksCallBackUrl": {
    "Base": "http://localhost:3000",
    "Jwks": "http://localhost:3000/api/auth/jwks"
  }
}
```

#### JwtData Section

| Setting | Description | Example |
|---------|-------------|---------|
| `Issuer` | JWT token issuer (typically your auth server) | `https://auth.yourdomain.com` |
| `Audience` | JWT token audience (typically this API) | `https://api.yourdomain.com` |

**Important:**
- Issuer and Audience must match the values in your JWT tokens
- Use **HTTPS** in production
- These values are used for **JWT signature validation**

#### JwksCallBackUrl Section

| Setting | Description | Example |
|---------|-------------|---------|
| `Base` | Base URL of your authentication server | `https://auth.yourdomain.com` |
| `Jwks` | Full URL to JWKS endpoint for public key retrieval | `https://auth.yourdomain.com/.well-known/jwks.json` |

**How it Works:**
1. API receives JWT token with `kid` (Key ID) in header
2. API fetches public keys from JWKS endpoint
3. API validates token signature using public key
4. JWKS keys are cached to reduce network calls

#### Environment Examples

**Development:**
```json
{
  "JwtData": {
    "Issuer": "http://localhost:3000",
    "Audience": "http://localhost:3000"
  },
  "JwksCallBackUrl": {
    "Base": "http://localhost:3000",
    "Jwks": "http://localhost:3000/api/auth/jwks"
  }
}
```

**Production:**
```json
{
  "JwtData": {
    "Issuer": "https://auth.yourdomain.com",
    "Audience": "https://api.yourdomain.com"
  },
  "JwksCallBackUrl": {
    "Base": "https://auth.yourdomain.com",
    "Jwks": "https://auth.yourdomain.com/.well-known/jwks.json"
  }
}
```

### CORS Configuration

Cross-Origin Resource Sharing (CORS) allows web browsers to access your API from different domains.

**Required:** ⚠️ Conditional (Required in Production)
**Location:** `appsettings.json` (Production) + `appsettings.Development.json` (Development)

#### Environment-Aware Behavior

- **Development**: Automatically allows any origin for easy local development
- **Production**: Requires explicit whitelist via `Cors:AllowedOrigins`

#### Development Configuration

**appsettings.Development.json:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200",
      "http://localhost:5173",
      "http://localhost:8080"
    ]
  }
}
```

#### Production Configuration

**appsettings.json:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://myapp.com",
      "https://www.myapp.com",
      "https://admin.myapp.com"
    ]
  }
}
```

#### Important Notes

⚠️ **Production Validation**: API throws `InvalidOperationException` on startup if:
- Running in Production environment
- No origins configured in `Cors:AllowedOrigins`

✅ **Security Best Practices**:
- Always use HTTPS in production origins
- Only whitelist domains you control
- Never use `*` wildcard in production
- Include all subdomains explicitly (e.g., `www.`, `api.`, `admin.`)

#### CORS Policy Features

The API CORS policy automatically includes:
- ✅ **Credentials**: Cookies and Authorization headers allowed
- ✅ **Any Headers**: All request headers permitted
- ✅ **Any Methods**: All HTTP methods (GET, POST, PUT, DELETE, etc.)
- ✅ **Preflight Caching**: OPTIONS requests cached for 1 hour

#### Testing CORS

**Browser Console:**
```javascript
fetch('https://your-api.com/health', {
  headers: { 'Origin': 'https://your-frontend.com' }
})
.then(res => console.log('CORS:', res.headers.get('Access-Control-Allow-Origin')));
```

**Expected Response Headers:**
- `Access-Control-Allow-Origin`: Your origin
- `Access-Control-Allow-Credentials`: `true`
- `Access-Control-Allow-Methods`: Your HTTP method

---

## Logging Configuration

### Serilog Settings

**Required:** ❌ No (has defaults)
**Location:** `appsettings.json` (base) + `appsettings.{Environment}.json` (overrides)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithProperty"
    ],
    "Properties": {
      "Application": "ForgeKit.Api"
    }
  }
}
```

### Log Levels

| Level | When to Use | Example |
|-------|-------------|---------|
| `Verbose` | Extremely detailed logs (rarely used) | Variable values, loop iterations |
| `Debug` | Detailed debugging information | Method entry/exit, query details |
| `Information` | General flow of application | Request received, item created |
| `Warning` | Abnormal or unexpected events | Deprecated API used, retry attempt |
| `Error` | Errors that stop current operation | Database connection failed, validation error |
| `Fatal` | Critical errors requiring immediate attention | Application crash, data corruption |

### Environment-Specific Logging

**Development:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**Production:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Sinks (Output Destinations)

#### Console Sink
```json
{
  "Name": "Console",
  "Args": {
    "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
  }
}
```

#### File Sink
```json
{
  "Name": "File",
  "Args": {
    "path": "logs/log-.txt",
    "rollingInterval": "Day",
    "fileSizeLimitBytes": 10485760,
    "retainedFileCountLimit": 31
  }
}
```

**Parameters:**
- `path` - Log file path (supports date placeholders)
- `rollingInterval` - When to create new file (`Day`, `Hour`, `Minute`)
- `fileSizeLimitBytes` - Max file size (10MB = 10485760)
- `retainedFileCountLimit` - Keep last N files (31 = ~1 month)

#### Seq Sink (Structured Logging Server)
```json
{
  "Name": "Seq",
  "Args": {
    "serverUrl": "http://localhost:5341",
    "apiKey": "YourSeqApiKey"
  }
}
```

#### Application Insights Sink (Azure)
```json
{
  "Name": "ApplicationInsights",
  "Args": {
    "connectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://..."
  }
}
```

### Filtering Third-Party Logs

The application automatically filters third-party license warnings in non-production environments.

**Implemented in:** `ForgeKit.Api/Program.cs:30-36`

To add custom filters:
```csharp
logConfig.Filter.ByExcluding(logEvent =>
    logEvent.MessageTemplate.Text.Contains("unwanted text") &&
    logEvent.Level == Serilog.Events.LogEventLevel.Warning);
```

---

## Email Configuration

**Required:** ⚠️ Only if using email features
**Location:** `appsettings.{Environment}.json` (DO NOT commit passwords!)

```json
{
  "SMTP": {
    "From": "noreply@yourdomain.com",
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": 587
  }
}
```

Set `SMTP:Password` with user-secrets, environment variables, or your deployment secret store.

### Settings Explained

| Setting | Description | Example |
|---------|-------------|---------|
| `From` | Sender email address | `noreply@yourdomain.com` |
| `Password` | Email account password or app password | Secret store only |
| `SmtpHost` | SMTP server hostname | `smtp.office365.com`, `smtp.gmail.com` |
| `SmtpPort` | SMTP port (usually 587 for TLS) | `587` (TLS), `465` (SSL) |

### Provider-Specific Examples

#### Office 365 / Outlook.com
```json
{
  "SMTP": {
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": 587
  }
}
```

#### Gmail
```json
{
  "SMTP": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587
  }
}
```
**Note:** Gmail requires an [App Password](https://support.google.com/accounts/answer/185833)

#### SendGrid
```json
{
  "SMTP": {
    "SmtpHost": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "From": "verified-sender@yourdomain.com"
  }
}
```

### Security Best Practices

✅ **DO:**
- Use **App Passwords** instead of account passwords
- Store SMTP passwords in **environment variables** or **secrets manager**
- Enable **TLS/SSL** (port 587 or 465)
- Use **SPF, DKIM, DMARC** for production email

❌ **DON'T:**
- Commit SMTP passwords to source control
- Use personal email accounts in production
- Send emails without TLS in production

---

## Environment-Specific Settings

### Development (appsettings.Development.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  },
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "Sqlite": "Data Source=./data/forgekit.db"
  },
  "JwtData": {
    "Issuer": "http://localhost:3000",
    "Audience": "http://localhost:3000"
  }
}
```

**Characteristics:**
- ✅ Detailed logging (Debug level)
- ✅ Local SQLite database
- ✅ Localhost URLs
- ✅ Sample/demo endpoints enabled (via ISampleModule)

### Staging (appsettings.Staging.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  },
  "Database": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "Postgres": "Host=staging-db.yourdomain.com;Database=ForgeKitDb_Staging;SSL Mode=Require;Trust Server Certificate=false;"
  },
  "JwtData": {
    "Issuer": "https://auth-staging.yourdomain.com",
    "Audience": "https://api-staging.yourdomain.com"
  }
}
```

**Characteristics:**
- ✅ Production-like logging (Information level)
- ✅ Staging database
- ✅ HTTPS URLs
- ✅ SSL enabled
- ✅ Sample endpoints may be enabled (for testing)

### Production (appsettings.Production.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "<from-env-var>"
        }
      }
    ]
  },
  "AllowedHosts": "api.yourdomain.com"
}
```

**Characteristics:**
- ✅ Minimal logging (Information/Warning)
- ✅ Structured logging to monitoring service
- ✅ Strict host filtering
- ✅ All secrets from environment variables
- ❌ Sample endpoints disabled (ISampleModule filtered out)

**Important:** DO NOT include sensitive data in `appsettings.Production.json`!

---

## Advanced Configuration

### Using Environment Variables

Environment variables override appsettings.json values. Use double underscores (`__`) for nested keys.

**Examples:**

```bash
# Windows PowerShell
$env:Database__Provider="SqlServer"
$env:ConnectionStrings__SqlServer="Server=prod-server;..."
$env:JwtData__Issuer="https://auth.yourdomain.com"
$env:Serilog__MinimumLevel__Default="Warning"

# Linux/macOS Bash
export Database__Provider="Postgres"
export ConnectionStrings__Postgres="Host=prod-server;..."
export JwtData__Issuer="https://auth.yourdomain.com"
export Serilog__MinimumLevel__Default="Warning"

# Docker
docker run -e Database__Provider="Postgres" -e ConnectionStrings__Postgres="..." myapi

# Kubernetes
env:
  - name: Database__Provider
    value: Postgres
  - name: ConnectionStrings__Postgres
    valueFrom:
      secretKeyRef:
        name: db-secret
        key: connection-string
```

### User Secrets (Development Only)

Use **User Secrets** to keep sensitive data out of source control.

**Initialize:**
```bash
cd api/ForgeKit.Api
dotnet user-secrets init
```

**Set secrets:**
```bash
dotnet user-secrets set "Database:Provider" "Postgres"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;..."
dotnet user-secrets set "SMTP:Password" "MyAppPassword123"
```

**List secrets:**
```bash
dotnet user-secrets list
```

**Location:**
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

### Azure Key Vault Integration

For production, use Azure Key Vault for secrets management.

**NuGet Packages:**
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

**Configuration:**
```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

**Key Vault Naming Convention:**
- Replace `:` with `--` in secret names
- Example: `ConnectionStrings:Postgres` -> `ConnectionStrings--Postgres`

### Configuration Validation

**Recommended:** Validate critical settings at startup.

```csharp
// Program.cs
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString(provider);
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string is not configured!");

var jwtIssuer = builder.Configuration["JwtData:Issuer"];
if (string.IsNullOrEmpty(jwtIssuer))
    throw new InvalidOperationException("JWT Issuer is not configured!");
```

---

## Troubleshooting

### Issue: "Cannot open database" error

**Cause:** Connection string is incorrect or database doesn't exist.

**Solution:**
1. Verify connection string in `appsettings.{Environment}.json`
2. For SQLite, ensure `api/ForgeKit.Api/data/` can be created and written
3. For PostgreSQL or SQL Server, test connection with the provider's CLI/client
4. Run migrations from the selected `ForgeKit.Api.Migrations.<Provider>` project as documented above

### Issue: "JWT token validation failed"

**Cause:** Issuer/Audience mismatch or JWKS endpoint unreachable.

**Solution:**
1. Verify `JwtData:Issuer` and `JwtData:Audience` match token claims
2. Test JWKS endpoint: `curl https://your-auth-server/api/auth/jwks`
3. Check network connectivity to auth server
4. Enable debug logging: `Serilog:MinimumLevel:Default = "Debug"`

### Issue: "Logs not appearing"

**Cause:** Log level too high or sink misconfigured.

**Solution:**
1. Lower log level: `"Default": "Debug"`
2. Check sink configuration (file path, permissions)
3. Verify `Enrich: ["FromLogContext"]` is present
4. Test console output first before file/remote sinks

### Issue: "Environment variables not working"

**Cause:** Incorrect naming or not set before application starts.

**Solution:**
1. Use double underscores: `Database__Provider`, `ConnectionStrings__Postgres`, `ConnectionStrings__SqlServer`
2. Set environment variables **before** running application
3. Restart terminal/IDE after setting environment variables
4. Verify with: `echo $env:YourVariable` (PowerShell) or `echo $YourVariable` (Bash)

### Issue: "Sample endpoints visible in production"

**Cause:** Environment not set to "Production".

**Solution:**
1. Set `ASPNETCORE_ENVIRONMENT=Production`
2. Verify with: `echo $env:ASPNETCORE_ENVIRONMENT`
3. ISampleModule endpoints are auto-filtered in Production

### Issue: "Configuration changes not taking effect"

**Cause:** Wrong environment file or caching.

**Solution:**
1. Check which environment is active: `echo $env:ASPNETCORE_ENVIRONMENT`
2. Ensure correct `appsettings.{Environment}.json` exists
3. Restart application (configuration is read at startup)
4. Check for typos in JSON (invalid JSON = ignored file)

---

## Summary

### Configuration Checklist

Before deploying, ensure:

- [ ] Connection strings configured for environment
- [ ] Connection strings NOT committed to source control
- [ ] JWT Issuer and Audience configured
- [ ] JWKS endpoint reachable
- [ ] Logging level appropriate for environment
- [ ] AllowedHosts restricted in production
- [ ] SMTP settings configured (if using email)
- [ ] Environment variables set for secrets
- [ ] SSL/TLS enabled in production
- [ ] User Secrets or Key Vault used for local/production secrets

### Quick Reference by Environment

| Setting | Development | Staging | Production |
|---------|-------------|---------|------------|
| Log Level | Debug | Information | Information/Warning |
| Database | Local | Staging DB | Production DB |
| SSL Required | No | Yes | Yes |
| Sample Endpoints | Enabled | Optional | Disabled |
| Secrets Storage | User Secrets | Key Vault | Key Vault / Env Vars |
| AllowedHosts | `*` | Specific domains | Specific domains |

---

**Version:** 1.0
**Last Updated:** February 2026
**Related Docs:** [USER_GUIDE.md](./USER_GUIDE.md), [EXCEPTION_HANDLING_GUIDE.md](./EXCEPTION_HANDLING_GUIDE.md)
