# Concrete Roadmap — Banking App Team B

> Every item below names the **exact file**, **exact line(s)**, and **exactly what to write**.  
> Work through sections in order; each batch unlocks the next.

---

## BATCH 1 — A2: Code Readability & Style (abbreviations, magic numbers, dead code)

Start here because these are mechanical, self-contained changes that don't break anything and make the rest easier.

---

### A2.1 — Lambda parameters must not be `_`, `p`, `param`, `s`, `e` etc.

#### `TransferViewModel.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 25 | `NextStepCommand = new RelayCommand(_ => ExecuteNextStep());` | `NextStepCommand = new RelayCommand(unusedParameter => ExecuteNextStep());` |
| 26 | `TransferCommand = new AsyncRelayCommand(_ => ExecuteTransferAsync());` | `TransferCommand = new AsyncRelayCommand(unusedParameter => ExecuteTransferAsync());` |
| 27 | `CancelCommand = new RelayCommand(_ => ExecuteCancel());` | `CancelCommand = new RelayCommand(unusedParameter => ExecuteCancel());` |
| 28 | `SendAgainCommand = new RelayCommand(_ => ExecuteSendAgain());` | `SendAgainCommand = new RelayCommand(unusedParameter => ExecuteSendAgain());` |
| 284 | `var result = await Task.Run(() => transferService.ExecuteTransfer(dto));` | *(no lambda param here — ok)* |

#### `BeneficiariesViewModel.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 87 | `AddCommand = new AsyncRelayCommand(_ => AddBeneficiaryAsync());` | `AddCommand = new AsyncRelayCommand(unusedParameter => AddBeneficiaryAsync());` |
| 88 | `DeleteCommand = new RelayCommand(p => DeleteBeneficiary(p as Beneficiary));` | `DeleteCommand = new RelayCommand(commandParameter => DeleteBeneficiary(commandParameter as Beneficiary));` |
| 89 | `ShowAddFormCommand = new RelayCommand(_ => ShowAddForm());` | `ShowAddFormCommand = new RelayCommand(unusedParameter => ShowAddForm());` |
| 90 | `UseForTransferCommand = new RelayCommand(p => UseForTransfer(p as Beneficiary));` | `UseForTransferCommand = new RelayCommand(commandParameter => UseForTransfer(commandParameter as Beneficiary));` |
| 95 | `var data = beneficiaryService.GetByUser(currentUserId);` / `foreach (var item in data)` — rename `item` → `beneficiary` | Change loop: `foreach (var beneficiary in data) Beneficiaries.Add(beneficiary);` |

#### `FXViewModel.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 187 | `LoadRatesCommand = new AsyncRelayCommand(LoadRatesAsync);` | *(method reference — ok, no lambda param)* |
| 188 | `LockRateCommand = new RelayCommand(LockRate);` | *(method reference — ok)* |
| 190 | `ExecuteExchangeCommand = new AsyncRelayCommand(ExecuteExchanges);` | *(method reference — ok)* |
| 191 | `CancelCommand = new RelayCommand(Cancel);` | *(method reference — ok)* |
| 192 | `NewExchangeCommand = new RelayCommand(Reset);` | *(method reference — ok)* |
| 198 | `private void Cancel(object? _)` | `private void Cancel(object? unusedParameter)` |
| 204 | `private void Reset(object? _)` | `private void Reset(object? unusedParameter)` |
| 233 | `private Task ExecuteExchanges(object? _)` | `private Task ExecuteExchanges(object? unusedParameter)` |
| 290 | `foreach (var account in UserSession.GetAccounts()) Accounts.Add(account);` | *(ok — `account` is descriptive)* |
| 297 | `private Task LoadRatesAsync(object? _)` | `private Task LoadRatesAsync(object? unusedParameter)` |
| 379 | `_timer.Tick += (s, e) =>` | `_timer.Tick += (timerSender, timerEventArgs) =>` |

#### `RateAlertViewModel.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 84 | `RefreshRatesCommand = new AsyncRelayCommand(_ => LoadRatesAsync());` | `RefreshRatesCommand = new AsyncRelayCommand(unusedParameter => LoadRatesAsync());` |
| 85 | `CreateAlertCommand = new AsyncRelayCommand(_ => CreateAlertAsync());` | `CreateAlertCommand = new AsyncRelayCommand(unusedParameter => CreateAlertAsync());` |
| 86 | `DeleteAlertCommand = new RelayCommand(param => DeleteAlert((RateAlert)param));` | `DeleteAlertCommand = new RelayCommand(commandParameter => DeleteAlert((RateAlert)commandParameter));` |
| 91 | `var alerts = await Task.Run(() => _exchangeService.GetUserAlerts(_userId));` | *(ok — no param)* |
| 99 | `var rates = await Task.Run(() => _exchangeService.GetLiveRates());` | *(ok)* |
| 104 | `.SelectMany(pair => pair.Split('/'))` | `.SelectMany(currencyPair => currencyPair.Split('/'))` |
| 107 | `.OrderBy(c => c)` | `.OrderBy(currencyCode => currencyCode)` |
| 109 | `foreach (var currency in currencies) AvailableCurrencies.Add(currency);` | *(ok — `currency` is descriptive)* |
| 161 | `var newAlert = await Task.Run(() =>` | *(ok — no param)* |
| 177 | `private void DeleteAlert(object param)` — rename parameter | `private void DeleteAlert(object commandParameter)` then `var alert = (RateAlert)commandParameter;` |

