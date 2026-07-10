import { config } from 'dotenv'
import { resolve } from 'path'

// Load environment variables before anything else
config({ path: resolve(process.cwd(), '.env.local') })

import { Kysely, MssqlDialect } from 'kysely'
import * as tarn from 'tarn'
import * as tedious from 'tedious'

import { DB as Database } from "@/types"

// Note: Do not throw on import. Values are read when a connection is created.
const mssqlServer = process.env.MSSQL_SERVER || ''
const mssqlDatabase = process.env.MSSQL_DATABASE || ''
const mssqlUser = process.env.MSSQL_USER || ''
const mssqlPassword = process.env.MSSQL_PASSWORD || ''
const mssqlPort = Number(process.env.MSSQL_PORT ?? '1433')
const mssqlTrustServerCertificate = (process.env.MSSQL_TRUST_SERVER_CERT ?? 'true') === 'true'

const dialect = new MssqlDialect({
    tarn: {
        ...tarn,
        options: {
            min: 0,
            max: 10,
        },
    },
    tedious: {
        ...tedious,
        connectionFactory: () => new tedious.Connection({
            authentication: {
                options: {
                    password: mssqlPassword,
                    userName: mssqlUser,
                },
                type: 'default',
            },
            options: {
                database: mssqlDatabase,
                port: mssqlPort,
                trustServerCertificate: mssqlTrustServerCertificate,
            },
            server: mssqlServer,
        }),
    },
})

// Database interface is passed to Kysely's constructor, and from now on, Kysely 
// knows your database structure.
// Dialect is passed to Kysely's constructor, and from now on, Kysely knows how 
// to communicate with your database.
export const db = new Kysely<Database>({
    dialect,
})
