using System.Threading.Tasks;
using System.Timers;
using BankingAppTeamB.Mocks;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;

namespace BankingAppTeamB.Configuration;

public static class ServiceLocator
{

    private static ITransactionRepository _transactionRepository = new TransactionRepository();
    private static ITransferRepository _transferRepository = new TransferRepository();
    private static IBeneficiaryRepository _beneficiaryRepository = new BeneficiaryRepository();
    private static IBillPaymentRepository _billPaymentRepository = new BillPaymentRepository();
    private static IRecurringPaymentRepository _recurringPaymentRepository = new RecurringPaymentRepository();
    private static IExchangeRepository _exchangeRepository = new ExchangeRepository();
    private static IUserSessionService _userSessionService = new UserSessionService();

    //mock
    private static IAccountService _accountService = new AccountService();
    private static ITransactionPipelineService _pipelineService = new TransactionPipelineService(_transactionRepository, _accountService);
    private static IExchangeService _exchangeService = new ExchangeService(_exchangeRepository, _pipelineService, _accountService);
    private static ITransferService _transferService = new TransferService(_transferRepository, _beneficiaryRepository, _pipelineService, _exchangeService);
    private static IBillPaymentService _billPaymentService = new BillPaymentService(_billPaymentRepository, _pipelineService);
    private static IRecurringPaymentService _recurringPaymentService = new RecurringPaymentService(_recurringPaymentRepository, _billPaymentService);
    private static IBeneficiaryService _beneficiaryService = new BeneficiaryService(_beneficiaryRepository);


    //mock
    private static readonly Timer timer = new Timer(30000) //30secs
    {
        AutoReset = true
    };
    
    private static RecurringScheduler _recurringScheduler = new RecurringScheduler(_recurringPaymentService, _exchangeService, timer);
    public static ITransferService TransferService => _transferService;
    public static IExchangeService ExchangeService => _exchangeService;
    public static IBillPaymentService BillPaymentService => _billPaymentService;
    public static IRecurringPaymentService RecurringPaymentService => _recurringPaymentService;
    public static IRecurringScheduler RecurringScheduler => _recurringScheduler;
    public static IBeneficiaryService BeneficiaryService => _beneficiaryService;
    public static IUserSessionService UserSessionService => _userSessionService;
    public static void Initialize()
    {
        _recurringScheduler.Start();
    }

}