#### `BillPayViewModel.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 73 | `SearchCommand = new RelayCommand(_ => ExecuteSearch());` | `SearchCommand = new RelayCommand(unusedParameter => ExecuteSearch());` |
| 75 | `NextStepCommand = new RelayCommand(_ => ExecuteNextStep());` | `NextStepCommand = new RelayCommand(unusedParameter => ExecuteNextStep());` |
| 76 | `BackCommand = new RelayCommand(_ => ExecuteBack());` | `BackCommand = new RelayCommand(unusedParameter => ExecuteBack());` |
| 77 | `PayAnotherBillCommand = new RelayCommand(_ => ResetForm());` | `PayAnotherBillCommand = new RelayCommand(unusedParameter => ResetForm());` |
| 78 | `PayBillCommand = new AsyncRelayCommand(_ => ExecutePayBillAsync());` | `PayBillCommand = new AsyncRelayCommand(unusedParameter => ExecutePayBillAsync());` |
| 79 | `CancelCommand = new RelayCommand(_ => NavigationService.NavigateTo<TransferPage>());` | `CancelCommand = new RelayCommand(unusedParameter => NavigationService.NavigateTo<TransferPage>());` |
| 437 | `var alreadySaved = SavedBillers.Any(sb =>` | `var alreadySaved = SavedBillers.Any(savedBillerEntry =>` then rename `sb` to `savedBillerEntry` inside |
| 502 | `var matchingSavedBiller = SavedBillers.FirstOrDefault(sb => sb.BillerId == SelectedBiller.Id);` | `var matchingSavedBiller = SavedBillers.FirstOrDefault(savedBillerEntry => savedBillerEntry.BillerId == SelectedBiller.Id);` |

#### `RecurringPaymentViewModel.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 60 | `CreateCommand = new AsyncRelayCommand(_ => ExecuteCreateAsync());` | `CreateCommand = new AsyncRelayCommand(unusedParameter => ExecuteCreateAsync());` |
| 169–170 | `var payments = await Task.Run(() =>` / `_recurringPaymentService.GetByUser(UserSession.CurrentUserId));` | *(ok — no lambda param)* |
| 172–173 | `var billers = await Task.Run(() =>` / `ServiceLocator.BillPaymentService...` | *(ok)* |
| 272 | `var existing = Payments.FirstOrDefault(p => p.Id == payment.Id);` | `var existing = Payments.FirstOrDefault(recurringPayment => recurringPayment.Id == payment.Id);` |
| 300 | `var existing = Payments.FirstOrDefault(p => p.Id == payment.Id);` | `var existing = Payments.FirstOrDefault(recurringPayment => recurringPayment.Id == payment.Id);` |
| 328 | `var existing = Payments.FirstOrDefault(p => p.Id == payment.Id);` | `var existing = Payments.FirstOrDefault(recurringPayment => recurringPayment.Id == payment.Id);` |

#### `TransferService.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 68 | `var match = beneficiaries.Find(b => b.IBAN == dto.RecipientIBAN);` | `var match = beneficiaries.Find(beneficiary => beneficiary.IBAN == dto.RecipientIBAN);` |

#### `ExchangeService.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 46 | `List<string> keys = new List<string>(rates.Keys);` | *(ok)* |
| 47 | `foreach (string pair in keys)` | *(ok — `pair` is descriptive enough but `currencyPair` is better)* → `foreach (string currencyPair in keys)` then rename `parts` to `currencyComponents` on line 49 |
| 49 | `string[] parts = pair.Split('/');` | `string[] currencyComponents = currencyPair.Split('/');` |
| 50 | `string inverseKey = $"{parts[1]}/{parts[0]}";` | `string inverseKey = $"{currencyComponents[1]}/{currencyComponents[0]}";` |
| 194 | `foreach (var alert in activeAlerts)` | *(ok)* |

#### `NotificationService.cs`
| Line | Current | Replace with |
|------|---------|--------------|
| 65 | `foreach (var payment in payments)` | *(ok)* |
| 78 | `foreach (var alert in alerts)` | *(ok)* |

---

### A2.2 — Magic Numbers → Named Constants

#### `TransferService.cs` — line 84: `if (iban.Length < 15 || iban.Length > 34)`
Add constants at the top of the class:
```csharp
private const int MinimumIbanLength = 15;
private const int MaximumIbanLength = 34;
```
Then change line 84 to:
```csharp
if (iban.Length < MinimumIbanLength || iban.Length > MaximumIbanLength) return false;
```

#### `TransferService.cs` — line 136: `return amount >= 1000;`
Add constant:
```csharp
private const decimal TwoFaAmountThreshold = 1000m;
```
Then change line 136 to:
```csharp
return amount >= TwoFaAmountThreshold;
```

#### `BillPaymentService.cs` — line 22: `return amount <= 100 ? 0.50m : 1.00m;`
Add constants:
```csharp
private const decimal SmallPaymentThreshold = 100m;
private const decimal SmallPaymentFee = 0.50m;
private const decimal StandardPaymentFee = 1.00m;
```
Then change line 22 to:
```csharp
return amount <= SmallPaymentThreshold ? SmallPaymentFee : StandardPaymentFee;
```

#### `BillPaymentService.cs` — line 69: `return amount >= 1000;`
Add constant (or reuse from a shared location):
```csharp
private const decimal TwoFaAmountThreshold = 1000m;
```
Change line 69 to:
```csharp
return amount >= TwoFaAmountThreshold;
```

#### `TransactionPipelineService.cs` — line 47: `if (ctx.Amount >= 1000 ...`
Add constant:
```csharp
private const decimal TwoFaAmountThreshold = 1000m;
```
Change line 47 to:
```csharp
if (ctx.Amount >= TwoFaAmountThreshold && string.IsNullOrWhiteSpace(twoFAToken))
```

