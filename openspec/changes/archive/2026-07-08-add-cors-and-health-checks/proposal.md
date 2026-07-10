# Proposal: Add CORS and Health Check Support

**Status**: PROPOSED  
**Created**: 2026-02-11  
**Author**: Development Team  
**Priority**: P0 (Critical for Production)

---

## 1. Problem Statement

### Current State

Scaffold-API currently lacks two critical production-ready features:

1. **No CORS Configuration**
   - Frontend applications cannot call the API from different origins
   - Browser blocks cross-origin requests with CORS policy errors
   - Prevents frontend-backend separated architecture deployment

2. **No Health Check Endpoints**
   - Load balancers cannot determine service health status
   - Kubernetes liveness/readiness probes cannot function
   - No automated health monitoring capability
   - No visibility into database connectivity status

### Impact

**CORS Missing**:
- ❌ Frontend development blocked (local dev: `http://localhost:3000` → `https://localhost:5001`)
- ❌ Cannot deploy to production with separate frontend domain
- ❌ Mobile apps cannot call API
- ❌ Third-party integrations impossible

**Health Checks Missing**:
- ❌ Cannot deploy to Kubernetes/Docker Swarm
- ❌ Load balancers cannot route traffic properly
- ❌ No automated failure detection and recovery
- ❌ Monitoring systems blind to service health

### Why Now?

This is a **blocking issue** for production deployment. Without these features:
- The API cannot be consumed by web/mobile frontends
- The API cannot be deployed to modern container orchestration platforms
- No automated health monitoring or auto-healing

---

## 2. Proposed Solution

### 2.1 CORS Configuration

Add comprehensive CORS support with:
- **Environment-aware policies** (Development vs Production)
- **Configuration-driven allowed origins** (from appsettings.json)
- **Security-first approach** (strict in production, relaxed in development)
- **Credentials support** (allow JWT tokens and cookies)

### 2.2 Health Check Endpoints

Implement three health check endpoints following industry standards:

1. **`/health`** - Overall health (database + dependencies)
2. **`/health/ready`** - Readiness probe (can serve traffic?)
3. **`/health/live`** - Liveness probe (is process alive?)

---

## 3. Success Criteria

### CORS Success Criteria

- [ ] Frontend running on `http://localhost:3000` can call API on `https://localhost:5001`
- [ ] Production environment only allows configured origins
- [ ] Browser console shows no CORS errors
- [ ] Preflight requests (OPTIONS) handled correctly
- [ ] Authorization headers allowed through CORS
- [ ] Integration tests verify CORS policies

### Health Check Success Criteria

- [ ] `/health` returns 200 OK when database is healthy
- [ ] `/health` returns 503 Unhealthy when database is down
- [ ] `/health/ready` suitable for Kubernetes readiness probe
- [ ] `/health/live` suitable for Kubernetes liveness probe
- [ ] Response format matches ASP.NET Core Health Check JSON schema
- [ ] Load balancer can use health checks for routing decisions
- [ ] Integration tests verify all health check endpoints

---

## 4. Implementation Estimate

**Total Time**: 3.5 hours

- CORS Implementation: 1 hour
- Health Checks Implementation: 1 hour
- Testing: 1 hour
- Documentation: 30 minutes

---

## 5. References

- [ASP.NET Core CORS Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/cors)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Kubernetes Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
