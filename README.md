# Binance API Automation Framework

[![CI/CD](https://github.com/habrasia/binance-automation-framework/actions/workflows/ci.yml/badge.svg)](https://github.com/habrasia/binance-automation-framework/actions)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

Test automation framework for external API systems with resilience patterns, parallel processing, and comprehensive failure mode coverage.

**Built for ControlUp QA Automation Engineer Assessment**

---

## Quick Start

**Prerequisites:** .NET 8.0 SDK • [RapidAPI Key](https://rapidapi.com/Glavier/api/binance43)
```bash
# Clone repository
git clone https://github.com/habrasia/binance-automation-framework.git
cd binance-automation-framework/BinanceAutomationFramework

# Configure API key - create this file:
# BinanceApi.Tests.Integration/appsettings.Development.json
```
```json
{
  "BinanceApi": {
    "BaseUrl": "https://binance43.p.rapidapi.com",
    "ApiKey": "YOUR_RAPIDAPI_KEY_HERE",
    "ApiHost": "binance43.p.rapidapi.com",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  }
}
```
```bash
# Run tests
dotnet test

# Local environment:
# Unit Tests: Passed 9/9 (~500ms)
# Integration Tests: Passed 10/10 (~15s, subject to rate limits)

# CI environment (GitHub Actions):
# Unit Tests: Passed 9/9 (~500ms)
# Integration Tests: 10 total - 2 passed, 8 skipped (HTTP 451 geo-blocking)
```

---

## What This Program Does

**Workflow:**
1. Fetch 24-hour price statistics for all cryptocurrency trading pairs
2. Identify top 3 symbols by price change percentage
3. Retrieve average price for each symbol via parallel API calls
4. Output formatted results to console and JSON file

**Console Output:**
```
═══════════════════════════════════════════════════════════════
           TOP CRYPTO GAINERS - LAST 24 HOURS
═══════════════════════════════════════════════════════════════

🏆 RANK #1: HYPERFDUSD
   Price Change:    +242.54%
   Average Price:   0.35350000
   Last Price:      0.35350000
   24h Volume:      1748253.10000000

🏆 RANK #2: RUNEGBP
   Price Change:    +66.84%
   Average Price:   1.28152632

🏆 RANK #3: ADAGBP
   Price Change:    +65.38%
   Average Price:   0.51300000

═══════════════════════════════════════════════════════════════
Report generated at: 2026-02-15 12:09:46 UTC
═══════════════════════════════════════════════════════════════

✅ Results saved to: results/top_gainers_20260215_120958.json
```

**JSON Output:**  
Saved to `results/` directory in test output folder. Example: [`docs/sample-output.json`](docs/sample-output.json)

**Purpose:** Demonstrates automation of external API integrations with focus on resilience patterns and failure handling.

---

## Architecture
```
┌─────────────────────────────────────┐
│ BinanceApi.Client                   │
│ • HTTP communication                │
│ • Polly resilience policies         │
│ • Mockable via interface            │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ BinanceApi.Services                 │
│ • Business logic                    │
│ • Parallel orchestration            │
│ • Error handling                    │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ BinanceApi.Reporter                 │
│ • Console output                    │
│ • JSON output                       │
└─────────────────────────────────────┘
```

**Resilience Patterns:**
- **Exponential backoff retry** - Recovers from transient failures and rate limiting (2s → 4s → 8s progression)
- **Circuit breaker** - Prevents cascading failures after consecutive errors
- **Graceful degradation** - Continues operation with partial results when individual calls fail
- **Parallel execution** - Reduces total response time compared to sequential calls

**Retry Behavior (observable in logs):**
```
[Retry 1/3] Waiting 2s before retry. Reason: TooManyRequests
[Retry 2/3] Waiting 4s before retry. Reason: TooManyRequests
✅ Request succeeded after retry
```

---

## Testing

### Test Strategy

**Unit Tests (9 tests, ~500ms)**
- Validate business logic in isolation
- Mock all external dependencies
- Deterministic and independent of external systems

**Coverage:**
```
✓ Sorting by price change percentage
✓ Rank assignment
✓ Error handling (null responses, API failures)
✓ Input validation
✓ Graceful degradation
```

**Integration Tests (10 tests, ~15s locally)**
- Validate end-to-end behavior against live API
- Real HTTP calls to RapidAPI
- Subject to external API availability

**Coverage:**
```
✓ API orchestration
✓ Retry policy behavior
✓ Circuit breaker pattern
✓ Rate limit handling (HTTP 429)
✓ Geo-blocking detection (HTTP 451)
✓ Parallel processing
✓ JSON contract validation
```

**Test Architecture:**
```
Unit Tests                Integration Tests
────────────              ─────────────────
Mocked dependencies      Live API
No network               Network required
<500ms                   ~15s (local)
Always pass              May skip in CI
```

**Run Tests:**
```bash
dotnet test                              # All tests
dotnet test --filter "Category=Unit"     # Unit only
dotnet test --filter "Category=Integration"  # Integration only
```

---

## CI/CD Pipeline

**Pipeline stages:**
1. Build solution
2. Run unit tests
3. Run integration tests
4. Upload test artifacts

**Integration Test Behavior:**

| Environment | Result | Reason |
|-------------|--------|--------|
| Local | 10/10 passing | Direct network access |
| CI (GitHub Actions) | 10 total: 2 passed, 8 skipped | Geo-blocking (HTTP 451) |

### HTTP 451 Handling

**Problem:**  
RapidAPI returns HTTP 451 (Unavailable for Legal Reasons) when requests originate from GitHub-hosted runners.

**Investigation:**
- Verified tests pass consistently locally
- Confirmed issue isolated to CI environment
- Identified status code 451 via response inspection
- Determined root cause: infrastructure-level geo restrictions
- Cross-referenced public reports ([CCXT #15872](https://github.com/ccxt/ccxt/issues/15872), [#15891](https://github.com/ccxt/ccxt/issues/15891))

**Solution Options:**

| Approach | Implementation | Trade-offs |
|----------|---------------|------------|
| Self-hosted runner | Deploy in non-restricted region | Requires infrastructure, beyond timebox |
| Explicit detection | Catch HTTP 451, skip gracefully | Shows limitation, maintains transparency |

**Implementation:**  
Tests detect HTTP 451 responses and skip with diagnostic messaging. Unit tests remain fully deterministic in CI.

**Rationale:**  
Integration tests remain visible and transparent in CI even when skipped. External infrastructure limitations are handled explicitly. Test behavior is documented and observable.

**Production approach:** Self-hosted runner in permitted region or staged validation strategy.

---

## Configuration

**Configuration sources (priority order):**
1. Environment variables (highest)
2. appsettings.Development.json (local, gitignored)
3. appsettings.json (defaults, committed)

**Local:**
```bash
export BINANCEAPI__APIKEY="your-key"
```

**CI/CD:**  
GitHub Settings → Secrets → Actions → `BINANCE_API_KEY`

---

## Project Structure
```
.github/workflows/ci.yml       # CI/CD automation
BinanceApi.Client/             # HTTP + Polly
BinanceApi.Services/           # Business logic
BinanceApi.Reporter/           # Output formatting
BinanceApi.Tests.Unit/         # 9 unit tests
BinanceApi.Tests.Integration/  # 10 integration tests
docs/
  ├── AI_USAGE.md              # Development methodology
  ├── sample-output.json       # Output example
  └── tests-passing.png        # Local test results
```

---

## Technical Decisions

| Decision | Rationale |
|----------|-----------|
| Polly resilience | Handle transient failures and rate limiting |
| Parallel API calls | Reduce total latency for multiple requests |
| Test categorization | Fast feedback (unit) + real validation (integration) |
| HttpClientFactory | Proper connection lifecycle and Polly integration |
| Interface abstraction | Enable dependency mocking |

---

## Requirements

✓ Automation framework for Binance API  
✓ Top 3 symbols by priceChangePercent  
✓ Average price retrieval  
✓ Consumable output (console + JSON)  
✓ CI/CD pipeline  
✓ Production-grade architecture  
✓ Maintainable design  
✓ Automated tests (19 total)  
✓ Documentation  
✓ 3-hour timebox  

---

## Development Approach

Developed with AI-assisted tooling for scaffolding and acceleration.

All architecture, design trade-offs, validation, and quality decisions were performed manually. Generated code was reviewed and refactored for resilience, clarity, and testability.

**See:** [docs/AI_USAGE.md](docs/AI_USAGE.md) for detailed methodology and examples.

---

## Contact

**Joanna Habrajska**  
📧 asiahabrajska@gmail.com  
[@habrasia](https://github.com/habrasia) • [Repository](https://github.com/habrasia/binance-automation-framework)

**ControlUp QA Automation Engineer Assessment • February 2026**
