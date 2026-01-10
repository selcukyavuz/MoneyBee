# MoneyBee Test Infrastructure

## Overview
This directory contains comprehensive unit and integration tests for the MoneyBee microservices platform.

## Test Projects

### 1. MoneyBee.Auth.Service.UnitTests
**Status:** ✅ **29 passing tests** (+10 cache tests)

Tests for authentication, API key management, and Redis caching.

**Test Coverage:**
- `ApiKeyHelperTests`: 14 tests
- `ApiKeyCacheServiceTests`: 10 tests (NEW)
  - API key generation (35-char format with `mb_` prefix)
  - SHA256 hashing (Base64 output)
  - Key masking for secure display
  - Format validation
  - Uniqueness guarantees

**Key Test Scenarios:**
```csharp
// API Key Format: "mb_" + 32 chars = 35 total
✅ Generate unique API keys with mb_ prefix
✅ Hash keys using SHA256 (44-char Base64)
✅ Mask keys: "mb_****...****last4"
✅ Validate key format
```

**Run Tests:**
```bash
dotnet test tests/MoneyBee.Auth.Service.UnitTests
```

---

### 2. MoneyBee.Customer.Service.UnitTests
**Status:** ✅ **16 passing tests**

Tests for customer validation logic, specifically Turkish National ID validation.

**Test Coverage:**
- `NationalIdValidatorTests`: 16 tests
  - TC Kimlik No validation algorithm
  - Normalization (removing spaces, dashes)
  - Edge cases (empty, null, invalid checksums)

**Validation Algorithm:**
```
Turkish National ID (11 digits):
1. First digit ≠ 0
2. Sum of first 10 digits % 10 = 11th digit
3. (Odd positions × 7 - Even positions) % 10 = 10th digit
```

**Valid Test IDs:**
- `10000000146` ✅
- `11111111110` ✅

**Run Tests:**
```bash
dotnet test tests/MoneyBee.Customer.Service.UnitTests
```

---

### 3. MoneyBee.Transfer.Service.UnitTests
**Status:** ✅ **18 passing tests**

Tests for transfer business logic and domain rules.

**Test Coverage:**
- `TransferDomainServiceTests`: 18 tests
  - Approval wait logic (amounts > 1000 TRY)
  - Transfer validation
  - Daily limit enforcement
  - Risk level assessment

**Business Rules Tested:**
```csharp
✅ High amount transfers (>1000 TRY) require 5-minute approval wait
✅ Transfer cannot be completed if status ≠ Pending
✅ Receiver identity must match
✅ Daily limit enforcement with remaining balance calculation
✅ High-risk transfers should be rejected
```

**Run Tests:**
```bash
dotnet test tests/MoneyBee.Transfer.Service.UnitTests
```

---

### 4. MoneyBee.IntegrationTests
**Status:** ⚠️ **Not Yet Implemented**

Planned integration tests for API endpoints using WebApplicationFactory.

**Planned Coverage:**
- Auth API endpoints (POST /api/apikeys, GET /api/apikeys, etc.)
- Customer API endpoints
- Transfer API endpoints
- End-to-end workflows

---

## Test Frameworks & Libraries

```xml
<PackageReference Include="xunit" Version="3.1.4" />
<PackageReference Include="FluentAssertions" Version="8.8.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.1" />
```

### Why These Tools?

**xUnit**
- Industry-standard for .NET testing
- Excellent VS Code/Rider integration
- Parallel test execution by default

**FluentAssertions**
- Readable, English-like assertions
- Better error messages
- Extensive assertion library

**Moq**
- Most popular mocking framework
- Simple, intuitive API
- Perfect for dependency injection testing

---

## Running All Tests

```bash
# Run all unit tests
cd /Users/selcukyavuz/repos/MoneyBee
dotnet test MoneyBee.sln --filter "FullyQualifiedName~UnitTests"

# With detailed output
dotnet test MoneyBee.sln --verbosity normal

# With coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Structure

```
tests/
├── MoneyBee.Auth.Service.UnitTests/
│   ├── Helpers/
│   │   └── ApiKeyHelperTests.cs          # 14 tests
│   └── Infrastructure/
│       └── Caching/
│           └── ApiKeyCacheServiceTests.cs    # 10 tests
├── MoneyBee.Customer.Service.UnitTests/
│   └── Helpers/
│       └── NationalIdValidatorTests.cs   # 16 tests
├── MoneyBee.Transfer.Service.UnitTests/
│   └── Domain/
│       └── Services/
│           └── TransferDomainServiceTests.cs  # 18 tests
└── MoneyBee.IntegrationTests/
    └── (planned)
```

---

## Test Naming Convention

```
MethodName_Scenario_ExpectedBehavior

Examples:
✅ GenerateApiKey_ShouldReturn35CharacterKeyWithPrefix
✅ IsValid_WithValidTurkishNationalId_ShouldReturnTrue
✅ RequiresApprovalWait_WithHighAmount_ShouldReturnTrue
```

---

## Writing New Tests

### Unit Test Template:

```csharp
public class MyServiceTests
{
    private readonly MyService _service;

    public MyServiceTests()
    {
        _service = new MyService();
    }

    [Fact]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        var input = "test-data";

        // Act
        var result = _service.DoSomething(input);

        // Assert
        result.Should().Be("expected-output");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void MethodName_WithVariousInputs_ShouldReturnCorrectResult(
        int input, bool expected)
    {
        // Act
        var result = _service.Check(input);

        // Assert
        result.Should().Be(expected);
    }
}
```

---

## Current Test Statistics

| Service | Tests | Status |
|---------|-------|--------|
| Auth | 29 | ✅ Passing |
| Customer | 16 | ✅ Passing |
| Transfer | 18 | ✅ Passing |
| Integration | 0 | ⚠️ Planned |
| **Total** | **63** | **✅ 100%** |

---

## Next Steps

1. ✅ Complete unit tests for all services (DONE)
2. ⚠️ Add integration tests with WebApplicationFactory
3. ⚠️ Implement test coverage reporting
4. ⚠️ Add repository layer tests with in-memory database
5. ⚠️ Add domain event handler tests
6. ⚠️ CI/CD pipeline integration

---

## Notes

- All tests use AAA pattern (Arrange-Act-Assert)
- FluentAssertions provides clear, readable assertions
- Tests target .NET 10.0 (test projects) with .NET 8.0 services
- No mocks needed for pure domain logic tests
- Future: Add code coverage with `coverlet.collector`

---

## Troubleshooting

**Issue:** EF Core version conflicts
**Solution:** Warnings are non-critical, tests run successfully

**Issue:** Fluent Assertions license warning
**Solution:** Library is free for non-commercial use, warning can be ignored

**Issue:** Test project not found
**Solution:** Ensure project is added to solution:
```bash
dotnet sln add tests/YourProject/YourProject.csproj
```
