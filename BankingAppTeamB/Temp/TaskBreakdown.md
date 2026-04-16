# Banking App – Task Breakdown

> **Audience:** Development team (Team B).
> **Sources:** `Assignment3.md` (engineering/process requirements) · `BankingAppFRS.md` (functional requirements).
> **Codebase snapshot:** WinUI 3 / MVVM, C#, SQLite. Modules implemented so far: Transfers, Bill Pay, FX / Rate Alerts, Recurring Payments, Beneficiaries.

---

## Table of Contents

1. [Part A – Assignment 3 Engineering Requirements](#part-a--assignment-3-engineering-requirements)
   - A1. Architectural Improvements
   - A2. Code Readability & Style
   - A3. Business Logic Separation (No Logic in GUI)
   - A4. Layer Decoupling via Interfaces
   - A5. Unit & Integration Tests
   - A6. StyleCop Static Analysis
   - A7. Ongoing / Process Requirements
2. [Part B – FRS Functional Features](#part-b--frs-functional-features)
   - B1. Authentication & Access Control
   - B2. Main Dashboard
   - B3. User Profile
   - B4. Card Management
   - B5. Money Transfers *(partially implemented)*
   - B6. Bill Payments *(partially implemented)*
   - B7. Transaction History *(partially implemented)*
   - B8. Currency Exchange *(partially implemented)*
   - B9. Statistics & Analytics
   - B10. Savings & Loans
   - B11. Investments & Trading
   - B12. Customer Support Chat
   - B13. Notifications *(partially implemented)*
   - B14. Cross-Cutting Constraints

---

## Part A – Assignment 3 Engineering Requirements

These are the **technical / process** requirements from `Assignment3.md`. They apply across the **entire** codebase, not just new features.

---

### A1. Architectural Improvements

> *"Make any architectural changes you deem relevant. Fix what is not working, it will make later assignments more manageable. Feel free to use design patterns if you want."*

| # | Task | Notes / Context in Codebase |
|---|------|-----------------------------|
| A1.1 | Audit overall app architecture and document current layer structure | MVVM skeleton exists: `ViewModels/`, `Services/`, `Repositories/`, `Models/` |
| A1.2 | Identify and fix broken/incomplete wiring (DI, navigation, service registration) | `App.xaml.cs` wires services; `NavigationService.cs` is minimal |
| A1.3 | Apply consistent design patterns where missing (e.g., Repository, Service, Strategy, Pipeline) | `TransactionPipelineService` already exists – verify consistent use |
| A1.4 | Evaluate `Mocks/` folder usage – ensure mock objects are only in test projects, not shipping code | `AccountService.cs` and `UserSession.cs` in `Mocks/` look like stubs |
| A1.5 | Ensure `DatabaseInitializer` runs SQL scripts in correct order and handles idempotency | `Database/` has 10 numbered `.sql` files |

---

### A2. Code Readability & Style

> *"All functions and variables must have a semantic meaning whenever possible, there may be no abbreviations (yes, not even lambdas). Deal with magic numbers."*

| # | Task | Notes / Context in Codebase |
|---|------|-----------------------------|
| A2.1 | Rename any abbreviated identifiers (variables, parameters, methods) to full descriptive names | Pay special attention to lambda parameters (`x =>`, `i =>`, etc.) |
| A2.2 | Extract all magic numbers / magic strings into named constants or enums | Check `BillPayViewModel.cs` (29 KB – largest file), `RecurringPaymentViewModel.cs`, `ExchangeService.cs` |
| A2.3 | Ensure all public members have XML doc-comments | Repositories and service interfaces are good candidates |
| A2.4 | Remove dead code, commented-out blocks, and TODO clutter | Full codebase sweep |
| A2.5 | Normalise file / class naming to PascalCase conventions consistently | |

---

### A3. Business Logic Separation (No Logic in GUI)

> *"Make sure there is no business logic in GUI. If you find some then move it to a non-GUI class."*

| # | Task | Notes / Context in Codebase |
|---|------|-----------------------------|
| A3.1 | Audit all `*.xaml.cs` code-behind files for business logic and move it to ViewModels or Services | `BillPayPage.xaml.cs` (2.7 KB), `RecurringPaymentsPage.xaml.cs` (3.5 KB), `BeneficiariesPage.xaml.cs` (2.2 KB) are the largest |
| A3.2 | Audit ViewModels for leakage of data-access or persistence logic into them; push down to Services | `BillPayViewModel.cs` is 30 KB – likely contains mixed concerns |
| A3.3 | Validate: ViewModels should only call Service interfaces, never Repositories directly | |
| A3.4 | Move any formatting / display logic that is currently in Services back up to ViewModels / Converters | `Converters/` directory exists – check it is being used |

---

### A4. Layer Decoupling via Interfaces

> *"No layer shall know directly about the next one, use interfaces to separate them."*

| # | Task | Notes / Context in Codebase |
|---|------|-----------------------------|
| A4.1 | Verify every concrete Service has a matching `I*Service` interface and that ViewModels depend only on the interface | Services have interfaces (`IBeneficiaryService`, `ITransferService`, etc.) – verify completeness |
| A4.2 | Verify every concrete Repository has a matching `I*Repository` interface and that Services depend only on the interface | Repository interfaces exist – verify completeness |
| A4.3 | Register all services and repositories via interfaces in the DI container (not concrete types) | Check `App.xaml.cs` registration code |
| A4.4 | Ensure `NavigationService` is also abstracted behind an interface | Currently `NavigationService.cs` has no interface counterpart |
| A4.5 | Ensure `DatabaseInitializer` and `AppDatabase` are accessed through an abstraction where needed | `Data/AppDatabase.cs` |

---

### A5. Unit & Integration Tests

> *"Write unit tests in the manner presented at the course. You should have 100% coverage on the parts that you should, and none elsewhere. Add integration tests wherever appropriate. You may use isolation frameworks."*

| # | Task | Notes / Context in Codebase |
|---|------|-----------------------------|
| A5.1 | Create a dedicated test project (xUnit or MSTest) in the solution | No test project detected in `BankingAppTeamB.sln` |
| A5.2 | Write unit tests for all **Service** classes (mock repositories with Moq/NSubstitute) | `TransferService`, `BillPaymentService`, `ExchangeService`, `RecurringPaymentService`, `NotificationService`, `TransactionPipelineService`, `TwoFAService` |
| A5.3 | Write unit tests for all **ViewModel** classes (mock services) | All ViewModels in `ViewModels/` |
| A5.4 | Write unit tests for **Repository** classes (use in-memory SQLite or test DB) | All Repositories in `Repositories/` |
| A5.5 | Add integration tests for the full Transfer pipeline (`TransactionPipelineService`) | End-to-end: ViewModel → Service → Repository → DB |
| A5.6 | Add integration tests for the Recurring Payment scheduler | `RecurringScheduler.cs` |
| A5.7 | Enforce uniformity: all tests follow AAA (Arrange–Act–Assert), consistent naming (`MethodName_StateUnderTest_ExpectedBehaviour`) | |
| A5.8 | Achieve 100% branch/line coverage on Services and Repositories; 0% on Views (xaml.cs) | |

---

### A6. StyleCop Static Analysis

> *"Use StyleCop static code analysis to find issues in the code and fix them."*

| # | Task | Steps |
|---|------|-------|
| A6.1 | Copy `SE.ruleset` file from Teams into the project root | Place alongside `BankingAppTeamB.csproj` |
| A6.2 | Install `StyleCop.Analyzers` NuGet package | Right-click project → Manage NuGet Packages → Browse → StyleCop.Analyzers |
| A6.3 | Add `<CodeAnalysisRuleSet>SE.ruleset</CodeAnalysisRuleSet>` to the `Debug\|AnyCPU` PropertyGroup in `.csproj` | Edit project file directly |
| A6.4 | Build the project and resolve all StyleCop warnings/errors | Prioritise SA1xxx (documentation), SA13xx (readability), SA16xx (ordering) |
| A6.5 | Re-run build until clean (zero warnings/errors related to StyleCop) | |
| A6.6 | Apply same setup to the test project once created | |

---

### A7. Ongoing / Process Requirements

| # | Task | Notes |
|---|------|-------|
| A7.1 | Team lead: prepare contribution report (tasks, git history, chat log, file history) before demo | |
| A7.2 | SQL queries must contain **no** business logic | Review all `.sql` files in `Database/` and any inline SQL in repositories |
| A7.3 | (Optional) Use tasks/issues for work tracking | GitHub Issues, Azure DevOps, or Trello |
| A7.4 | (Optional) Target feature-completeness a few days before the deadline for buffer time | |

---

## Part B – FRS Functional Features

These are the **product / feature** requirements from `BankingAppFRS.md`. They describe what the application must do from a user-facing perspective.

Features are marked with a status:

- 🟢 **Implemented** – exists in codebase
- 🟡 **Partial** – scaffolded but incomplete
- 🔴 **Missing** – not found in codebase

---

### B1. Authentication & Access Control

> Module: `FR-AUTH-001 / 002 / 003`  
> **Status: 🔴 Missing** – No login page, no session management found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B1.1 | Login page with Email + Password fields, client-side email format validation | FR-AUTH-001.1 | 🔴 |
| B1.2 | Password field with show/hide toggle | FR-AUTH-001.1 | 🔴 |
| B1.3 | Issue session token (JWT or equivalent) on success; redirect to Dashboard | FR-AUTH-001.1 | 🔴 |
| B1.4 | Generic "Invalid email or password" error (no field-specific hint) | FR-AUTH-001.1 | 🔴 |
| B1.5 | Account lockout after 5 failed attempts for 15 min + email notification | FR-AUTH-001.1 | 🔴 |
| B1.6 | "Sign in with Google" / "Sign in with GitHub" OAuth 2.0 buttons | FR-AUTH-001.2 | 🔴 |
| B1.7 | Forgot Password flow (email form → reset token → Set New Password page) | FR-AUTH-002 | 🔴 |
| B1.8 | Password policy: ≥8 chars, uppercase, lowercase, digit, special char | FR-AUTH-002 | 🔴 |
| B1.9 | 2FA challenge screen (TOTP + SMS/email OTP, 60 s / 5 min expiry) | FR-AUTH-001.3 | 🔴 |
| B1.10 | Invalidate session after 3 wrong OTP attempts | FR-AUTH-001.3 | 🔴 |

---

### B2. Main Dashboard

> Module: `FR-DASH-001`  
> **Status: 🔴 Missing** – No Dashboard / Home page found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B2.1 | Persistent top/side navigation bar on every page | FR-DASH-001.1 | 🔴 |
| B2.2 | Nav items: Dashboard, Transfers, Bills, Cards, History, Exchange, Savings & Loans, Investments, Statistics, Chat, Profile | FR-DASH-001.1 | 🔴 |
| B2.3 | Active module highlight in nav | FR-DASH-001.1 | 🔴 |
| B2.4 | User name/avatar + notification bell with unread badge + Log Out | FR-DASH-001.1 | 🔴 |
| B2.5 | Cards Overview Panel with card brand, masked number, name, expiry, balance | FR-DASH-001.2 | 🔴 |
| B2.6 | "Show Details" re-auth gate (password/biometric), auto-hide after 60 s | FR-DASH-001.2 | 🔴 |
| B2.7 | Quick Actions panel: New Transfer, Pay a Bill, Exchange Currency, View History | FR-DASH-001.3 | 🔴 |
| B2.8 | Recent Transactions widget (last 5, with date, counterparty, amount, status) | FR-DASH-001.4 | 🔴 |

---

### B3. User Profile

> Module: `FR-PROF-001 / 002 / 003`  
> **Status: 🔴 Missing** – No profile page found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B3.1 | Display: full name, email, phone, DOB, address, nationality | FR-PROF-001 | 🔴 |
| B3.2 | Editable: phone, address; name/DOB requires KYC / support ticket | FR-PROF-001 | 🔴 |
| B3.3 | Require password confirmation before saving profile changes | FR-PROF-001 | 🔴 |
| B3.4 | Change password (requires current password) | FR-PROF-002 | 🔴 |
| B3.5 | Enable / disable / reconfigure 2FA methods | FR-PROF-002 | 🔴 |
| B3.6 | Link / unlink OAuth providers (Google, GitHub) | FR-PROF-002 | 🔴 |
| B3.7 | Active sessions list with device/IP/last-active + per-session revoke | FR-PROF-002 | 🔴 |
| B3.8 | Notification preferences: channels (push/email/SMS) per event type | FR-PROF-003 | 🔴 |
| B3.9 | Minimum debit threshold for transaction alerts | FR-PROF-003 | 🔴 |

---

### B4. Card Management

> Module: `FR-CARD-001 / 002 / 003 / 004`  
> **Status: 🔴 Missing** – No card model or card management page found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B4.1 | List all cards (active, frozen, expired) with type, brand, last 4 digits, IBAN, expiry, status | FR-CARD-001 | 🔴 |
| B4.2 | Expanded card view: daily/monthly limits, contactless status, online transaction status, ATM limits | FR-CARD-001 | 🔴 |
| B4.3 | Full card number and CVV masked by default | FR-CARD-002 | 🔴 |
| B4.4 | "Show Full Card Details" re-auth gate; auto-re-mask after 60 s; log reveal event | FR-CARD-002 | 🔴 |
| B4.5 | Reorder cards via drag-and-drop or "Set as Primary"; persist preference across sessions | FR-CARD-003 | 🔴 |
| B4.6 | Sort cards by custom order / balance / card type | FR-CARD-003 | 🔴 |
| B4.7 | Freeze / Unfreeze card (with confirmation) | FR-CARD-004 | 🔴 |
| B4.8 | Set spending limits (daily, monthly, ATM, contactless) | FR-CARD-004 | 🔴 |
| B4.9 | Toggle online / contactless transactions per card | FR-CARD-004 | 🔴 |
| B4.10 | Permanently cancel card (2FA required, irreversible dialog; keep as "Cancelled" for 90 days) | FR-CARD-004 | 🔴 |
| B4.11 | Request new physical or virtual card | FR-CARD-004 | 🔴 |

---

### B5. Money Transfers

> Module: `FR-TRANS-001 / 002`  
> **Status: 🟡 Partial** – `TransferService`, `TransferRepository`, `TransferViewModel`, `TransferPage` exist; Beneficiaries exist.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B5.1 | Source account selector with live balance | FR-TRANS-001 | 🟡 |
| B5.2 | Recipient entry: name, IBAN/account number, bank (auto-populated), optional reference | FR-TRANS-001 | 🟡 |
| B5.3 | Real-time exchange rate + converted amount display when source/target currencies differ | FR-TRANS-001 | 🟡 |
| B5.4 | Transfer review screen: source, recipient, amount, fees, estimated arrival, total debit | FR-TRANS-001 | 🟡 |
| B5.5 | 2FA confirmation for transfers exceeding configurable threshold (e.g., €1,000) | FR-TRANS-001 | 🟡 – `ITwoFAService` exists |
| B5.6 | Success screen with transaction reference; error screen with code + retry | FR-TRANS-001 | 🟡 |
| B5.7 | Save recipient as Beneficiary | FR-TRANS-002 | 🟢 |
| B5.8 | View, edit, delete Beneficiaries | FR-TRANS-002 | 🟢 |
| B5.9 | Select from saved Beneficiaries to auto-populate recipient fields | FR-TRANS-002 | 🟢 |
| B5.10 | Transfer History & Recipients Log | FR-TRANS-003 | 🟡 – `TransactionRepository` exists |

---

### B6. Bill Payments

> Module: `FR-BILL-001 / 002`  
> **Status: 🟡 Partial** – `BillPaymentService`, `BillPayViewModel`, `BillPayPage` exist; `RecurringPaymentService` exists.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B6.1 | Biller search / categorized directory (Utilities, Telecom, Insurance, Rent, Government, Subscriptions, Other) | FR-BILL-001 | 🟡 |
| B6.2 | Biller directory populated via bill payment API integration | FR-BILL-001 | 🔴 – `08_seed_billers.sql` seeds static data |
| B6.3 | Biller-specific reference entry, amount, source account | FR-BILL-001 | 🟡 |
| B6.4 | Bill payment review: biller name, reference, amount, fees; confirm | FR-BILL-001 | 🟡 |
| B6.5 | Receipt number on success; recorded in Transaction History | FR-BILL-001 | 🟡 |
| B6.6 | Save frequently used billers | FR-BILL-002 | 🟢 |
| B6.7 | Recurring automatic payments: frequency, start/end dates, fixed or "full balance" | FR-BILL-002 | 🟢 |
| B6.8 | 24-hour advance notification + execution confirmation notification | FR-BILL-002 | 🟡 – `NotificationService` exists |
| B6.9 | View, pause, modify, cancel recurring payments | FR-BILL-002 | 🟢 |

---

### B7. Transaction History

> Module: `FR-HIST-001 / 002 / 003 / 004`  
> **Status: 🟡 Partial** – `TransactionRepository` and `Transaction` model exist; no dedicated History UI page found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B7.1 | Chronological list of completed, pending, failed transactions with all required fields (type, counterparty, amount, status, running balance) | FR-HIST-001 | 🟡 |
| B7.2 | Free-text search (merchant name, reference, description) | FR-HIST-002 | 🔴 |
| B7.3 | Filters: date range, type, amount range, account/card, status, debit/credit | FR-HIST-002 | 🔴 |
| B7.4 | Sort by date or amount (ascending/descending); combinable with filters; persist during session | FR-HIST-002 | 🔴 |
| B7.5 | Transaction detail view: all list fields + full IBANs, fees, FX rate, downloadable PDF receipt | FR-HIST-003 | 🔴 |
| B7.6 | Export as PDF, CSV, Excel (.xlsx) for selected date range | FR-HIST-004 | 🔴 |

---

### B8. Currency Exchange

> Module: `FR-FX-001 / 002`  
> **Status: 🟡 Partial** – `ExchangeService`, `FXViewModel`, `FXPage`, `RateAlertViewModel`, `RateAlertsPage` exist.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B8.1 | Source account + currency selector | FR-FX-001 | 🟡 |
| B8.2 | Target currency selector from supported list | FR-FX-001 | 🟡 |
| B8.3 | Real-time exchange rate from rate provider API; show converted amount, commission, final amount | FR-FX-001 | 🟡 |
| B8.4 | Rate locked for 30 s; auto-refresh if not confirmed in time | FR-FX-001 | 🟢 – `LockedRate` model exists |
| B8.5 | On confirm: debit source, credit target; confirmation screen with reference | FR-FX-001 | 🟡 |
| B8.6 | Live rate ticker / table for popular pairs (EUR/USD, EUR/GBP, EUR/RON, etc.) | FR-FX-002 | 🔴 |
| B8.7 | Rate alerts: user sets pair + target rate; push/email notification when reached | FR-FX-002 | 🟢 – `RateAlert` model + `RateAlertViewModel` |

---

### B9. Statistics & Analytics

> Module: `FR-STAT-001 / 002 / 003 / 004`  
> **Status: 🔴 Missing** – No statistics view or analytics service found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B9.1 | Spending breakdown by category (Groceries, Dining, Transport, etc.) as pie and bar charts | FR-STAT-001 | 🔴 |
| B9.2 | Time period selector: current month, last 3/6/12 months, custom range | FR-STAT-001 | 🔴 |
| B9.3 | Auto-categorise transactions by merchant; allow manual re-categorisation | FR-STAT-001 | 🔴 |
| B9.4 | Income vs. Expenses line/bar chart per month + net savings + cumulative trend | FR-STAT-002 | 🔴 |
| B9.5 | Daily closing balance time-series chart per account with per-account toggle | FR-STAT-003 | 🔴 |
| B9.6 | Top 10 merchants by spend, top 10 recipients by amount, top billers by total paid | FR-STAT-004 | 🔴 |

---

### B10. Savings & Loans

> Module: `FR-SAV-001 / FR-LOAN-001`  
> **Status: 🔴 Missing** – No savings or loan models / services found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B10.1 | View savings accounts: type, balance, interest, maturity date, status | FR-SAV-001 | 🔴 |
| B10.2 | Open new savings product (type, initial deposit, target, funding source, frequency) | FR-SAV-001 | 🔴 |
| B10.3 | Manage savings: additional deposits, auto-deposit setup, withdraw, close | FR-SAV-001 | 🔴 |
| B10.4 | View loans: type, principal, outstanding, rate, installment, next payment, term, progress % | FR-LOAN-001 | 🔴 |
| B10.5 | Request new loan (type, amount, term, purpose) with indicative rate and installment estimate | FR-LOAN-001 | 🔴 |
| B10.6 | Manual loan installment payment (source account, amount selection) | FR-LOAN-001 | 🔴 |
| B10.7 | Full amortization schedule (downloadable): per-installment date, principal, interest, remaining balance | FR-LOAN-001 | 🔴 |

---

### B11. Investments & Trading

> Module: `FR-INV-001 / 002 / 003 / 004`  
> **Status: 🔴 Missing** – No investment models or services found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B11.1 | Portfolio overview: total value, gain/loss, holdings by type (Stocks, ETFs, Bonds, Crypto, Other) | FR-INV-001 | 🔴 |
| B11.2 | Per-asset: name, ticker, quantity, avg purchase price, current market price (real-time), unrealized gain/loss | FR-INV-001 | 🔴 |
| B11.3 | Buy and sell securities flow (market/limit orders) | FR-INV-002 | 🔴 |
| B11.4 | Crypto trading: wallet view, network fee estimation, volatility disclaimer | FR-INV-003 | 🔴 |
| B11.5 | Investment transaction log: date, asset, action, quantity, price, fees, order type | FR-INV-004 | 🔴 |
| B11.6 | Export investment statements (PDF / CSV) for a selected period | FR-INV-004 | 🔴 |

---

### B12. Customer Support Chat

> Module: `FR-CHAT-001 / 002 / 003`  
> **Status: 🔴 Missing** – No chat module found.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B12.1 | Dedicated in-app chat interface accessible from nav menu and persistent floating icon | FR-CHAT-001 | 🔴 |
| B12.2 | Welcome message with estimated wait time + issue category selector before connecting | FR-CHAT-001 | 🔴 |
| B12.3 | Persisted chat history across sessions per user | FR-CHAT-001 | 🔴 |
| B12.4 | Real-time text messaging with support agent | FR-CHAT-002 | 🔴 |
| B12.5 | File sharing: images + PDFs up to 10 MB | FR-CHAT-002 | 🔴 |
| B12.6 | Chat transcript emailed at end of session (optional) | FR-CHAT-002 | 🔴 |
| B12.7 | Post-chat satisfaction rating (1–5 stars + optional written feedback) | FR-CHAT-002 | 🔴 |
| B12.8 | AI chatbot / FAQ assistant before escalating to live agent; preserve conversation context on escalation | FR-CHAT-003 | 🔴 |

---

### B13. Notifications

> Module: `FR-NOTIF-001 / 002`  
> **Status: 🟡 Partial** – `NotificationService` and `INotificationService` exist; no Notification Center UI.

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B13.1 | Transaction alerts (debit/credit), login alerts, bill payment reminders, marketing messages | FR-NOTIF-001 | 🟡 |
| B13.2 | Notification Center: aggregated list of recent notifications, read/unread state, mark-all-read, delete | FR-NOTIF-002 | 🔴 |

---

### B14. Cross-Cutting Constraints

> Module: `FR-SYS-001 / 002 / 003 / 004 / 005`

| # | Requirement | FR ID | Status |
|---|-------------|-------|--------|
| B14.1 | Responsive design: desktop (≥1920×1080), tablet (768×1024), mobile (375×812); no horizontal scroll on critical flows | FR-SYS-001 | 🔴 – WinUI app; requires adaptive layouts |
| B14.2 | Accessibility: WCAG 2.1 AA; keyboard navigation; alt text on images/icons; labels on form fields | FR-SYS-002 | 🔴 |
| B14.3 | Localization: English + Romanian (at minimum); locale-aware date, time, currency formatting | FR-SYS-003 | 🔴 |
| B14.4 | Error handling: user-friendly messages with corrective action; auto-retry (3x, exponential backoff); explicit success/failure confirmation for transactions | FR-SYS-004 | 🟡 |
| B14.5 | Audit logging of sensitive operations (card detail reveal, security setting changes) | FR-SYS-005 | 🔴 |

---

## Summary Matrix

| Area | Priority | Effort | Blocked By |
|------|----------|--------|------------|
| **A1–A4** Arch / Refactoring | 🔴 High | Medium | Nothing – start immediately |
| **A5** Tests | 🔴 High | High | A1–A4 (cleaner code = easier tests) |
| **A6** StyleCop | 🔴 High | Low | Nothing – can be done in parallel |
| **B1** Auth | 🔴 High | High | Architecture cleanup (A1–A4) |
| **B2** Dashboard | 🔴 High | Medium | B1 (needs session/user context) |
| **B5** Transfers (complete) | 🟡 Medium | Low | A3 (remove GUI logic) |
| **B6** Bill Pay (complete) | 🟡 Medium | Low | A3 |
| **B7** Transaction History UI | 🟡 Medium | Medium | B1, B2 |
| **B8** FX (complete) | 🟡 Medium | Low | A3 |
| **B3** User Profile | 🟠 Medium | Medium | B1 |
| **B4** Card Management | 🟠 Medium | High | B1, B2 |
| **B13** Notification Center | 🟠 Medium | Medium | B2 |
| **B9** Statistics | 🟠 Low–Medium | High | B7 (needs transaction data) |
| **B10** Savings & Loans | 🟠 Low–Medium | High | B1, B2 |
| **B11** Investments | 🟠 Low | Very High | B1, B2, external APIs |
| **B12** Support Chat | 🟠 Low | Very High | B1, external chat backend |
| **B14** Cross-cutting (A11y, i18n) | 🟠 Low–Medium | Medium | All UI work |
