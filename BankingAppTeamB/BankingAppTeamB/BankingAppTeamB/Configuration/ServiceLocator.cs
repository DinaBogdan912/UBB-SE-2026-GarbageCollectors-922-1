using System.Timers;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;

namespace BankingAppTeamB.Configuration;

public static class ServiceLocator
{
    private const int RecurringSchedulerIntervalInMilliseconds = 30000;

    private static readonly ITransactionRepository TransactionRepository = new TransactionRepository();
    private static readonly ITransferRepository TransferRepository = new TransferRepository();
    private static readonly IBeneficiaryRepository BeneficiaryRepository = new BeneficiaryRepository();
    private static readonly IBillPaymentRepository BillPaymentRepository = new BillPaymentRepository();
    private static readonly IRecurringPaymentRepository RecurringPaymentRepository = new RecurringPaymentRepository();
    private static readonly IExchangeRepository ExchangeRepository = new ExchangeRepository();
    private static readonly IUserSessionService UserSessionServiceInstance = new UserSessionService();

    private static readonly IAccountService AccountService = new AccountService(UserSessionServiceInstance);
    private static readonly ITransactionPipelineService TransactionPipelineService = new TransactionPipelineService(TransactionRepository, AccountService);
    private static readonly IExchangeService ExchangeServiceInstance = new ExchangeService(ExchangeRepository, TransactionPipelineService, AccountService);
    private static readonly ITransferService TransferServiceInstance = new TransferService(TransferRepository, BeneficiaryRepository, TransactionPipelineService, ExchangeServiceInstance);
    private static readonly IBillPaymentService BillPaymentServiceInstance = new BillPaymentService(BillPaymentRepository, TransactionPipelineService);
    private static readonly IRecurringPaymentService RecurringPaymentServiceInstance = new RecurringPaymentService(RecurringPaymentRepository, BillPaymentServiceInstance);
    private static readonly IBeneficiaryService BeneficiaryServiceInstance = new BeneficiaryService(BeneficiaryRepository);

    private static readonly Timer RecurringSchedulerTimer = new Timer(RecurringSchedulerIntervalInMilliseconds)
    {
        AutoReset = true
    };

    private static readonly RecurringScheduler RecurringSchedulerInstance = new RecurringScheduler(RecurringPaymentServiceInstance, ExchangeServiceInstance, RecurringSchedulerTimer);

    public static ITransferService TransferService => TransferServiceInstance;

    public static IExchangeService ExchangeService => ExchangeServiceInstance;

    public static IBillPaymentService BillPaymentService => BillPaymentServiceInstance;

    public static IRecurringPaymentService RecurringPaymentService => RecurringPaymentServiceInstance;

    public static IRecurringScheduler RecurringScheduler => RecurringSchedulerInstance;

    public static IBeneficiaryService BeneficiaryService => BeneficiaryServiceInstance;

    public static IUserSessionService UserSessionService => UserSessionServiceInstance;

    public static void Initialize()
    {
        RecurringSchedulerInstance.Start();
    }
}