#### `TransactionPipelineService.cs` — line 35: `if (ctx.Currency == null || ctx.Currency.Length != 3)`
Add constant:
```csharp
private const int ExpectedCurrencyCodeLength = 3;
```
Change line 35 to:
```csharp
if (ctx.Currency == null || ctx.Currency.Length != ExpectedCurrencyCodeLength)
```

#### `ExchangeService.cs` — line 21: `private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);`
*(Already a named constant — OK.)*

#### `ExchangeService.cs` — line 97–99: `decimal percentage = amount * 0.005m; return Math.Max(0.50m, percentage);`
Add constants:
```csharp
private const decimal CommissionRate = 0.005m;
private const decimal MinimumCommission = 0.50m;
```
Change lines 97–99 to:
```csharp
decimal commissionAmount = amount * CommissionRate;
return Math.Max(MinimumCommission, commissionAmount);
```

#### `ExchangeService.cs` — lines 39–44 (hardcoded FX rates dictionary):
Extract to a private static factory or configuration; at minimum name the constant block:
```csharp
// TODO: Replace hardcoded seed rates with live API call (FR-FX-001)
private static Dictionary<string, decimal> GetSeedExchangeRates() => new()
{
    { "EUR/USD", 1.15m },
    { "EUR/GBP", 0.86m },
    { "EUR/RON", 5.09m },
    { "USD/RON", 4.41m },
    { "GBP/RON", 5.90m }
};
```
Then in `GetLiveRates()` replace the inline dictionary with:
```csharp
Dictionary<string, decimal> rates = GetSeedExchangeRates();
```

#### `TwoFAService.cs` — line 29: `string placeholder = "123456";`
Change to:
```csharp
const string placeholderToken = "123456";
Debug.WriteLine($"[2FA] Placeholder token generated for userId={userId}: {placeholderToken}");
return placeholderToken;
```

#### `NotificationService.cs` — line 95: receipt string in `BillPaymentService.cs` line 95
```csharp
string receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
```
Extract to a private method in `BillPaymentService`:
```csharp
private static string GenerateReceiptNumber()
{
    const int receiptSuffixLength = 6;
    string uniqueSuffix = Guid.NewGuid().ToString("N")[..receiptSuffixLength].ToUpper();
    return $"RCP-{DateTime.UtcNow:yyyyMMdd}-{uniqueSuffix}";
}
```
Then on line 95 replace with:
```csharp
string receiptNumber = GenerateReceiptNumber();
```

#### `BillPayViewModel.cs` — line 220 (broken `ErrorMessageVisibility` expression):
```csharp
// CURRENT (line 220) — has a tautological bug:
public Visibility ErrorMessageVisibility =>
    string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Collapsed == Visibility.Visible ? Visibility.Visible : Visibility.Visible;
// REPLACE WITH:
public Visibility ErrorMessageVisibility =>
    string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
```

---

### A2.4 — Remove Dead Code (Commented-Out Blocks)

#### `BillPayViewModel.cs` — **lines 514–932**: entire file below line 513 is a commented-out duplicate of the class.
**Delete lines 514–932 in their entirety.**

#### `TransactionPipelineService.cs`
| Lines | Action |
|-------|--------|
| 4 | `using BankingAppTeamB.Mocks;` is duplicated on lines 1, 4, 9 — remove duplicates (keep only line 1) |
| 25–28 | Remove commented-out old constructor |
| 39 | Remove `// if (!AccountService.IsAccountValid(ctx.SourceAccountId))` |
| 59 | Remove `// AccountService.DebitAccount(ctx.SourceAccountId, totalDebit);` |
| 100–101 | Remove `// BalanceAfter = AccountService.GetBalance(ctx.SourceAccountId),` |

#### `TransferService.cs`
| Lines | Action |
|-------|--------|
| 66–76 | Remove `// newly added` and `// end of newly added` comment markers (keep the code, remove the markers) |

#### `NotificationService.cs`
| Lines | Action |
|-------|--------|
| 20–22 | Remove `// TODO: replace with real email/toast delivery` and the two commented-out `EmailService`/`ToastService` lines |
| 35–36, 47–48 | Remove duplicate `// TODO: replace` comment stubs |

#### `RateAlertsPage.xaml.cs`
| Lines | Action |
|-------|--------|
| 19–20 | Remove `// To learn more about WinUI...` boilerplate comment |
| 11–16 | Remove unused `using` imports: `Windows.Foundation`, `Windows.Foundation.Collections`, `System.Runtime.InteropServices.WindowsRuntime`, `System.IO`, `System.Linq`, `System.Collections.Generic` |

---

## BATCH 2 — A3: Business Logic Out of GUI + A4: Interface Decoupling

---

### A3.1 & A4.3 — `BeneficiariesPage.xaml.cs` line 17: directly newing up concrete classes

**Problem:** `new BeneficiariesViewModel(new BeneficiaryService(new BeneficiaryRepository()))` — the View is constructing the full dependency graph.

**Fix:**
1. Add `BeneficiaryService` to `ServiceLocator.cs` (currently missing from the public surface):
   - Add after line 27 in `ServiceLocator.cs`:
     ```csharp
     private static BeneficiaryService _beneficiaryService = new BeneficiaryService(_beneficiaryRepository);
     ```
   - Add public accessor after line 40:
     ```csharp
     public static BeneficiaryService BeneficiaryService => _beneficiaryService;
     ```
