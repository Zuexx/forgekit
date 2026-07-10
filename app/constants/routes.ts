
/**
 * The prefix for API routes
 * Routes starting with this prefix are used to handle various backend interactions,  
 * including authentication, data retrieval, and other resource or endpoint operations.
 * @type {string}
 */
const apiPrefix: string = "/api";

/**
 * An array of routes that are accessible to the public
 * These routes do not require authentication
 * @type {string[]}
 */
const publicRoutes: string[] = [
];

/**
 * An array of routes that are used for authentication 
 * @type {string[]}
 */
const authRoutes: string[] = [
    "/sign-in",
    "/sign-up"
];

export { apiPrefix, authRoutes,publicRoutes }