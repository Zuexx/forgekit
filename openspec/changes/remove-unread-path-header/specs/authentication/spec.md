# authentication Specification

## MODIFIED Requirements

### Requirement: Route Authorization

The system SHALL decide access to every non-API route from the visitor's session and the route's classification, and SHALL NOT add authorization or route-resolution headers to an allowed response.

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

#### Scenario: An allowed request carries no authorization headers

- **WHEN** a request is allowed through
- **THEN** the response SHALL NOT carry headers describing the authorization decision or the resolved route
- **AND** headers added by locale handling or by the framework SHALL be unaffected