2. Change `BeneficiariesPage.xaml.cs` line 17 from:
   ```csharp
   DataContext = new BeneficiariesViewModel(new BeneficiaryService(new BeneficiaryRepository()));
   ```
   to:
   ```csharp
   DataContext = new BeneficiariesViewModel(ServiceLocator.BeneficiaryService);
   ```

### A3.1 — `RecurringPaymentsPage.xaml.cs` — `UpdateErrorVisibility()` is display logic that belongs in the ViewModel

**Problem:** Lines 94–106 — `UpdateErrorVisibility()` reads `_viewModel.ErrorMessage` and manually sets `ErrorMessageTextBlock.Visibility` and `.Text`. This is display logic in the View.

**Fix:**
1. In `RecurringPaymentViewModel.cs`, `HasError` property already exists at line 120. Add a `ErrorMessageVisibility` computed property:
   ```csharp
   // Add after line 120 in RecurringPaymentViewModel.cs
   public Microsoft.UI.Xaml.Visibility ErrorMessageVisibility =>
       HasError ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
   ```
   Also fire `OnPropertyChanged(nameof(ErrorMessageVisibility));` inside the `ErrorMessage` setter (line 113) after `OnPropertyChanged(nameof(HasError));`.

2. In `RecurringPaymentsPage.xaml.cs`:
   - Delete the entire `UpdateErrorVisibility()` method (lines 94–106).
   - Remove all 9 calls to `UpdateErrorVisibility()` (lines 25, 32, 38, 48, 58, 64, 72, 81, 90).
   - Bind `ErrorMessageTextBlock.Visibility` and `ErrorMessageTextBlock.Text` directly in XAML to `{x:Bind _viewModel.ErrorMessageVisibility}` and `{x:Bind _viewModel.ErrorMessage, Mode=OneWay}`.

### A3.2 — `BillPayPage.xaml.cs` lines 58–83: `AmountBox_ValueChanged` and `ViewModel_PropertyChanged` do conversion

**Problem:** Converting `double` ↔ `decimal` between `NumberBox` and ViewModel is a UI-layer concern mixed with data conversion.

**Fix:**
- The `AmountBox_ValueChanged` (lines 58–68) converts `sender.Value` (double) to decimal and assigns `ViewModel.Amount`. This is acceptable UI-to-VM mapping.
- However, `ViewModel_PropertyChanged` (lines 70–84) listens to property changes and pushes back into the control's `.Value`. This creates a feedback loop managed in View code.
- **Move to XAML binding instead:** Remove `ViewModel_PropertyChanged` (lines 70–84) and the subscription on line 20 (`ViewModel.PropertyChanged += ViewModel_PropertyChanged`). Use `{x:Bind ViewModel.Amount, ...}` on `AmountBox.Value` with a converter, or better yet expose `AmountDouble` in the ViewModel:
  ```csharp
  // Add to BillPayViewModel.cs after the Amount property
  public double AmountAsDouble
  {
      get => (double)_amount;
      set => Amount = (decimal)value;
  }
  ```
  Then bind `NumberBox.Value` to `{x:Bind ViewModel.AmountAsDouble, Mode=TwoWay}` and remove both event handlers from `BillPayPage.xaml.cs`.

### A3.3 — ViewModels must not reference concrete Service or Repository types

#### `TransferViewModel.cs` — line 13 and 15: uses `TransferService` concrete type
```csharp
// Line 13 — CURRENT:
private readonly TransferService transferService;
// REPLACE WITH:
private readonly ITransferService transferService;

// Line 15 — CURRENT:
public TransferViewModel(TransferService transferService)
// REPLACE WITH:
public TransferViewModel(ITransferService transferService)
```

#### `BeneficiariesViewModel.cs` — line 18 and 82: uses `BeneficiaryService` concrete type
```csharp
// Line 18 — CURRENT:
private readonly BeneficiaryService beneficiaryService;
// REPLACE WITH:
private readonly IBeneficiaryService beneficiaryService;

// Line 82 — CURRENT:
public BeneficiariesViewModel(BeneficiaryService beneficiaryService)
// REPLACE WITH:
public BeneficiariesViewModel(IBeneficiaryService beneficiaryService)
```

#### `FXViewModel.cs` — line 16 and 183: uses `ExchangeService` concrete type
```csharp
// Line 16 — CURRENT:
private readonly ExchangeService _exchangeService;
// REPLACE WITH:
private readonly IExchangeService _exchangeService;

// Line 183 — CURRENT:
public FXViewModel(ExchangeService exchangeService)
// REPLACE WITH:
public FXViewModel(IExchangeService exchangeService)
```

#### `RateAlertViewModel.cs` — line 15 and 79: uses `ExchangeService` concrete type
```csharp
// Line 15 — CURRENT:
private readonly ExchangeService _exchangeService;
// REPLACE WITH:
private readonly IExchangeService _exchangeService;

// Line 79 — CURRENT:
public RateAlertViewModel(ExchangeService exchangeService, int userId)
// REPLACE WITH:
public RateAlertViewModel(IExchangeService exchangeService, int userId)
```

#### `BillPayViewModel.cs` — line 18 and 64: uses `BillPaymentService` concrete type
```csharp
// Line 18 — CURRENT:
private readonly BillPaymentService _billPaymentService;
// REPLACE WITH:
private readonly IBillPaymentService _billPaymentService;

// Line 64 — CURRENT:
public BillPayViewModel(BillPaymentService billPaymentService)
// REPLACE WITH:
public BillPayViewModel(IBillPaymentService billPaymentService)
```

