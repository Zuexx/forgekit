import { config } from 'dotenv'
import { resolve } from 'path'

// Load environment variables before anything else
config({ path: resolve(process.cwd(), '.env.local') })

import { Pool } from 'pg'

// PostgreSQL connection pool
const databaseUrl = process.env.DATABASE_URL || ''

export const db = new Pool({
    connectionString: databaseUrl,
})
