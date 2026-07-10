## ADDED Requirements

### Requirement: Environment-Aware CORS
The system SHALL configure CORS policies according to the hosting environment.

#### Scenario: Development CORS allows local frontend access
- **WHEN** the API runs in Development
- **THEN** browser clients from local development origins can call API endpoints without CORS failures
- **AND** standard request methods and headers are allowed

#### Scenario: Production CORS requires configured origins
- **WHEN** the API runs outside Development
- **THEN** only origins configured in `Cors:AllowedOrigins` are allowed
- **AND** startup fails if no production origins are configured

### Requirement: Health Check Endpoints
The system SHALL expose health endpoints for service monitoring and orchestration probes.

#### Scenario: Overall health check reports dependency status
- **WHEN** a client requests `GET /health`
- **THEN** the response includes aggregate health status and registered health check entries
- **AND** unhealthy dependencies produce a service-unavailable response

#### Scenario: Readiness and liveness probes are available
- **WHEN** a client requests `GET /health/ready`
- **THEN** the response reports readiness checks tagged for serving traffic
- **WHEN** a client requests `GET /health/live`
- **THEN** the response reports that the process is alive without requiring dependency checks