#### `RecurringPaymentViewModel.cs` — lines 17 and 36: uses `RecurringPaymentService` concrete type
```csharp
// Line 17 — CURRENT:
private readonly RecurringPaymentService _recurringPaymentService;
// REPLACE WITH:
private readonly IRecurringPaymentService _recurringPaymentService;

// Line 36 — CURRENT:
public RecurringPaymentViewModel(RecurringPaymentService recurringPaymentService)
// REPLACE WITH:
public RecurringPaymentViewModel(IRecurringPaymentService recurringPaymentService)
```

#### `RecurringPaymentViewModel.cs` — line 173: calls `ServiceLocator` directly from ViewModel
```csharp
// Line 172–173 — CURRENT:
var billers = await Task.Run(() =>
    ServiceLocator.BillPaymentService.GetBillerDirectory(null));
```
**Fix:** Inject `IBillPaymentService` as a second constructor parameter:
```csharp
// Updated constructor signature (line 36):
public RecurringPaymentViewModel(IRecurringPaymentService recurringPaymentService, IBillPaymentService billPaymentService)
{
    _recurringPaymentService = recurringPaymentService;
    _billPaymentService = billPaymentService;
    // ...
}
// Add backing field at top of class:
private readonly IBillPaymentService _billPaymentService;
// Replace line 173 with:
var billers = await Task.Run(() => _billPaymentService.GetBillerDirectory(null));
```
Then update `RecurringPaymentsPage.xaml.cs` line 19:
```csharp
// CURRENT:
_viewModel = new RecurringPaymentViewModel(ServiceLocator.RecurringPaymentService);
// REPLACE WITH:
_viewModel = new RecurringPaymentViewModel(ServiceLocator.RecurringPaymentService, ServiceLocator.BillPaymentService);
```

### A4.4 — `NavigationService` needs an interface

Create new file `Services/INavigationService.cs`:
```csharp
namespace BankingAppTeamB.Services
{
    public interface INavigationService
    {
        void NavigateTo<T>(object? parameter = null);
        void GoBack();
    }
}
```
Then change `NavigationService.cs`:
```csharp
// Line 5 — CURRENT:
public static class NavigationService
// REPLACE WITH:
public static class NavigationService : INavigationService
// Note: static classes can't implement interfaces directly.
// Solution: convert to non-static singleton registered in ServiceLocator.
```
**Full plan for NavigationService:**
1. Change `NavigationService.cs` to a non-static class implementing `INavigationService`.
2. Keep a `static Frame? Frame` property for WinUI integration.
3. Register as singleton in `ServiceLocator`.
4. Inject `INavigationService` into any ViewModel that currently calls `NavigationService` directly (currently `BeneficiariesViewModel.cs` line 155 and `BillPayViewModel.cs` line 79).

#### `BeneficiariesViewModel.cs` line 155 — direct static call to `NavigationService`
```csharp
// CURRENT:
NavigationService.NavigateTo<TransferPage>(transferDto);
// After interface injection, REPLACE WITH:
_navigationService.NavigateTo<TransferPage>(transferDto);
// And add to constructor:
private readonly INavigationService _navigationService;
public BeneficiariesViewModel(IBeneficiaryService beneficiaryService, INavigationService navigationService)
{
    _navigationService = navigationService;
    ...
}
```

#### `BillPayViewModel.cs` line 79 — direct static call to `NavigationService`
```csharp
// CURRENT:
CancelCommand = new RelayCommand(_ => NavigationService.NavigateTo<TransferPage>());
// After injection, REPLACE WITH:
CancelCommand = new RelayCommand(unusedParameter => _navigationService.NavigateTo<TransferPage>());
```

### A4.5 — Services depend on concrete `AccountService` (from Mocks), not an interface

#### `TransactionPipelineService.cs` — line 16 and 19: references `AccountService` (Mocks)
```csharp
// Line 16 — CURRENT:
private readonly AccountService accountService;
// REPLACE WITH:
private readonly IAccountService accountService;

// Line 19 — CURRENT:
public TransactionPipelineService(ITransactionRepository transactionRepo, AccountService accountService)
// REPLACE WITH:
public TransactionPipelineService(ITransactionRepository transactionRepo, IAccountService accountService)
```
Create new file `Services/IAccountService.cs`:
```csharp
namespace BankingAppTeamB.Services
{
    public interface IAccountService
    {
        void DebitAccount(int accountId, decimal amount);
        void CreditAccount(int accountId, decimal amount);
        bool IsAccountValid(int accountId);
        decimal GetBalance(int accountId);
    }
}
```
Move `AccountService` from `Mocks/` to `Services/` and rename namespace to `BankingAppTeamB.Services`, then implement `IAccountService`.

#### `ExchangeService.cs` — line 25 and 15: references `AccountService` concrete type
```csharp
// Line 15 — CURRENT:
private readonly AccountService _accountService;
// REPLACE WITH:
private readonly IAccountService _accountService;

// Line 25 — CURRENT constructor parameter:
AccountService accountService
// REPLACE WITH:
IAccountService accountService
```

#### `ServiceLocator.cs` — lines 20–22: wires concrete types without interfaces
```csharp
// CURRENT line 20:
private static AccountService _accountService = new AccountService();
// REPLACE WITH:
private static IAccountService _accountService = new AccountService();

// CURRENT line 37–41: exposes concrete service types publicly
// REPLACE:
public static TransferService TransferService => _transferService;
// WITH:
public static ITransferService TransferService => _transferService;

public static ExchangeService ExchangeService => _exchangeService;
// WITH:
public static IExchangeService ExchangeService => _exchangeService;

public static BillPaymentService BillPaymentService => _billPaymentService;
// WITH:
public static IBillPaymentService BillPaymentService => _billPaymentService;

public static RecurringPaymentService RecurringPaymentService => _recurringPaymentService;
// WITH:
public static IRecurringPaymentService RecurringPaymentService => _recurringPaymentService;
```

