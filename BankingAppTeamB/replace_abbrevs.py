import os
import re

test_dir = r"d:\Projects\Isis\BankingAppTeamB\BankingAppTeamB\BankingAppTeamB.Tests"

for root, dirs, files in os.walk(test_dir):
    for file in files:
        if file.endswith(".cs"):
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()

            new_content = content

            # Expand lambda parameters:
            # s => s.Method
            new_content = re.sub(r'\b(r)\s*=>\s*\1\.', r'repository => repository.', new_content)
            new_content = re.sub(r'\b(s)\s*=>\s*\1\.', r'service => service.', new_content)
            new_content = re.sub(r'\b(a)\s*=>\s*\1\.', r'account => account.', new_content)
            new_content = re.sub(r'\b(b)\s*=>\s*\1\.', r'biller => biller.', new_content) # or billPaymentService
            new_content = re.sub(r'\b(sb)\s*=>\s*\1\.', r'savedBiller => savedBiller.', new_content)
            new_content = re.sub(r'\b(bp)\s*=>\s*\1\.', r'billPayment => billPayment.', new_content)
            new_content = re.sub(r'\b(dto)\s*=>\s*\1\.', r'dataTransferObject => dataTransferObject.', new_content)
            new_content = re.sub(r'\b(p)\s*=>\s*\1\.', r'payment => payment.', new_content)

            # Some variable name expansions based on context:
            new_content = re.sub(r'\bmockRepository\b', 'mockBeneficiaryRepository', new_content)
            new_content = re.sub(r'\bmockPipelineService\b', 'mockTransactionPipelineService', new_content)
            new_content = re.sub(r'\bdto\b', 'dataTransferObject', new_content)
            new_content = re.sub(r'\breq\b', 'request', new_content)
            new_content = re.sub(r'\bres\b', 'response', new_content)
            new_content = re.sub(r'\bvar service = new\b', 'var testedService = new', new_content) # Generic replacement if needed, but let's be more specific.

            if file == "BillPaymentServiceTests.cs":
                new_content = new_content.replace("mockBeneficiaryRepository", "mockBillPaymentRepository")
            elif file == "ExchangeServiceTests.cs":
                new_content = new_content.replace("mockBeneficiaryRepository", "mockExchangeRepository")
                new_content = new_content.replace("biller => biller", "rateAlert => rateAlert")
            elif file == "TransferServiceTests.cs":
                new_content = new_content.replace("mockBeneficiaryRepository", "mockTransferRepository")
                new_content = new_content.replace("mockBeneficiaryRepository.Setup(repository", "mockTransferRepository.Setup(repository")
                new_content = new_content.replace("mockBeneficiaryRepository.Verify(repository", "mockTransferRepository.Verify(repository")
            elif file == "ReccuringSchedulerTests.cs":
                new_content = new_content.replace("recurringRepoMock", "mockRecurringPaymentRepository")
                new_content = new_content.replace("billPaymentServiceMock", "mockBillPaymentService")
                new_content = new_content.replace("repository => repository.", "repository => repository.")
                new_content = new_content.replace("biller => biller.", "billPaymentService => billPaymentService.")
            
            new_content = new_content.replace("var service = new", f"var {file.replace('Tests.cs', '')[0].lower()}{file.replace('Tests.cs', '')[1:]} = new")
            new_content = new_content.replace("service.", f"{file.replace('Tests.cs', '')[0].lower()}{file.replace('Tests.cs', '')[1:]}.")
            
            with open(path, "w", encoding="utf-8") as f:
                f.write(new_content)
