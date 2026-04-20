using System.Threading.Tasks;
using System.Timers;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;

namespace BankingAppTeamB.Configuration;

public static class ServiceLocator
{
    private const int RecurringSchedulerIntervalMilliseconds = 30000;

    private static ITransactionRepository transactionRepository = new TransactionRepository();
    private static ITransferRepository transferRepository = new TransferRepository();
    private static IBeneficiaryRepository beneficiaryRepository = new BeneficiaryRepository();
    private static IBillPaymentRepository billPaymentRepository = new BillPaymentRepository();
    private static IRecurringPaymentRepository recurringPaymentRepository = new RecurringPaymentRepository();
    private static IExchangeRepository exchangeRepository = new ExchangeRepository();
    private static IUserSessionService userSessionService = new UserSessionService();

    private static IAccountService accountService = new AccountService(userSessionService);
    private static ITransactionPipelineService pipelineService = new TransactionPipelineService(transactionRepository, accountService);
    private static IExchangeService exchangeService = new ExchangeService(exchangeRepository, pipelineService, accountService);
    private static ITransferService transferService = new TransferService(transferRepository, beneficiaryRepository, pipelineService, exchangeService);
    private static IBillPaymentService billPaymentService = new BillPaymentService(billPaymentRepository, pipelineService);
    private static IRecurringPaymentService recurringPaymentService = new RecurringPaymentService(recurringPaymentRepository, billPaymentService);
    private static IBeneficiaryService beneficiaryService = new BeneficiaryService(beneficiaryRepository);

    // mock
    private static readonly Timer RecurringSchedulerTimer = new Timer(RecurringSchedulerIntervalMilliseconds)
    {
        AutoReset = true
    };
    private static RecurringScheduler recurringScheduler = new RecurringScheduler(recurringPaymentService, exchangeService, RecurringSchedulerTimer);
    public static ITransferService TransferService => transferService;
    public static IExchangeService ExchangeService => exchangeService;
    public static IBillPaymentService BillPaymentService => billPaymentService;
    public static IRecurringPaymentService RecurringPaymentService => recurringPaymentService;
    public static IRecurringScheduler RecurringScheduler => recurringScheduler;
    public static IBeneficiaryService BeneficiaryService => beneficiaryService;
    public static IUserSessionService UserSessionService => userSessionService;
    public static void Initialize()
    {
        recurringScheduler.Start();
    }
}