### A1.4 — `UserSession` in `Mocks/` is used in production code — must be replaced

`UserSession.cs` is referenced in:
- `TransferViewModel.cs` line 194, 275
- `BillPayViewModel.cs` lines 253, 256, 424, 444
- `FXViewModel.cs` lines 209, 259, 290
- `RecurringPaymentViewModel.cs` lines 41, 170, 236

**Fix:**
1. Create `Services/IUserSessionService.cs`:
   ```csharp
   using System.Collections.Generic;
   using BankingAppTeamB.Models;
   namespace BankingAppTeamB.Services
   {
       public interface IUserSessionService
       {
           int CurrentUserId { get; }
           string CurrentUserName { get; }
           List<Account> GetAccounts();
       }
   }
   ```
2. Create `Services/UserSessionService.cs` (temporary stub matching current mock data):
   ```csharp
   using System.Collections.Generic;
   using BankingAppTeamB.Models;
   namespace BankingAppTeamB.Services
   {
       public class UserSessionService : IUserSessionService
       {
           public int CurrentUserId { get; private set; } = 1;
           public string CurrentUserName { get; private set; } = "Ion Popescu";
           public List<Account> GetAccounts() => new()
           {
               new Account { Id=1, IBAN="RO49AAAA1B31007593840000", Currency="EUR", Balance=5000.00m, AccountName="Main EUR Account", Status="Active" },
               new Account { Id=2, IBAN="RO49AAAA1B31007593840001", Currency="USD", Balance=1200.00m, AccountName="USD Account", Status="Active" },
               new Account { Id=3, IBAN="RO49AAAA1B31007593840002", Currency="RON", Balance=8500.00m, AccountName="RON Account", Status="Active" },
               new Account { Id=4, IBAN="RO49AAAA1B31007593840003", Currency="EUR", Balance=300.00m, AccountName="Savings EUR Account", Status="Active" }
           };
       }
   }
   ```
3. Register in `ServiceLocator.cs`:
   ```csharp
   private static IUserSessionService _userSessionService = new UserSessionService();
   public static IUserSessionService UserSessionService => _userSessionService;
   ```
4. Inject `IUserSessionService` into every ViewModel that currently calls `UserSession.*` and replace all usages.

---

## BATCH 3 — A1: Architecture Fixes

---

### A1.2 — `App.xaml.cs` — error handling swallows exceptions silently

**Problem:** Lines 23–26 and 32–35 catch exceptions and write to `Debug.WriteLine` then continue — if DB or ServiceLocator fail, the app silently runs broken.

**Fix (line 25):**
```csharp
// CURRENT:
System.Diagnostics.Debug.WriteLine($"DB error: {ex.Message}");
// REPLACE WITH:
throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
```
**Fix (line 34):**
```csharp
// CURRENT:
System.Diagnostics.Debug.WriteLine($"ServiceLocator error: {ex.Message}");
// REPLACE WITH:
throw new InvalidOperationException($"Service initialization failed: {ex.Message}", ex);
```

### A1.2 — `MainWindow.xaml.cs` lines 20–31: navigation switch uses magic tag strings

```csharp
// CURRENT lines 23–31:
switch (item.Tag?.ToString())
{
    case "transfer": NavigationService.NavigateTo<TransferPage>(); break;
    case "beneficiaries": NavigationService.NavigateTo<BeneficiariesPage>(); break;
    ...
}
// REPLACE WITH (add constants to NavigationTags static class):
```
Create `Configuration/NavigationTags.cs`:
```csharp
namespace BankingAppTeamB.Configuration
{
    public static class NavigationTags
    {
        public const string Transfer = "transfer";
        public const string Beneficiaries = "beneficiaries";
        public const string Bill = "bill";
        public const string Recurring = "recurring";
        public const string Exchange = "exchange";
        public const string Alerts = "alerts";
    }
}
```
Then update `MainWindow.xaml.cs`:
```csharp
case NavigationTags.Transfer: ...
case NavigationTags.Beneficiaries: ...
// etc.
```

### A1.4 — `RateAlertsPage.xaml.cs` line 33: hardcoded `userId = 0`
```csharp
// CURRENT:
this.DataContext = new RateAlertViewModel(ServiceLocator.ExchangeService, 0);
// REPLACE WITH:
this.DataContext = new RateAlertViewModel(ServiceLocator.ExchangeService, ServiceLocator.UserSessionService.CurrentUserId);
```

### A1.2 — `TransactionPipelineService.cs` — exposes `GetAccountService()` public method (line 112)
This breaks encapsulation. Delete lines 112–115:
```csharp
// DELETE (lines 112–115):
public AccountService GetAccountService()
{
    return accountService;
}
```

### A1.5 — `DatabaseInitializer.cs` — scripts run every startup without idempotency

**Problem:** `Database/*.sql` scripts run on every launch. If tables already exist, SQL Server will throw. Scripts need `IF NOT EXISTS` guards.

For each `.sql` file in `Database/`:
- `01_create_transaction.sql` — wrap table creation with:
  ```sql
  IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transactions' AND xtype='U')
  BEGIN
      CREATE TABLE Transactions ( ... );
  END
  ```
- Apply the same `IF NOT EXISTS` guard to `02` through `10`.

**Also fix:** `DatabaseInitializer.cs` line 33 re-throws with a new `Exception`, losing the inner type. Change to:
```csharp
// CURRENT:
throw new Exception($"Script failed: {Path.GetFileName(script)} — {ex.Message}");
// REPLACE WITH:
throw new InvalidOperationException($"Script failed: {Path.GetFileName(script)}", ex);
```

