# authentication Specification

## Purpose
Defines who may reach which route, how a visitor becomes and stays authenticated, and
the configuration boundaries a generated product inherits.

Written after five defects shipped in this area unnoticed — credential authentication
that never reached the database, an authorization path that failed open, and a
registration form that could not submit — none of which had a stated expectation to be
checked against.
## Requirements
### Requirement: Credential Registration

The system SHALL let a visitor create an account with a name, an email address, and a password, and SHALL establish a session for that account on success.

#### Scenario: A valid registration creates an account and signs the visitor in

- **WHEN** a visitor submits a name, a valid email address, and a password of at least eight characters, confirmed
- **THEN** the account SHALL be persisted
- **AND** a session SHALL be established without a second sign-in
- **AND** the visitor SHALL no longer be on the registration route

#### Scenario: The confirmation must match

- **WHEN** the password and its confirmation differ
- **THEN** registration SHALL be refused
- **AND** the error SHALL be reported against the confirmation field

#### Scenario: The same rules apply on both sides of the wire

- **WHEN** a registration is validated by the client schema and by the server schema
- **THEN** both SHALL reach the same verdict for the same input

### Requirement: Credential Sign-In

The system SHALL let a registered visitor sign in with an email address and password, and SHALL refuse any other credentials.

#### Scenario: Correct credentials establish a session

- **WHEN** a registered visitor submits their email address and password
- **THEN** a session SHALL be established
- **AND** the visitor SHALL no longer be on the sign-in route

#### Scenario: A session survives a page reload

- **WHEN** a signed-in visitor reloads the page
- **THEN** the session SHALL still identify the same account

#### Scenario: A wrong password signs nobody in

- **WHEN** a visitor submits a registered email address with the wrong password
- **THEN** no session SHALL be established
- **AND** the visitor SHALL remain on the sign-in route

### Requirement: Authentication Reaches The Database

The system SHALL persist accounts and sessions to the configured database.

#### Scenario: Registration writes rows

- **WHEN** a visitor registers successfully
- **THEN** the account SHALL be readable from the database after the request completes

#### Scenario: The schema can be created from the configuration

- **WHEN** the auth migration is run against an empty database
- **THEN** it SHALL create the auth schema without error

### Requirement: Route Authorization

The system SHALL decide access to every non-API route from the visitor's session and the route's classification.

#### Scenario: A protected route turns an anonymous visitor away

- **WHEN** an anonymous visitor requests a route that is neither public nor an auth route
- **THEN** they SHALL be redirected to sign-in

#### Scenario: An auth route turns a signed-in visitor away

- **WHEN** a signed-in visitor requests sign-in or registration
- **THEN** they SHALL be redirected away from it

#### Scenario: Public routes are open

- **WHEN** an anonymous visitor requests a route classified as public
- **THEN** the request SHALL be allowed

#### Scenario: Redirects keep the visitor's locale

- **WHEN** a visitor on a non-default locale is redirected
- **THEN** the destination SHALL carry that locale prefix
- **AND** a visitor on the default locale SHALL be redirected without one

### Requirement: Authorization Fails Closed

The system SHALL refuse a request whose authorization decision it cannot act on, rather than allowing it.

#### Scenario: A deny decision refuses the request

- **WHEN** the policy returns a deny decision
- **THEN** the request SHALL be refused

#### Scenario: An unusable redirect decision refuses the request

- **WHEN** the policy returns a redirect decision without a destination
- **THEN** the request SHALL be refused rather than allowed through

### Requirement: API Routes Bypass Authorization On A Segment Boundary

The system SHALL exempt API routes from route authorization, matching the API prefix on a path-segment boundary.

#### Scenario: An API route is exempt

- **WHEN** a request targets a path under the API prefix
- **THEN** route authorization SHALL NOT run for it

#### Scenario: A page that merely starts with the prefix is not exempt

- **WHEN** a request targets a page whose path begins with the API prefix without being under it
- **THEN** route authorization SHALL run for it

### Requirement: Session Cookie Name Has One Source

The system SHALL derive the session cookie name used for authorization from the same value that configures it, so the two cannot disagree.

#### Scenario: Renaming the application keeps sessions readable

- **WHEN** the configured cookie prefix changes
- **THEN** session detection SHALL continue to identify signed-in visitors

#### Scenario: Both the plain and secure cookie are recognised

- **WHEN** a session cookie is present under either the plain or the `__Secure-` prefixed name
- **THEN** the visitor SHALL be treated as signed in

### Requirement: A Fork Inherits No Administrators

The system SHALL take administrator membership from configuration and SHALL grant it to nobody by default.

#### Scenario: An unconfigured deployment has no administrators

- **WHEN** the administrator user id setting is empty or unset
- **THEN** no account SHALL hold the administrator role by virtue of configuration

#### Scenario: Administrators are named per deployment

- **WHEN** the setting names one or more user ids
- **THEN** exactly those accounts SHALL hold the administrator role

