# Secure & Observable API Gateway - Implementation Guide

## Overview
This update introduces **Token-Based Rate Limiting**, **Global Concurrency Protection**, **Redis Distributed Support**, and **Structured Logging** to the API Gateway.

## features

### 1. Token-Based Throttling (JWT)
- **Logic**: Throttling is now based on the `SHA256` hash of the JWT `sub` (or other claims), not IP address.
- **Component**: `TokenClientIdResolver` located in `Security/RateLimiting`.
- **Configuration**:
    - If `Authorization: Bearer <token>` is present, it is used.
    - If not, falls back to IP.
    - Claims used: `sub`, `uid`, `jti`.

### 2. Distributed Rate Limiting (Redis)
- **Toggle**: Controlled via `RateLimitOptions:UseRedis` in `appsettings.json`.
- **Default Check**: `false` (In-Memory) for easy local dev.
- **Production**: Set `"UseRedis": true` and provide `ConnectionStrings:Redis` to enable multi-instance throttling.

### 3. Resilience (Concurrency Limiter & Polly)
- **Global Protection**: `ConcurrencyLimiter` middleware rejects requests with `503` if active requests > 1000 (configurable).
- **Polly Bulkhead**: Ocelot pipeline is protected with `BulkheadPolicy` (Queue=0) to fail-fast.

### 4. Structured Observability
- **Middleware**: `GatewayLoggingMiddleware` captures high-cardinality data.
- **Logs**: Enriched with `TokenHash`, `CorrelationId`, `Service`, `Environment`, `IsThrottled`.
- **Format**: Using `Serilog` with JSON formatter (configured in `Program.cs` via `UseSerilogRequestLogging`).

## How to Verify

### 1. Configuration
Check `src/Gateway/ApiGatewayKit.Gateway/appsettings.json`:
```json
"RateLimitOptions": { "UseRedis": false },
"ConnectionStrings": { "Redis": "localhost:6379" }
```

### 2. Run k6 Load Test
A `k6` script is provided at `tests/gateway-load-test.js`.
```bash
k6 run tests/gateway-load-test.js
```
Expected output:
- **Token Flood**: Should show `throttled_requests` increasing.
- **Concurrency Storm**: Should show `bulkhead_rejected` increasing if limit breached.

## Critical Files
- `Program.cs`: Service registration & Pipeline.
- `Security/RateLimiting/TokenClientIdResolver.cs`: Logic for ID extraction.
- `Middleware/GatewayLoggingMiddleware.cs`: Observability logic.
