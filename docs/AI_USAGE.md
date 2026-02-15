AI Usage in Binance Automation Framework
Summary
This project was developed using an AI-augmented workflow (Cursor with Claude 3.5 Sonnet and GitHub Copilot).
AI was used for scaffolding, boilerplate, and initial pattern implementation. All architectural decisions, trade-offs, refinements, and validation were performed manually.
The solution was delivered within the 3-hour assessment timebox.

AI-Assisted vs Human Decisions
Architecture & Design (Human-led)
Key architectural decisions made manually:

Defined 3-layer architecture (Client → Services → Reporter)
Selected resilience strategy (Retry + Circuit Breaker via Polly)
Designed unit vs integration test separation
Established dependency injection and configuration structure
Introduced fail-fast configuration validation

AI provided an initial clean structure, which I reviewed and refined for clarity and resilience.
Implementation
AI-assisted scaffolding included:

HttpClient configuration
Polly retry + circuit breaker setup
Service and model structure
Initial test scaffolding
CI workflow draft

Human refinements included:

Adjusting circuit breaker thresholds (3 → 5 failures)
Explicit handling of HTTP 429 (rate limiting)
Structured logging for observability
Graceful degradation strategy for partial failures
Improved test assertions with descriptive failure messages
Runtime conditional skipping for HTTP 451 in CI

Example: Error Handling Enhancement
AI scaffolded success path:
csharpvar avgPrice = await _apiClient.GetAveragePriceAsync(ticker.Symbol);
return CreateResult(ticker, avgPrice.PriceValue, rank);
Refined with resilience:
csharptry
{
    var avgPrice = await _apiClient.GetAveragePriceAsync(ticker.Symbol, cancellationToken);
    _logger.LogDebug("Retrieved average price for {Symbol}: {Price}", ticker.Symbol, avgPrice.Price);
    return CreateResult(ticker, avgPrice.PriceValue, rank);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to retrieve average price for {Symbol}. Using zero as fallback.", ticker.Symbol);
    return CreateResult(ticker, averagePrice: 0, rank);
}
This ensures one failing symbol does not break the entire workflow.

CI Investigation: HTTP 451
Integration tests returned HTTP 451 responses in GitHub-hosted runners.
Investigation steps:

Verified consistent local success
Observed correlation with CI environment
Cross-referenced community reports on RapidAPI geo-restrictions

Solution:

Detect HTTP 451 at runtime
Skip integration test with clear diagnostic message
Preserve full validation locally

This maintains CI transparency while documenting external constraints.

AI Strengths & Required Human Judgment
AI was effective at:

Boilerplate generation (DI setup, configuration classes)
Pattern implementation (retry policies, circuit breakers)
Producing syntactically correct code
Creating comprehensive initial test coverage

Human expertise was required for:

Domain-specific edge cases (filtering zero-change tickers)
Error handling philosophy (graceful degradation vs fail-fast)
Observability decisions (logging levels, structured logging)
Circuit breaker threshold tuning
CI behavior decisions (conditional test skipping)
Production-style trade-offs


Code Review Process
Every AI-generated block was reviewed for:

Correctness - Does it compile and run?
Testability - Can it be unit tested?
Error handling - Does it handle failures appropriately?
Logging clarity - Are diagnostics sufficient?
Performance - Are there unnecessary operations?
Architecture alignment - Does it follow clean architecture principles?

No generated code was accepted without manual review and refinement.

Appendix: Key AI Prompts Used
<details>
<summary>Click to expand prompts</summary>
Initial Architecture
I need to build a C# automation framework for the Binance API via RapidAPI.

Requirements:
- Clean architecture with Client, Services, and Reporter layers
- Dependency injection throughout
- NUnit tests with FluentAssertions
- Both unit tests (mocked) and integration tests (real API)
- Handle API failures gracefully with retry policies
- Output results to console and JSON file

Please create the project structure and initial classes.
Polly Configuration
Add Polly retry policies to the HttpClient configuration:
- Exponential backoff (2^retry seconds)
- Handle 5xx, 408, and 429 status codes
- Add circuit breaker (5 failures, 30s break)
- Log retry attempts and circuit breaker state changes
Test Generation
Generate comprehensive NUnit tests for TopGainersService:
- Use Moq for IBinanceApiClient
- Use FluentAssertions for assertions
- Cover: happy path, null handling, error scenarios, edge cases
- Add test for parallel execution
- Add validation tests (negative count, zero count)
CI/CD Pipeline
Create a GitHub Actions workflow:
- Trigger on push to main/develop
- Run unit tests separately from integration tests
- Use Category attribute to filter tests
- Handle geo-blocking in integration tests (HTTP 451)
- Use secrets for API keys
- Cache NuGet packages
- Upload test results as artifacts
</details>

Conclusion
This project demonstrates an AI-augmented workflow where:

AI accelerates repetitive and pattern-based tasks
Human expertise defines architecture and trade-offs
Combined, they enable quality delivery within tight time constraints

AI was used as a productivity multiplier — not as a replacement for engineering judgment.