---

## BATCH 4 — A6: StyleCop Setup

**Steps (one-time, per developer machine):**

1. Copy `SE.ruleset` into `d:\Projects\Isis\BankingAppTeamB\BankingAppTeamB\BankingAppTeamB\` (same folder as `BankingAppTeamB.csproj`).

2. Open `BankingAppTeamB.csproj` and find the property group for Debug|AnyCPU. Add `<CodeAnalysisRuleSet>` if missing:
   ```xml
   <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
     <CodeAnalysisRuleSet>SE.ruleset</CodeAnalysisRuleSet>
   </PropertyGroup>
   ```

3. Install via Package Manager Console:
   ```powershell
   Install-Package StyleCop.Analyzers -Project BankingAppTeamB
   ```

4. Build → fix all `SA` prefixed warnings. The most common ones you'll hit given this codebase:

   | Warning | File | Line | Fix |
   |---------|------|------|-----|
   | SA1200 – using inside namespace | `FXViewModel.cs` | 12 — file-scoped namespace | Move `using` directives above namespace |
   | SA1309 – field starts with underscore | All ViewModels | All `_camelCase` fields | Rename to `camelCase` (remove leading `_`) |
   | SA1101 – prefix with `this.` | TransferViewModel, FXViewModel | Various assignments | Add `this.` prefix |
   | SA1633 – file header missing | All .cs files | Top of each file | Add standard file header comment |
   | SA1516 – elements must be separated by blank line | BillPayViewModel.cs | Various | Add blank lines between members |

---

## BATCH 5 — A5: Test Project Setup

### A5.1 — Create the test project

```powershell
# Run from solution root:
dotnet new mstest -n BankingAppTeamB.Tests -o BankingAppTeamB.Tests
dotnet sln BankingAppTeamB.sln add BankingAppTeamB.Tests/BankingAppTeamB.Tests.csproj
cd BankingAppTeamB.Tests
dotnet add reference ../BankingAppTeamB/BankingAppTeamB/BankingAppTeamB.csproj
dotnet add package Moq
dotnet add package FluentAssertions
```

### A5.2 — Unit tests for `TransferService`

Create `BankingAppTeamB.Tests/Services/TransferServiceTests.cs`.

Tests to write (filename → method → scenario → expected):

| Test Method Name | Arrange | Assert |
|-----------------|---------|--------|
| `ValidateIBAN_WhenIbanIsEmpty_ReturnsFalse` | `iban = ""` | `ValidateIBAN(iban) == false` |
| `ValidateIBAN_WhenIbanIsTooShort_ReturnsFalse` | `iban = "RO1"` | `false` |
| `ValidateIBAN_WhenIbanHasInvalidCountryCode_ReturnsFalse` | `iban = "12AAAA1234567890"` | `false` |
| `ValidateIBAN_WhenIbanIsValid_ReturnsTrue` | `iban = "RO49AAAA1B31007593840000"` | `true` |
| `GetBankNameFromIBAN_WhenRomanianIBAN_ReturnsRomanianBank` | `iban = "RO..."` | `"Romanian Bank"` |
| `GetFxPreview_WhenSameCurrency_ReturnsRateOne` | `src = "EUR", tgt = "EUR"` | `preview.Rate == 1` |
| `Requires2FA_WhenAmountIsExactlyThreshold_ReturnsTrue` | `amount = 1000m` | `true` |
| `Requires2FA_WhenAmountIsBelowThreshold_ReturnsFalse` | `amount = 999.99m` | `false` |
| `ExecuteTransfer_WhenInvalidIBAN_ThrowsInvalidOperationException` | mock `ITransferRepository`, invalid IBAN | throws `InvalidOperationException` |

### A5.2 — Unit tests for `BillPaymentService`

Create `BankingAppTeamB.Tests/Services/BillPaymentServiceTests.cs`.

| Test Method Name | Scenario |
|-----------------|---------|
| `CalculateFee_WhenAmountIsUnder100_ReturnsHalfRON` | `amount = 50m` → `fee = 0.50m` |
| `CalculateFee_WhenAmountIsExactly100_ReturnsHalfRON` | `amount = 100m` → `fee = 0.50m` |
| `CalculateFee_WhenAmountIsOver100_Returns1RON` | `amount = 101m` → `fee = 1.00m` |
| `Requires2FA_WhenAmountOver1000_ReturnsTrue` | `amount = 1500m` → `true` |
| `PayBill_WhenBillerNotFound_ThrowsInvalidOperationException` | mock repo returns `null` biller |
| `PayBill_WithValidDto_ReturnsPaymentWithReceiptNumber` | mock repo returns biller + mock pipeline runs |

### A5.2 — Unit tests for `ExchangeService`

Create `BankingAppTeamB.Tests/Services/ExchangeServiceTests.cs`.

| Test Method Name | Scenario |
|-----------------|---------|
| `GetRate_WhenDirectPairExists_ReturnsRate` | `EUR/USD` → `1.15m` |
| `GetRate_WhenInversePairExists_ReturnsInverseRate` | `USD/EUR` → `~0.87m` |
| `GetRate_WhenPairUnknown_ThrowsException` | `XYZ/ABC` |
| `CalculateCommission_WhenAmountIsSmall_ReturnsMinimumCommission` | `amount = 50m` → `0.50m` |
| `CalculateCommission_WhenAmountIsLarge_ReturnsPercentage` | `amount = 2000m` → `10m` |
| `LockRate_WhenCalled_StoresLockedRateForUser` | lock for `userId=1` → `IsRateLockValid(1) == true` |
| `ExecuteExchange_WhenNoLockExists_ThrowsException` | call without locking first |
| `CreateAlert_WhenSameCurrency_ThrowsArgumentException` | `source == target` |
| `CreateAlert_WhenRateIsZero_ThrowsArgumentException` | `rate = 0` |

### A5.3 — Unit tests for `TransferViewModel`

Create `BankingAppTeamB.Tests/ViewModels/TransferViewModelTests.cs`.

Mock `ITransferService`, `IUserSessionService`. Key scenarios:

| Test Method Name | Scenario |
|-----------------|---------|
| `Constructor_SetsCurrentStepToOne` | `viewModel.CurrentStep == 1` |
| `SetRecipientIBAN_WhenIBANValid_SetsIsIBANValidTrue` | mock service returns `true` for `ValidateIBAN` |
| `SetAmountText_WhenValidDecimal_ParsesIntoAmount` | `AmountText = "150.50"` → `Amount == 150.50m` |
| `SetAmountText_WhenInvalid_SetsAmountToZero` | `AmountText = "abc"` → `Amount == 0` |
| `ExecuteNextStep_WhenStep2AndInvalidIBAN_SetsErrorAndGoesToStep7` | `CurrentStep = 2, IsIBANValid = false` → `CurrentStep == 7` |
| `ExecuteNextStep_WhenStep3AndAmountZero_SetsError` | `Amount = 0` → error message set |
| `ExecuteTransferAsync_WhenNoAccountSelected_SetsError` | `SelectedAccount = null` → `HasError == true` |

### A5.4 — Unit tests for Repositories (integration-style, in-memory DB)

Create `BankingAppTeamB.Tests/Repositories/TransferRepositoryTests.cs`.

Use a real SQL Server LocalDB or test connection string pointing to a test DB. Each test should wrap in a transaction and roll back. Key tests:

| Test Method Name |
|-----------------|
| `Add_WhenValidTransfer_CanBeRetrievedByUserId` |
| `GetByUserId_WhenNoTransfers_ReturnsEmptyList` |

### A5.6 — Integration test for `RecurringScheduler`

Create `BankingAppTeamB.Tests/Services/RecurringSchedulerTests.cs`.

| Test |
|-----|
| `ProcessDuePayments_WhenPaymentIsDue_UpdatesNextExecutionDate` |
| `GetDueSoon_WhenPaymentDueWithin24Hours_ReturnsPayment` |

---

## BATCH 6 — A7: SQL Queries — No Business Logic

### Review each SQL file and inline SQL in repositories

#### `Database/08_seed_billers.sql` — OK, pure data seeding
#### Repository inline SQL — check each:

Open `Repositories/BillPaymentRepository.cs` and look for any `WHERE` clauses that embed business rules (e.g. fee thresholds, status logic). If found, move those conditions to the Service layer.

Open `Repositories/TransactionRepository.cs` — verify no `CASE WHEN amount > 1000` or equivalent.

The **threshold check** (`amount >= 1000 → 2FA`) **must live only** in `TransferService.Requires2FA()` and `BillPaymentService.Requires2FA()`, not in any SQL.

---

## BATCH 7 — ITransferService interface: parameter naming

**Problem:** `ITransferService.cs` line 11 — method uses abbreviated parameter names:
```csharp
FxPreview GetFxPreview(string src, string tgt, decimal amt);
```
**Fix:**
```csharp
FxPreview GetFxPreview(string sourceCurrency, string targetCurrency, decimal amount);
```
Update `TransferService.cs` line 107 accordingly:
```csharp
// CURRENT:
public FxPreview GetFxPreview(string src, string tgt, decimal amt)
// REPLACE WITH:
public FxPreview GetFxPreview(string sourceCurrency, string targetCurrency, decimal amount)
```
And all body occurrences: `src` → `sourceCurrency`, `tgt` → `targetCurrency`, `amt` → `amount`.

---

## Summary Checklist

| Batch | Requirement | Files Touched |
|-------|-------------|---------------|
| 1 | A2.1 Lambda renames | TransferVM, BeneficiariesVM, FXVM, RateAlertVM, BillPayVM, RecurringVM, TransferService, ExchangeService |
| 1 | A2.2 Magic numbers | TransferService, BillPaymentService, TransactionPipelineService, ExchangeService, TwoFAService |
| 1 | A2.4 Dead code removal | BillPayViewModel (delete 400+ lines), TransactionPipelineService, NotificationService, RateAlertsPage |
| 2 | A3.1 GUI logic out | RecurringPaymentsPage, BillPayPage, BeneficiariesPage |
| 2 | A3.3 Concrete → interfaces in VMs | All 6 ViewModels |
| 2 | A4.4 INavigationService | New interface file, NavigationService refactor, BeneficiariesVM, BillPayVM |
| 2 | A4.5 IAccountService | New interface, AccountService moved, TransactionPipelineService, ExchangeService |
| 2 | A1.4 UserSession → IUserSessionService | New interface + service, all 5 ViewModels |
| 3 | A1.2 App.xaml.cs error handling | App.xaml.cs |
| 3 | A1.2 Navigation magic strings | MainWindow.xaml.cs, new NavigationTags.cs |
| 3 | A1.5 DB idempotency | All 10 .sql files |
| 4 | A6 StyleCop | .csproj + all .cs files |
| 5 | A5 Test project + unit tests | New test project, ~25+ test methods |
| 6 | A7 SQL logic check | All repositories |
| 7 | ITransferService param names | ITransferService.cs, TransferService.cs |
